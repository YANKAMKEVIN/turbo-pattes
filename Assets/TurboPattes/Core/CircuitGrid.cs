using System;

namespace TurboPattes.Core
{
    /// <summary>Grille des vignettes du choix de circuit : cinq colonnes, déplacement au clavier ou à la manette.</summary>
    public static class CircuitGrid
    {
        public const int Columns = 5;

        public static int Rows(int count) => (count + Columns - 1) / Columns;

        /// <summary>Case atteinte depuis <paramref name="index"/> en se déplaçant de (dx, dy), sans sortir de la grille.</summary>
        public static int Move(int index, int dx, int dy, int count)
        {
            if (count <= 0) return 0;
            index = Math.Max(0, Math.Min(count - 1, index));
            int col = Math.Max(0, Math.Min(Columns - 1, index % Columns + dx));
            int row = Math.Max(0, Math.Min(Rows(count) - 1, index / Columns + dy));
            return Math.Min(count - 1, row * Columns + col);
        }
    }
}
