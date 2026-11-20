using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Traits de vitesse radiaux sur les bords de l'écran, pendant le turbo et les sauts. Leur nombre suit le niveau
    /// de qualité (0 = le plus beau) ; chaque trait a un angle, une distance au centre et une longueur fixes, et
    /// clignote à son rythme.
    /// </summary>
    public static class SpeedLines
    {
        static readonly int[] Counts = { 30, 26, 20, 14, 8 };

        public static int Count(int qualityLevel) => Counts[Math.Max(0, Math.Min(Counts.Length - 1, qualityLevel))];

        /// <summary>Intensité visée : pleine en turbo, partielle en l'air selon la vitesse, nulle sinon.</summary>
        public static float Target(bool boosting, bool airborne, float speed01)
        {
            if (boosting) return 1f;
            return airborne ? 0.6f * Math.Max(0f, Math.Min(1f, speed01)) : 0f;
        }

        /// <summary>
        /// Trait <paramref name="i"/> sur <paramref name="count"/> : angle (degrés), départ et longueur en fraction de la
        /// demi-diagonale de l'écran, opacité (0 à 1) à l'instant <paramref name="time"/>.
        /// </summary>
        public static void Line(int i, int count, float time, out float degrees, out float inner, out float length, out float alpha)
        {
            float a = Frac(i * 0.618034f), b = Frac(i * 0.414214f + 0.17f);
            degrees = (i + (a - 0.5f) * 0.6f) * 360f / Math.Max(1, count);
            // Les traits naissent près des bords (au-delà de 62 % de la demi-diagonale) et filent vers l'extérieur.
            float travel = Frac(time * (1.6f + b) + a);
            inner = 0.62f + 0.2f * b + 0.18f * travel;
            length = 0.12f + 0.16f * a;
            alpha = (float)Math.Sin(Math.PI * travel);
        }

        static float Frac(float v) => v - (float)Math.Floor(v);
    }
}
