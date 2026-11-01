using TurboPattes.Core;
using TurboPattes.Game.Items;
using UnityEngine;

namespace TurboPattes.Game.Kart
{
    /// <summary>
    /// Pilote IA : vise un point devant lui sur sa trajectoire, ralentit avant les virages serrés,
    /// drifte dans les grandes courbes pour gagner des turbos et utilise ses objets.
    /// </summary>
    public sealed class AiDriver
    {
        readonly KartMotor _kart;
        readonly System.Random _rng;
        readonly float _skill;
        float _lane, _laneTarget, _laneTimer;
        float _itemDelay;
        bool _drifting;
        float _stuckTime, _reverseTime;

        /// <param name="skill">0 (prudent) à 1 (excellent).</param>
        public AiDriver(KartMotor kart, int seed, float skill)
        {
            _kart = kart;
            _rng = new System.Random(seed);
            _skill = skill;
            _laneTarget = _lane = Rand(-3f, 3f);
            _laneTimer = Rand(2f, 5f);
            _itemDelay = Rand(0.5f, 2.5f);
        }

        float Rand(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        public KartInput Think(float dt, KartMotor[] karts, ItemSystem items, out bool wantsItem)
        {
            wantsItem = false;
            var k = _kart;
            var path = k.Path;
            float s = k.Laps.S;
            float speed = k.ForwardSpeed;

            _laneTimer -= dt;
            if (_laneTimer <= 0f)
            {
                _laneTarget = Rand(-path.HalfWidth + 2f, path.HalfWidth - 2f);
                _laneTimer = Rand(2.5f, 6f);
            }
            // Éviter les pots de miel devant soi.
            float laneSpeed = 1.5f;
            if (items != null && items.HoneyAhead(k, 28f, out var honey))
            {
                float hl = path.Lateral(path.Nearest(honey, k.TrackIndex), honey);
                if (Mathf.Abs(_laneTarget - hl) < 3f)
                    _laneTarget = Mathf.Clamp(hl > 0f ? hl - 3.5f : hl + 3.5f, -path.HalfWidth + 1.5f, path.HalfWidth - 1.5f);
                laneSpeed = 3.5f + 2f * _skill;
            }
            _lane = Mathf.MoveTowards(_lane, _laneTarget, laneSpeed * dt);

            // Corde : dans les virages, se rapprocher de l'intérieur.
            float turnAhead = path.TurnBetween(s + 8f, s + 38f);
            int turnSign = path.TurnSign(s + 8f, s + 38f);
            float lane = _lane;
            if (turnAhead > 25f) lane = Mathf.Lerp(_lane, turnSign * (path.HalfWidth - 4f), Mathf.Clamp01((turnAhead - 25f) / 60f));
            // Hors du bitume : revenir vers la piste plutôt que couper par l'herbe.
            if (Mathf.Abs(k.Lateral) > path.HalfWidth) lane = Mathf.Clamp(k.Lateral * 0.3f, -path.HalfWidth + 2f, path.HalfWidth - 2f);

            // Viser plus près quand ça tourne, sinon la corde passe dans l'herbe.
            float look = Mathf.Lerp(7f + Mathf.Max(0f, speed) * 0.45f, 5f + Mathf.Max(0f, speed) * 0.22f, Mathf.Clamp01(turnAhead / 70f));
            var target = path.PointAt(s + look, lane);
            Vector3 to = target - k.Position;
            to.y = 0f; // seul compte le cap : une cible en haut d'une côte ne doit pas fausser le volant
            float angle = Vector3.SignedAngle(k.Forward, to, Vector3.up);

            var input = new KartInput { Steer = Mathf.Clamp(angle / 22f, -1f, 1f), Throttle = 1f };

            // Vitesse de passage selon la courbure à venir.
            float sharp = path.TurnBetween(s + 5f, s + 30f);
            float cornerSpeed = k.TopSpeed * Mathf.Lerp(1f, 0.62f + 0.1f * _skill, Mathf.Clamp01((sharp - 30f) / 80f));
            if (speed > cornerSpeed + 2f && !_drifting) { input.Throttle = 0f; input.Brake = 0.4f; }

            // Drift dans les grandes courbes.
            if (!_drifting && turnAhead > 40f && speed > 15f && Mathf.Abs(input.Steer) > 0.35f && _rng.NextDouble() < 0.3 + 0.6 * _skill)
                _drifting = true;
            if (_drifting)
            {
                input.Drift = true;
                bool straight = path.TurnBetween(s + 3f, s + 22f) < 14f;
                if (straight || k.Drift.Active && k.Drift.Level >= 3 && k.Drift.Duration > 2.6f || speed < 10f) _drifting = false;
                if (!k.Drift.Active && k.HopTime <= 0f && Mathf.Abs(input.Steer) < 0.3f) _drifting = false;
            }

            // Décoincer : marche arrière si bloqué contre un mur.
            if (k.ControlsEnabled && Mathf.Abs(speed) < 1.5f && k.SpinTime <= 0f) _stuckTime += dt; else _stuckTime = 0f;
            if (_stuckTime > 1.5f) { _reverseTime = 1.0f; _stuckTime = 0f; }
            if (_reverseTime > 0f)
            {
                _reverseTime -= dt;
                input = new KartInput { Steer = -input.Steer, Brake = 1f };
            }

            // Figure en l'air sur les grands sauts (turbo à la réception), plus souvent chez les bons pilotes.
            if (k.Airborne && !k.Trick.Active && !k.Trick.Done && k.Vertical.AirTime > AirTrick.MinAirTime + 0.05f && _trickRoll < 0.35 + 0.6 * _skill)
                input.Drift = !_trickPressed;
            _trickPressed = input.Drift && k.Airborne;
            if (!k.Airborne) _trickRoll = _rng.NextDouble();

            wantsItem = DecideItem(dt, karts, items);
            return input;
        }

        bool _trickPressed;
        double _trickRoll;

        bool DecideItem(float dt, KartMotor[] karts, ItemSystem items)
        {
            var k = _kart;
            if (k.Item == ItemKind.None || k.ItemRoll > 0f) return false;
            _itemDelay -= dt;
            if (_itemDelay > 0f) return false;
            bool use = false;
            switch (k.Item)
            {
                case ItemKind.Carrot:
                    // Sur une ligne droite, ou pour sortir vite de l'herbe.
                    use = k.Path.TurnBetween(k.Laps.S, k.Laps.S + 50f) < 25f || k.Offroad && k.ForwardSpeed < 14f;
                    break;
                case ItemKind.Shield:
                    // Gardé pour parer un projectile ou quand quelqu'un colle derrière.
                    use = items != null && items.ThreatNear(k, 25f) || AnyoneBehind(karts, 12f) || _itemDelay < -12f;
                    break;
                case ItemKind.Feather:
                    // Sauter par-dessus un pot de miel ou un projectile, sinon prendre l'élan sur une ligne droite.
                    use = items != null && (items.HoneyAhead(k, 14f, out _) || items.ThreatNear(k, 12f))
                          || _itemDelay < -6f && k.Path.TurnBetween(k.Laps.S, k.Laps.S + 40f) < 15f;
                    break;
                case ItemKind.Yarn:
                    use = AnyoneAheadProgress(karts, 150f) || _itemDelay < -6f;
                    break;
                case ItemKind.Honey:
                    use = AnyoneBehind(karts, 30f) || _itemDelay < -8f;
                    break;
                case ItemKind.Coconut:
                    use = AnyoneAhead(karts, 40f) || _itemDelay < -10f;
                    break;
            }
            if (use) _itemDelay = Rand(1f, 3f);
            return use;
        }

        bool AnyoneBehind(KartMotor[] karts, float range)
        {
            foreach (var o in karts)
            {
                if (o == _kart) continue;
                float d = _kart.Progress - o.Progress;
                if (d > 2f && d < range) return true;
            }
            return false;
        }

        bool AnyoneAheadProgress(KartMotor[] karts, float range)
        {
            foreach (var o in karts)
            {
                if (o == _kart) continue;
                float d = o.Progress - _kart.Progress;
                if (d > 5f && d < range) return true;
            }
            return false;
        }

        bool AnyoneAhead(KartMotor[] karts, float range)
        {
            foreach (var o in karts)
            {
                if (o == _kart) continue;
                Vector3 to = o.Position - _kart.Position;
                float dist = to.magnitude;
                if (dist < range && dist > 3f && Vector3.Angle(_kart.Forward, to) < 12f) return true;
            }
            return false;
        }
    }
}
