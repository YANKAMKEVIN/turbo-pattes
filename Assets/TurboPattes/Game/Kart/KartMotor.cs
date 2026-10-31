using System;
using TurboPattes.Core;
using TurboPattes.Game.Track;
using TurboPattes.Game.Visuals;
using UnityEngine;

namespace TurboPattes.Game.Kart
{
    /// <summary>
    /// Conduite arcade d'un kart sur un circuit plat, sans moteur physique : vitesse avant/latérale,
    /// drift à trois niveaux, turbos, tête-à-queue, et rebond sur les barrières calculé depuis la ligne médiane.
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

        public AnimalSpec Spec { get; private set; }
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

        public int TrackIndex { get; private set; }
        public float Lateral { get; private set; }
        public bool Offroad { get; private set; }
        public readonly DriftCharge Drift = new();

        public float BoostTime { get; private set; }
        public float SpinTime { get; private set; }
        public float ShieldTime { get; set; }
        public float HopTime { get; private set; }
        public bool Finished { get; set; }
        public float FinishTime { get; set; }
        public int Rank { get; set; }

        public ItemKind Item { get; set; }
        /// <summary>Temps restant de la roulette d'objet (l'objet n'est utilisable qu'à 0).</summary>
        public float ItemRoll { get; set; }

        /// <summary>Événements pour le son et l'interface.</summary>
        public event Action<KartMotor> BarrierHit, Boosted, SpunOut, DriftLevelUp;

        public Vector3 Forward => new(Mathf.Sin(Yaw), 0f, Mathf.Cos(Yaw));
        public Vector3 Right => new(Mathf.Cos(Yaw), 0f, -Mathf.Sin(Yaw));
        public float ForwardSpeed => Vector3.Dot(Velocity, Forward);
        public float Speed => Velocity.magnitude;
        public float Progress => Laps.Progress;
        public float TopSpeed => MaxSpeed * SpeedScale;

        float _hopWindow;
        bool _prevDrift;
        float _steerVisual, _spinAngle, _wheelAngle, _driftVisual, _tilt;
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

        public void Init(AnimalSpec spec, KartRig rig, TrackPath path, float startS, float startLateral)
        {
            Spec = spec;
            Rig = rig;
            Path = path;
            path.Sample(startS, out var p, out var t, out var r);
            Position = p + r * startLateral;
            Yaw = Mathf.Atan2(t.x, t.z);
            TrackIndex = path.Nearest(Position, -1);
            Laps = new LapTracker(path.Length, path.DistanceAlong(TrackIndex, Position));
            BuildFx();
            UpdateVisuals(0f);
        }

        // ------------------------------------------------------------------ simulation

        public void Tick(KartInput input, float dt)
        {
            if (Finished) input = AutoPilotInput();
            else if (!ControlsEnabled) input = default;

            Vector3 fwd = Forward, right = Right;
            float vF = Vector3.Dot(Velocity, fwd);
            float vR = Vector3.Dot(Velocity, right);
            float top = TopSpeed * (Offroad && BoostTime <= 0f ? OffroadFactor : 1f);
            float accelMul = 1f;
            if (BoostTime > 0f)
            {
                top = TopSpeed * BoostFactor;
                accelMul = 3f;
                if (vF < top * 0.85f) vF = Mathf.Min(top * 0.85f, vF + 40f * dt);
            }

            float turn = 0f;
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
                // Avancer / freiner / reculer
                if (input.Throttle > 0.05f && vF < top)
                    vF = Mathf.Min(top, vF + Accel * accelMul * input.Throttle * dt * (vF < 8f ? 1.4f : 1f));
                else if (input.Brake > 0.05f)
                    vF = vF > 0.5f ? vF - BrakeDecel * input.Brake * dt : Mathf.Max(-ReverseMax, vF - Accel * 0.6f * dt);
                else if (input.Throttle <= 0.05f)
                    vF = Mathf.MoveTowards(vF, 0f, Coast * dt);
                if (vF > top) vF = Mathf.MoveTowards(vF, top, (Offroad ? 22f : 9f) * dt);

                float grip = Mathf.Clamp01(Mathf.Abs(vF) / 7f);

                // Saut + drift
                bool driftEdge = input.Drift && !_prevDrift;
                if (driftEdge && HopTime <= 0f) { HopTime = 0.28f; _hopWindow = HopWindow; }
                if (_hopWindow > 0f) _hopWindow -= dt;

                if (Drift.Active)
                {
                    float into = input.Steer * Drift.Direction;
                    turn = Drift.Direction * Mathf.Lerp(0.5f, 1.35f, (into + 1f) * 0.5f) * TurnRate;
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
                    // Glisse vers l'extérieur du virage
                    vR = Mathf.Lerp(vR, -Drift.Direction * vF * 0.22f, 1f - Mathf.Exp(-4f * dt));
                    int level = Drift.Level;
                    if (level > _lastDriftLevel) DriftLevelUp?.Invoke(this);
                    _lastDriftLevel = level;
                }
                else
                {
                    _lastDriftLevel = 0;
                    turn = input.Steer * TurnRate * (vF < -0.5f ? -1f : 1f);
                    if (input.Drift && _hopWindow > 0f && Mathf.Abs(input.Steer) > 0.3f && vF > MinDriftSpeed)
                        Drift.Start(input.Steer > 0f ? 1 : -1);
                    vR *= Mathf.Exp(-10f * dt);
                }
                Yaw += turn * grip * dt;
            }
            _prevDrift = input.Drift;

            fwd = Forward;
            right = Right;
            Velocity = fwd * vF + right * vR;
            Position += Velocity * dt;
            Position.y = 0f;

            if (BoostTime > 0f) BoostTime -= dt;
            if (ShieldTime > 0f) ShieldTime -= dt;
            if (HopTime > 0f) HopTime -= dt;
            if (ItemRoll > 0f) ItemRoll -= dt;

            _steerVisual = Mathf.Lerp(_steerVisual, SpinTime > 0f ? 0f : input.Steer, 1f - Mathf.Exp(-12f * dt));
            _wheelAngle += vF * dt / 0.33f * Mathf.Rad2Deg;
            ResolveBarriers();
        }

        /// <summary>Après l'arrivée : l'IA reprend le volant pour finir le tour d'honneur.</summary>
        KartInput AutoPilotInput()
        {
            float s = Laps.S + 10f + Mathf.Max(0f, ForwardSpeed) * 0.4f;
            var target = Path.PointAt(s, Mathf.Clamp(Lateral, -3f, 3f));
            var to = target - Position;
            float angle = Vector3.SignedAngle(Forward, to, Vector3.up);
            return new KartInput { Steer = Mathf.Clamp(angle / 25f, -1f, 1f), Throttle = ForwardSpeed < TopSpeed * 0.6f ? 1f : 0f };
        }

        void ResolveBarriers()
        {
            TrackIndex = Path.Nearest(Position, TrackIndex);
            float lat = Path.Lateral(TrackIndex, Position);
            float limit = Path.BarrierOffset - Radius * 0.8f;
            if (Mathf.Abs(lat) > limit)
            {
                float side = Mathf.Sign(lat);
                Vector3 n = Path.Rights[TrackIndex] * side;
                Position -= n * (Mathf.Abs(lat) - limit);
                float vn = Vector3.Dot(Velocity, n);
                if (vn > 0f)
                {
                    Velocity -= n * vn * 1.4f;
                    Velocity *= 0.88f;
                    if (vn > 3f) BarrierHit?.Invoke(this);
                }
                lat = side * limit;
            }
            Lateral = lat;
            Offroad = Mathf.Abs(lat) > Path.HalfWidth + Path.CurbWidth;
            Laps.Update(Path.DistanceAlong(TrackIndex, Position));
        }

        public void GiveBoost(float seconds)
        {
            BoostTime = Mathf.Max(BoostTime, seconds);
            Boosted?.Invoke(this);
        }

        /// <summary>Touché par un objet : le bouclier absorbe, sinon tête-à-queue.</summary>
        public bool Hit(float spinSeconds)
        {
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
            d.y = 0f;
            float dist = d.magnitude;
            const float min = Radius * 2f;
            if (dist >= min || dist < 1e-4f) return;
            Vector3 n = d / dist;
            float push = (min - dist) * 0.5f;
            a.Position -= n * push;
            b.Position += n * push;
            float rel = Vector3.Dot(b.Velocity - a.Velocity, n);
            if (rel < 0f)
            {
                // Le kart sous turbo ou bouclier pousse plus fort.
                float wa = a.BoostTime > 0f || a.ShieldTime > 0f ? 0.25f : 0.5f;
                float wb = b.BoostTime > 0f || b.ShieldTime > 0f ? 0.25f : 0.5f;
                float j = -rel * 1.2f;
                a.Velocity -= n * j * (1f - wa) * 0.8f;
                b.Velocity += n * j * (1f - wb) * 0.8f;
            }
        }

        // ------------------------------------------------------------------ rendu

        public void UpdateVisuals(float dt)
        {
            var root = Rig.Root;
            root.SetPositionAndRotation(Position, Quaternion.Euler(0f, Yaw * Mathf.Rad2Deg, 0f));

            float targetDrift = Drift.Active ? Drift.Direction * 28f : 0f;
            _driftVisual = Mathf.Lerp(_driftVisual, targetDrift, 1f - Mathf.Exp(-10f * dt));
            _tilt = Mathf.Lerp(_tilt, -_steerVisual * Mathf.Clamp01(Speed / MaxSpeed) * 6f, 1f - Mathf.Exp(-8f * dt));
            float hop = HopTime > 0f ? Mathf.Sin((1f - HopTime / 0.28f) * Mathf.PI) * 0.45f : 0f;
            float bob = Mathf.Sin(Time.time * 18f) * 0.015f * Mathf.Clamp01(Speed / 5f);
            Rig.Body.localPosition = new Vector3(0f, hop + bob, 0f);
            Rig.Body.localRotation = Quaternion.Euler(0f, _driftVisual + (SpinTime > 0f ? _spinAngle : 0f), _tilt);
            if (SpinTime <= 0f) _spinAngle = 0f;

            for (int i = 0; i < Rig.Wheels.Length; i++)
                Rig.Wheels[i].localRotation = Quaternion.Euler(_wheelAngle, 0f, 0f);
            foreach (var p in Rig.FrontPivots)
                p.localRotation = Quaternion.Euler(0f, _steerVisual * 28f + _driftVisual * 0.5f, 0f);

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
            bool sparks = Drift.Active && HopTime <= 0f;
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
            SetEmitting(_dust, Offroad && Speed > 5f);
        }
    }
}
