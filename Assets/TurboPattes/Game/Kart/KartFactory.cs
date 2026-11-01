using TurboPattes.Game.Visuals;
using UnityEngine;

namespace TurboPattes.Game.Kart
{
    /// <summary>Pièces animées d'un kart construit par <see cref="KartFactory"/>.</summary>
    public sealed class KartRig
    {
        public Transform Root;
        /// <summary>Caisse + pilote : penche, saute, dérape et tourne sur lui-même.</summary>
        public Transform Body;
        public Transform[] Wheels;
        public Transform[] FrontPivots;
        public Transform Driver;
        public Transform[] Exhausts;
        public GameObject Shield;
    }

    /// <summary>Construit un kart d'atelier (châssis en tubes soudés, moteur latéral) et son pilote. Avant = +z.</summary>
    public static class KartFactory
    {
        public static KartRig Build(AnimalSpec spec, Transform parent)
        {
            var rig = new KartRig();
            var root = new GameObject("Kart_" + spec.Name).transform;
            root.SetParent(parent, false);
            rig.Root = root;
            var body = new GameObject("Body").transform;
            body.SetParent(root, false);
            rig.Body = body;

            var paint = Mats.Lit(spec.Kart, 0.35f, 0.05f);
            var trim = Mats.Lit(spec.KartTrim, 0.3f);
            var dark = Mats.Lit(new Color(0.11f, 0.13f, 0.12f), 0.2f);
            var metal = Mats.Lit(new Color(0.62f, 0.62f, 0.6f), 0.55f, 0.7f);
            var cream = Mats.Lit(new Color(0.949f, 0.906f, 0.808f), 0.2f);

            // Châssis en tubes soudés (peints à la couleur du liseré), plancher sombre.
            const float tube = 0.075f, y0 = 0.3f;
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, y0 - 0.04f, -0.05f), new Vector3(1.0f, 0.04f, 2.0f), dark);
            foreach (float sx in new[] { -1f, 1f })
            {
                Tube(body, new Vector3(0.55f * sx, y0, -1.05f), new Vector3(0.55f * sx, y0, 1.05f), tube, trim);           // longeron
                Tube(body, new Vector3(0.55f * sx, y0, 1.05f), new Vector3(0.3f * sx, y0 + 0.05f, 1.42f), tube, trim);    // nez
                Tube(body, new Vector3(0.55f * sx, y0, 0.5f), new Vector3(1.0f * sx, y0 + 0.04f, 0.38f), tube, trim);     // pare-chocs latéral
                Tube(body, new Vector3(0.55f * sx, y0, -0.45f), new Vector3(1.0f * sx, y0 + 0.04f, -0.3f), tube, trim);
                Tube(body, new Vector3(1.0f * sx, y0 + 0.04f, 0.38f), new Vector3(1.0f * sx, y0 + 0.04f, -0.3f), tube, trim);
                Tube(body, new Vector3(0.55f * sx, y0, -1.05f), new Vector3(0.82f * sx, y0 + 0.08f, -1.38f), tube, trim);  // pare-chocs arrière
                // Pontons peints sur les pare-chocs latéraux.
                MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0.95f * sx, y0 + 0.12f, 0.04f), new Vector3(0.2f, 0.16f, 0.62f), paint);
            }
            Tube(body, new Vector3(-0.3f, y0 + 0.05f, 1.42f), new Vector3(0.3f, y0 + 0.05f, 1.42f), tube, trim);
            Tube(body, new Vector3(-0.82f, y0 + 0.08f, -1.38f), new Vector3(0.82f, y0 + 0.08f, -1.38f), tube, trim);
            Tube(body, new Vector3(-0.55f, y0, 0.2f), new Vector3(0.55f, y0, 0.2f), tube, trim);
            Tube(body, new Vector3(-0.55f, y0, -0.95f), new Vector3(0.55f, y0, -0.95f), tube, trim);

            // Carénage avant avec plaque de numéro, et réservoir.
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, y0 + 0.16f, 1.22f), new Vector3(0.86f, 0.18f, 0.5f), paint, Quaternion.Euler(-14, 0, 0));
            var plate = MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, y0 + 0.27f, 1.38f), new Vector3(0.5f, 0.3f, 0.04f), cream,
                Quaternion.Euler(-30, 0, 0)).transform;
            NumberOn(plate, spec.Number, dark);
            MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0, y0 + 0.2f, 0.62f), new Vector3(0.3f, 0.1f, 0.3f), cream, Quaternion.Euler(90, 0, 0));

            // Siège baquet, colonne et volant.
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, y0 + 0.16f, -0.38f), new Vector3(0.62f, 0.12f, 0.6f), dark);
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, y0 + 0.5f, -0.7f), new Vector3(0.66f, 0.62f, 0.12f), dark, Quaternion.Euler(-14, 0, 0));
            Tube(body, new Vector3(0, y0 + 0.08f, 1.0f), new Vector3(0, y0 + 0.62f, 0.38f), 0.05f, metal);
            MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0, y0 + 0.64f, 0.36f), new Vector3(0.4f, 0.025f, 0.4f), dark, Quaternion.Euler(-55, 0, 0));
            MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0, y0 + 0.65f, 0.37f), new Vector3(0.12f, 0.03f, 0.12f), trim, Quaternion.Euler(-55, 0, 0));

            // Moteur latéral : bloc, ailettes, pot.
            var engine = new Vector3(0.42f, y0 + 0.3f, -0.95f);
            MeshKit.Prim(PrimitiveType.Cube, body, engine, new Vector3(0.34f, 0.36f, 0.4f), metal);
            for (int i = 0; i < 4; i++)
                MeshKit.Prim(PrimitiveType.Cube, body, engine + new Vector3(0, 0.2f + i * 0.055f, 0), new Vector3(0.4f - i * 0.03f, 0.025f, 0.44f - i * 0.03f), dark);
            MeshKit.Prim(PrimitiveType.Cylinder, body, engine + new Vector3(-0.22f, -0.02f, 0.05f), new Vector3(0.16f, 0.04f, 0.16f), trim, Quaternion.Euler(0, 0, 90));
            Tube(body, engine + new Vector3(0.18f, 0.05f, 0), new Vector3(0.62f, y0 + 0.4f, -1.2f), 0.06f, metal);
            MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0.36f, y0 + 0.42f, -1.3f), new Vector3(0.2f, 0.26f, 0.2f), metal, Quaternion.Euler(0, 0, 90));
            rig.Exhausts = new Transform[2];
            for (int i = 0; i < 2; i++)
                rig.Exhausts[i] = MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(i == 0 ? 0.08f : 0.64f, y0 + 0.42f, -1.3f),
                    new Vector3(0.08f, 0.06f, 0.08f), dark, Quaternion.Euler(0, 0, 90)).transform;

            // Roues : les avant tournent autour d'un pivot pour braquer.
            rig.Wheels = new Transform[4];
            rig.FrontPivots = new Transform[2];
            var tire = Mats.Lit(new Color(0.09f, 0.09f, 0.1f), 0.12f);
            var rim = Mats.Lit(spec.KartTrim, 0.4f, 0.4f);
            Tube(body, new Vector3(-0.85f, 0.36f, -0.85f), new Vector3(0.85f, 0.36f, -0.85f), 0.06f, metal); // essieu arrière
            for (int i = 0; i < 4; i++)
            {
                bool front = i < 2;
                float x = (i % 2 == 0 ? -1f : 1f) * (front ? 0.86f : 0.88f);
                float z = front ? 0.9f : -0.85f;
                float r = front ? 0.56f : 0.72f;
                float wdt = front ? 0.2f : 0.28f;
                Transform holder = root;
                if (front)
                {
                    var pivot = new GameObject("FrontPivot").transform;
                    pivot.SetParent(root, false);
                    pivot.localPosition = new Vector3(x, r * 0.5f, z);
                    rig.FrontPivots[i] = pivot;
                    holder = pivot;
                }
                var wheel = new GameObject("Wheel").transform;
                wheel.SetParent(holder, false);
                wheel.localPosition = front ? Vector3.zero : new Vector3(x, r * 0.5f, z);
                float side = x > 0 ? 1f : -1f;
                MeshKit.Prim(PrimitiveType.Cylinder, wheel, Vector3.zero, new Vector3(r, wdt, r), tire, Quaternion.Euler(0, 0, 90));
                // Flancs arrondis : deux disques un peu plus petits de part et d'autre.
                MeshKit.Prim(PrimitiveType.Cylinder, wheel, new Vector3(side * wdt * 0.98f, 0, 0), new Vector3(r * 0.9f, 0.025f, r * 0.9f), tire, Quaternion.Euler(0, 0, 90));
                MeshKit.Prim(PrimitiveType.Cylinder, wheel, new Vector3(side * (wdt + 0.02f), 0, 0), new Vector3(r * 0.56f, 0.03f, r * 0.56f), rim, Quaternion.Euler(0, 0, 90));
                MeshKit.Prim(PrimitiveType.Cylinder, wheel, new Vector3(side * (wdt + 0.04f), 0, 0), new Vector3(r * 0.18f, 0.03f, r * 0.18f), metal, Quaternion.Euler(0, 0, 90));
                // Branches de jante visibles pour sentir la rotation.
                for (int k = 0; k < 3; k++)
                    MeshKit.Prim(PrimitiveType.Cube, wheel, new Vector3(side * (wdt + 0.045f), 0, 0), new Vector3(0.02f, r * 0.5f, 0.05f), dark,
                        Quaternion.Euler(k * 60f, 0, 0));
                rig.Wheels[i] = wheel;
                if (front) Tube(body, new Vector3(0.55f * side, y0, z), new Vector3((Mathf.Abs(x) - wdt - 0.05f) * side, r * 0.5f, z), 0.05f, metal); // fusée
            }

            rig.Driver = BuildDriver(spec, body);

            // Bulle bouclier (masquée par défaut).
            rig.Shield = MeshKit.Prim(PrimitiveType.Sphere, root, new Vector3(0, 0.9f, 0), new Vector3(3.4f, 2.6f, 3.6f),
                Mats.Transparent(new Color(0.55f, 0.85f, 1f, 0.28f)), null, "Shield");
            rig.Shield.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rig.Shield.SetActive(false);
            return rig;
        }

        /// <summary>Tube entre deux points (cylindre orienté). Diamètre 0 : rien n'est dessiné.</summary>
        static void Tube(Transform parent, Vector3 a, Vector3 b, float diameter, Material mat)
        {
            if (diameter <= 0f) return;
            var d = b - a;
            MeshKit.Prim(PrimitiveType.Cylinder, parent, (a + b) * 0.5f, new Vector3(diameter, d.magnitude * 0.5f, diameter), mat,
                Quaternion.FromToRotation(Vector3.up, d));
        }

        // Afficheur à 7 segments : a, b, c, d, e, f, g pour chaque chiffre.
        static readonly int[] Segments = { 0x3F, 0x06, 0x5B, 0x4F, 0x66, 0x6D, 0x7D, 0x07, 0x7F, 0x6F };

        /// <summary>Numéro de course peint sur la face avant d'une plaque (cube unité mis à l'échelle).</summary>
        static void NumberOn(Transform plate, int number, Material ink)
        {
            string digits = number.ToString("00");
            for (int n = 0; n < digits.Length; n++)
            {
                int mask = Segments[digits[n] - '0'];
                float cx = (n - (digits.Length - 1) * 0.5f) * 0.42f;
                const float w = 0.26f, h = 0.32f, t = 0.07f, z = 0.55f;
                var seg = new (bool horizontal, float x, float y)[]
                {
                    (true, 0, h), (false, w * 0.5f, h * 0.5f), (false, w * 0.5f, -h * 0.5f), (true, 0, -h),
                    (false, -w * 0.5f, -h * 0.5f), (false, -w * 0.5f, h * 0.5f), (true, 0, 0),
                };
                for (int i = 0; i < 7; i++)
                {
                    if ((mask & (1 << i)) == 0) continue;
                    var (hz, x, y) = seg[i];
                    var size = hz ? new Vector3(w * 0.84f, t * 1.4f, 0.1f) : new Vector3(t, h * 0.92f, 0.1f);
                    var go = MeshKit.Prim(PrimitiveType.Cube, plate, new Vector3(cx + x, y, z), size, ink);
                    go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        static Transform BuildDriver(AnimalSpec spec, Transform body)
        {
            var model = Resources.Load<GameObject>("Pilots/" + spec.Kind);
            if (model != null) return SculptedDriver(spec, body, model);

            var d = new GameObject("Driver").transform;
            d.SetParent(body, false);
            d.localPosition = new Vector3(0, 0.6f, -0.25f);

            var fur = Mats.Lit(spec.Fur, 0.2f);
            var accent = Mats.Lit(spec.Accent, 0.2f);
            var white = Mats.Lit(Color.white, 0.6f);
            var black = Mats.Lit(new Color(0.05f, 0.05f, 0.07f), 0.8f);
            var pinkInner = Mats.Lit(new Color(1f, 0.65f, 0.7f), 0.2f);

            // Corps et bras vers le volant
            var torsoMat = spec.Kind == AnimalKind.Penguin ? fur : Mats.Lit(spec.KartTrim == Color.white ? spec.Kart : spec.KartTrim, 0.3f);
            MeshKit.Prim(PrimitiveType.Sphere, d, new Vector3(0, 0.38f, 0), new Vector3(0.78f, 0.75f, 0.62f), torsoMat);
            if (spec.Kind == AnimalKind.Penguin)
                MeshKit.Prim(PrimitiveType.Sphere, d, new Vector3(0, 0.36f, 0.12f), new Vector3(0.6f, 0.62f, 0.45f), white);
            MeshKit.Prim(PrimitiveType.Capsule, d, new Vector3(-0.3f, 0.42f, 0.33f), new Vector3(0.17f, 0.24f, 0.17f), fur, Quaternion.Euler(70, 0, 15));
            MeshKit.Prim(PrimitiveType.Capsule, d, new Vector3(0.3f, 0.42f, 0.33f), new Vector3(0.17f, 0.24f, 0.17f), fur, Quaternion.Euler(70, 0, -15));

            var head = new GameObject("Head").transform;
            head.SetParent(d, false);
            head.localPosition = new Vector3(0, 1.0f, 0.02f);
            MeshKit.Prim(PrimitiveType.Sphere, head, Vector3.zero, new Vector3(0.82f, 0.76f, 0.76f), fur);

            // Yeux (sauf grenouille : globes sur le dessus)
            if (spec.Kind == AnimalKind.Frog)
            {
                foreach (float x in new[] { -0.2f, 0.2f })
                {
                    MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(x, 0.33f, 0.12f), new Vector3(0.3f, 0.3f, 0.3f), fur);
                    MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(x, 0.37f, 0.22f), new Vector3(0.2f, 0.2f, 0.14f), white);
                    MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(x, 0.37f, 0.29f), new Vector3(0.09f, 0.11f, 0.05f), black);
                }
                MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(0, -0.12f, 0.3f), new Vector3(0.55f, 0.14f, 0.2f), accent);
            }
            else
            {
                foreach (float x in new[] { -0.16f, 0.16f })
                {
                    if (spec.Kind == AnimalKind.Panda)
                        MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(x * 1.1f, 0.06f, 0.3f), new Vector3(0.22f, 0.26f, 0.1f), accent,
                            Quaternion.Euler(0, 0, x > 0 ? -25 : 25));
                    MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(x, 0.07f, 0.34f), new Vector3(0.15f, 0.18f, 0.08f), white);
                    MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(x, 0.07f, 0.38f), new Vector3(0.08f, 0.11f, 0.04f), black);
                }
            }

            switch (spec.Kind)
            {
                case AnimalKind.Fox:
                    Snout(head, accent, black, 0.26f, 0.3f);
                    PointedEars(head, fur, accent, 0.5f, 0.24f);
                    break;
                case AnimalKind.Cat:
                    Snout(head, accent, pinkInner, 0.2f, 0.18f);
                    PointedEars(head, fur, pinkInner, 0.34f, 0.2f);
                    break;
                case AnimalKind.Panda:
                    Snout(head, Mats.Lit(Color.white, 0.2f), black, 0.24f, 0.16f);
                    RoundEars(head, accent, 0.26f);
                    break;
                case AnimalKind.Bear:
                    Snout(head, accent, black, 0.26f, 0.18f);
                    RoundEars(head, fur, 0.24f);
                    break;
                case AnimalKind.Rabbit:
                    Snout(head, Mats.Lit(Color.white, 0.2f), pinkInner, 0.2f, 0.14f);
                    foreach (float x in new[] { -0.15f, 0.15f })
                    {
                        var ear = MeshKit.Prim(PrimitiveType.Capsule, head, new Vector3(x, 0.6f, -0.05f), new Vector3(0.18f, 0.36f, 0.1f), fur,
                            Quaternion.Euler(-12, 0, x > 0 ? -10 : 10)).transform;
                        MeshKit.Prim(PrimitiveType.Capsule, ear, new Vector3(0, 0, 0.3f), new Vector3(0.6f, 0.85f, 0.4f), accent);
                    }
                    break;
                case AnimalKind.Penguin:
                    MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(0, -0.05f, 0.18f), new Vector3(0.6f, 0.5f, 0.5f), white);
                    MeshKit.Shape(MeshKit.Cone, head, new Vector3(0, -0.08f, 0.36f), new Vector3(0.18f, 0.28f, 0.12f),
                        Mats.Lit(new Color(1f, 0.6f, 0.1f), 0.4f), Quaternion.Euler(90, 0, 0));
                    break;
                case AnimalKind.Pig:
                    MeshKit.Prim(PrimitiveType.Cylinder, head, new Vector3(0, -0.08f, 0.38f), new Vector3(0.26f, 0.06f, 0.2f), accent, Quaternion.Euler(90, 0, 0));
                    foreach (float x in new[] { -0.05f, 0.05f })
                        MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(x, -0.08f, 0.45f), new Vector3(0.05f, 0.07f, 0.03f), black);
                    PointedEars(head, fur, accent, 0.22f, 0.22f);
                    break;
                case AnimalKind.Frog:
                    break;
            }

            // Casque-bandeau aux couleurs du kart
            MeshKit.Prim(PrimitiveType.Cylinder, head, new Vector3(0, 0.2f, -0.02f), new Vector3(0.84f, 0.05f, 0.8f),
                Mats.Lit(spec.Kart, 0.5f), Quaternion.Euler(-8, 0, 0));
            return d;
        }

        /// <summary>
        /// Pilote sculpté sous Blender (<c>tools/blender/pilote.py</c>), assis sur le baquet. Les matériaux nommés
        /// TP_* du modèle sont remplacés par la livrée du pilote.
        /// </summary>
        static Transform SculptedDriver(AnimalSpec spec, Transform body, GameObject model)
        {
            var d = new GameObject("Driver").transform;
            d.SetParent(body, false);
            d.localPosition = new Vector3(0, 0.5f, -0.36f);
            var go = Object.Instantiate(model, d, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(0f, SculptYaw, 0f);
            go.transform.localScale = Vector3.one * SculptScale;
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = Livery(spec, mats[i] != null ? mats[i].name : "");
                r.sharedMaterials = mats;
            }
            return d;
        }

        /// <summary>Rotation du modèle importé pour qu'il regarde vers l'avant du kart (+z).</summary>
        const float SculptYaw = 0f;
        /// <summary>Échelle du modèle : silhouette lisible derrière le kart, à la taille des autres pilotes.</summary>
        const float SculptScale = 1.35f;

        static Material Livery(AnimalSpec spec, string name)
        {
            if (name.StartsWith("TP_Suit") || name.StartsWith("TP_Helmet")) return Mats.Lit(spec.Kart, 0.3f);
            if (name.StartsWith("TP_Trim")) return Mats.Lit(spec.KartTrim, 0.3f);
            if (name.StartsWith("TP_Fur")) return Mats.Lit(spec.Fur, 0.12f);
            if (name.StartsWith("TP_Accent")) return Mats.Lit(spec.Accent, 0.12f);
            if (name.StartsWith("TP_Eye") || name.StartsWith("TP_Nose") || name.StartsWith("TP_Visor"))
                return Mats.Lit(new Color(0.04f, 0.04f, 0.05f), 0.7f);
            return Mats.Lit(new Color(0.11f, 0.13f, 0.12f), 0.2f); // TP_Dark : gants, bottines, col
        }

        static void Snout(Transform head, Material muzzle, Material nose, float width, float length)
        {
            MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(0, -0.12f, 0.3f + length * 0.3f), new Vector3(width * 1.4f, width, length + 0.12f), muzzle);
            MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(0, -0.06f, 0.36f + length * 0.75f), new Vector3(0.1f, 0.08f, 0.08f), nose);
        }

        static void PointedEars(Transform head, Material fur, Material inner, float height, float width)
        {
            foreach (float x in new[] { -0.24f, 0.24f })
            {
                var rot = Quaternion.Euler(-8, 0, x > 0 ? -22 : 22);
                MeshKit.Shape(MeshKit.Cone, head, new Vector3(x, 0.24f, -0.02f), new Vector3(width, height, width * 0.55f), fur, rot);
                MeshKit.Shape(MeshKit.Cone, head, new Vector3(x * 0.97f, 0.26f, 0.03f), new Vector3(width * 0.6f, height * 0.75f, width * 0.3f), inner, rot);
            }
        }

        static void RoundEars(Transform head, Material mat, float size)
        {
            foreach (float x in new[] { -0.3f, 0.3f })
                MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(x, 0.3f, -0.04f), new Vector3(size, size, size * 0.6f), mat);
        }
    }
}
