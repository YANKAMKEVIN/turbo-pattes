using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Recherche de l'échantillon de piste le plus proche d'une position. La hauteur compte aussi : là où la piste
    /// se croise elle-même (pont), un kart sur le pont et un kart dessous ne se trompent pas de tronçon.
    /// </summary>
    public sealed class TrackProjection
    {
        /// <summary>Poids de l'écart vertical face à l'écart horizontal.</summary>
        public const float VerticalWeight = 1.5f;

        readonly float[] _x, _y, _z;

        public int Count => _x.Length;

        public TrackProjection(float[] x, float[] y, float[] z)
        {
            if (x == null || y == null || z == null || x.Length != y.Length || x.Length != z.Length || x.Length == 0)
                throw new ArgumentException("échantillons invalides");
            _x = x;
            _y = y;
            _z = z;
        }

        public float DistanceSqr(int i, float x, float y, float z)
        {
            float dx = _x[i] - x, dy = (_y[i] - y) * VerticalWeight, dz = _z[i] - z;
            return dx * dx + dy * dy + dz * dz;
        }

        /// <summary>Échantillon le plus proche ; recherche locale autour de <paramref name="hint"/> (-1 = tout le circuit).</summary>
        public int Nearest(float x, float y, float z, int hint, int window = 40)
        {
            int n = _x.Length;
            int best = 0;
            float bestD = float.MaxValue;
            if (hint < 0 || 2 * window + 1 >= n)
            {
                for (int i = 0; i < n; i++)
                {
                    float d = DistanceSqr(i, x, y, z);
                    if (d < bestD) { bestD = d; best = i; }
                }
                return best;
            }
            for (int o = -window; o <= window; o++)
            {
                int i = ((hint + o) % n + n) % n;
                float d = DistanceSqr(i, x, y, z);
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }
    }
}
