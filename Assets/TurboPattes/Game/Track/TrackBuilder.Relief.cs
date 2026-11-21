using System;
using System.Collections.Generic;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace TurboPattes.Game.Track
{
    /// <summary>
    /// Sol en relief autour d'une piste qui monte et descend : talus sous les tronçons surélevés (sauf sur les ponts,
    /// portés par des piles), dunes au loin. Sert aussi à poser le décor à la bonne hauteur.
    /// </summary>
    public sealed class Terrain
    {
        /// <summary>Pente des talus (m de dénivelé par m horizontal).</summary>
        const float BankSlope = 0.55f;
        /// <summary>Le sol reste un peu sous le bitume pour ne jamais le traverser entre deux points de la grille.</summary>
        const float UnderRoad = 0.12f;

        readonly TrackPath _path;
        readonly float[] _x, _z, _h;
        readonly float _flatZone;
        readonly ThemeStyle _style;

        public Terrain(TrackPath path, ThemeStyle style)
        {
            _path = path;
            _style = style;
            _flatZone = path.BarrierOffset + 1.5f;
            var x = new List<float>();
            var z = new List<float>();
            var h = new List<float>();
            for (int i = 0; i < path.Count; i++)
            {
                if (path.Elevated[i]) continue;
                x.Add(path.Points[i].x);
                z.Add(path.Points[i].z);
                h.Add(path.BaseHeights[i]);
            }
            _x = x.ToArray();
            _z = z.ToArray();
            _h = h.ToArray();
        }

        /// <summary>
        /// Hauteur du sol en (x, z), et distance horizontale à la piste (hors ponts). Près de la piste, le sol suit
        /// l'échantillon le plus proche (sinon, en côte, le talus du bout de piste suivant recouvrirait le bitume) ;
        /// plus loin, chaque échantillon surélevé dessine un talus en cône et le plus haut l'emporte.
        /// </summary>
        public float Height(float x, float z, out float trackDistance)
        {
            float bank = float.MinValue, nearSqr = float.MaxValue, nearH = 0f;
            for (int i = 0; i < _x.Length; i++)
            {
                float dx = _x[i] - x, dz = _z[i] - z;
                float dd = dx * dx + dz * dz;
                if (dd < nearSqr) { nearSqr = dd; nearH = _h[i]; }
                if (_h[i] - UnderRoad <= bank) continue; // ce talus ne peut pas dépasser le meilleur
                float top = _h[i] - UnderRoad - BankSlope * Mathf.Max(0f, Mathf.Sqrt(dd) - _flatZone);
                if (top > bank) bank = top;
            }
            float minD = Mathf.Sqrt(nearSqr);
            trackDistance = minD;
            if (minD <= _flatZone) bank = nearH - UnderRoad;
            // Dunes douces loin de la piste.
            // Loin de la piste : niveau du thème (fond du lagon…) et dunes douces.
            float dune = (Mathf.Sin(x * 0.021f) * Mathf.Cos(z * 0.017f) + 0.6f * Mathf.Sin((x + z) * 0.035f) + 1.6f) * _style.Dunes;
            float baseH = (_style.FarLevel + dune) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(_style.FarFrom, _style.FarTo, minD));
            return Mathf.Max(baseH, bank);
        }

        public float Height(float x, float z) => Height(x, z, out _);

        /// <summary>Distance horizontale aux échantillons de piste au sol plus bas que <paramref name="below"/> (passage sous un pont).</summary>
        public float DistanceToLowRoad(Vector3 p, float below)
        {
            float best = float.MaxValue;
            for (int i = 0; i < _x.Length; i++)
            {
                if (_h[i] > below) continue;
                float dx = _x[i] - p.x, dz = _z[i] - p.z;
                best = Mathf.Min(best, dx * dx + dz * dz);
            }
            return Mathf.Sqrt(best);
        }
    }

    public static partial class TrackBuilder
    {
        const float TerrainCell = 4f;

        static void BuildTerrain(TrackPath path, Terrain terrain, ThemeStyle style, Theme theme, Transform root)
        {
            var min = new Vector3(float.MaxValue, 0, float.MaxValue);
            var max = new Vector3(float.MinValue, 0, float.MinValue);
            foreach (var p in path.Points) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            const float margin = 170f;
            float x0 = min.x - margin, z0 = min.z - margin;
            int nx = Mathf.CeilToInt((max.x - min.x + 2f * margin) / TerrainCell) + 1;
            int nz = Mathf.CeilToInt((max.z - min.z + 2f * margin) / TerrainCell) + 1;
            var v = new Vector3[nx * nz];
            var uv = new Vector2[v.Length];
            for (int j = 0; j < nz; j++)
            for (int i = 0; i < nx; i++)
            {
                float x = x0 + i * TerrainCell, z = z0 + j * TerrainCell;
                float h = terrain.Height(x, z);
                // Le bord de la grille rejoint la plaine (y = 0) pour ne pas laisser de falaise.
                int edge = Mathf.Min(Mathf.Min(i, nx - 1 - i), Mathf.Min(j, nz - 1 - j));
                h = Mathf.Lerp(style.FarLevel, h, Mathf.Clamp01(edge / 8f));
                v[j * nx + i] = new Vector3(x, h, z);
                uv[j * nx + i] = new Vector2(x / 10f, z / 10f);
            }
            var t = new int[(nx - 1) * (nz - 1) * 6];
            int k = 0;
            for (int j = 0; j < nz - 1; j++)
            for (int i = 0; i < nx - 1; i++)
            {
                int a = j * nx + i, b = a + 1, c = a + nx, d = c + 1;
                t[k++] = a; t[k++] = c; t[k++] = b;
                t[k++] = b; t[k++] = c; t[k++] = d;
            }
            var mesh = new Mesh { name = "Terrain", indexFormat = IndexFormat.UInt32 };
            mesh.vertices = v;
            mesh.uv = uv;
            mesh.triangles = t;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var sand = theme == Theme.Canyon
                ? Mats.Textured(ProceduralTextures.Sand, Color.white, Vector2.one, 0.08f)
                : Mats.Textured(ProceduralTextures.Ground, style.GroundTint, Vector2.one, theme == Theme.Ice ? 0.45f : 0.08f);
            MeshKit.MeshObject("Terrain", mesh, sand, root, false).GetComponent<MeshRenderer>().receiveShadows = true;

            // Plaine au-delà de la grille, un peu plus bas pour éviter le scintillement au raccord.
            var c0 = (min + max) * 0.5f;
            const float size = 1600f;
            float plainY = style.FarLevel - 0.15f;
            var plain = new Mesh { name = "Plain" };
            float hs = size * 0.5f;
            plain.vertices = new[]
            {
                new Vector3(c0.x - hs, plainY, c0.z - hs), new Vector3(c0.x - hs, plainY, c0.z + hs),
                new Vector3(c0.x + hs, plainY, c0.z + hs), new Vector3(c0.x + hs, plainY, c0.z - hs),
            };
            plain.uv = new[] { new Vector2(0, 0), new Vector2(0, size / 10f), new Vector2(size / 10f, size / 10f), new Vector2(size / 10f, 0) };
            plain.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            plain.RecalculateNormals();
            plain.RecalculateBounds();
            MeshKit.MeshObject("Plain", plain, sand, root, false);
        }

        /// <summary>
        /// Bande le long des échantillons [<paramref name="i0"/>, <paramref name="i1"/>] : chaque échantillon donne deux
        /// points (décalage latéral, hauteur). Les deux faces sont construites (pas de souci d'orientation).
        /// </summary>
        static Mesh Strip(TrackPath path, int i0, int i1, Func<int, (Vector3 a, Vector3 b)> edge, float tile, string name)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tri = new List<int>();
            int rows = 0;
            for (int i = i0; i <= i1; i++, rows++)
            {
                int k = (i % path.Count + path.Count) % path.Count;
                var (a, b) = edge(k);
                v.Add(a);
                v.Add(b);
                float d = (i - i0) * (path.Length / path.Count);
                uv.Add(new Vector2(0, d / tile));
                uv.Add(new Vector2(1, d / tile));
            }
            int n = v.Count;
            for (int side = 0; side < 2; side++)
            {
                int off = side * n;
                if (side == 1)
                    for (int q = 0; q < n; q++) { v.Add(v[q]); uv.Add(uv[q]); }
                for (int r = 0; r < rows - 1; r++)
                {
                    int a = off + r * 2, b = a + 1, c = a + 2, d = a + 3;
                    if (side == 0) { tri.Add(a); tri.Add(c); tri.Add(b); tri.Add(b); tri.Add(c); tri.Add(d); }
                    else { tri.Add(a); tri.Add(b); tri.Add(c); tri.Add(b); tri.Add(d); tri.Add(c); }
                }
            }
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(v);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(tri, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Plages contiguës d'échantillons vérifiant <paramref name="test"/> (une plage peut boucler sur la ligne).</summary>
        static List<(int from, int to)> Runs(TrackPath path, Func<int, bool> test)
        {
            var runs = new List<(int, int)>();
            int n = path.Count;
            int start = 0;
            while (start < n && test(start)) start++;
            if (start == n) { runs.Add((0, n)); return runs; }
            int begin = -1;
            for (int i = start + 1; i <= start + n; i++)
            {
                bool on = test(i % n);
                if (on && begin < 0) begin = i;
                else if (!on && begin >= 0) { runs.Add((begin, i - 1)); begin = -1; }
            }
            return runs;
        }

        static Vector3 BaseAt(TrackPath path, int k, float lateral, float dy = 0f)
        {
            var p = path.Points[k];
            return new Vector3(p.x, path.BaseHeights[k] + dy + path.LiftAt(k, lateral), p.z) + path.Rights[k] * lateral;
        }

        /// <summary>Ponts : tablier sous les bas-côtés, dessous, poutres de rive colorées et piles jusqu'au sol.</summary>
        static void BuildBridges(TrackPath path, Terrain terrain, Transform root)
        {
            var concrete = Mats.Lit(new Color(0.86f, 0.8f, 0.72f), 0.15f);
            var girder = Mats.Lit(new Color(0.15f, 0.62f, 0.66f), 0.45f, 0.3f);
            var pillarMat = Mats.Lit(new Color(0.78f, 0.7f, 0.6f), 0.1f);
            float inner = path.HalfWidth + path.CurbWidth, outer = path.BarrierOffset + 0.6f;
            const float deck = 1.3f;
            foreach (var (from, to) in Runs(path, k => path.Elevated[k]))
            {
                var bridge = new GameObject("Bridge").transform;
                bridge.SetParent(root, false);
                // Bas-côtés du tablier (entre vibreurs et murets), dessous, poutres de rive.
                foreach (int side in new[] { -1, 1 })
                    MeshKit.MeshObject("DeckSide", Strip(path, from, to, k => (BaseAt(path, k, side * inner, 0.01f), BaseAt(path, k, side * outer, 0.01f)), 6f, "DeckSide"),
                        concrete, bridge);
                MeshKit.MeshObject("DeckUnder", Strip(path, from, to, k => (BaseAt(path, k, -outer, -deck), BaseAt(path, k, outer, -deck)), 6f, "DeckUnder"),
                    concrete, bridge);
                foreach (int side in new[] { -1, 1 })
                    MeshKit.MeshObject("Girder", Strip(path, from, to, k => (BaseAt(path, k, side * outer, -deck), BaseAt(path, k, side * outer, -0.55f)), 6f, "Girder"),
                        girder, bridge);

                // Piles : par paires, jamais sur la route qui passe dessous.
                int step = Mathf.Max(1, Mathf.RoundToInt(13f / (path.Length / path.Count)));
                for (int i = from + step / 2; i < to; i += step)
                {
                    int k = (i % path.Count + path.Count) % path.Count;
                    float top = path.BaseHeights[k] - deck;
                    foreach (float lat in new[] { -5f, 5f })
                    {
                        var p = BaseAt(path, k, lat);
                        if (terrain == null) continue; // piste suspendue : pas de sol pour poser des piles
                        if (terrain.DistanceToLowRoad(p, top - 3f) < path.BarrierOffset + 2.5f) continue;
                        float ground = terrain.Height(p.x, p.z);
                        float h = top - ground + 0.5f;
                        if (h < 0.5f) continue;
                        MeshKit.Prim(PrimitiveType.Cylinder, bridge, new Vector3(p.x, ground - 0.5f + h * 0.5f, p.z), new Vector3(1.6f, h * 0.5f, 1.6f), pillarMat);
                    }
                }
            }
        }

        /// <summary>Tremplins : flancs rayés le long de la rampe, face avant au bord et fanions.</summary>
        static void BuildKickers(TrackPath path, Transform root)
        {
            var stripes = Mats.Textured(ProceduralTextures.Chevrons, Color.white, new Vector2(1f, 1f), 0.3f);
            var pole = Mats.Lit(new Color(0.95f, 0.95f, 0.95f), 0.3f);
            var flag = Mats.Lit(new Color(1f, 0.45f, 0.1f), 0.2f);
            float w = path.HalfWidth + path.CurbWidth;
            foreach (var kicker in path.Profile.Kickers)
            {
                var ramp = new GameObject("Kicker").transform;
                ramp.SetParent(root, false);
                int i1 = path.IndexAt(kicker.LipS - 0.01f), i0 = path.IndexAt(kicker.LipS - kicker.Length);
                if (i1 < i0) i1 += path.Count;
                foreach (int side in new[] { -1, 1 })
                    MeshKit.MeshObject("KickerSide", Strip(path, i0, i1,
                        k => (BaseAt(path, k, side * w, -0.2f), new Vector3(path.Points[k].x, path.Points[k].y + 0.03f + path.LiftAt(k, side * w), path.Points[k].z) + path.Rights[k] * side * w),
                        3f, "KickerSide"), stripes, ramp);

                path.Sample(kicker.LipS, out var lip, out var t, out var r);
                float baseH = path.Profile.BaseHeight(kicker.LipS);
                var rot = Quaternion.LookRotation(t);
                MeshKit.Prim(PrimitiveType.Cube, ramp, new Vector3(lip.x, baseH + kicker.Rise * 0.5f - 0.1f, lip.z) - t * 0.2f,
                    new Vector3(w * 2f, kicker.Rise + 0.2f, 0.4f), stripes, rot, "KickerLip");
                foreach (int side in new[] { -1, 1 })
                {
                    var p = new Vector3(lip.x, baseH + kicker.Rise, lip.z) + r * side * (w + 0.6f) - t * 0.5f;
                    MeshKit.Prim(PrimitiveType.Cylinder, ramp, p + Vector3.up * 1.6f, new Vector3(0.15f, 1.6f, 0.15f), pole);
                    MeshKit.Prim(PrimitiveType.Cube, ramp, p + Vector3.up * 2.7f - t * 0.6f, new Vector3(0.05f, 0.8f, 1.2f), flag, rot);
                }
            }
        }

        /// <summary>Canyon : mesas à strates au loin, cheminées de fée, cactus, rochers, dunes et quelques nuages.</summary>
        static void BuildCanyonDecor(TrackPath path, Terrain terrain, Transform root, System.Random rng)
        {
            var decor = new GameObject("Decor").transform;
            decor.SetParent(root, false);
            var c = Center(path);
            var strata = Mats.Textured(ProceduralTextures.Strata, Color.white, Vector2.one, 0.05f);
            var rock = Mats.Lit(new Color(0.8f, 0.42f, 0.24f), 0.08f);
            var rockDark = Mats.Lit(new Color(0.62f, 0.3f, 0.2f), 0.08f);
            var cactus = Mats.Lit(new Color(0.3f, 0.6f, 0.3f), 0.2f);
            var cactusDark = Mats.Lit(new Color(0.22f, 0.5f, 0.28f), 0.2f);
            var bloom = Mats.Lit(new Color(1f, 0.4f, 0.6f), 0.3f);

            // Mesas au loin : grands plateaux à flancs rayés, parfois un second étage.
            for (int k = 0; k < 16; k++)
            {
                float a = k / 16f * Mathf.PI * 2f + Rand(rng, -0.12f, 0.12f);
                float dist = Rand(rng, 300, 470);
                var pos = c + new Vector3(Mathf.Cos(a) * dist * 1.15f, 0, Mathf.Sin(a) * dist);
                float w = Rand(rng, 50, 110), h = Rand(rng, 30, 70);
                var m = MeshKit.Prim(PrimitiveType.Cylinder, decor, pos + Vector3.up * (h * 0.5f - 1f), new Vector3(w, h * 0.5f, w * Rand(rng, 0.6f, 1f)), strata);
                m.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                if (rng.NextDouble() < 0.5)
                {
                    float h2 = h * Rand(rng, 0.25f, 0.45f);
                    var top = MeshKit.Prim(PrimitiveType.Cylinder, decor, pos + Vector3.up * (h + h2 * 0.5f - 1.5f),
                        new Vector3(w * 0.5f, h2 * 0.5f, w * 0.4f), strata);
                    top.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            int spires = 0, cacti = 0, rocks = 0;
            for (int attempt = 0; attempt < 4000 && (spires < 26 || cacti < 130 || rocks < 70); attempt++)
            {
                var pos = c + new Vector3(Rand(rng, -330, 330), 0, Rand(rng, -230, 230));
                float ground = terrain.Height(pos.x, pos.z, out float d);
                if (d < path.BarrierOffset + 5f || path.FlatDistance(pos, out _) < path.BarrierOffset + 5f) continue;
                pos.y = ground;
                double roll = rng.NextDouble();
                if (roll < 0.12 && spires < 26 && d > 28f)
                {
                    // Cheminée de fée : colonne rayée coiffée d'un rocher plus sombre.
                    spires++;
                    float h = Rand(rng, 8, 18), r = Rand(rng, 2.5f, 4.5f);
                    MeshKit.Prim(PrimitiveType.Cylinder, decor, pos + Vector3.up * (h * 0.5f - 0.5f), new Vector3(r, h * 0.5f, r), strata);
                    MeshKit.Prim(PrimitiveType.Sphere, decor, pos + Vector3.up * (h - 0.2f), new Vector3(r * 1.8f, r * 0.9f, r * 1.6f), rockDark);
                }
                else if (roll < 0.7 && cacti < 130)
                {
                    cacti++;
                    BuildCactus(decor, pos, Rand(rng, 0.7f, 1.4f), rng.NextDouble() < 0.5 ? cactus : cactusDark, rng.NextDouble() < 0.25 ? bloom : null, rng);
                }
                else if (rocks < 70)
                {
                    rocks++;
                    float s = Rand(rng, 1f, 3.2f);
                    var rk = MeshKit.Prim(PrimitiveType.Sphere, decor, pos + Vector3.up * s * 0.2f, new Vector3(s * 1.4f, s * 0.8f, s),
                        rng.NextDouble() < 0.5 ? rock : rockDark, Quaternion.Euler(0, Rand(rng, 0, 360), Rand(rng, -10, 10)));
                    rk.GetComponent<MeshRenderer>().shadowCastingMode = s < 1.8f ? ShadowCastingMode.Off : ShadowCastingMode.On;
                }
            }

            var cloud = Mats.Lit(new Color(1f, 0.97f, 0.93f), 0.0f);
            for (int k = 0; k < 8; k++)
            {
                var cl = new GameObject("Cloud").transform;
                cl.SetParent(decor, false);
                cl.localPosition = c + new Vector3(Rand(rng, -400, 400), Rand(rng, 85, 120), Rand(rng, -400, 400));
                int puffs = rng.Next(3, 5);
                for (int p = 0; p < puffs; p++)
                {
                    float s = Rand(rng, 12, 20);
                    var puff = MeshKit.Prim(PrimitiveType.Sphere, cl, new Vector3(p * 11f - puffs * 5f, Rand(rng, -2, 3), Rand(rng, -4, 4)),
                        new Vector3(s, s * 0.6f, s), cloud);
                    puff.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
                }
            }

            BuildGrandstand(path, decor, rng);
        }

        /// <summary>Cactus à bras : tronc, un ou deux bras coudés, parfois une fleur au sommet.</summary>
        static void BuildCactus(Transform parent, Vector3 pos, float scale, Material mat, Material flower, System.Random rng)
        {
            var t = new GameObject("Cactus").transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localScale = Vector3.one * scale;
            t.localRotation = Quaternion.Euler(0, Rand(rng, 0, 360), 0);
            const float h = 4.2f;
            MeshKit.Prim(PrimitiveType.Capsule, t, new Vector3(0, h * 0.5f, 0), new Vector3(0.9f, h * 0.5f, 0.9f), mat);
            int arms = rng.Next(1, 3);
            for (int a = 0; a < arms; a++)
            {
                float side = a == 0 ? 1f : -1f, y = Rand(rng, 1.4f, 2.4f), up = Rand(rng, 0.9f, 1.5f);
                MeshKit.Prim(PrimitiveType.Capsule, t, new Vector3(side * 0.75f, y, 0), new Vector3(0.55f, 0.5f, 0.55f), mat, Quaternion.Euler(0, 0, 90));
                MeshKit.Prim(PrimitiveType.Capsule, t, new Vector3(side * 1.15f, y + up * 0.5f, 0), new Vector3(0.55f, up * 0.5f + 0.1f, 0.55f), mat);
            }
            if (flower != null) MeshKit.Prim(PrimitiveType.Sphere, t, new Vector3(0, h + 0.05f, 0), new Vector3(0.6f, 0.35f, 0.6f), flower);
        }
    }
}
