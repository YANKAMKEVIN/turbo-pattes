using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Virage relevé : la piste s'incline autour de sa ligne médiane sur une zone (demi-longueur <see cref="HalfLength"/>),
    /// avec une entrée et une sortie douces sur <see cref="Fade"/> mètres. Angle positif = côté droit relevé
    /// (virage à gauche). Le relevé retient le kart : il glisse moins en drift et peut passer plus vite.
    /// </summary>
    public sealed class Banking
    {
        public const float Fade = 20f;
        /// <summary>Adhérence de référence pour la vitesse de passage (pneus de kart sur bitume, arcade).</summary>
        public const float Grip = 0.8f;

        public readonly float CenterS, HalfLength, AngleDeg, TrackLength;

        public Banking(float centerS, float halfLength, float angleDeg, float trackLength)
        {
            if (trackLength <= 0f) throw new ArgumentOutOfRangeException(nameof(trackLength));
            if (halfLength <= 0f) throw new ArgumentOutOfRangeException(nameof(halfLength));
            if (Math.Abs(angleDeg) >= 30f) throw new ArgumentOutOfRangeException(nameof(angleDeg), "relevé arcade : moins de 30°");
            TrackLength = trackLength;
            CenterS = TrackZone.Wrap(centerS, trackLength);
            HalfLength = halfLength;
            AngleDeg = angleDeg;
        }

        /// <summary>Angle d'inclinaison (degrés) à la distance <paramref name="s"/>.</summary>
        public float Angle(float s) => AngleDeg * TrackZone.Weight(s, CenterS, HalfLength, Fade, TrackLength);

        /// <summary>Surélévation du sol au décalage <paramref name="lateral"/> (positif à droite) : la ligne médiane ne bouge pas.</summary>
        public float Lift(float s, float lateral) => lateral * (float)Math.Tan(Angle(s) * Math.PI / 180.0);

        /// <summary>Variation de la surélévation le long de la piste (entrée et sortie du relevé), dh/ds.</summary>
        public float LiftSlope(float s, float lateral)
        {
            const float h = 0.25f;
            return (Lift(s + h, lateral) - Lift(s - h, lateral)) / (2f * h);
        }

        /// <summary>Part de la glisse de drift qui reste sur un relevé de cet angle (1 à plat, 0,5 à 15°).</summary>
        public static float SlideFactor(float angleDeg) => 1f - Math.Min(1f, Math.Abs(angleDeg) / 30f);

        /// <summary>
        /// Vitesse de passage possible dans un virage relevé, rapportée à la même courbe à plat :
        /// v² = r g (sin θ + μ cos θ) / (cos θ - μ sin θ), à comparer à v² = r g μ.
        /// </summary>
        public static float CornerSpeedRatio(float angleDeg, float grip = Grip)
        {
            double a = Math.Abs(angleDeg) * Math.PI / 180.0, sin = Math.Sin(a), cos = Math.Cos(a);
            double den = cos - grip * sin;
            if (den <= 0.0) return float.PositiveInfinity;
            return (float)Math.Sqrt((sin + grip * cos) / den / grip);
        }
    }

    /// <summary>Zone le long d'un tour fermé : poids 1 au milieu, transition douce (smoothstep) sur ses bords.</summary>
    public static class TrackZone
    {
        public static float Wrap(float s, float length)
        {
            s %= length;
            return s < 0f ? s + length : s;
        }

        /// <summary>Écart signé le plus court entre <paramref name="s"/> et <paramref name="center"/>, bouclage compris.</summary>
        public static float Offset(float s, float center, float length)
        {
            float d = Wrap(s - center, length);
            return d > length * 0.5f ? d - length : d;
        }

        public static float Weight(float s, float center, float half, float fade, float length)
        {
            float d = Math.Abs(Offset(s, center, length));
            if (d <= half) return 1f;
            if (d >= half + fade) return 0f;
            float u = 1f - (d - half) / fade;
            return u * u * (3f - 2f * u);
        }
    }
}
