using System.Collections.Generic;
using TurboPattes.Core;
using TurboPattes.Game.Audio;
using TurboPattes.Game.Items;
using TurboPattes.Game.Kart;
using TurboPattes.Game.Track;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TurboPattes.Game.Race
{
    /// <summary>
    /// Déroulé d'une course : compte à rebours, simulation des karts (joueur + IA), objets, tapis turbo,
    /// classement, arrivée et relance.
    /// </summary>
    public sealed class RaceManager : MonoBehaviour
    {
        public enum Phase { Countdown, Racing, Results }

        const float CountdownLength = 3.6f;
        const float StepDt = 1f / 60f;

        public int Laps { get; private set; } = 3;
        public Phase State { get; private set; }
        public float CountdownLeft { get; private set; }
        public float RaceTime { get; private set; }
        public TrackPath Path { get; private set; }
        public Circuit Circuit { get; private set; }
        public KartMotor[] Karts { get; private set; }
        public KartMotor Player { get; private set; }
        public PlayerInput Input { get; private set; }
        public ItemSystem Items { get; private set; }
        public int[] Order { get; private set; }
        public bool WrongWay => _wrongWayTime > 1f;
        /// <summary>Message temporaire au centre de l'écran (« Dernier tour ! », « Super départ ! »).</summary>
        public string Banner { get; private set; }
        public float BannerTime { get; private set; }
        /// <summary>Le joueur est conduit par l'IA (mode autotest).</summary>
        public bool Autopilot { get; set; }
        public float[] LapTimes { get; private set; }

        AiDriver[] _ai;
        float[] _baseScale;
        List<BoostPad> _pads;
        SoundBank _sound;
        ChaseCamera _cam;
        float _wrongWayTime;
        int _lastBeep = 4;
        float _firstThrottleAt = -1f;
        int _playerLap = -1;
        float _lapStart;
        float _resultsDelay = -1f;

        public void Setup(Circuit circuit, TrackPath path, List<BoostPad> pads, ChaseCamera cam, SoundBank sound, int playerSlot, int laps, int seed)
        {
            Circuit = circuit;
            Path = path;
            _pads = pads;
            _cam = cam;
            _sound = sound;
            Laps = laps;
            Input = new PlayerInput();
            Items = new ItemSystem(circuit, path, transform, seed);
            LapTimes = new float[laps];

            var roster = AnimalSpec.Roster;
            Karts = new KartMotor[roster.Length];
            _ai = new AiDriver[roster.Length];
            _baseScale = new float[roster.Length];
            var rng = new System.Random(seed);
            for (int slot = 0; slot < roster.Length; slot++)
            {
                // Le joueur pilote toujours Roxy ; les autres se répartissent la grille.
                int specIndex = slot == playerSlot ? 0 : (slot < playerSlot ? slot + 1 : slot);
                var spec = roster[specIndex];
                var rig = KartFactory.Build(spec, transform);
                var kart = rig.Root.gameObject.AddComponent<KartMotor>();
                int row = slot / 2, col = slot % 2;
                float s = path.Length - 7f - row * 6f - col * 3f;
                float lat = col == 0 ? -3.2f : 3.2f;
                kart.Init(spec, rig, path, s, lat);
                kart.IsPlayer = slot == playerSlot;
                if (kart.IsPlayer) Player = kart;
                float skill = Mathf.Lerp(0.55f, 0.95f, (float)rng.NextDouble());
                kart.SpeedScale = kart.IsPlayer ? 1f : Mathf.Lerp(0.93f, 0.99f, skill);
                _baseScale[slot] = kart.SpeedScale;
                _ai[slot] = new AiDriver(kart, seed * 31 + slot, kart.IsPlayer ? 0.8f : skill);
                Karts[slot] = kart;

                kart.BarrierHit += k => { if (k.IsPlayer) _sound.Bump(); };
                kart.Boosted += k => { if (k.IsPlayer) _sound.Boost(); };
                kart.SpunOut += k => { if (k.IsPlayer) _sound.Hit(); };
                kart.DriftLevelUp += k => { if (k.IsPlayer) _sound.LevelUp(); };
            }
            Items.PickedUp += k => { if (k.IsPlayer) _sound.Pop(); };
            Items.Used += k => { if (k.IsPlayer) _sound.Throw(); };
            Items.KartHit += k => { if (!k.IsPlayer && Vector3.Distance(k.Position, Player.Position) < 25f) _sound.Hit(); };
            Items.ShieldPopped += k => { if (k.IsPlayer) _sound.Pop(); };

            _cam.Target = Player;
            _cam.IntroBlend = 0f;
            State = Phase.Countdown;
            CountdownLeft = CountdownLength;
            ComputeOrder();
        }

        void Update()
        {
            if (Karts == null) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            var playerInput = Input.Read();
            if (Input.RestartPressed) { Restart(); return; }

            if (State == Phase.Countdown) UpdateCountdown(dt, playerInput);
            else RaceTime += dt;

            // Sous-pas fixes pour une conduite identique quel que soit le framerate.
            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / StepDt));
            float h = dt / steps;
            var inputs = new KartInput[Karts.Length];
            for (int i = 0; i < Karts.Length; i++)
            {
                var k = Karts[i];
                bool wantsItem;
                if (k.IsPlayer && !Autopilot)
                {
                    inputs[i] = playerInput;
                    wantsItem = playerInput.UseItem;
                }
                else inputs[i] = _ai[i].Think(dt, Karts, Items, out wantsItem);
                if (wantsItem && k.ControlsEnabled && !k.Finished) Items.Use(k);
            }
            for (int step = 0; step < steps; step++)
            {
                for (int i = 0; i < Karts.Length; i++) Karts[i].Tick(inputs[i], h);
                for (int i = 0; i < Karts.Length; i++)
                for (int j = i + 1; j < Karts.Length; j++)
                    KartMotor.Collide(Karts[i], Karts[j]);
            }

            CheckBoostPads();
            Items.Tick(dt, Karts, RankOf);
            ComputeOrder();
            CheckFinish();
            ApplyRubberBand();
            UpdateWrongWay(dt);

            foreach (var k in Karts) k.UpdateVisuals(dt);
            _sound.SetEngine(Mathf.Clamp01(Player.Speed / KartMotor.MaxSpeed), Player.BoostTime > 0f, true);
            _sound.SetDrift(Player.Drift.Active);
            if (Player.ItemRoll > 0f && Mathf.Repeat(Player.ItemRoll, 0.12f) < dt) _sound.Tick();
            if (BannerTime > 0f) BannerTime -= dt;

            if (_resultsDelay > 0f)
            {
                _resultsDelay -= dt;
                if (_resultsDelay <= 0f) State = Phase.Results;
            }
        }

        void UpdateCountdown(float dt, KartInput input)
        {
            CountdownLeft -= dt;
            _cam.IntroBlend = Mathf.Clamp01((CountdownLength - CountdownLeft) / 2.4f);
            int whole = Mathf.CeilToInt(CountdownLeft);
            if (whole < _lastBeep && whole >= 1 && whole <= 3) { _sound.Beep(); _lastBeep = whole; }

            // Super départ : accélérer dans la dernière seconde, mais pas avant.
            bool throttle = input.Throttle > 0.5f || Autopilot;
            if (throttle && _firstThrottleAt < 0f) _firstThrottleAt = CountdownLeft;
            if (!throttle && CountdownLeft > 1.0f) _firstThrottleAt = -1f;

            if (CountdownLeft <= 0f)
            {
                State = Phase.Racing;
                _sound.Go();
                ShowBanner("PARTEZ !", 1.0f);
                foreach (var k in Karts) k.ControlsEnabled = true;
                if (_firstThrottleAt > 0f && _firstThrottleAt <= 1.0f)
                {
                    Player.GiveBoost(1.2f);
                    ShowBanner("Super départ !", 1.5f);
                }
                // Les IA ont aussi leurs chances de super départ.
                for (int i = 0; i < Karts.Length; i++)
                    if (!Karts[i].IsPlayer && Random.value < 0.4f) Karts[i].GiveBoost(Random.Range(0.6f, 1.2f));
                _lapStart = 0f;
            }
        }

        void CheckBoostPads()
        {
            foreach (var k in Karts)
            {
                float s = k.Laps.S;
                foreach (var pad in _pads)
                {
                    float into = Path.Wrap(s - pad.S);
                    if (into > pad.Length) continue;
                    if (Mathf.Abs(k.Lateral - pad.Lateral) > pad.HalfWidth + 0.4f) continue;
                    if (k.BoostTime < 0.9f) k.GiveBoost(1.0f);
                }
            }
        }

        int RankOf(KartMotor k) => k.Rank;

        void ComputeOrder()
        {
            var standings = new RacerStanding[Karts.Length];
            for (int i = 0; i < Karts.Length; i++)
                standings[i] = new RacerStanding(Karts[i].Finished, Karts[i].FinishTime, Karts[i].Progress);
            Order = RaceRanking.Order(standings);
            for (int r = 0; r < Order.Length; r++) Karts[Order[r]].Rank = r;
        }

        void CheckFinish()
        {
            // Temps au tour du joueur et annonce du dernier tour.
            int lap = Player.Laps.Lap;
            if (lap != _playerLap)
            {
                if (lap > _playerLap && _playerLap >= 0 && _playerLap < Laps)
                {
                    LapTimes[_playerLap] = RaceTime - _lapStart;
                    _lapStart = RaceTime;
                }
                if (lap == Laps - 1 && lap > _playerLap && State == Phase.Racing) ShowBanner("Dernier tour !", 2f);
                _playerLap = Mathf.Max(_playerLap, lap);
            }

            foreach (var k in Karts)
            {
                if (k.Finished || State == Phase.Countdown || !k.Laps.HasFinished(Laps)) continue;
                k.Finished = true;
                k.FinishTime = RaceTime;
                if (k.IsPlayer)
                {
                    _sound.Finish();
                    ShowBanner(k.Rank == 0 ? "Victoire !" : "Arrivée !", 2.5f);
                    _cam.Orbit = true;
                    _resultsDelay = 2.5f;
                }
            }
        }

        /// <summary>Élastique léger : les IA très en avance ralentissent un peu, celles à la traîne accélèrent.</summary>
        void ApplyRubberBand()
        {
            for (int i = 0; i < Karts.Length; i++)
            {
                var k = Karts[i];
                if (k.IsPlayer) continue;
                float gap = k.Progress - Player.Progress;
                float band = Mathf.Clamp(-gap / 1000f, -0.06f, 0.06f);
                k.SpeedScale = Mathf.MoveTowards(k.SpeedScale, Mathf.Clamp(_baseScale[i] + band, 0.85f, 1.05f), 0.02f * Time.deltaTime);
            }
        }

        void UpdateWrongWay(float dt)
        {
            var t = Path.Tangents[Player.TrackIndex];
            bool wrong = State == Phase.Racing && !Player.Finished && Vector3.Dot(Player.Forward, t) < -0.3f && Player.Speed > 3f;
            _wrongWayTime = wrong ? _wrongWayTime + dt : 0f;
        }

        void ShowBanner(string text, float seconds)
        {
            Banner = text;
            BannerTime = seconds;
        }

        public void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
