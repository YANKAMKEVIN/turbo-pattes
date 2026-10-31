using System.Collections.Generic;
using UnityEngine;

namespace TurboPattes.Game.Track
{
    /// <summary>
    /// Ligne médiane du circuit : spline de Catmull-Rom fermée, échantillonnée finement.
    /// Le circuit est plat (y = 0). « Droite » = vers la droite dans le sens de la course.
    /// </summary>
    public sealed class TrackPath
    {
        public readonly Vector3[] Points;
        public readonly Vector3[] Tangents;
        public readonly Vector3[] Rights;
        /// <summary>Distance cumulée au début de chaque échantillon.</summary>
        public readonly float[] Dist;
        public readonly float Length;
        /// <summary>Demi-largeur du bitume.</summary>
        public readonly float HalfWidth;
        /// <summary>Largeur des vibreurs, au-delà du bitume.</summary>
        public readonly float CurbWidth;
        /// <summary>Distance de la barrière à la ligne médiane.</summary>
        public readonly float BarrierOffset;

        public int Count => Points.Length;

        public TrackPath(IList<Vector2> control, int samplesPerSegment, float halfWidth, float curbWidth, float barrierOffset)
        {
            HalfWidth = halfWidth;
            CurbWidth = curbWidth;
            BarrierOffset = barrierOffset;
            int n = control.Count;
            int count = n * samplesPerSegment;
            Points = new Vector3[count];
            Tangents = new Vector3[count];
            Rights = new Vector3[count];
            Dist = new float[count];
            for (int i = 0; i < n; i++)
            {
                Vector2 p0 = control[(i - 1 + n) % n], p1 = control[i], p2 = control[(i + 1) % n], p3 = control[(i + 2) % n];
                for (int k = 0; k < samplesPerSegment; k++)
                {
                    float t = k / (float)samplesPerSegment;
                    Vector2 p = CatmullRom(p0, p1, p2, p3, t);
                    Points[i * samplesPerSegment + k] = new Vector3(p.x, 0f, p.y);
                }
            }
            float d = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 next = Points[(i + 1) % count], prev = Points[(i - 1 + count) % count];
                Vector3 t = (next - prev);
                t.y = 0f;
                t.Normalize();
                Tangents[i] = t;
                Rights[i] = new Vector3(t.z, 0f, -t.x);
                Dist[i] = d;
                d += Vector3.Distance(Points[i], next);
            }
            Length = d;
        }

        static Vector2 CatmullRom(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        public float Wrap(float s)
        {
            s %= Length;
            return s < 0f ? s + Length : s;
        }

        /// <summary>Échantillon le plus proche ; recherche locale autour de <paramref name="hint"/> (-1 = tout le circuit).</summary>
        public int Nearest(Vector3 pos, int hint, int window = 40)
        {
            int best = 0;
            float bestD = float.MaxValue;
            if (hint < 0)
            {
                for (int i = 0; i < Count; i++)
                {
                    float dd = FlatSqr(Points[i], pos);
                    if (dd < bestD) { bestD = dd; best = i; }
                }
                return best;
            }
            for (int o = -window; o <= window; o++)
            {
                int i = ((hint + o) % Count + Count) % Count;
                float dd = FlatSqr(Points[i], pos);
                if (dd < bestD) { bestD = dd; best = i; }
            }
            return best;
        }

        static float FlatSqr(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        /// <summary>Distance le long du circuit du point projeté près de l'échantillon <paramref name="i"/>.</summary>
        public float DistanceAlong(int i, Vector3 pos)
        {
            float along = Vector3.Dot(pos - Points[i], Tangents[i]);
            return Wrap(Dist[i] + along);
        }

        /// <summary>Décalage latéral (positif à droite) par rapport à l'échantillon <paramref name="i"/>.</summary>
        public float Lateral(int i, Vector3 pos) => Vector3.Dot(pos - Points[i], Rights[i]);

        public int IndexAt(float s)
        {
            s = Wrap(s);
            int lo = 0, hi = Count - 1;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) >> 1;
                if (Dist[mid] <= s) lo = mid; else hi = mid - 1;
            }
            return lo;
        }

        /// <summary>Point, tangente et droite interpolés à la distance <paramref name="s"/>.</summary>
        public void Sample(float s, out Vector3 point, out Vector3 tangent, out Vector3 right)
        {
            s = Wrap(s);
            int i = IndexAt(s);
            int j = (i + 1) % Count;
            float segLen = (j == 0 ? Length : Dist[j]) - Dist[i];
            float t = segLen > 1e-4f ? (s - Dist[i]) / segLen : 0f;
            point = Vector3.Lerp(Points[i], Points[j], t);
            tangent = Vector3.Slerp(Tangents[i], Tangents[j], t).normalized;
            right = new Vector3(tangent.z, 0f, -tangent.x);
        }

        public Vector3 PointAt(float s, float lateral = 0f)
        {
            Sample(s, out var p, out _, out var r);
            return p + r * lateral;
        }

        /// <summary>Angle (degrés, toujours positif) entre les tangentes à <paramref name="s0"/> et <paramref name="s1"/>.</summary>
        public float TurnBetween(float s0, float s1)
        {
            Sample(s0, out _, out var t0, out _);
            Sample(s1, out _, out var t1, out _);
            return Vector3.Angle(t0, t1);
        }

        /// <summary>Signe du virage entre deux distances : +1 à droite, -1 à gauche.</summary>
        public int TurnSign(float s0, float s1)
        {
            Sample(s0, out _, out var t0, out _);
            Sample(s1, out _, out var t1, out _);
            return Vector3.Cross(t0, t1).y >= 0f ? 1 : -1;
        }
    }
}
