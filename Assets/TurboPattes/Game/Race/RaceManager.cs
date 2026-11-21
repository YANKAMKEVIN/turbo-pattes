using System;
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
        public KartInput LastPlayerInput { get; private set; }
        public ItemSystem Items { get; private set; }
        public int[] Order { get; private set; }
        public bool WrongWay => _wrongWayTime > 1f;
        /// <summary>Message temporaire au centre de l'écran (« Dernier tour ! », « Super départ ! »).</summary>
        public string Banner { get; private set; }
        public float BannerTime { get; private set; }
        /// <summary>Le joueur est conduit par l'IA (mode autotest).</summary>
        public bool Autopilot { get; set; }
        public float[] LapTimes { get; private set; }
        public bool Paused { get; private set; }
        /// <summary>Contre-la-montre : le joueur seul, sans bulles d'objets.</summary>
        public bool Solo { get; set; }
        /// <summary>Début du tour en cours du joueur (temps de course).</summary>
        public float LapStartTime => _lapStart;
        /// <summary>Le joueur vient de finir un tour : numéro du tour (0 = premier) et temps.</summary>
        public event Action<int, float> LapCompleted;
        /// <summary>Passage à l'écran de résultats.</summary>
        public event Action RaceFinished;
        /// <summary>Projectile qui arrive dans le dos du joueur (<see cref="ItemKind.None"/> sinon) et son urgence (0 à 1).</summary>
        public ItemKind Threat { get; private set; }
        public float ThreatUrgency { get; private set; }
        /// <summary>Musique jouée au « PARTEZ ! » (une par circuit).</summary>
        public Song Music { get; set; } = Songs.Race;

        AiDriver[] _ai;
        ProgressClock[] _clocks;
        readonly ThreatBeeper _threatBeeper = new();
        /// <summary>Place du joueur et ses changements (rebond et flèche du HUD).</summary>
        public PlaceTracker Place { get; } = new();
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

        public void Setup(Circuit circuit, TrackPath path, List<BoostPad> pads, ChaseCamera cam, SoundBank sound, int playerSlot, int laps, int seed,
            int pilot = 0, Vector2? aiSpeed = null, int[] grid = null)
        {
            Circuit = circuit;
            Path = path;
            _pads = pads;
            _cam = cam;
            _sound = sound;
            Laps = laps;
            Input = new PlayerInput();
            Items = new ItemSystem(circuit, path, transform, seed, bubbles: !Solo);
            LapTimes = new float[laps];

            var roster = AnimalSpec.Roster;
            var karts = new List<KartMotor>();
            var ai = new List<AiDriver>();
            var baseScale = new List<float>();
            var rng = new System.Random(seed);
            // Grille imposée (coupe : ordre inverse du classement) : le joueur part là où le place son rang.
            if (grid != null && !Solo) playerSlot = System.Array.IndexOf(grid, pilot);
            for (int slot = 0; slot < roster.Length; slot++)
            {
                if (Solo && slot != playerSlot) continue;
                // Le joueur pilote l'animal choisi ; les autres se répartissent le reste de la grille.
                int other = slot < playerSlot ? slot : slot - 1;
                int specIndex = grid != null && !Solo ? grid[slot] : slot == playerSlot ? pilot : (other < pilot ? other : other + 1);
                var spec = roster[specIndex];
                var rig = KartFactory.Build(spec, transform);
                var kart = rig.Root.gameObject.AddComponent<KartMotor>();
                int row = slot / 2, col = slot % 2;
                float s = path.Length - 7f - row * 6f - col * 3f;
                float lat = col == 0 ? -3.2f : 3.2f;
                kart.Init(spec, rig, path, s, lat);
                kart.Stats = Drivers.For(specIndex);
                kart.IsPlayer = slot == playerSlot;
                if (kart.IsPlayer) Player = kart;
                float skill = Mathf.Lerp(0.55f, 0.95f, (float)rng.NextDouble());
                var range = aiSpeed ?? new Vector2(0.93f, 0.99f);
                kart.SpeedScale = kart.IsPlayer ? 1f : Mathf.Lerp(range.x, range.y, skill);
                baseScale.Add(kart.SpeedScale);
                ai.Add(new AiDriver(kart, seed * 31 + slot, kart.IsPlayer ? 0.8f : skill));
                karts.Add(kart);

                kart.BarrierHit += k => { if (k.IsPlayer) { _sound.Bump(); App.Haptics.Pulse(35); _cam.Shake(0.25f); } };
                kart.TrickLanded += k => { if (k.IsPlayer) { _sound.LevelUp(); ShowBanner("Figure !", 0.8f); } };
                kart.Boosted += k => { if (k.IsPlayer) _sound.Boost(); };
                kart.SpunOut += k => { if (k.IsPlayer) { _sound.Hit(); App.Haptics.Pulse(140); _cam.Shake(0.45f); } };
                kart.DriftLevelUp += k => { if (k.IsPlayer) { _sound.LevelUp(); App.Haptics.Pulse(15); } };
                kart.Landed += k => { if (k.IsPlayer && k.Vertical.LandingSpeed > 4f) { _sound.Bump(); App.Haptics.Pulse(45); _cam.Shake(Mathf.Clamp(k.Vertical.LandingSpeed / 20f, 0.15f, 0.5f)); } };
            }
            Karts = karts.ToArray();
            foreach (var k in Karts) k.Racers = Karts.Length;
            _clocks = Array.ConvertAll(Karts, _ => new ProgressClock());
            _ai = ai.ToArray();
            _baseScale = baseScale.ToArray();
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
            if (Paused) return;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            var playerInput = Input.Read();
            LastPlayerInput = playerInput;
            if (Input.RestartPressed) { Restart(); return; }
            if (Input.PausePressed && State != Phase.Results) { SetPaused(true); return; }

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
                if (wantsItem && k.ControlsEnabled && !k.Finished) Items.Use(k, Karts);
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
            UpdateThreat(dt);
            ComputeOrder();
            if (State != Phase.Countdown)
                for (int i = 0; i < Karts.Length; i++) _clocks[i].Record(Karts[i].Progress, RaceTime);
            UpdatePlace(dt);
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
                if (_resultsDelay <= 0f)
                {
                    State = Phase.Results;
                    RaceFinished?.Invoke();
                }
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
                Jukebox.Instance.Play(Music, MusicLevel);
                ShowBanner("PARTEZ !", 1.0f);
                foreach (var k in Karts) k.ControlsEnabled = true;
                if (_firstThrottleAt > 0f && _firstThrottleAt <= 1.0f)
                {
                    Player.GiveBoost(1.2f);
                    ShowBanner("Super départ !", 1.5f);
                }
                // Les IA ont aussi leurs chances de super départ.
                for (int i = 0; i < Karts.Length; i++)
                    if (!Karts[i].IsPlayer && UnityEngine.Random.value < 0.4f) Karts[i].GiveBoost(UnityEngine.Random.Range(0.6f, 1.2f));
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

        void UpdatePlace(float dt)
        {
            // Pas de réaction pendant le compte à rebours ni après l'arrivée.
            if (State != Phase.Racing || Player.Finished || Solo) return;
            int change = Place.Update(Player.Rank, dt);
            if (change == 0) return;
            _sound.Place(change > 0);
            if (change > 0) App.Haptics.Pulse(12);
        }

        void UpdateThreat(float dt)
        {
            var kind = ItemKind.None;
            float distance = 0f;
            bool incoming = !Player.Finished && Items.IncomingThreat(Player, out kind, out distance);
            Threat = incoming ? kind : ItemKind.None;
            ThreatUrgency = incoming ? ThreatAlert.Urgency(distance) : 0f;
            if (_threatBeeper.Tick(dt, incoming, ThreatUrgency)) _sound.Warn();
        }

        /// <summary>
        /// Écart en secondes avec le pilote juste devant (<paramref name="offset"/> = -1) ou juste derrière (+1) le joueur,
        /// mesuré aux repères de passage. Faux s'il n'y a personne à cette place ou pas encore de mesure.
        /// </summary>
        public bool GapToNeighbour(int offset, out KartMotor other, out float seconds)
        {
            other = null;
            seconds = 0f;
            int r = Player.Rank + offset;
            if (_clocks == null || Order == null || r < 0 || r >= Order.Length || offset == 0) return false;
            int index = Order[r];
            other = Karts[index];
            return offset < 0
                ? RaceGaps.Ahead(_clocks[index], Player.Progress, RaceTime, out seconds)
                : RaceGaps.Behind(_clocks[Array.IndexOf(Karts, Player)], other.Progress, RaceTime, out seconds);
        }

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
                    LapCompleted?.Invoke(_playerLap, LapTimes[_playerLap]);
                }
                if (lap == Laps - 1 && lap > _playerLap && State == Phase.Racing)
                {
                    ShowBanner("Dernier tour !", 2f);
                    // Course de plusieurs tours : jingle, puis la musique du circuit repart plus vite pour le sprint final.
                    if (Laps > 1) Jukebox.Instance.LastLap(Music, MusicLevel);
                }
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
                    App.Haptics.Pulse(220);
                    Jukebox.Instance.SetLevel(0.15f);
                    Jukebox.Instance.Sting(Songs.Finish);
                    ShowBanner(k.Rank == 0 && !Solo ? "Victoire !" : "Arrivée !", 2.5f);
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

        /// <summary>Numéro du pilote dans <see cref="AnimalSpec.Roster"/> (sert aux points de la coupe).</summary>
        public static int SpecIndex(KartMotor k) => Array.IndexOf(AnimalSpec.Roster, k.Spec);

        public void ShowBanner(string text, float seconds)
        {
            Banner = text;
            BannerTime = seconds;
        }

        const float MusicLevel = 0.4f;

        // Appli mise en arrière-plan (appel, bouton accueil) : la course se met en pause toute seule.
        void OnApplicationPause(bool paused)
        {
            if (paused && !Paused && State != Phase.Results && !Autopilot) SetPaused(true);
        }

        void OnApplicationFocus(bool focus)
        {
            if (!focus && Application.isMobilePlatform && !Paused && State != Phase.Results && !Autopilot) SetPaused(true);
        }

        public void SetPaused(bool paused)
        {
            Paused = paused;
            Time.timeScale = paused ? 0f : 1f;
            AudioListener.pause = paused;
            if (State == Phase.Racing) Jukebox.Instance.SetLevel(paused ? 0.15f : MusicLevel);
        }

        /// <summary>Recharge la scène : <see cref="App.GameSession.ShowMenu"/> dit si l'on revient au menu ou si l'on recourt.</summary>
        public void Restart()
        {
            SetPaused(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
