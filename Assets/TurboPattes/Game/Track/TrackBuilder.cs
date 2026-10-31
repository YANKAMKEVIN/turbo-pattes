using System.Collections.Generic;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace TurboPattes.Game.Track
{
    /// <summary>
    /// Construit la scène du circuit : sol, bitume, vibreurs, barrières, ligne d'arrivée, tapis turbo et décor.
    /// Le relief (talus, ponts, tremplins) et le décor du canyon sont dans <c>TrackBuilder.Relief.cs</c>.
    /// </summary>
    public static partial class TrackBuilder
    {
        public static List<BoostPad> Build(Circuit circuit, TrackPath path, Transform root, int seed)
        {
            var rng = new System.Random(seed);
            var terrain = path.HasRelief ? new Terrain(path) : null;
            if (terrain != null) BuildTerrain(path, terrain, root);
            else BuildGround(path, root);
            BuildRoad(path, root);
            BuildCurbs(path, root);
            BuildBarriers(path, root, circuit.Theme);
            if (terrain != null)
            {
                BuildBridges(path, terrain, root);
                BuildKickers(path, root);
            }
            BuildStartLine(path, root);
            var pads = BuildBoostPads(circuit, path, root);
            if (circuit.Theme == Theme.Canyon) BuildCanyonDecor(path, terrain, root, rng);
            else BuildDecor(path, root, rng);
            return pads;
        }

        static Vector3 Center(TrackPath path)
        {
            var min = new Vector3(float.MaxValue, 0, float.MaxValue);
            var max = new Vector3(float.MinValue, 0, float.MinValue);
            foreach (var p in path.Points) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            var c = (min + max) * 0.5f;
            c.y = 0f;
            return c;
        }

        static void BuildGround(TrackPath path, Transform root)
        {
            const float size = 1400f;
            var c = Center(path);
            var mesh = new Mesh { name = "Ground" };
            float h = size * 0.5f;
            mesh.vertices = new[]
            {
                new Vector3(c.x - h, 0, c.z - h), new Vector3(c.x - h, 0, c.z + h),
                new Vector3(c.x + h, 0, c.z + h), new Vector3(c.x + h, 0, c.z - h),
            };
            float tiles = size / 10f;
            mesh.uv = new[] { new Vector2(0, 0), new Vector2(0, tiles), new Vector2(tiles, tiles), new Vector2(tiles, 0) };
            mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = MeshKit.MeshObject("Ground", mesh, Mats.Textured(ProceduralTextures.Grass, Color.white, Vector2.one, 0.1f), root, false);
            go.GetComponent<MeshRenderer>().receiveShadows = true;
        }

        /// <summary>
        /// Ruban le long du circuit entre deux décalages latéraux. u va de 0 (côté <paramref name="from"/>) à 1,
        /// v avance d'une unité tous les <paramref name="tile"/> mètres.
        /// </summary>
        static Mesh Ribbon(TrackPath path, float from, float to, float y, float tile, string name)
        {
            int n = path.Count;
            var v = new Vector3[(n + 1) * 2];
            var uv = new Vector2[v.Length];
            var t = new int[n * 6];
            for (int i = 0; i <= n; i++)
            {
                int k = i % n;
                Vector3 p = path.Points[k], r = path.Rights[k];
                float d = i == n ? path.Length : path.Dist[k];
                v[i * 2] = p + r * from + Vector3.up * y;
                v[i * 2 + 1] = p + r * to + Vector3.up * y;
                uv[i * 2] = new Vector2(0, d / tile);
                uv[i * 2 + 1] = new Vector2(1, d / tile);
            }
            bool flip = to < from;
            for (int i = 0; i < n; i++)
            {
                int a = i * 2, b = a + 1, c = a + 2, d = a + 3;
                // Le sens des triangles dépend du côté pour garder la face visible vers le haut.
                if (!flip) { t[i * 6] = a; t[i * 6 + 1] = c; t[i * 6 + 2] = b; t[i * 6 + 3] = b; t[i * 6 + 4] = c; t[i * 6 + 5] = d; }
                else { t[i * 6] = a; t[i * 6 + 1] = b; t[i * 6 + 2] = c; t[i * 6 + 3] = b; t[i * 6 + 4] = d; t[i * 6 + 5] = c; }
            }
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.vertices = v;
            mesh.uv = uv;
            mesh.triangles = t;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void BuildRoad(TrackPath path, Transform root)
        {
            var mesh = Ribbon(path, -path.HalfWidth, path.HalfWidth, 0.02f, 16f, "Road");
            var go = MeshKit.MeshObject("Road", mesh, Mats.Textured(ProceduralTextures.Asphalt, Color.white, Vector2.one, 0.15f), root, false);
            go.GetComponent<MeshRenderer>().receiveShadows = true;
        }

        static void BuildCurbs(TrackPath path, Transform root)
        {
            var mat = Mats.Textured(ProceduralTextures.Curb, Color.white, Vector2.one, 0.3f);
            float w = path.CurbWidth;
            MeshKit.MeshObject("CurbRight", Ribbon(path, path.HalfWidth, path.HalfWidth + w, 0.035f, 4f, "CurbR"), mat, root, false);
            MeshKit.MeshObject("CurbLeft", Ribbon(path, -path.HalfWidth, -path.HalfWidth - w, 0.035f, 4f, "CurbL"), mat, root, false);
        }

        /// <summary>Murets des deux côtés : face intérieure + dessus, texturés en bandes.</summary>
        static void BuildBarriers(TrackPath path, Transform root, Theme theme)
        {
            var tex = theme == Theme.Canyon ? ProceduralTextures.CanyonBarrier : ProceduralTextures.Barrier;
            var mat = Mats.Textured(tex, Color.white, Vector2.one, 0.35f);
            foreach (int side in new[] { 1, -1 })
            {
                int n = path.Count;
                const float height = 1.1f, thick = 0.6f;
                var v = new List<Vector3>();
                var uv = new List<Vector2>();
                var tri = new List<int>();
                // Trois bandes : face intérieure, dessus, face extérieure. Le pied descend sous le sol (talus, pont).
                float[] offs = { path.BarrierOffset, path.BarrierOffset, path.BarrierOffset + thick, path.BarrierOffset + thick };
                float[] ys = { -0.6f, height, height, -0.6f };
                float[] vs = { 0f, 0.8f, 1f, 0.8f };
                for (int i = 0; i <= n; i++)
                {
                    int k = i % n;
                    float d = i == n ? path.Length : path.Dist[k];
                    for (int j = 0; j < 4; j++)
                    {
                        var basePoint = new Vector3(path.Points[k].x, path.BaseHeights[k], path.Points[k].z);
                        v.Add(basePoint + path.Rights[k] * (offs[j] * side) + Vector3.up * ys[j]);
                        uv.Add(new Vector2(d / 6f, vs[j]));
                    }
                }
                for (int i = 0; i < n; i++)
                for (int j = 0; j < 3; j++)
                {
                    int a = i * 4 + j, b = a + 1, c = a + 4, d = c + 1;
                    if (side < 0) { tri.Add(a); tri.Add(b); tri.Add(c); tri.Add(b); tri.Add(d); tri.Add(c); }
                    else { tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d); }
                }
                var mesh = new Mesh { name = "Barrier", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(v);
                mesh.SetUVs(0, uv);
                mesh.SetTriangles(tri, 0);
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                MeshKit.MeshObject(side > 0 ? "BarrierRight" : "BarrierLeft", mesh, mat, root);
            }
        }

        static void BuildStartLine(TrackPath path, Transform root)
        {
            path.Sample(0f, out var p, out var t, out var r);
            var rot = Quaternion.LookRotation(t, Vector3.up);
            var line = MeshKit.Prim(PrimitiveType.Quad, root, p + Vector3.up * 0.04f, new Vector3(path.HalfWidth * 2f, 2.5f, 1f),
                Mats.Textured(ProceduralTextures.Checker, Color.white, new Vector2(7f, 1.25f), 0.2f), rot * Quaternion.Euler(90, 0, 0), "StartLine");
            line.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Portique : deux piliers et une banderole damier, avec des pattes dorées.
            var arch = new GameObject("StartArch").transform;
            arch.SetParent(root, false);
            arch.SetPositionAndRotation(p, rot);
            float x = path.HalfWidth + 2.2f;
            var pillar = Mats.Lit(new Color(0.95f, 0.45f, 0.15f), 0.4f);
            MeshKit.Prim(PrimitiveType.Cylinder, arch, new Vector3(-x, 3.5f, 0), new Vector3(1f, 3.5f, 1f), pillar);
            MeshKit.Prim(PrimitiveType.Cylinder, arch, new Vector3(x, 3.5f, 0), new Vector3(1f, 3.5f, 1f), pillar);
            MeshKit.Prim(PrimitiveType.Cube, arch, new Vector3(0, 7.3f, 0), new Vector3(x * 2f + 1.5f, 1.8f, 0.5f),
                Mats.Textured(ProceduralTextures.Checker, Color.white, new Vector2(10f, 1f), 0.3f));
            var gold = Mats.Lit(new Color(1f, 0.8f, 0.25f), 0.7f, 0.6f);
            foreach (float px in new[] { -x * 0.5f, x * 0.5f })
                PawSign(arch, new Vector3(px, 7.3f, -0.4f), 1.1f, gold);
        }

        /// <summary>Emblème de patte (coussinet + 4 doigts) dans le plan local XY.</summary>
        public static Transform PawSign(Transform parent, Vector3 localPos, float size, Material mat)
        {
            var paw = new GameObject("Paw").transform;
            paw.SetParent(parent, false);
            paw.localPosition = localPos;
            paw.localScale = Vector3.one * size;
            MeshKit.Prim(PrimitiveType.Sphere, paw, new Vector3(0, -0.15f, 0), new Vector3(0.7f, 0.55f, 0.25f), mat);
            float[] xs = { -0.42f, -0.15f, 0.15f, 0.42f };
            float[] ys = { 0.2f, 0.42f, 0.42f, 0.2f };
            for (int i = 0; i < 4; i++)
                MeshKit.Prim(PrimitiveType.Sphere, paw, new Vector3(xs[i], ys[i], 0), new Vector3(0.28f, 0.32f, 0.22f), mat);
            return paw;
        }

        static List<BoostPad> BuildBoostPads(Circuit circuit, TrackPath path, Transform root)
        {
            var pads = new List<BoostPad>();
            var mat = Mats.NewEmissive(Color.white, new Color(1f, 0.55f, 0.1f) * 1.2f);
            mat.SetTexture("_BaseMap", ProceduralTextures.Chevrons);
            mat.SetTexture("_EmissionMap", ProceduralTextures.Chevrons);
            mat.SetTextureScale("_BaseMap", new Vector2(1f, 2f));
            foreach (var (fraction, lateral) in circuit.Boosts)
            {
                var pad = new BoostPad(fraction * path.Length, lateral);
                pads.Add(pad);
                path.Sample(pad.S + pad.Length * 0.5f, out var p, out var t, out var r);
                var go = MeshKit.Prim(PrimitiveType.Quad, root, p + r * lateral + Vector3.up * 0.05f,
                    new Vector3(pad.HalfWidth * 2f, pad.Length, 1f), mat, Quaternion.LookRotation(t) * Quaternion.Euler(90, 0, 0), "BoostPad");
                go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            return pads;
        }

        static float Rand(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        static void BuildDecor(TrackPath path, Transform root, System.Random rng)
        {
            var decor = new GameObject("Decor").transform;
            decor.SetParent(root, false);
            var c = Center(path);

            var trunk = Mats.Lit(new Color(0.48f, 0.3f, 0.18f), 0.1f);
            Material[] leaves =
            {
                Mats.Lit(new Color(0.22f, 0.62f, 0.25f), 0.15f),
                Mats.Lit(new Color(0.35f, 0.72f, 0.2f), 0.15f),
                Mats.Lit(new Color(0.15f, 0.5f, 0.3f), 0.15f),
            };
            Material[] flowers =
            {
                Mats.Lit(new Color(1f, 0.4f, 0.55f)), Mats.Lit(new Color(1f, 0.85f, 0.2f)), Mats.Lit(new Color(0.7f, 0.5f, 1f)),
            };

            int placed = 0;
            for (int attempt = 0; attempt < 2500 && placed < 170; attempt++)
            {
                var pos = c + new Vector3(Rand(rng, -260, 260), 0, Rand(rng, -220, 220));
                int i = path.Nearest(pos, -1);
                float d = Vector3.Distance(new Vector3(pos.x, 0, pos.z), path.Points[i]);
                if (d < path.BarrierOffset + 5f) continue;
                placed++;
                var tree = new GameObject("Tree").transform;
                tree.SetParent(decor, false);
                tree.localPosition = pos;
                float s = Rand(rng, 0.8f, 1.5f);
                tree.localScale = Vector3.one * s;
                if (rng.NextDouble() < 0.5)
                {
                    // Arbre rond
                    MeshKit.Prim(PrimitiveType.Cylinder, tree, new Vector3(0, 1.2f, 0), new Vector3(0.6f, 1.2f, 0.6f), trunk);
                    var leaf = leaves[rng.Next(leaves.Length)];
                    MeshKit.Prim(PrimitiveType.Sphere, tree, new Vector3(0, 3.6f, 0), new Vector3(3.6f, 3.2f, 3.6f), leaf);
                    MeshKit.Prim(PrimitiveType.Sphere, tree, new Vector3(0.8f, 4.6f, 0.3f), new Vector3(2.2f, 2f, 2.2f), leaf);
                }
                else
                {
                    // Sapin
                    MeshKit.Prim(PrimitiveType.Cylinder, tree, new Vector3(0, 0.6f, 0), new Vector3(0.5f, 0.6f, 0.5f), trunk);
                    var leaf = leaves[2];
                    MeshKit.Shape(MeshKit.Cone, tree, new Vector3(0, 1f, 0), new Vector3(3.6f, 3.2f, 3.6f), leaf);
                    MeshKit.Shape(MeshKit.Cone, tree, new Vector3(0, 2.8f, 0), new Vector3(2.8f, 2.8f, 2.8f), leaf);
                    MeshKit.Shape(MeshKit.Cone, tree, new Vector3(0, 4.4f, 0), new Vector3(1.9f, 2.4f, 1.9f), leaf);
                }
            }

            // Massifs de fleurs près des barrières (côté extérieur).
            for (int k = 0; k < 90; k++)
            {
                float s = Rand(rng, 0, path.Length);
                int side = rng.NextDouble() < 0.5 ? 1 : -1;
                var pos = path.PointAt(s, side * (path.BarrierOffset + Rand(rng, 1.6f, 4f)));
                int i = path.Nearest(pos, -1);
                if (Vector3.Distance(pos, path.Points[i]) < path.BarrierOffset + 1.2f) continue;
                var f = MeshKit.Prim(PrimitiveType.Sphere, decor, pos + Vector3.up * 0.25f, new Vector3(0.7f, 0.5f, 0.7f), flowers[rng.Next(flowers.Length)]);
                f.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            // Collines au loin.
            var hill = Mats.Lit(new Color(0.36f, 0.68f, 0.32f), 0.05f);
            var hill2 = Mats.Lit(new Color(0.28f, 0.58f, 0.36f), 0.05f);
            for (int k = 0; k < 18; k++)
            {
                float a = k / 18f * Mathf.PI * 2f + Rand(rng, -0.1f, 0.1f);
                float dist = Rand(rng, 360, 480);
                var pos = c + new Vector3(Mathf.Cos(a) * dist, 0, Mathf.Sin(a) * dist);
                float w = Rand(rng, 120, 220), hh = Rand(rng, 40, 90);
                var h = MeshKit.Shape(MeshKit.Hemisphere, decor, pos, new Vector3(w, hh * 2f, w * 0.8f), k % 2 == 0 ? hill : hill2);
                h.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            }

            // Nuages.
            var cloud = Mats.Lit(new Color(1f, 1f, 1f), 0.0f);
            for (int k = 0; k < 14; k++)
            {
                var cl = new GameObject("Cloud").transform;
                cl.SetParent(decor, false);
                cl.localPosition = c + new Vector3(Rand(rng, -380, 380), Rand(rng, 70, 110), Rand(rng, -380, 380));
                int puffs = rng.Next(3, 6);
                for (int p = 0; p < puffs; p++)
                {
                    float s = Rand(rng, 12, 22);
                    var puff = MeshKit.Prim(PrimitiveType.Sphere, cl, new Vector3(p * 11f - puffs * 5f, Rand(rng, -2, 3), Rand(rng, -4, 4)),
                        new Vector3(s, s * 0.7f, s), cloud);
                    puff.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            BuildGrandstand(path, decor, rng);
        }

        /// <summary>Tribune près de la ligne d'arrivée, côté gauche, avec un public de petits cubes colorés.</summary>
        static void BuildGrandstand(TrackPath path, Transform parent, System.Random rng)
        {
            path.Sample(path.Length - 25f, out var p, out var t, out var r);
            var stand = new GameObject("Grandstand").transform;
            stand.SetParent(parent, false);
            stand.SetPositionAndRotation(p - r * (path.BarrierOffset + 3f), Quaternion.LookRotation(t));
            var concrete = Mats.Lit(new Color(0.85f, 0.85f, 0.9f), 0.2f);
            var roof = Mats.Lit(new Color(0.95f, 0.3f, 0.3f), 0.4f);
            Material[] crowd =
            {
                Mats.Lit(new Color(1f, 0.5f, 0.2f)), Mats.Lit(new Color(0.3f, 0.6f, 1f)), Mats.Lit(new Color(1f, 0.85f, 0.3f)),
                Mats.Lit(new Color(0.5f, 0.85f, 0.4f)), Mats.Lit(new Color(0.9f, 0.4f, 0.8f)), Mats.Lit(new Color(1f, 1f, 1f)),
            };
            const float len = 40f;
            for (int step = 0; step < 5; step++)
            {
                float x = -step * 1.6f - 1f;
                float height = step * 1.6f + 0.8f;
                MeshKit.Prim(PrimitiveType.Cube, stand, new Vector3(x, height * 0.5f, 0), new Vector3(1.6f, height, len), concrete);
                for (float z = -len * 0.5f + 1f; z < len * 0.5f; z += 1.3f)
                {
                    if (rng.NextDouble() < 0.2) continue;
                    var fan = MeshKit.Prim(PrimitiveType.Cube, stand, new Vector3(x, height + 0.4f, z + Rand(rng, -0.2f, 0.2f)),
                        new Vector3(0.55f, 0.8f, 0.55f), crowd[rng.Next(crowd.Length)]);
                    fan.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }
            MeshKit.Prim(PrimitiveType.Cube, stand, new Vector3(-4.5f, 10f, 0), new Vector3(10f, 0.4f, len + 2f), roof,
                Quaternion.Euler(0, 0, -8f));
            foreach (float z in new[] { -len * 0.5f, 0f, len * 0.5f })
                MeshKit.Prim(PrimitiveType.Cylinder, stand, new Vector3(-8.5f, 5f, z), new Vector3(0.4f, 5f, 0.4f), concrete);
        }
    }
}
