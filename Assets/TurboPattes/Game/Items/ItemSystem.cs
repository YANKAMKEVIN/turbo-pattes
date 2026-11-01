using System;
using System.Collections.Generic;
using TurboPattes.Core;
using TurboPattes.Game.Kart;
using TurboPattes.Game.Track;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.Rendering;

namespace TurboPattes.Game.Items
{
    /// <summary>
    /// Bulles surprise à ramasser, objets lancés (noix de coco) et posés (pots de miel), et leurs effets.
    /// </summary>
    public sealed class ItemSystem
    {
        public const float RollDuration = 1.1f;
        const float BubbleRespawn = 3f;
        const float PickupRadius = 2.0f;
        const float CoconutRadius = 1.5f;
        const float HoneyRadius = 1.9f;

        sealed class Bubble { public Vector3 Pos; public Transform Go; public float Respawn; public float Phase; }
        sealed class Coconut { public Vector3 Pos; public Vector3 Dir; public float Speed; public float Life; public KartMotor Owner; public float OwnerGrace; public int Hint; public Transform Go; }
        sealed class Honey { public Vector3 Pos; public float Life; public KartMotor Owner; public float OwnerGrace; public Transform Go; }

        readonly TrackPath _path;
        readonly Transform _root;
        readonly System.Random _rng;
        readonly List<Bubble> _bubbles = new();
        readonly List<Coconut> _coconuts = new();
        readonly List<Honey> _honeys = new();
        readonly Material _bubbleMat, _pawMat, _coconutMat, _coconutHair, _honeyMat, _potMat;

        /// <summary>Événements pour le son : ramassage, objet utilisé, kart touché, bouclier éclaté.</summary>
        public event Action<KartMotor> PickedUp, Used, KartHit, ShieldPopped;

        public ItemSystem(Circuit circuit, TrackPath path, Transform parent, int seed, bool bubbles = true)
        {
            _path = path;
            _rng = new System.Random(seed);
            _root = new GameObject("Items").transform;
            _root.SetParent(parent, false);
            _bubbleMat = Mats.Transparent(new Color(0.7f, 0.9f, 1f, 0.38f));
            _pawMat = Mats.Emissive(new Color(1f, 0.8f, 0.25f), new Color(1f, 0.6f, 0.1f) * 0.8f);
            _coconutMat = Mats.Lit(new Color(0.42f, 0.26f, 0.14f), 0.15f);
            _coconutHair = Mats.Lit(new Color(0.6f, 0.42f, 0.25f), 0.05f);
            _honeyMat = Mats.Lit(new Color(1f, 0.68f, 0.08f), 0.9f);
            _potMat = Mats.Lit(new Color(0.85f, 0.5f, 0.25f), 0.3f);

            if (bubbles)
            foreach (float row in circuit.ItemRows)
            {
                float s = row * path.Length;
                for (int i = 0; i < 4; i++)
                {
                    float lat = Mathf.Lerp(-path.HalfWidth + 1.8f, path.HalfWidth - 1.8f, i / 3f);
                    var b = new Bubble { Pos = path.PointAt(s, lat) + Vector3.up * 1.2f, Phase = i * 0.7f + row * 10f };
                    b.Go = BuildBubble(b.Pos);
                    _bubbles.Add(b);
                }
            }
        }

        Transform BuildBubble(Vector3 pos)
        {
            var t = new GameObject("Bubble").transform;
            t.SetParent(_root, false);
            t.position = pos;
            var shell = MeshKit.Prim(PrimitiveType.Sphere, t, Vector3.zero, Vector3.one * 1.7f, _bubbleMat);
            shell.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            TrackBuilder.PawSign(t, Vector3.zero, 0.85f, _pawMat);
            return t;
        }

        public void Tick(float dt, KartMotor[] karts, Func<KartMotor, int> rankOf)
        {
            float time = Time.time;
            foreach (var b in _bubbles)
            {
                if (b.Respawn > 0f)
                {
                    b.Respawn -= dt;
                    if (b.Respawn <= 0f) b.Go.gameObject.SetActive(true);
                    continue;
                }
                b.Go.position = b.Pos + Vector3.up * Mathf.Sin(time * 2f + b.Phase) * 0.25f;
                b.Go.rotation = Quaternion.Euler(0f, time * 90f + b.Phase * 30f, 0f);
                foreach (var k in karts)
                {
                    if (Flat(k.Position, b.Pos) > PickupRadius * PickupRadius) continue;
                    b.Respawn = BubbleRespawn;
                    b.Go.gameObject.SetActive(false);
                    if (k.Item == ItemKind.None)
                    {
                        k.Item = ItemRoll.Pick(rankOf(k), karts.Length, _rng.NextDouble());
                        k.ItemRoll = RollDuration;
                    }
                    PickedUp?.Invoke(k);
                    break;
                }
            }

            for (int i = _coconuts.Count - 1; i >= 0; i--)
            {
                var c = _coconuts[i];
                c.Life -= dt;
                c.OwnerGrace -= dt;
                c.Pos += c.Dir * c.Speed * dt;
                c.Hint = _path.Nearest(c.Pos, c.Hint);
                float lat = _path.Lateral(c.Hint, c.Pos);
                // La noix de coco roule au ras de la piste : côtes, descentes et tremplins.
                c.Pos.y = _path.GroundHeight(_path.DistanceAlong(c.Hint, c.Pos), lat);
                float limit = _path.BarrierOffset - 0.6f;
                if (Mathf.Abs(lat) > limit)
                {
                    // Rebond sur la barrière.
                    Vector3 n = _path.Rights[c.Hint] * Mathf.Sign(lat);
                    c.Pos -= n * (Mathf.Abs(lat) - limit);
                    c.Dir = Vector3.Reflect(c.Dir, n).normalized;
                }
                c.Go.position = c.Pos + Vector3.up * 0.55f;
                c.Go.Rotate(Vector3.Cross(Vector3.up, c.Dir), c.Speed * dt / 0.55f * Mathf.Rad2Deg, Space.World);

                bool remove = c.Life <= 0f;
                foreach (var k in karts)
                {
                    if (remove) break;
                    if (k == c.Owner && c.OwnerGrace > 0f) continue;
                    if (Flat(k.Position, c.Pos) > CoconutRadius * CoconutRadius) continue;
                    Strike(k, 1.4f);
                    remove = true;
                }
                for (int h = _honeys.Count - 1; h >= 0 && !remove; h--)
                {
                    if (Flat(_honeys[h].Pos, c.Pos) > HoneyRadius * HoneyRadius) continue;
                    RemoveHoney(h);
                    remove = true;
                }
                if (remove)
                {
                    UnityEngine.Object.Destroy(c.Go.gameObject);
                    _coconuts.RemoveAt(i);
                }
            }

            for (int i = _honeys.Count - 1; i >= 0; i--)
            {
                var h = _honeys[i];
                h.Life -= dt;
                h.OwnerGrace -= dt;
                bool remove = h.Life <= 0f;
                foreach (var k in karts)
                {
                    if (remove) break;
                    if (k == h.Owner && h.OwnerGrace > 0f) continue;
                    if (Flat(k.Position, h.Pos) > HoneyRadius * HoneyRadius) continue;
                    Strike(k, 0.9f);
                    remove = true;
                }
                if (remove) RemoveHoney(i);
            }
        }

        void Strike(KartMotor k, float spin)
        {
            bool shielded = k.ShieldTime > 0f;
            if (k.Hit(spin)) KartHit?.Invoke(k);
            else if (shielded) ShieldPopped?.Invoke(k);
        }

        void RemoveHoney(int i)
        {
            UnityEngine.Object.Destroy(_honeys[i].Go.gameObject);
            _honeys.RemoveAt(i);
        }

        /// <summary>Distance horizontale au carré, ou l'infini si les deux sont à des niveaux différents (pont).</summary>
        static float Flat(Vector3 a, Vector3 b)
        {
            if (Mathf.Abs(a.y - b.y) > 2.5f) return float.MaxValue;
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }

        /// <summary>Utilise l'objet du kart s'il en a un (et que la roulette est finie).</summary>
        public bool Use(KartMotor k)
        {
            if (k.Item == ItemKind.None || k.ItemRoll > 0f || k.SpinTime > 0f) return false;
            switch (k.Item)
            {
                case ItemKind.Carrot:
                    k.GiveBoost(1.6f);
                    break;
                case ItemKind.Shield:
                    k.ShieldTime = 8f;
                    break;
                case ItemKind.Coconut:
                    SpawnCoconut(k);
                    break;
                case ItemKind.Honey:
                    SpawnHoney(k);
                    break;
            }
            k.Item = ItemKind.None;
            Used?.Invoke(k);
            return true;
        }

        void SpawnCoconut(KartMotor k)
        {
            var go = new GameObject("Coconut").transform;
            go.SetParent(_root, false);
            MeshKit.Prim(PrimitiveType.Sphere, go, Vector3.zero, Vector3.one * 1.1f, _coconutMat);
            MeshKit.Prim(PrimitiveType.Sphere, go, new Vector3(0f, 0.2f, 0f), new Vector3(1.15f, 0.6f, 1.15f), _coconutHair);
            var c = new Coconut
            {
                Pos = k.Position + k.Forward * 2.4f,
                Dir = k.Forward,
                Speed = Mathf.Max(k.ForwardSpeed + 16f, 34f),
                Life = 5f,
                Owner = k,
                OwnerGrace = 0.6f,
                Hint = k.TrackIndex,
                Go = go,
            };
            go.position = c.Pos;
            _coconuts.Add(c);
        }

        void SpawnHoney(KartMotor k)
        {
            var go = new GameObject("Honey").transform;
            go.SetParent(_root, false);
            var puddle = MeshKit.Prim(PrimitiveType.Cylinder, go, new Vector3(0f, 0.05f, 0f), new Vector3(3.2f, 0.03f, 2.6f), _honeyMat);
            puddle.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            MeshKit.Prim(PrimitiveType.Sphere, go, new Vector3(0.4f, 0.35f, 0.2f), new Vector3(0.7f, 0.65f, 0.7f), _potMat);
            MeshKit.Prim(PrimitiveType.Cylinder, go, new Vector3(0.4f, 0.65f, 0.2f), new Vector3(0.45f, 0.06f, 0.45f), _honeyMat);
            var pos = k.Position - k.Forward * 2.8f;
            int i = _path.Nearest(pos, k.TrackIndex);
            float lat = _path.Lateral(i, pos);
            pos.y = _path.GroundHeight(_path.DistanceAlong(i, pos), lat);
            go.position = pos;
            go.rotation = Quaternion.Euler(0f, (float)_rng.NextDouble() * 360f, 0f);
            _honeys.Add(new Honey { Pos = pos, Life = 30f, Owner = k, OwnerGrace = 0.8f, Go = go });
        }

        /// <summary>Pot de miel devant le kart à moins de <paramref name="range"/> m (pour l'IA).</summary>
        public bool HoneyAhead(KartMotor k, float range, out Vector3 pos)
        {
            foreach (var h in _honeys)
            {
                Vector3 to = h.Pos - k.Position;
                if (to.magnitude < range && Vector3.Dot(to, k.Forward) > 0f) { pos = h.Pos; return true; }
            }
            pos = default;
            return false;
        }
    }
}
