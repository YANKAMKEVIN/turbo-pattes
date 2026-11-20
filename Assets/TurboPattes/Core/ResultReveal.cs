using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Mise en scène de l'écran de résultats d'une course : les lignes entrent une à une, du dernier au premier,
    /// puis le podium est fêté si le joueur finit dans les trois premiers.
    /// </summary>
    public static class ResultReveal
    {
        /// <summary>Attente avant la première ligne, écart entre deux lignes, durée d'une entrée (s).</summary>
        public const float Delay = 0.3f, Step = 0.16f, Slide = 0.35f;

        /// <summary>Instant d'entrée de la ligne <paramref name="row"/> (0 = premier) parmi <paramref name="rows"/>.</summary>
        public static float RowStart(int row, int rows) => Delay + (rows - 1 - row) * Step;

        /// <summary>Avancement de l'entrée de la ligne (0 à 1, freinée en fin de course).</summary>
        public static float RowProgress(int row, int rows, float time)
        {
            float t = Math.Max(0f, Math.Min(1f, (time - RowStart(row, rows)) / Slide));
            float u = 1f - t;
            return 1f - u * u * u;
        }

        /// <summary>Toutes les lignes sont en place (le premier entre en dernier).</summary>
        public static float Done(int rows) => RowStart(0, rows) + Slide;

        /// <summary>Confettis quand le joueur finit dans les trois premiers (place comptée depuis 0).</summary>
        public static bool Celebrate(int playerRank) => playerRank >= 0 && playerRank < 3;
    }
}
