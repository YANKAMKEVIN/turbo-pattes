using System;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace TurboPattes.Game.Track
{
    /// <summary>Décors des thèmes jungle, lagon, récif, ciel, espace, banquise, volcan et ville (tout en primitives).</summary>
    public static partial class TrackBuilder
    {
        /// <summary>Lave à animer, laissée par le décor du volcan pour <see cref="Build"/> (null ailleurs).</summary>
        static (Material mat, Color emission)? _animatedLava;

        static void BuildThemeDecor(Theme theme, TrackPath path, Terrain terrain, ThemeStyle style, Transform root, System.Random rng)
        {
            var decor = new GameObject("Decor").transform;
            decor.SetParent(root, false);
            var ctx = new DecorContext(path, terrain, rng, decor);
            switch (theme)
            {
                case Theme.Jungle: Jungle(ctx); break;
                case Theme.Lagoon: Lagoon(ctx, style); break;
                case Theme.Reef: Reef(ctx); break;
                case Theme.Sky: SkyIslands(ctx); break;
                case Theme.Space: Space(ctx, style); break;
                case Theme.Ice: Ice(ctx); break;
                case Theme.Volcano: Volcano(ctx); break;
                case Theme.City: City(ctx); break;
            }
            if (!path.Floating) BuildGrandstand(path, decor, rng);
        }

        /// <summary>Ce dont chaque décor a besoin : piste, sol, hasard, parent, et la boîte englobante du circuit.</summary>
        sealed class DecorContext
        {
            public readonly TrackPath Path;
            public readonly Terrain Terrain;
            public readonly System.Random Rng;
            public readonly Transform Root;
            public readonly Vector3 Min, Max, Center;
            public readonly float LowY, HighY;

            public DecorContext(TrackPath path, Terrain terrain, System.Random rng, Transform root)
            {
                Path = path;
                Terrain = terrain;
                Rng = rng;
                Root = root;
                Min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                Max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
                foreach (var p in path.Points) { Min = Vector3.Min(Min, p); Max = Vector3.Max(Max, p); }
                LowY = Min.y;
                HighY = Max.y;
                Center = new Vector3((Min.x + Max.x) * 0.5f, 0f, (Min.z + Max.z) * 0.5f);
            }

            public float R(float a, float b) => a + (float)Rng.NextDouble() * (b - a);
            public bool Chance(double p) => Rng.NextDouble() < p;
            public T Pick<T>(T[] items) => items[Rng.Next(items.Length)];
            public float Ground(float x, float z) => Terrain?.Height(x, z) ?? 0f;

            /// <summary>
            /// Pose jusqu'à <paramref name="count"/> objets à une distance de la piste comprise entre
            /// <paramref name="minD"/> et <paramref name="maxD"/>, posés sur le sol (y = hauteur du sol).
            /// </summary>
            public void Scatter(int count, float minD, float maxD, Action<Vector3, float> place)
            {
                int placed = 0;
                for (int attempt = 0; attempt < count * 25 && placed < count; attempt++)
                {
                    var pos = new Vector3(R(Min.x - maxD, Max.x + maxD), 0f, R(Min.z - maxD, Max.z + maxD));
                    float d = Path.FlatDistance(pos, out _);
                    if (d < minD || d > maxD) continue;
                    pos.y = Ground(pos.x, pos.z);
                    place(pos, d);
                    placed++;
                }
            }

            /// <summary>Points sur un anneau autour du circuit (montagnes, planètes au loin).</summary>
            public void Ring(int count, float minR, float maxR, Action<Vector3> place)
            {
                for (int k = 0; k < count; k++)
                {
                    float a = k / (float)count * Mathf.PI * 2f + R(-0.12f, 0.12f), r = R(minR, maxR);
                    place(Center + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
                }
            }

            public GameObject P(PrimitiveType type, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null, bool shadows = true)
            {
                var go = MeshKit.Prim(type, Root, pos, scale, mat, rot);
                if (!shadows) go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                return go;
            }

            public GameObject S(Mesh mesh, Vector3 pos, Vector3 scale, Material mat, Quaternion? rot = null, bool shadows = true)
            {
                var go = MeshKit.Shape(mesh, Root, pos, scale, mat, rot);
                if (!shadows) go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                return go;
            }

            public Quaternion Yaw() => Quaternion.Euler(0f, R(0f, 360f), 0f);
        }

        static void BuildWater(TrackPath path, ThemeStyle style, Transform root)
        {
            var c = Center(path);
            var water = MeshKit.Prim(PrimitiveType.Plane, root, new Vector3(c.x, style.WaterLevel, c.z), new Vector3(160f, 1f, 160f),
                Mats.Lit(style.Water, 0.9f), null, "Water");
            water.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ jungle

        static void Jungle(DecorContext c)
        {
            var bark = Mats.Lit(new Color(0.42f, 0.3f, 0.2f), 0.1f);
            Material[] leaves = { Mats.Lit(new Color(0.16f, 0.5f, 0.2f), 0.2f), Mats.Lit(new Color(0.28f, 0.62f, 0.18f), 0.2f), Mats.Lit(new Color(0.1f, 0.4f, 0.25f), 0.2f) };
            var papaya = Mats.Lit(new Color(1f, 0.55f, 0.15f), 0.4f);
            var stone = Mats.Lit(new Color(0.5f, 0.55f, 0.45f), 0.1f);
            var moss = Mats.Lit(new Color(0.3f, 0.5f, 0.25f), 0.1f);
            Material[] flowers = { Mats.Lit(new Color(1f, 0.3f, 0.5f)), Mats.Lit(new Color(1f, 0.85f, 0.2f)), Mats.Lit(new Color(0.9f, 0.4f, 1f)) };

            c.Scatter(130, 17f, 170f, (p, d) =>
            {
                // Arbre géant : haut tronc, couronne en trois boules, parfois des papayes.
                float h = c.R(9f, 16f), r = c.R(0.7f, 1.2f);
                c.P(PrimitiveType.Cylinder, p + Vector3.up * h * 0.5f, new Vector3(r, h * 0.5f, r), bark);
                var leaf = c.Pick(leaves);
                for (int i = 0; i < 3; i++)
                    c.P(PrimitiveType.Sphere, p + new Vector3(c.R(-2.5f, 2.5f), h + c.R(-0.5f, 2f), c.R(-2.5f, 2.5f)), new Vector3(7f, 3.5f, 7f) * c.R(0.8f, 1.2f), leaf);
                if (c.Chance(0.35))
                    for (int i = 0; i < 3; i++)
                        c.P(PrimitiveType.Sphere, p + new Vector3(c.R(-1f, 1f), h - 1.2f, c.R(-1f, 1f)), new Vector3(0.6f, 0.8f, 0.6f), papaya, null, false);
            });
            c.Scatter(110, 14.5f, 45f, (p, d) =>
            {
                // Fougères et buissons au bord de la piste.
                var leaf = c.Pick(leaves);
                float s = c.R(1.5f, 3f);
                c.S(MeshKit.Cone, p, new Vector3(s * 1.6f, s, s * 1.6f), leaf, c.Yaw(), false);
                if (c.Chance(0.3)) c.P(PrimitiveType.Sphere, p + Vector3.up * s, Vector3.one * 0.5f, c.Pick(flowers), null, false);
            });
            c.Scatter(9, 30f, 110f, (p, d) =>
            {
                // Ruines : socle, colonnes moussues, linteau.
                var rot = c.Yaw();
                c.P(PrimitiveType.Cube, p + Vector3.up * 0.6f, new Vector3(10f, 1.2f, 7f), stone, rot);
                foreach (float x in new[] { -3.5f, 3.5f })
                {
                    float h = c.R(3f, 6f);
                    c.P(PrimitiveType.Cube, p + rot * new Vector3(x, 1.2f + h * 0.5f, 0f), new Vector3(1.4f, h, 1.4f), stone, rot);
                    c.P(PrimitiveType.Cube, p + rot * new Vector3(x, 1.2f + h, 0f), new Vector3(1.6f, 0.4f, 1.6f), moss, rot);
                }
                c.P(PrimitiveType.Cube, p + rot * new Vector3(0f, 7f, 0f), new Vector3(9f, 1f, 1.6f), stone, rot * Quaternion.Euler(0, 0, c.R(-6f, 6f)));
            });
            var far = Mats.Lit(new Color(0.18f, 0.42f, 0.22f), 0.05f);
            c.Ring(18, 360f, 480f, p => c.S(MeshKit.Hemisphere, p, new Vector3(c.R(120, 200), c.R(90, 160), c.R(100, 160)), far, null, false));
        }

        // ------------------------------------------------------------------ lagon

        static void Lagoon(DecorContext c, ThemeStyle style)
        {
            var trunk = Mats.Lit(new Color(0.6f, 0.45f, 0.3f), 0.1f);
            var frond = Mats.Lit(new Color(0.2f, 0.65f, 0.25f), 0.25f);
            var coco = Mats.Lit(new Color(0.4f, 0.25f, 0.12f), 0.2f);
            Material[] parasol = { Mats.Lit(new Color(1f, 0.35f, 0.35f)), Mats.Lit(new Color(1f, 0.85f, 0.2f)), Mats.Lit(new Color(0.3f, 0.6f, 1f)) };
            var hull = Mats.Lit(new Color(0.95f, 0.95f, 0.95f), 0.5f);
            var hullStripe = Mats.Lit(new Color(0.15f, 0.45f, 0.8f), 0.4f);
            var sail = Mats.Lit(new Color(1f, 0.97f, 0.9f), 0.1f);
            var rock = Mats.Lit(new Color(0.55f, 0.5f, 0.45f), 0.1f);
            var sand = Mats.Lit(new Color(0.95f, 0.85f, 0.6f), 0.05f);
            float water = style.WaterLevel;

            c.Scatter(90, 14.5f, 60f, (p, d) =>
            {
                if (p.y < water + 0.3f) return;
                Palm(c, p, c.R(0.8f, 1.3f), trunk, frond, coco);
            });
            c.Scatter(16, 14.5f, 30f, (p, d) =>
            {
                if (p.y < water + 0.3f) return;
                c.P(PrimitiveType.Cylinder, p + Vector3.up * 1.3f, new Vector3(0.1f, 1.3f, 0.1f), trunk, null, false);
                c.S(MeshKit.Cone, p + Vector3.up * 2.3f, new Vector3(3.2f, 0.7f, 3.2f), c.Pick(parasol));
            });
            c.Scatter(14, 45f, 220f, (p, d) =>
            {
                if (p.y > water - 0.8f) return;
                // Voilier au mouillage.
                var rot = c.Yaw();
                var b = new Vector3(p.x, water, p.z);
                c.P(PrimitiveType.Cube, b + Vector3.up * 0.3f, new Vector3(2.4f, 1f, 7f), hull, rot);
                c.P(PrimitiveType.Cube, b + Vector3.up * 0.15f, new Vector3(2.45f, 0.3f, 7.05f), hullStripe, rot, false);
                c.P(PrimitiveType.Cylinder, b + Vector3.up * 4.5f, new Vector3(0.15f, 4f, 0.15f), trunk, rot, false);
                c.P(PrimitiveType.Cube, b + rot * new Vector3(0f, 4.5f, -1.5f), new Vector3(0.05f, 6f, 3f), sail, rot, false);
            });
            c.Scatter(40, 14.5f, 90f, (p, d) =>
            {
                float s = c.R(1f, 3f);
                c.P(PrimitiveType.Sphere, p + Vector3.up * s * 0.2f, new Vector3(s * 1.4f, s * 0.8f, s), rock, c.Yaw(), s > 1.8f);
            });
            // Îles au loin, chacune avec quelques palmiers.
            c.Ring(10, 300f, 460f, p =>
            {
                float w = c.R(40f, 90f);
                var basePos = new Vector3(p.x, water - 2f, p.z);
                c.S(MeshKit.Hemisphere, basePos, new Vector3(w, c.R(14f, 26f), w * 0.8f), sand, null, false);
                for (int i = 0; i < 4; i++) Palm(c, basePos + new Vector3(c.R(-w * 0.2f, w * 0.2f), 7f, c.R(-w * 0.2f, w * 0.2f)), 2.2f, trunk, frond, coco);
            });
        }

        /// <summary>Palmier : tronc courbé en segments, couronne de palmes et noix de coco.</summary>
        static void Palm(DecorContext c, Vector3 p, float scale, Material trunk, Material frond, Material coco)
        {
            var lean = Quaternion.Euler(0f, c.R(0f, 360f), 0f);
            Vector3 top = p;
            for (int i = 0; i < 5; i++)
            {
                var tilt = lean * Quaternion.Euler(i * 4f, 0f, 0f);
                Vector3 step = tilt * Vector3.up * 1.4f * scale;
                c.P(PrimitiveType.Cylinder, top + step * 0.5f, new Vector3(0.45f, 0.75f, 0.45f) * scale, trunk, tilt);
                top += step;
            }
            for (int i = 0; i < 6; i++)
            {
                var rot = Quaternion.Euler(0f, i * 60f + c.R(-10f, 10f), 0f) * Quaternion.Euler(25f, 0f, 0f);
                c.P(PrimitiveType.Cube, top + rot * new Vector3(0f, 0f, 1.8f * scale), new Vector3(1.1f, 0.08f, 3.6f) * scale, frond, rot);
            }
            for (int i = 0; i < 3; i++)
                c.P(PrimitiveType.Sphere, top + new Vector3(c.R(-0.4f, 0.4f), -0.4f, c.R(-0.4f, 0.4f)) * scale, Vector3.one * 0.45f * scale, coco, null, false);
        }

        // ------------------------------------------------------------------ récif

        static void Reef(DecorContext c)
        {
            Material[] coral =
            {
                Mats.Lit(new Color(1f, 0.4f, 0.55f), 0.3f), Mats.Lit(new Color(1f, 0.6f, 0.25f), 0.3f),
                Mats.Lit(new Color(0.65f, 0.35f, 1f), 0.3f), Mats.Lit(new Color(1f, 0.9f, 0.35f), 0.3f),
            };
            Material[] weed = { Mats.Lit(new Color(0.2f, 0.6f, 0.3f), 0.2f), Mats.Lit(new Color(0.35f, 0.7f, 0.25f), 0.2f) };
            var rock = Mats.Lit(new Color(0.35f, 0.4f, 0.45f), 0.1f);
            var bubble = Mats.Transparent(new Color(0.8f, 0.95f, 1f, 0.45f));
            Material[] fish = { Mats.Lit(new Color(1f, 0.55f, 0.1f), 0.5f), Mats.Lit(new Color(0.2f, 0.8f, 1f), 0.5f), Mats.Lit(new Color(1f, 0.95f, 0.3f), 0.5f) };

            c.Scatter(130, 14.5f, 130f, (p, d) =>
            {
                var m = c.Pick(coral);
                switch (c.Rng.Next(3))
                {
                    case 0: // corail branchu
                        for (int i = 0; i < 5; i++)
                        {
                            var rot = Quaternion.Euler(c.R(-35f, 35f), c.R(0f, 360f), c.R(-35f, 35f));
                            float h = c.R(1.5f, 3.5f);
                            c.P(PrimitiveType.Capsule, p + rot * Vector3.up * h * 0.5f, new Vector3(0.45f, h * 0.5f, 0.45f), m, rot);
                        }
                        break;
                    case 1: // corail cerveau
                        float s = c.R(1.5f, 3.5f);
                        c.P(PrimitiveType.Sphere, p + Vector3.up * s * 0.3f, new Vector3(s, s * 0.7f, s), m);
                        break;
                    default: // éventail
                        c.P(PrimitiveType.Cube, p + Vector3.up * 2f, new Vector3(4f, 4f, 0.15f), m, c.Yaw());
                        break;
                }
            });
            c.Scatter(110, 14.5f, 90f, (p, d) =>
            {
                var m = c.Pick(weed);
                for (int i = 0; i < 3; i++)
                {
                    float h = c.R(3f, 7f);
                    var rot = Quaternion.Euler(c.R(-10f, 10f), 0f, c.R(-10f, 10f));
                    c.P(PrimitiveType.Capsule, p + new Vector3(c.R(-0.8f, 0.8f), h * 0.5f, c.R(-0.8f, 0.8f)), new Vector3(0.35f, h * 0.5f, 0.2f), m, rot, false);
                }
            });
            c.Scatter(45, 14.5f, 140f, (p, d) =>
            {
                float s = c.R(2f, 6f);
                c.P(PrimitiveType.Sphere, p + Vector3.up * s * 0.2f, new Vector3(s * 1.3f, s * 0.8f, s), rock, c.Yaw());
            });
            c.Scatter(40, 15f, 80f, (p, d) =>
            {
                // Colonne de bulles qui monte d'une faille.
                for (int i = 0; i < 8; i++)
                    c.P(PrimitiveType.Sphere, p + new Vector3(c.R(-0.6f, 0.6f), 1f + i * c.R(1.2f, 2.2f), c.R(-0.6f, 0.6f)), Vector3.one * c.R(0.3f, 0.7f), bubble, null, false);
            });
            c.Scatter(16, 16f, 70f, (p, d) =>
            {
                // Banc de poissons.
                var m = c.Pick(fish);
                var rot = c.Yaw();
                float y = c.R(3f, 9f);
                for (int i = 0; i < 8; i++)
                {
                    var f = p + Vector3.up * y + rot * new Vector3(c.R(-3f, 3f), c.R(-1.5f, 1.5f), c.R(-3f, 3f));
                    c.P(PrimitiveType.Sphere, f, new Vector3(0.35f, 0.6f, 1f), m, rot, false);
                    c.P(PrimitiveType.Cube, f + rot * new Vector3(0f, 0f, -0.6f), new Vector3(0.05f, 0.5f, 0.4f), m, rot, false);
                }
            });
        }

        // ------------------------------------------------------------------ ciel

        static void SkyIslands(DecorContext c)
        {
            var cloud = Mats.Lit(new Color(1f, 1f, 1f), 0f);
            var cloudShade = Mats.Lit(new Color(0.9f, 0.92f, 1f), 0f);
            var earth = Mats.Lit(new Color(0.55f, 0.4f, 0.3f), 0.05f);
            var grass = Mats.Lit(new Color(0.4f, 0.75f, 0.35f), 0.1f);
            var leaf = Mats.Lit(new Color(0.25f, 0.6f, 0.3f), 0.15f);
            Material[] balloon = { Mats.Lit(new Color(1f, 0.35f, 0.35f), 0.4f), Mats.Lit(new Color(1f, 0.8f, 0.2f), 0.4f), Mats.Lit(new Color(0.45f, 0.5f, 1f), 0.4f), Mats.Lit(new Color(0.5f, 0.9f, 0.5f), 0.4f) };
            var basket = Mats.Lit(new Color(0.6f, 0.4f, 0.2f), 0.1f);

            // Mer de nuages sous la piste.
            float seaY = c.LowY - 22f;
            for (int i = 0; i < 170; i++)
            {
                var p = new Vector3(c.R(c.Min.x - 260f, c.Max.x + 260f), seaY + c.R(-3f, 4f), c.R(c.Min.z - 260f, c.Max.z + 260f));
                float s = c.R(30f, 60f);
                c.P(PrimitiveType.Sphere, p, new Vector3(s, s * 0.35f, s * c.R(0.7f, 1.2f)), c.Chance(0.5) ? cloud : cloudShade, null, false);
            }
            // Nuages flottants au bord de la piste.
            c.Scatter(45, 22f, 150f, (p, d) =>
            {
                p.y = c.R(c.LowY - 8f, c.HighY + 15f);
                for (int i = 0; i < 4; i++)
                {
                    float s = c.R(6f, 12f);
                    c.P(PrimitiveType.Sphere, p + new Vector3(i * 6f - 9f, c.R(-1.5f, 2f), c.R(-3f, 3f)), new Vector3(s, s * 0.6f, s), cloud, null, false);
                }
            });
            // Îles volantes avec un arbre.
            c.Scatter(9, 50f, 200f, (p, d) =>
            {
                p.y = c.R(c.LowY, c.HighY + 10f);
                float w = c.R(12f, 24f);
                c.S(MeshKit.Cone, p, new Vector3(w, w * 1.1f, w), earth, Quaternion.Euler(180f, 0f, 0f));
                c.P(PrimitiveType.Cylinder, p + Vector3.up * 0.3f, new Vector3(w, 0.6f, w), grass);
                c.P(PrimitiveType.Cylinder, p + Vector3.up * 3f, new Vector3(0.8f, 2.5f, 0.8f), earth);
                c.P(PrimitiveType.Sphere, p + Vector3.up * 6.5f, new Vector3(6f, 5f, 6f), leaf);
            });
            // Montgolfières.
            c.Scatter(12, 40f, 260f, (p, d) =>
            {
                p.y = c.R(c.HighY + 5f, c.HighY + 45f);
                float s = c.R(6f, 10f);
                c.P(PrimitiveType.Sphere, p, new Vector3(s, s * 1.2f, s), c.Pick(balloon));
                c.P(PrimitiveType.Cube, p - Vector3.up * s * 1.1f, Vector3.one * s * 0.22f, basket);
            });
        }

        // ------------------------------------------------------------------ espace

        static void Space(DecorContext c, ThemeStyle style)
        {
            Material[] stars = { Mats.Emissive(Color.white, Color.white * 3f), Mats.Emissive(Color.white, new Color(0.6f, 0.75f, 1f) * 3f), Mats.Emissive(Color.white, new Color(1f, 0.9f, 0.6f) * 3f) };
            var rock = Mats.Lit(new Color(0.45f, 0.42f, 0.4f), 0.1f);
            var rockDark = Mats.Lit(new Color(0.3f, 0.28f, 0.3f), 0.1f);
            Material[] planets = { Mats.Lit(new Color(0.9f, 0.5f, 0.3f), 0.2f), Mats.Lit(new Color(0.35f, 0.55f, 1f), 0.3f), Mats.Lit(new Color(0.6f, 0.9f, 0.7f), 0.2f), Mats.Lit(new Color(0.85f, 0.75f, 0.95f), 0.2f) };
            var ring = Mats.Transparent(new Color(0.95f, 0.85f, 0.6f, 0.5f));
            var sun = Mats.Emissive(Color.white, new Color(1f, 0.85f, 0.5f) * 4f);

            float midY = (c.LowY + c.HighY) * 0.5f;
            var centre = c.Center + Vector3.up * midY;
            for (int i = 0; i < 520; i++)
            {
                var dir = new Vector3((float)(c.Rng.NextDouble() * 2 - 1), (float)(c.Rng.NextDouble() * 2 - 1), (float)(c.Rng.NextDouble() * 2 - 1)).normalized;
                c.P(PrimitiveType.Sphere, centre + dir * c.R(650f, 820f), Vector3.one * c.R(1.5f, 4f), c.Pick(stars), null, false);
            }
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.6f + c.R(0f, 0.5f), r = c.R(380f, 560f);
                var p = centre + new Vector3(Mathf.Cos(a) * r, c.R(-120f, 160f), Mathf.Sin(a) * r);
                float s = c.R(60f, 150f);
                c.P(PrimitiveType.Sphere, p, Vector3.one * s, planets[i], null, false);
                if (i % 2 == 0) c.P(PrimitiveType.Cylinder, p, new Vector3(s * 2.1f, 0.3f, s * 2.1f), ring, Quaternion.Euler(c.R(10f, 30f), 0f, c.R(-20f, 20f)), false);
            }
            // Soleil dans la direction d'où vient la lumière.
            c.P(PrimitiveType.Sphere, centre - Quaternion.Euler(style.SunAngles) * Vector3.forward * 780f, Vector3.one * 90f, sun, null, false);
            c.Scatter(70, 22f, 200f, (p, d) =>
            {
                p.y = c.R(c.LowY - 40f, c.HighY + 40f);
                float s = c.R(2f, 9f);
                c.P(PrimitiveType.Sphere, p, new Vector3(s * c.R(0.8f, 1.4f), s * c.R(0.6f, 1f), s), c.Chance(0.5) ? rock : rockDark, Quaternion.Euler(c.R(0, 360), c.R(0, 360), 0));
            });
        }

        // ------------------------------------------------------------------ banquise

        static void Ice(DecorContext c)
        {
            var bark = Mats.Lit(new Color(0.4f, 0.3f, 0.22f), 0.1f);
            var pine = Mats.Lit(new Color(0.15f, 0.42f, 0.32f), 0.15f);
            var snow = Mats.Lit(new Color(0.96f, 0.98f, 1f), 0.4f);
            var ice = Mats.Lit(new Color(0.65f, 0.88f, 1f), 0.9f, 0.1f);
            var carrot = Mats.Lit(new Color(1f, 0.5f, 0.1f), 0.3f);
            var coal = Mats.Lit(new Color(0.1f, 0.1f, 0.1f), 0.2f);

            c.Scatter(120, 16f, 160f, (p, d) =>
            {
                float s = c.R(0.9f, 1.6f);
                c.P(PrimitiveType.Cylinder, p + Vector3.up * 0.6f * s, new Vector3(0.5f, 0.6f, 0.5f) * s, bark);
                for (int i = 0; i < 3; i++)
                {
                    float y = (1f + i * 1.8f) * s, w = (3.6f - i * 0.9f) * s;
                    c.S(MeshKit.Cone, p + Vector3.up * y, new Vector3(w, 2.8f * s, w), pine);
                    c.S(MeshKit.Cone, p + Vector3.up * (y + 1.4f * s), new Vector3(w * 0.55f, 1.4f * s, w * 0.55f), snow, null, false);
                }
            });
            c.Scatter(10, 22f, 90f, (p, d) =>
            {
                var rot = c.Yaw();
                c.S(MeshKit.Hemisphere, p, new Vector3(7f, 5f, 7f), snow, rot);
                c.P(PrimitiveType.Cylinder, p + rot * new Vector3(0f, 1f, 3.4f), new Vector3(2.2f, 1.2f, 2.2f), snow, rot * Quaternion.Euler(90f, 0f, 0f));
            });
            c.Scatter(30, 15f, 110f, (p, d) =>
            {
                for (int i = 0; i < 3; i++)
                {
                    float h = c.R(2f, 6f);
                    c.P(PrimitiveType.Cube, p + new Vector3(c.R(-1.5f, 1.5f), h * 0.4f, c.R(-1.5f, 1.5f)), new Vector3(1f, h, 1f),
                        ice, Quaternion.Euler(c.R(-20f, 20f), c.R(0f, 360f), c.R(-20f, 20f)));
                }
            });
            c.Scatter(8, 15f, 40f, (p, d) =>
            {
                // Bonhomme de neige.
                c.P(PrimitiveType.Sphere, p + Vector3.up * 0.9f, Vector3.one * 1.8f, snow);
                c.P(PrimitiveType.Sphere, p + Vector3.up * 2.3f, Vector3.one * 1.3f, snow);
                c.P(PrimitiveType.Sphere, p + Vector3.up * 3.3f, Vector3.one * 0.9f, snow);
                var face = c.Yaw();
                c.S(MeshKit.Cone, p + Vector3.up * 3.3f + face * Vector3.forward * 0.45f, new Vector3(0.18f, 0.6f, 0.18f), carrot, face * Quaternion.Euler(90f, 0f, 0f), false);
                foreach (float x in new[] { -0.15f, 0.15f })
                    c.P(PrimitiveType.Sphere, p + Vector3.up * 3.45f + face * new Vector3(x, 0f, 0.4f), Vector3.one * 0.1f, coal, null, false);
            });
            c.Ring(16, 380f, 520f, p =>
            {
                float w = c.R(90f, 150f), h = c.R(110f, 200f);
                c.S(MeshKit.Cone, p, new Vector3(w, h, w), snow, null, false);
            });
        }

        // ------------------------------------------------------------------ volcan

        static void Volcano(DecorContext c)
        {
            var rock = Mats.Lit(new Color(0.22f, 0.18f, 0.18f), 0.1f);
            var rockRed = Mats.Lit(new Color(0.4f, 0.2f, 0.15f), 0.1f);
            // Instance à part : sa lueur palpite (DecorMotion, posé après la fusion du décor).
            var lavaGlow = new Color(1f, 0.35f, 0.05f) * 2.5f;
            var lava = Mats.NewEmissive(new Color(1f, 0.45f, 0.1f), lavaGlow);
            _animatedLava = (lava, lavaGlow);
            var smoke = Mats.Lit(new Color(0.3f, 0.27f, 0.27f), 0f);
            var dead = Mats.Lit(new Color(0.18f, 0.14f, 0.12f), 0.05f);

            // Le grand volcan, au milieu de la boucle s'il y a la place, sinon au loin.
            float inside = c.Path.FlatDistance(c.Center, out _);
            Vector3 vp = inside > 70f ? c.Center : c.Center + new Vector3(0f, 0f, 380f);
            float vr = inside > 70f ? inside - 30f : 160f;
            vp.y = c.Ground(vp.x, vp.z) - 2f;
            c.S(MeshKit.Cone, vp, new Vector3(vr * 2f, vr * 1.1f, vr * 2f), rock, null, false);
            c.P(PrimitiveType.Cylinder, vp + Vector3.up * vr * 1.05f, new Vector3(vr * 0.35f, 0.4f, vr * 0.35f), lava, null, false);
            for (int i = 0; i < 9; i++)
            {
                float s = 12f + i * 5f;
                c.P(PrimitiveType.Sphere, vp + new Vector3(c.R(-6f, 6f) + i * 3f, vr * 1.1f + 8f + i * 9f, c.R(-6f, 6f)), Vector3.one * s, smoke, null, false);
            }
            c.Scatter(40, 16f, 120f, (p, d) =>
            {
                float s = c.R(3f, 8f);
                c.P(PrimitiveType.Cylinder, p + Vector3.up * 0.05f, new Vector3(s, 0.05f, s * c.R(0.6f, 1f)), lava, c.Yaw(), false);
            });
            c.Scatter(90, 14.5f, 140f, (p, d) =>
            {
                float s = c.R(1f, 4f);
                c.P(PrimitiveType.Sphere, p + Vector3.up * s * 0.25f, new Vector3(s * 1.3f, s * 0.9f, s), c.Chance(0.6) ? rock : rockRed, c.Yaw(), s > 2f);
            });
            c.Scatter(30, 16f, 90f, (p, d) =>
            {
                float h = c.R(3f, 6f);
                c.P(PrimitiveType.Cylinder, p + Vector3.up * h * 0.5f, new Vector3(0.3f, h * 0.5f, 0.3f), dead);
                for (int i = 0; i < 2; i++)
                {
                    var rot = c.Yaw() * Quaternion.Euler(0f, 0f, c.R(35f, 60f));
                    c.P(PrimitiveType.Cylinder, p + Vector3.up * h * c.R(0.6f, 0.9f) + rot * Vector3.up * 0.8f, new Vector3(0.15f, 0.8f, 0.15f), dead, rot);
                }
            });
            c.Ring(14, 400f, 520f, p =>
            {
                float w = c.R(90f, 150f), h = c.R(80f, 150f);
                c.S(MeshKit.Cone, p, new Vector3(w, h, w), rock, null, false);
                c.P(PrimitiveType.Sphere, p + Vector3.up * h, new Vector3(w * 0.15f, 3f, w * 0.15f), lava, null, false);
            });
        }

        // ------------------------------------------------------------------ ville

        static void City(DecorContext c)
        {
            var facades = new Material[4];
            Vector2[] tiling = { new(2f, 4f), new(3f, 6f), new(2f, 8f), new(4f, 5f) };
            Color[] tints = { new(0.8f, 0.85f, 1f), new(1f, 0.85f, 0.9f), new(0.85f, 1f, 0.9f), new(1f, 1f, 1f) };
            for (int i = 0; i < facades.Length; i++)
            {
                var m = Mats.NewEmissive(tints[i], Color.white * 1.1f);
                m.SetTexture("_BaseMap", ProceduralTextures.Windows);
                m.SetTexture("_EmissionMap", ProceduralTextures.Windows);
                m.SetTextureScale("_BaseMap", tiling[i]);
                facades[i] = m;
            }
            var roof = Mats.Lit(new Color(0.12f, 0.12f, 0.16f), 0.3f);
            var pole = Mats.Lit(new Color(0.25f, 0.25f, 0.3f), 0.5f, 0.4f);
            var lamp = Mats.Emissive(Color.white, new Color(1f, 0.85f, 0.55f) * 3f);
            var red = Mats.Emissive(Color.white, new Color(1f, 0.1f, 0.1f) * 3f);
            Material[] neon = { Mats.Emissive(Color.white, new Color(1f, 0.2f, 0.7f) * 3f), Mats.Emissive(Color.white, new Color(0.2f, 0.9f, 1f) * 3f), Mats.Emissive(Color.white, new Color(0.9f, 1f, 0.2f) * 3f) };

            c.Scatter(170, 22f, 170f, (p, d) =>
            {
                float w = c.R(10f, 22f), dd = c.R(10f, 22f), h = c.R(14f, 30f) + Mathf.Max(0f, d - 40f) * c.R(0.1f, 0.4f);
                var rot = c.Yaw();
                c.P(PrimitiveType.Cube, p + Vector3.up * h * 0.5f, new Vector3(w, h, dd), c.Pick(facades), rot);
                c.P(PrimitiveType.Cube, p + Vector3.up * (h + 0.3f), new Vector3(w * 1.02f, 0.6f, dd * 1.02f), roof, rot, false);
                if (c.Chance(0.5)) c.P(PrimitiveType.Sphere, p + Vector3.up * (h + 2.5f), Vector3.one * 0.8f, red, null, false);
                if (c.Chance(0.3))
                    c.P(PrimitiveType.Cube, p + Vector3.up * h * 0.6f + rot * new Vector3(0f, 0f, dd * 0.51f), new Vector3(w * 0.7f, 1.2f, 0.2f), c.Pick(neon), rot, false);
            });
            // Lampadaires des deux côtés de la piste.
            for (float s = 0f; s < c.Path.Length; s += 28f)
                foreach (int side in new[] { -1, 1 })
                {
                    c.Path.Sample(s, out var p, out var t, out var r);
                    int i = c.Path.IndexAt(s);
                    var b = new Vector3(p.x, c.Path.BaseHeights[i], p.z) + r * side * (c.Path.BarrierOffset + 1.2f);
                    c.P(PrimitiveType.Cylinder, b + Vector3.up * 3.5f, new Vector3(0.2f, 3.5f, 0.2f), pole, null, false);
                    c.P(PrimitiveType.Cube, b + Vector3.up * 7f - r * side * 1f, new Vector3(0.2f, 0.2f, 2f), pole, Quaternion.LookRotation(r), false);
                    c.P(PrimitiveType.Sphere, b + Vector3.up * 6.8f - r * side * 1.9f, new Vector3(0.7f, 0.35f, 0.7f), lamp, null, false);
                }
            c.Ring(30, 330f, 480f, p =>
            {
                float h = c.R(60f, 160f), w = c.R(25f, 45f);
                c.P(PrimitiveType.Cube, p + Vector3.up * h * 0.5f, new Vector3(w, h, w), c.Pick(facades), c.Yaw(), false);
            });
        }
    }
}
