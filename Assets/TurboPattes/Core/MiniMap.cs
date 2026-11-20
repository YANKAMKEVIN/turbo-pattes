using System;
using System.Collections.Generic;

namespace TurboPattes.Core
{
    /// <summary>
    /// Cadrage d'un tracé vu de dessus dans un rectangle d'écran : même échelle sur les deux axes, tracé centré,
    /// nord (z croissant) vers le haut. Sert à la mini-carte du HUD et aux vignettes du choix de circuit.
    /// </summary>
    public readonly struct MapFit
    {
        public readonly float Scale, Left, Top, MinX, MaxZ;

        public MapFit(float scale, float left, float top, float minX, float maxZ)
        {
            Scale = scale;
            Left = left;
            Top = top;
            MinX = minX;
            MaxZ = maxZ;
        }

        /// <summary>Position à l'écran (y vers le bas) d'un point du monde (x, z).</summary>
        public void Project(float x, float z, out float sx, out float sy)
        {
            sx = Left + (x - MinX) * Scale;
            sy = Top + (MaxZ - z) * Scale;
        }
    }

    public static class MiniMap
    {
        /// <summary>Cadre les points (x, z) dans le rectangle (left, top, width, height) avec une marge intérieure.</summary>
        public static MapFit Fit(IReadOnlyList<float> xs, IReadOnlyList<float> zs, float left, float top, float width, float height, float padding = 0f)
        {
            if (xs == null || zs == null || xs.Count == 0 || xs.Count != zs.Count) throw new ArgumentException("Points du tracé manquants");
            float minX = float.MaxValue, maxX = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
            for (int i = 0; i < xs.Count; i++)
            {
                minX = Math.Min(minX, xs[i]);
                maxX = Math.Max(maxX, xs[i]);
                minZ = Math.Min(minZ, zs[i]);
                maxZ = Math.Max(maxZ, zs[i]);
            }
            float w = Math.Max(1e-3f, width - 2f * padding), h = Math.Max(1e-3f, height - 2f * padding);
            float spanX = Math.Max(1e-3f, maxX - minX), spanZ = Math.Max(1e-3f, maxZ - minZ);
            float scale = Math.Min(w / spanX, h / spanZ);
            // Centre le tracé dans le rectangle.
            float l = left + padding + (w - spanX * scale) * 0.5f;
            float t = top + padding + (h - spanZ * scale) * 0.5f;
            return new MapFit(scale, l, t, minX, maxZ);
        }

        /// <summary><paramref name="dots"/> indices régulièrement espacés parmi <paramref name="count"/> points (tracé en pointillés).</summary>
        public static int[] Spread(int count, int dots)
        {
            if (count <= 0 || dots <= 0) return Array.Empty<int>();
            dots = Math.Min(dots, count);
            var idx = new int[dots];
            for (int i = 0; i < dots; i++) idx[i] = (int)((long)i * count / dots);
            return idx;
        }
    }
}
