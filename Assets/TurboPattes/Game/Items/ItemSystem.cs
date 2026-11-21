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
        const float YarnRadius = 1.6f;
        const float YarnSpeed = 38f;

        sealed class Bubble { public Vector3 Pos; public Transform Go; public float Respawn; public float Phase; }
        sealed class Coconut { public Vector3 Pos; public Vector3 Dir; public float Speed; public float Life; public KartMotor Owner; public float OwnerGrace; public int Hint; public Transform Go; }
        sealed class Honey { public Vector3 Pos; public float Life; public KartMotor Owner; public float OwnerGrace; public Transform Go; }
        /// <summary>Pelote : avance le long de la piste (distance parcourue, comme les karts) vers sa cible.</summary>
        sealed class Yarn { public float Progress; public float Lateral; public float Life; public KartMotor Owner; public KartMotor Target; public Vector3 Pos; public Transform Go; }

        readonly TrackPath _path;
        readonly Transform _root;
        readonly System.Random _rng;
        readonly List<Bubble> _bubbles = new();
        readonly List<Coconut> _coconuts = new();
        readonly List<Honey> _honeys = new();
        readonly List<Yarn> _yarns = new();
        readonly Material _bubbleMat, _pawMat, _coconutMat, _coconutHair, _honeyMat, _potMat, _yarnMat, _yarnThread;

        /// <summary>Événements pour le son : ramassage, objet utilisé, kart touché, bouclier éclaté.</summary>
        public event Action<KartMotor> PickedUp, Used, KartHit, ShieldPopped;
        /// <summary>Objets utilisés par sorte (journal de l'autotest), indexés par <see cref="ItemKind"/>.</summary>
        public readonly int[] UseCounts = new int[8];

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
            _yarnMat = Mats.Lit(new Color(0.95f, 0.42f, 0.62f), 0.1f);
            _yarnThread = Mats.Lit(new Color(1f, 0.75f, 0.85f), 0.1f);

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

        /// <summary>
        /// Projectile le plus proche qui arrive dans le dos de <paramref name="target"/> : pelote qui le vise, ou noix
        /// de coco lancée derrière lui et qui roule vers lui. Distance en mètres.
        /// </summary>
        public bool IncomingThreat(KartMotor target, out ItemKind kind, out float distance)
        {
            kind = ItemKind.None;
            distance = float.MaxValue;
            foreach (var y in _yarns)
            {
                if (y.Target != target) continue;
                float d = target.Progress - y.Progress;
                if (d >= 0f && d < distance) { distance = d; kind = ItemKind.Yarn; }
            }
            foreach (var c in _coconuts)
            {
                if (c.Owner == target && c.OwnerGrace > 0f) continue;
                var to = target.Position - c.Pos;
                to.y = 0f;
                float d = to.magnitude;
                if (d < 0.01f || d >= ThreatAlert.Range || d >= distance) continue;
                var dir = to / d;
                // Elle roule vers lui, et elle est derrière lui (pas une coco qu'il croise de face).
                if (Vector3.Dot(c.Dir, dir) > 0.8f && Vector3.Dot(target.Forward, dir) > 0.3f) { distance = d; kind = ItemKind.Coconut; }
            }
            return kind != ItemKind.None && distance < ThreatAlert.Range;
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
                float cs = _path.DistanceAlong(c.Hint, c.Pos);
                c.Pos.y = _path.GroundHeight(cs, lat);
                float limit = _path.BarrierAt(cs, lat < 0f ? -1 : 1) - 0.6f;
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

            for (int i = _yarns.Count - 1; i >= 0; i--)
            {
                var y = _yarns[i];
                y.Life -= dt;
                y.Progress += YarnSpeed * dt;
                // Se range dans la file de sa cible quand elle approche.
                if (y.Target != null && y.Target.Progress - y.Progress < 60f)
                    y.Lateral = Mathf.MoveTowards(y.Lateral, y.Target.Lateral, 10f * dt);
                y.Pos = _path.PointAt(Mathf.Repeat(y.Progress, _path.Length), y.Lateral);
                y.Go.position = y.Pos + Vector3.up * 0.6f;
                y.Go.rotation = Quaternion.LookRotation(_path.Tangents[_path.Nearest(y.Pos, y.Owner.TrackIndex)]) * Quaternion.Euler(Time.time * 600f, 0f, 0f);
                bool remove = y.Life <= 0f || y.Target != null && y.Target.Finished;
                foreach (var k in karts)
                {
                    if (remove) break;
                    if (k == y.Owner) continue;
                    if (Flat(k.Position, y.Pos) > YarnRadius * YarnRadius) continue;
                    Strike(k, 1.4f);
                    remove = true;
                }
                // Dépassée par sa cible (bond de plume…) : elle s'arrête là.
                if (y.Target != null && y.Progress > y.Target.Progress + 25f) remove = true;
                if (remove)
                {
                    UnityEngine.Object.Destroy(y.Go.gameObject);
                    _yarns.RemoveAt(i);
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
        public bool Use(KartMotor k, KartMotor[] karts = null)
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
                case ItemKind.Feather:
                    if (k.Airborne) return false;
                    k.Leap();
                    break;
                case ItemKind.Yarn:
                    SpawnYarn(k, karts);
                    break;
                case ItemKind.Comet:
                    if (k.Airborne) return false;
                    k.StartComet();
                    break;
            }
            UseCounts[(int)k.Item]++;
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

        void SpawnYarn(KartMotor k, KartMotor[] karts)
        {
            // Cible : le kart juste devant au classement (aucune pour le premier : elle file tout droit).
            KartMotor target = null;
            if (karts != null)
                foreach (var o in karts)
                    if (o != k && !o.Finished && o.Rank == k.Rank - 1) target = o;
            var go = new GameObject("Yarn").transform;
            go.SetParent(_root, false);
            MeshKit.Prim(PrimitiveType.Sphere, go, Vector3.zero, Vector3.one * 1.1f, _yarnMat);
            for (int i = 0; i < 3; i++)
            {
                var band = MeshKit.Prim(PrimitiveType.Cylinder, go, Vector3.zero, new Vector3(1.14f, 0.02f, 1.14f), _yarnThread);
                band.transform.localRotation = Quaternion.Euler(i * 60f, i * 35f, 90f);
            }
            var y = new Yarn
            {
                Progress = k.Progress + 3f,
                Lateral = k.Lateral,
                Life = 7f,
                Owner = k,
                Target = target,
                Go = go,
            };
            y.Pos = _path.PointAt(Mathf.Repeat(y.Progress, _path.Length), y.Lateral);
            go.position = y.Pos;
            _yarns.Add(y);
        }

        /// <summary>Une noix de coco ou une pelote arrive sur ce kart (pour que l'IA lève son bouclier ou saute).</summary>
        public bool ThreatNear(KartMotor k, float range)
        {
            foreach (var y in _yarns)
                if (y.Owner != k && (y.Target == k || y.Target == null) && Flat(y.Pos, k.Position) < range * range) return true;
            foreach (var c in _coconuts)
            {
                if (c.Owner == k && c.OwnerGrace > 0f) continue;
                Vector3 to = k.Position - c.Pos;
                if (Flat(c.Pos, k.Position) < range * range && Vector3.Dot(to, c.Dir) > 0f) return true;
            }
            return false;
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
