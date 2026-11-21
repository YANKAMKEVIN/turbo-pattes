using System;
using TurboPattes.Core;
using TurboPattes.Game.Track;
using TurboPattes.Game.Visuals;
using UnityEngine;

namespace TurboPattes.Game.Kart
{
    /// <summary>
    /// Conduite arcade d'un kart sans moteur physique : vitesse avant/latérale, drift à trois niveaux, turbos,
    /// tête-à-queue, rebond sur les barrières calculé depuis la ligne médiane. Le kart suit la hauteur de la piste
    /// (les côtes freinent, les descentes poussent) et décolle des tremplins (<see cref="VerticalMotion"/>).
    /// Piloté par <c>RaceManager</c> qui appelle <see cref="Tick"/> puis <see cref="UpdateVisuals"/>.
    /// </summary>
    public sealed class KartMotor : MonoBehaviour
    {
        public const float Radius = 1.1f;
        public const float MaxSpeed = 27f;
        const float Accel = 13f;
        const float BrakeDecel = 30f;
        const float ReverseMax = 8f;
        const float Coast = 5f;
        const float TurnRate = 2.0f;
        const float OffroadFactor = 0.55f;
        const float BoostFactor = 1.32f;
        const float MinDriftSpeed = 11f;
        const float HopWindow = 0.3f;
        /// <summary>Part de la gravité qui freine en côte et pousse en descente.</summary>
        const float SlopePull = 0.45f;
        /// <summary>Volant en l'air : on peut corriger un peu la trajectoire.</summary>
        const float AirSteer = 0.35f;
        /// <summary>Plume : vitesse verticale du bond et gravité réduite pendant le vol plané.</summary>
        const float FeatherLeap = 9f;
        const float FeatherGravity = 0.45f;

        public DriverSpec Spec { get; private set; }
        public KartRig Rig { get; private set; }
        public TrackPath Path { get; private set; }
        public LapTracker Laps { get; private set; }
        public bool IsPlayer { get; set; }

        public Vector3 Position;
        public Vector3 Velocity;
        public float Yaw;

        /// <summary>Multiplicateur de vitesse maximale (niveau de l'IA, élastique).</summary>
        public float SpeedScale = 1f;
        public bool ControlsEnabled;
        /// <summary>Caractère du pilote (vitesse, accélération, adhérence, poids).</summary>
        public DriverStats Stats = Drivers.Stats[0];
        public readonly AirTrick Trick = new();
        /// <summary>Figures réussies (journal de l'autotest).</summary>
        public int Tricks { get; private set; }
        /// <summary>Vol plané de la plume en cours.</summary>
        public bool Gliding { get; private set; }

        public int TrackIndex { get; private set; }
        public float Lateral { get; private set; }
        public bool Offroad { get; private set; }
        /// <summary>Dans le sable d'un raccourci : plus lent que l'herbe, sauf sous turbo ou en l'air.</summary>
        public bool InSand { get; private set; }
        public readonly DriftCharge Drift = new();
        public readonly VerticalMotion Vertical = new();
        public bool Airborne => !Vertical.Grounded;
        /// <summary>Nombre de sauts depuis le départ (journal de l'autotest).</summary>
        public int Jumps { get; private set; }
        public float LongestJump { get; private set; }

        public float BoostTime { get; private set; }
        public float SpinTime { get; private set; }
        public float ShieldTime { get; set; }
        public float HopTime { get; private set; }
        /// <summary>Temps restant de la comète (pilote automatique en turbo, intouchable).</summary>
        public float CometTime { get; private set; }
        public bool Finished { get; set; }
        public float FinishTime { get; set; }
        public int Rank { get; set; }
        /// <summary>Nombre de pilotes de la course (animation de fin : victoire ou défaite selon la place).</summary>
        public int Racers { get; set; } = 8;
        /// <summary>Temps depuis l'arrivée, pour l'animation du pilote.</summary>
        float _celebration;

        public ItemKind Item { get; set; }
        /// <summary>Temps restant de la roulette d'objet (l'objet n'est utilisable qu'à 0).</summary>
        public float ItemRoll { get; set; }

        /// <summary>Événements pour le son et l'interface.</summary>
        public event Action<KartMotor> BarrierHit, Boosted, SpunOut, DriftLevelUp, TookOff, Landed, TrickLanded;

        public Vector3 Forward => new(Mathf.Sin(Yaw), 0f, Mathf.Cos(Yaw));
        public Vector3 Right => new(Mathf.Cos(Yaw), 0f, -Mathf.Sin(Yaw));
        public float ForwardSpeed => Vector3.Dot(Velocity, Forward);
        public float Speed => Velocity.magnitude;
        public float Progress => Laps.Progress;
        public float TopSpeed => MaxSpeed * SpeedScale * Stats.TopSpeedFactor;

        float _hopWindow;
        bool _prevDrift;
        float _steerVisual, _spinAngle, _wheelAngle, _driftVisual, _tilt, _pitch, _squash, _roll;
        /// <summary>Inclinaison du sol sous le kart (degrés, virage relevé ; positive = droite de la piste relevée).</summary>
        float _bank;
        /// <summary>Pente du sol dans l'axe du kart (positive en montée).</summary>
        float _slopeAhead;
        int _lastDriftLevel;
        ParticleSystem[] _sparks;
        ParticleSystem[] _flames;
        ParticleSystem _dust;
        Material _sparkMat;

        public static readonly Color[] DriftColors =
        {
            Color.clear,
            new(1f, 0.95f, 0.45f),
            new(0.5f, 1f, 0.35f),
            new(1f, 0.35f, 0.9f),
        };

        public void Init(DriverSpec spec, KartRig rig, TrackPath path, float startS, float startLateral)
        {
            Spec = spec;
            Rig = rig;
            Path = path;
            path.Sample(startS, out var p, out var t, out var r);
            Position = p + r * startLateral;
            Yaw = Mathf.Atan2(t.x, t.z);
            TrackIndex = path.Nearest(Position, -1);
            float s0 = path.DistanceAlong(TrackIndex, Position);
            Position.y = path.GroundHeight(s0, startLateral);
            Vertical.GravityScale = path.Gravity;
            Vertical.Reset(Position.y);
            Laps = new LapTracker(path.Length, s0);
            BuildFx();
            UpdateVisuals(0f);
        }

        // ------------------------------------------------------------------ simulation

        public void Tick(KartInput input, float dt)
        {
            if (Finished) input = AutoPilotInput();
            else if (!ControlsEnabled) input = default;
            else if (CometTime > 0f)
            {
                CometTime -= dt;
                input = CometInput();
            }

            Vector3 fwd = Forward, right = Right;
            float vF = Vector3.Dot(Velocity, fwd);
            float vR = Vector3.Dot(Velocity, right);
            // Les pilotes adhérents perdent un peu moins hors piste.
            float top = TopSpeed * (Offroad && BoostTime <= 0f ? (InSand ? Shortcut.SandFactor : OffroadFactor) + (Stats.Grip - 3) * 0.03f : 1f);
            float accelMul = 1f;
            if (BoostTime > 0f)
            {
                top = TopSpeed * BoostFactor;
                accelMul = 3f;
                if (vF < top * 0.85f) vF = Mathf.Min(top * 0.85f, vF + 40f * dt);
            }

            float turn = 0f;
            bool air = Airborne;
            // Côtes et descentes : une part de la gravité freine ou pousse le kart.
            if (!air) vF -= VerticalMotion.Gravity * Path.Gravity * SlopePull * _slopeAhead * dt;
            if (SpinTime > 0f)
            {
                SpinTime -= dt;
                vF = Mathf.MoveTowards(vF, 0f, 14f * dt);
                vR = Mathf.MoveTowards(vR, 0f, 10f * dt);
                _spinAngle += 900f * dt;
                if (Drift.Active) Drift.Cancel();
            }
            else
            {
                // Avancer / freiner / reculer (rien à faire en plein saut)
                if (air) { }
                else if (input.Throttle > 0.05f && vF < top)
                    vF = Mathf.Min(top, vF + Accel * Stats.AccelFactor * accelMul * input.Throttle * dt * (vF < 8f ? 1.4f : 1f));
                else if (input.Brake > 0.05f)
                    vF = vF > 0.5f ? vF - BrakeDecel * input.Brake * dt : Mathf.Max(-ReverseMax, vF - Accel * 0.6f * dt);
                else if (input.Throttle <= 0.05f)
                    vF = Mathf.MoveTowards(vF, 0f, Coast * dt);
                if (vF > top && !air) vF = Mathf.MoveTowards(vF, top, (Offroad ? 22f : 9f) * dt);

                float grip = Mathf.Clamp01(Mathf.Abs(vF) / 7f) * (air ? AirSteer : 1f);

                // Saut + drift
                bool driftEdge = input.Drift && !_prevDrift;
                // En l'air, le bouton fait une figure (turbo à la réception si elle est finie) au lieu du petit saut.
                if (driftEdge && air) Trick.TryStart(true, Vertical.AirTime);
                else if (driftEdge && HopTime <= 0f) { HopTime = 0.28f; _hopWindow = HopWindow; }
                Trick.Tick(dt);
                if (_hopWindow > 0f) _hopWindow -= dt;

                if (Drift.Active)
                {
                    float into = input.Steer * Drift.Direction;
                    turn = Drift.Direction * Mathf.Lerp(0.5f, 1.35f, (into + 1f) * 0.5f) * TurnRate * Stats.GripFactor;
                    Drift.Tick(dt, into);
                    if (!input.Drift)
                    {
                        float boost = Drift.Release();
                        if (boost > 0f) GiveBoost(boost);
                    }
                    else if (vF < MinDriftSpeed * 0.7f || Offroad && BoostTime <= 0f && vF < MinDriftSpeed)
                    {
                        Drift.Cancel();
                    }
                    // Glisse vers l'extérieur du virage (moins sur un virage relevé, qui retient le kart).
                    float slide = air ? 1f : Banking.SlideFactor(_bank);
                    vR = Mathf.Lerp(vR, -Drift.Direction * vF * 0.22f * slide, 1f - Mathf.Exp(-4f * dt));
                    int level = Drift.Level;
                    if (level > _lastDriftLevel) DriftLevelUp?.Invoke(this);
                    _lastDriftLevel = level;
                }
                else
                {
                    _lastDriftLevel = 0;
                    turn = input.Steer * TurnRate * Stats.GripFactor * (vF < -0.5f ? -1f : 1f);
                    if (input.Drift && _hopWindow > 0f && Mathf.Abs(input.Steer) > 0.3f && vF > MinDriftSpeed && !air)
                        Drift.Start(input.Steer > 0f ? 1 : -1);
                    vR *= Mathf.Exp(-(air ? 1f : 10f) * dt);
                }
                Yaw += turn * grip * dt;
            }
            _prevDrift = input.Drift;

            fwd = Forward;
            right = Right;
            Velocity = fwd * vF + right * vR;
            Position += Velocity * dt;

            if (BoostTime > 0f) BoostTime -= dt;
            if (ShieldTime > 0f) ShieldTime -= dt;
            if (HopTime > 0f) HopTime -= dt;
            if (ItemRoll > 0f) ItemRoll -= dt;

            _steerVisual = Mathf.Lerp(_steerVisual, SpinTime > 0f ? 0f : input.Steer, 1f - Mathf.Exp(-12f * dt));
            _wheelAngle += vF * dt / 0.33f * Mathf.Rad2Deg;
            ResolveBarriers(dt);
        }

        /// <summary>Après l'arrivée : l'IA reprend le volant pour finir le tour d'honneur.</summary>
        KartInput AutoPilotInput()
        {
            float s = Laps.S + 10f + Mathf.Max(0f, ForwardSpeed) * 0.4f;
            var target = Path.PointAt(s, Mathf.Clamp(Lateral, -3f, 3f));
            var to = target - Position;
            to.y = 0f;
            float angle = Vector3.SignedAngle(Forward, to, Vector3.up);
            return new KartInput { Steer = Mathf.Clamp(angle / 25f, -1f, 1f), Throttle = ForwardSpeed < TopSpeed * 0.6f ? 1f : 0f };
        }

        /// <summary>Comète : file sur la ligne médiane, plein gaz, en visant loin devant.</summary>
        KartInput CometInput()
        {
            float s = Laps.S + 12f + Mathf.Max(0f, ForwardSpeed) * 0.35f;
            var to = Path.PointAt(s, 0f) - Position;
            to.y = 0f;
            float angle = Vector3.SignedAngle(Forward, to, Vector3.up);
            return new KartInput { Steer = Mathf.Clamp(angle / 20f, -1f, 1f), Throttle = 1f };
        }

        /// <summary>Objet rare : la comète (turbo, bouclier et pilote automatique pendant <see cref="CometRun.Duration"/> s).</summary>
        public void StartComet()
        {
            CometTime = CometRun.Duration;
            ShieldTime = Mathf.Max(ShieldTime, CometRun.Duration);
            Drift.Cancel();
            GiveBoost(CometRun.Duration);
        }

        void ResolveBarriers(float dt)
        {
            TrackIndex = Path.Nearest(Position, TrackIndex);
            float lat = Path.Lateral(TrackIndex, Position);
            // Le muret intérieur recule dans les raccourcis.
            float limit = Path.BarrierAt(Path.DistanceAlong(TrackIndex, Position), lat < 0f ? -1 : 1) - Radius * 0.8f;
            if (Mathf.Abs(lat) > limit)
            {
                float side = Mathf.Sign(lat);
                Vector3 n = Path.Rights[TrackIndex] * side;
                Position -= n * (Mathf.Abs(lat) - limit);
                float vn = Vector3.Dot(Velocity, n);
                if (vn > 0f)
                {
                    if (vn > 3f)
                    {
                        // Vrai choc : rebond et perte de vitesse.
                        Velocity -= n * vn * 1.4f;
                        Velocity *= 0.88f;
                        BarrierHit?.Invoke(this);
                    }
                    else Velocity -= n * vn; // simple frottement : seule la poussée vers le mur disparaît
                    // Le muret redresse le kart dans l'axe de la piste : il longe le mur au lieu d'y rester planté.
                    Vector3 t = Path.Tangents[TrackIndex];
                    if (Vector3.Dot(Forward, t) < 0f) t = -t;
                    Yaw = Mathf.LerpAngle(Yaw * Mathf.Rad2Deg, Mathf.Atan2(t.x, t.z) * Mathf.Rad2Deg, 0.12f) * Mathf.Deg2Rad;
                }
                lat = side * limit;
            }
            Lateral = lat;
            Offroad = Mathf.Abs(lat) > Path.HalfWidth + Path.CurbWidth;
            float s = Path.DistanceAlong(TrackIndex, Position);
            InSand = Offroad && Path.InSand(s, lat);
            Laps.Update(s);
            FollowGround(s, lat, dt);
        }

        /// <summary>Hauteur : collé à la piste, ou en vol après un tremplin.</summary>
        void FollowGround(float s, float lat, float dt)
        {
            float slope = Path.GroundSlope(s, lat);
            float along = Vector3.Dot(Velocity, Path.Tangents[TrackIndex]);
            _slopeAhead = slope * Vector3.Dot(Forward, Path.Tangents[TrackIndex]);
            _bank = Path.BankAngle(s);
            switch (Vertical.Step(dt, Path.GroundHeight(s, lat), slope * along))
            {
                case VerticalEvent.TookOff:
                    Jumps++;
                    TookOff?.Invoke(this);
                    break;
                case VerticalEvent.Landed:
                    LongestJump = Mathf.Max(LongestJump, Vertical.AirTime);
                    _squash = Mathf.Clamp01(Vertical.LandingSpeed / 10f);
                    if (Gliding)
                    {
                        Gliding = false;
                        Vertical.GravityScale = Path.Gravity;
                    }
                    float trick = Trick.Land();
                    if (trick > 0f)
                    {
                        Tricks++;
                        GiveBoost(trick);
                        TrickLanded?.Invoke(this);
                    }
                    // Poussière à la réception d'un vrai saut.
                    if (Vertical.LandingSpeed > 4f && _dust != null) _dust.Emit(8 + (int)Mathf.Min(16f, Vertical.LandingSpeed * 1.5f));
                    Landed?.Invoke(this);
                    break;
            }
            Position.y = Vertical.Y;
        }

        /// <summary>Plume : grand bond plané (par-dessus les objets au sol) avec un petit élan.</summary>
        public void Leap()
        {
            if (Airborne) return;
            Vertical.GravityScale = Path.Gravity * FeatherGravity;
            Vertical.Launch(FeatherLeap * Mathf.Sqrt(Path.Gravity));
            Gliding = true;
            Jumps++;
            TookOff?.Invoke(this);
            GiveBoost(0.9f);
        }

        public void GiveBoost(float seconds)
        {
            BoostTime = Mathf.Max(BoostTime, seconds);
            Boosted?.Invoke(this);
        }

        /// <summary>Touché par un objet : le bouclier absorbe, sinon tête-à-queue.</summary>
        public bool Hit(float spinSeconds)
        {
            if (CometTime > 0f) return false;
            if (ShieldTime > 0f)
            {
                ShieldTime = 0f;
                return false;
            }
            if (SpinTime > 0f) return false;
            SpinTime = spinSeconds;
            BoostTime = 0f;
            Drift.Cancel();
            SpunOut?.Invoke(this);
            return true;
        }

        /// <summary>Choc entre karts : sépare et échange une partie de la vitesse.</summary>
        public static void Collide(KartMotor a, KartMotor b)
        {
            Vector3 d = b.Position - a.Position;
            // Un kart sur le pont ne touche pas celui qui passe dessous.
            if (Mathf.Abs(d.y) > 2f) return;
            d.y = 0f;
            float dist = d.magnitude;
            const float min = Radius * 2f;
            if (dist >= min || dist < 1e-4f) return;
            // La comète renverse ceux qu'elle percute.
            if (a.CometTime > 0f && b.CometTime <= 0f) b.Hit(CometRun.SpinOnHit);
            else if (b.CometTime > 0f && a.CometTime <= 0f) a.Hit(CometRun.SpinOnHit);
            Vector3 n = d / dist;
            // Le plus lourd est moins déplacé.
            float push = min - dist, shareA = DriverStats.PushShare(a.Stats.Mass, b.Stats.Mass);
            a.Position -= n * push * shareA;
            b.Position += n * push * (1f - shareA);
            float rel = Vector3.Dot(b.Velocity - a.Velocity, n);
            if (rel < 0f)
            {
                // Le kart sous turbo ou bouclier pousse plus fort.
                // Part du choc encaissée selon le poids ; sous turbo ou bouclier, on encaisse moitié moins.
                float wa = DriverStats.PushShare(a.Stats.Mass, b.Stats.Mass) * (a.BoostTime > 0f || a.ShieldTime > 0f ? 0.5f : 1f);
                float wb = DriverStats.PushShare(b.Stats.Mass, a.Stats.Mass) * (b.BoostTime > 0f || b.ShieldTime > 0f ? 0.5f : 1f);
                float j = -rel * 1.2f;
                a.Velocity -= n * j * wa * 0.8f;
                b.Velocity += n * j * wb * 0.8f;
            }
        }

        // ------------------------------------------------------------------ rendu

        public void UpdateVisuals(float dt)
        {
            var root = Rig.Root;
            // Tangage : le nez suit la pente, et en vol la trajectoire (légèrement).
            float targetPitch = Airborne
                ? -Mathf.Atan2(Vertical.Vy, Mathf.Max(8f, ForwardSpeed)) * Mathf.Rad2Deg * 0.6f
                : -Mathf.Atan(_slopeAhead) * Mathf.Rad2Deg;
            _pitch = Mathf.Lerp(_pitch, targetPitch, 1f - Mathf.Exp(-(Airborne ? 4f : 12f) * dt));
            // Roulis : le kart épouse le virage relevé (droite de la piste relevée = droite du kart s'il roule dans le sens de la course).
            float targetRoll = Airborne ? 0f : _bank * Vector3.Dot(Forward, Path.Tangents[TrackIndex]);
            _roll = Mathf.Lerp(_roll, targetRoll, 1f - Mathf.Exp(-10f * dt));
            root.SetPositionAndRotation(Position, Quaternion.Euler(_pitch, Yaw * Mathf.Rad2Deg, _roll));
            _squash = Mathf.MoveTowards(_squash, 0f, dt * 4f);

            float targetDrift = Drift.Active ? Drift.Direction * 28f : 0f;
            _driftVisual = Mathf.Lerp(_driftVisual, targetDrift, 1f - Mathf.Exp(-10f * dt));
            _tilt = Mathf.Lerp(_tilt, -_steerVisual * Mathf.Clamp01(Speed / MaxSpeed) * 6f, 1f - Mathf.Exp(-8f * dt));
            float hop = HopTime > 0f ? Mathf.Sin((1f - HopTime / 0.28f) * Mathf.PI) * 0.45f : 0f;
            float bob = Mathf.Sin(Time.time * 18f) * 0.015f * Mathf.Clamp01(Speed / 5f);
            Rig.Body.localPosition = new Vector3(0f, hop + bob - _squash * 0.25f, 0f);
            // Figure en l'air : un tonneau complet.
            float roll = Trick.Progress * 360f;
            Rig.Body.localRotation = Quaternion.Euler(0f, _driftVisual + (SpinTime > 0f ? _spinAngle : 0f), _tilt + roll);
            if (SpinTime <= 0f) _spinAngle = 0f;

            for (int i = 0; i < Rig.Wheels.Length; i++)
                Rig.Wheels[i].localRotation = Quaternion.Euler(_wheelAngle, 0f, 0f);
            foreach (var p in Rig.FrontPivots)
                p.localRotation = Quaternion.Euler(0f, _steerVisual * 28f + _driftVisual * 0.5f, 0f);

            // Arrivée : le pilote fête sa place (podium) ou baisse la tête (seconde moitié du peloton).
            if (Finished)
            {
                _celebration += dt;
                KartFactory.Pose(Rig, DriverPoses.Pose(DriverPoses.For(Rank, Racers), _celebration));
            }

            Rig.Shield.SetActive(ShieldTime > 0f);
            if (ShieldTime > 0f)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.04f;
                Rig.Shield.transform.localScale = new Vector3(3.4f, 2.6f, 3.6f) * pulse;
            }
            UpdateFx();
        }

        void BuildFx()
        {
            _sparks = new ParticleSystem[2];
            _sparkMat = new Material(Mats.Particle(Color.white));
            for (int i = 0; i < 2; i++)
            {
                // Étincelles de drift sous les roues arrière.
                var wheel = Rig.Wheels[2 + i];
                _sparks[i] = MakeParticles("Sparks", Rig.Root, wheel.localPosition + new Vector3(0f, -0.25f, -0.35f), _sparkMat,
                    lifetime: 0.25f, speed: 4f, size: 0.22f, rate: 70f, gravity: 1.5f, spread: 35f, back: true);
            }
            _flames = new ParticleSystem[Rig.Exhausts.Length];
            var flameMat = Mats.Particle(new Color(1f, 0.55f, 0.15f, 1f));
            for (int i = 0; i < _flames.Length; i++)
                _flames[i] = MakeParticles("Flame", Rig.Exhausts[i].parent, Rig.Exhausts[i].localPosition + new Vector3(0, 0, -0.15f), flameMat,
                    lifetime: 0.18f, speed: 6f, size: 0.45f, rate: 90f, gravity: 0f, spread: 8f, back: true);
            _dust = MakeParticles("Dust", Rig.Root, new Vector3(0f, 0.1f, -1.1f), Mats.Particle(new Color(0.75f, 0.6f, 0.4f, 0.5f)),
                lifetime: 0.6f, speed: 2f, size: 0.9f, rate: 30f, gravity: -0.3f, spread: 40f, back: true);
        }

        static ParticleSystem MakeParticles(string name, Transform parent, Vector3 localPos, Material mat,
            float lifetime, float speed, float size, float rate, float gravity, float spread, bool back)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = back ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = lifetime;
            main.startSpeed = speed;
            main.startSize = size;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            main.playOnAwake = false;
            var emission = ps.emission;
            emission.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = spread;
            shape.radius = 0.05f;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return ps;
        }

        static void SetEmitting(ParticleSystem ps, bool on)
        {
            if (on && !ps.isEmitting) ps.Play();
            else if (!on && ps.isEmitting) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }

        void UpdateFx()
        {
            int level = Drift.Active ? Drift.Level : 0;
            bool sparks = Drift.Active && HopTime <= 0f && !Airborne;
            if (sparks)
            {
                var c = level == 0 ? new Color(1f, 1f, 1f, 0.35f) : DriftColors[level];
                _sparkMat.SetColor(Mats.BaseColorProperty, c);
            }
            foreach (var s in _sparks)
            {
                SetEmitting(s, sparks);
                if (sparks)
                {
                    var main = s.main;
                    main.startSize = level == 0 ? 0.12f : 0.18f + level * 0.07f;
                }
            }
            foreach (var f in _flames) SetEmitting(f, BoostTime > 0f);
            SetEmitting(_dust, Offroad && Speed > 5f && !Airborne);
        }
    }
}
