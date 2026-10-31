using System;
using UnityEngine;

namespace TurboPattes.Game.Visuals
{
    /// <summary>
    /// Petite toile pour dessiner des icônes cartoon par code : chaque forme est une fonction de distance signée
    /// (négative à l'intérieur), remplie avec anticrénelage et un contour sombre. Coordonnées en unités de la toile :
    /// (0, 0) en haut à gauche, (1, 1) en bas à droite.
    /// </summary>
    public sealed class IconCanvas
    {
        public static readonly Color Ink = new(0.1f, 0.09f, 0.16f);
        /// <summary>Épaisseur de contour par défaut (unités de toile).</summary>
        public const float Line = 0.024f;

        readonly int _n;
        readonly Color[] _px;

        public IconCanvas(int size = 160)
        {
            _n = size;
            _px = new Color[size * size];
        }

        /// <summary>
        /// Remplit la forme. <paramref name="outline"/> trace un contour de cette épaisseur autour ;
        /// <paramref name="shade"/> éclaire le haut et assombrit le bas (volume).
        /// </summary>
        public void Fill(Func<Vector2, float> sdf, Color color, float outline = Line, float shade = 0.18f)
        {
            float px = 1f / _n;
            for (int y = 0; y < _n; y++)
            {
                float v = (y + 0.5f) * px;
                float light = 1f + shade * (0.45f - v);
                var c = new Color(Mathf.Clamp01(color.r * light), Mathf.Clamp01(color.g * light), Mathf.Clamp01(color.b * light), color.a);
                for (int x = 0; x < _n; x++)
                {
                    float d = sdf(new Vector2((x + 0.5f) * px, v)) * _n;
                    float reach = d - outline * _n;
                    if (reach > 1f) continue;
                    int i = y * _n + x;
                    if (outline > 0f) _px[i] = Over(_px[i], Ink, Mathf.Clamp01(0.5f - reach));
                    float cov = Mathf.Clamp01(0.5f - d);
                    if (cov > 0f) _px[i] = Over(_px[i], c, cov * c.a);
                }
            }
        }

        static Color Over(Color dst, Color src, float a)
        {
            float outA = a + dst.a * (1f - a);
            if (outA <= 0f) return Color.clear;
            var rgb = ((Vector4)src * a + (Vector4)dst * (dst.a * (1f - a))) / outA;
            return new Color(rgb.x, rgb.y, rgb.z, outA);
        }

        public Texture2D ToTexture(string name)
        {
            // IMGUI ne décode pas les textures sRGB en espace linéaire (couleurs délavées) : on stocke
            // directement les valeurs linéaires dans une texture marquée linéaire, comme UiKit.Box.
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            var tex = new Texture2D(_n, _n, TextureFormat.RGBA32, true, linear)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
            };
            // La texture compte ses lignes depuis le bas.
            var flipped = new Color[_px.Length];
            for (int y = 0; y < _n; y++) Array.Copy(_px, y * _n, flipped, (_n - 1 - y) * _n, _n);
            if (linear)
                for (int i = 0; i < flipped.Length; i++)
                {
                    var c = flipped[i];
                    flipped[i] = new Color(c.linear.r, c.linear.g, c.linear.b, c.a);
                }
            tex.SetPixels(flipped);
            tex.Apply(true, false);
            return tex;
        }
    }

    /// <summary>Fonctions de distance signée 2D (formules d'Inigo Quilez).</summary>
    public static class Sdf
    {
        public static Func<Vector2, float> Circle(float cx, float cy, float r) =>
            p => Vector2.Distance(p, new Vector2(cx, cy)) - r;

        /// <summary>Ellipse tournée de <paramref name="degrees"/> (distance approchée, très bonne près du bord).</summary>
        public static Func<Vector2, float> Ellipse(float cx, float cy, float rx, float ry, float degrees = 0f)
        {
            float a = degrees * Mathf.Deg2Rad, cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            return p =>
            {
                float dx = p.x - cx, dy = p.y - cy;
                var q = new Vector2(dx * cos + dy * sin, -dx * sin + dy * cos);
                float k0 = new Vector2(q.x / rx, q.y / ry).magnitude;
                float k1 = new Vector2(q.x / (rx * rx), q.y / (ry * ry)).magnitude;
                return k1 < 1e-6f ? -Mathf.Min(rx, ry) : k0 * (k0 - 1f) / k1;
            };
        }

        /// <summary>Segment épais aux bouts arrondis.</summary>
        public static Func<Vector2, float> Capsule(float ax, float ay, float bx, float by, float r)
        {
            var a = new Vector2(ax, ay);
            var ba = new Vector2(bx - ax, by - ay);
            float len2 = Mathf.Max(1e-8f, ba.sqrMagnitude);
            return p =>
            {
                var pa = p - a;
                float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / len2);
                return (pa - ba * h).magnitude - r;
            };
        }

        /// <summary>Capsule dont le rayon passe de <paramref name="ra"/> (en a) à <paramref name="rb"/> (en b).</summary>
        public static Func<Vector2, float> Cone(float ax, float ay, float ra, float bx, float by, float rb)
        {
            var pa = new Vector2(ax, ay);
            var pb = new Vector2(bx, by) - pa;
            float h = pb.sqrMagnitude, b = ra - rb;
            var c = new Vector2(Mathf.Sqrt(Mathf.Max(0f, h - b * b)), b);
            return p0 =>
            {
                var p = p0 - pa;
                var q = new Vector2(Mathf.Abs(Vector2.Dot(p, new Vector2(pb.y, -pb.x))), Vector2.Dot(p, pb)) / h;
                float k = c.x * q.y - c.y * q.x, m = Vector2.Dot(c, q), n = Vector2.Dot(q, q);
                if (k < 0f) return Mathf.Sqrt(h * n) - ra;
                if (k > c.x) return Mathf.Sqrt(h * (n + 1f - 2f * q.y)) - rb;
                return m - ra;
            };
        }

        /// <summary>Polygone quelconque (sommets x0, y0, x1, y1…), arrondi de <paramref name="round"/>.</summary>
        public static Func<Vector2, float> Polygon(float round, params float[] xy)
        {
            int n = xy.Length / 2;
            var v = new Vector2[n];
            for (int i = 0; i < n; i++) v[i] = new Vector2(xy[2 * i], xy[2 * i + 1]);
            return p =>
            {
                float d = (p - v[0]).sqrMagnitude, s = 1f;
                for (int i = 0, j = n - 1; i < n; j = i, i++)
                {
                    var e = v[j] - v[i];
                    var w = p - v[i];
                    var b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / e.sqrMagnitude);
                    d = Mathf.Min(d, b.sqrMagnitude);
                    bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                    if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
                }
                return s * Mathf.Sqrt(d) - round;
            };
        }

        /// <summary>Anneau d'épaisseur 2 × <paramref name="half"/>.</summary>
        public static Func<Vector2, float> Ring(float cx, float cy, float r, float half) =>
            p => Mathf.Abs(Vector2.Distance(p, new Vector2(cx, cy)) - r) - half;

        /// <summary>Sourire : moitié basse d'un anneau, bouts arrondis.</summary>
        public static Func<Vector2, float> Smile(float cx, float cy, float r, float half, float degrees = 140f)
        {
            float a = degrees * 0.5f * Mathf.Deg2Rad;
            var sc = new Vector2(Mathf.Sin(a), Mathf.Cos(a));
            return p =>
            {
                var q = p - new Vector2(cx, cy);
                q.x = Mathf.Abs(q.x);
                // Arc centré vers le bas (y croît vers le bas).
                float d = sc.y * q.x > sc.x * q.y ? (q - sc * r).magnitude : Mathf.Abs(q.magnitude - r);
                return d - half;
            };
        }

        public static Func<Vector2, float> Union(params Func<Vector2, float>[] parts) => p =>
        {
            float d = float.MaxValue;
            foreach (var f in parts) d = Mathf.Min(d, f(p));
            return d;
        };

        public static Func<Vector2, float> Intersect(Func<Vector2, float> a, Func<Vector2, float> b) => p => Mathf.Max(a(p), b(p));

        /// <summary>Miroir gauche/droite autour de x = 0,5 (pour dessiner une seule oreille, un seul œil…).</summary>
        public static Func<Vector2, float> Mirror(Func<Vector2, float> f) => p => f(new Vector2(0.5f + Mathf.Abs(p.x - 0.5f), p.y));
    }
}
