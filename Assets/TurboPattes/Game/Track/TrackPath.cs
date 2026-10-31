using System.Collections.Generic;
using TurboPattes.Core;
using UnityEngine;

namespace TurboPattes.Game.Track
{
    /// <summary>
    /// Ligne médiane du circuit : spline de Catmull-Rom fermée, échantillonnée finement, avec sa hauteur
    /// (<see cref="HeightProfile"/>). Tangentes et « droite » restent horizontales : la piste n'est jamais inclinée
    /// sur le côté. « Droite » = vers la droite dans le sens de la course.
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
        /// <summary>Hauteur de la piste (null = circuit plat).</summary>
        public readonly HeightProfile Profile;
        /// <summary>Hauteur de chaque échantillon sans les tremplins (talus, murets).</summary>
        public readonly float[] BaseHeights;
        /// <summary>Échantillon porté par un pont (pas de talus dessous).</summary>
        public readonly bool[] Elevated;
        /// <summary>Tronçons de pont [début, fin] en distance le long du circuit.</summary>
        public readonly List<(float from, float to)> Bridges = new();
        /// <summary>Gravité relative du circuit.</summary>
        public readonly float Gravity = 1f;
        /// <summary>Piste suspendue (ciel, espace) : pas de sol dessous.</summary>
        public readonly bool Floating;
        readonly TrackProjection _projection;

        public int Count => Points.Length;
        public bool HasRelief => Profile != null;

        public TrackPath(Circuit circuit, int samplesPerSegment = 40)
            : this(circuit.Control, samplesPerSegment, circuit.HalfWidth, circuit.CurbWidth, circuit.BarrierOffset, circuit) { }

        public TrackPath(IList<Vector2> control, int samplesPerSegment, float halfWidth, float curbWidth, float barrierOffset, Circuit relief = null)
        {
            HalfWidth = halfWidth;
            CurbWidth = curbWidth;
            BarrierOffset = barrierOffset;
            if (relief != null) { Gravity = relief.Gravity; Floating = relief.Floating; }
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

            BaseHeights = new float[count];
            Elevated = new bool[count];
            if (relief?.Heights != null)
            {
                var keyS = new float[n];
                for (int i = 0; i < n; i++) keyS[i] = Dist[i * samplesPerSegment];
                var kickers = new List<Kicker>();
                foreach (var (c, len, rise) in relief.Kickers ?? new (int, float, float)[0])
                    kickers.Add(new Kicker(keyS[c], len, rise));
                Profile = new HeightProfile(keyS, relief.Heights, Length, kickers);
                foreach (var (c, half) in relief.Bridges ?? new (int, float)[0])
                    Bridges.Add((Wrap(keyS[c] - half), Wrap(keyS[c] + half)));
                for (int i = 0; i < count; i++)
                {
                    BaseHeights[i] = Profile.BaseHeight(Dist[i]);
                    Points[i].y = Profile.Height(Dist[i]);
                    Elevated[i] = Floating || OnBridge(Dist[i]);
                }
            }

            var xs = new float[count];
            var ys = new float[count];
            var zs = new float[count];
            for (int i = 0; i < count; i++) (xs[i], ys[i], zs[i]) = (Points[i].x, BaseHeights[i], Points[i].z);
            _projection = new TrackProjection(xs, ys, zs);
        }

        /// <summary>La distance <paramref name="s"/> est-elle sur un pont ?</summary>
        public bool OnBridge(float s)
        {
            s = Wrap(s);
            foreach (var (from, to) in Bridges)
                if (from <= to ? s >= from && s <= to : s >= from || s <= to) return true;
            return false;
        }

        /// <summary>Le tremplin couvre le bitume et les vibreurs, pas l'herbe ni le sable au-delà.</summary>
        public bool OnRamp(float lateral) => Mathf.Abs(lateral) <= HalfWidth + CurbWidth;

        /// <summary>Hauteur du sol à la distance <paramref name="s"/> et au décalage <paramref name="lateral"/>.</summary>
        public float GroundHeight(float s, float lateral) => Profile?.Height(s, OnRamp(lateral)) ?? 0f;

        /// <summary>Pente du sol (dh/ds, dans le sens de la course).</summary>
        public float GroundSlope(float s, float lateral) => Profile?.Slope(s, OnRamp(lateral)) ?? 0f;

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

        /// <summary>
        /// Échantillon le plus proche ; recherche locale autour de <paramref name="hint"/> (-1 = tout le circuit).
        /// La hauteur départage les deux niveaux d'un croisement.
        /// </summary>
        public int Nearest(Vector3 pos, int hint, int window = 40) => _projection.Nearest(pos.x, pos.y, pos.z, hint, window);

        /// <summary>Distance horizontale la plus courte à la ligne médiane, tous tronçons confondus (pour le décor).</summary>
        public float FlatDistance(Vector3 pos, out int nearest)
        {
            nearest = 0;
            float best = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                float dx = Points[i].x - pos.x, dz = Points[i].z - pos.z;
                float dd = dx * dx + dz * dz;
                if (dd < best) { best = dd; nearest = i; }
            }
            return Mathf.Sqrt(best);
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
