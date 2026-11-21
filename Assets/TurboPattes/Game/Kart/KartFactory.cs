using TurboPattes.Core;
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
        /// <summary>Position de repos du pilote dans la caisse (le saut de victoire part de là).</summary>
        public Vector3 DriverRest;
        /// <summary>Tête et épaules (gauche, droite) du pilote, pour les animations de fin de course.</summary>
        public Transform Head;
        public Transform[] Shoulders;
        public Transform[] Exhausts;
        public GameObject Shield;
    }

    /// <summary>Construit un kart et son pilote animal à partir de primitives. Avant = +z.</summary>
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

            var paint = Mats.Lit(spec.Kart, 0.65f, 0.1f);
            var trim = Mats.Lit(spec.KartTrim, 0.5f);
            var dark = Mats.Lit(new Color(0.12f, 0.12f, 0.14f), 0.3f);
            var metal = Mats.Lit(new Color(0.75f, 0.75f, 0.8f), 0.8f, 0.8f);

            // Caisse
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.42f, 0), new Vector3(1.45f, 0.32f, 2.3f), paint);
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.5f, 1.15f), new Vector3(1.15f, 0.26f, 0.7f), paint, Quaternion.Euler(-12, 0, 0));
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.36f, 1.55f), new Vector3(1.7f, 0.18f, 0.3f), trim);
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.66f, 0.55f), new Vector3(0.9f, 0.08f, 0.5f), trim);
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0.78f, 0.45f, 0), new Vector3(0.16f, 0.22f, 1.5f), trim);
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(-0.78f, 0.45f, 0), new Vector3(0.16f, 0.22f, 1.5f), trim);
            // Siège et moteur
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.85f, -0.62f), new Vector3(0.95f, 0.6f, 0.18f), dark, Quaternion.Euler(-10, 0, 0));
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.62f, -1.05f), new Vector3(1.0f, 0.42f, 0.5f), metal);
            rig.Exhausts = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.3f : 0.3f;
                rig.Exhausts[i] = MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(x, 0.62f, -1.35f), new Vector3(0.16f, 0.18f, 0.16f), metal,
                    Quaternion.Euler(90, 0, 0)).transform;
            }
            // Volant
            MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0, 0.95f, 0.35f), new Vector3(0.42f, 0.03f, 0.42f), dark, Quaternion.Euler(-60, 0, 0));
            // Aileron
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 1.05f, -1.25f), new Vector3(1.4f, 0.06f, 0.35f), trim);
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0.5f, 0.88f, -1.25f), new Vector3(0.06f, 0.32f, 0.2f), dark);
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(-0.5f, 0.88f, -1.25f), new Vector3(0.06f, 0.32f, 0.2f), dark);

            // Roues : les avant tournent autour d'un pivot pour braquer.
            rig.Wheels = new Transform[4];
            rig.FrontPivots = new Transform[2];
            var tire = Mats.Lit(new Color(0.1f, 0.1f, 0.11f), 0.2f);
            var hub = Mats.Lit(spec.KartTrim, 0.6f, 0.3f);
            for (int i = 0; i < 4; i++)
            {
                bool front = i < 2;
                float x = (i % 2 == 0 ? -1f : 1f) * 0.85f;
                float z = front ? 0.85f : -0.8f;
                float r = front ? 0.6f : 0.72f;
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
                MeshKit.Prim(PrimitiveType.Cylinder, wheel, Vector3.zero, new Vector3(r, front ? 0.17f : 0.21f, r), tire, Quaternion.Euler(0, 0, 90));
                MeshKit.Prim(PrimitiveType.Cylinder, wheel, new Vector3(x > 0 ? 0.1f : -0.1f, 0, 0), new Vector3(r * 0.5f, 0.12f, r * 0.5f), hub,
                    Quaternion.Euler(0, 0, 90));
                // Rayon visible pour sentir la rotation.
                MeshKit.Prim(PrimitiveType.Cube, wheel, new Vector3(x > 0 ? 0.12f : -0.12f, 0, 0), new Vector3(0.05f, r * 0.85f, 0.1f), dark);
                rig.Wheels[i] = wheel;
            }

            rig.Driver = BuildDriver(spec, body, rig);
            rig.DriverRest = rig.Driver.localPosition;

            // Bulle bouclier (masquée par défaut).
            rig.Shield = MeshKit.Prim(PrimitiveType.Sphere, root, new Vector3(0, 0.9f, 0), new Vector3(3.4f, 2.6f, 3.6f),
                Mats.Transparent(new Color(0.55f, 0.85f, 1f, 0.28f)), null, "Shield");
            rig.Shield.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rig.Shield.SetActive(false);
            return rig;
        }

        /// <summary>Applique une posture (<see cref="DriverPoses"/>) au pilote ; <see cref="DriverPose.Rest"/> le remet au volant.</summary>
        public static void Pose(KartRig rig, DriverPose pose)
        {
            if (rig.Head == null || rig.Shoulders == null) return;
            rig.Driver.localPosition = rig.DriverRest + Vector3.up * pose.Bounce;
            rig.Head.localRotation = Quaternion.Euler(pose.HeadPitch, 0f, pose.HeadRoll);
            // Lever : rotation vers le haut autour de l'axe des épaules ; écarter : vers l'extérieur de chaque côté.
            for (int i = 0; i < rig.Shoulders.Length; i++)
                rig.Shoulders[i].localRotation = Quaternion.Euler(-pose.ArmLift, 0f, (i == 0 ? 1f : -1f) * pose.ArmSpread);
        }

        static Transform BuildDriver(AnimalSpec spec, Transform body, KartRig rig)
        {
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
            // Col de la combinaison.
            MeshKit.Prim(PrimitiveType.Cylinder, d, new Vector3(0, 0.71f, 0.02f), new Vector3(0.44f, 0.04f, 0.4f), Mats.Lit(spec.Kart, 0.4f));
            // Bras : manches de combinaison et gants, accrochés à une épaule qui pivote (animations de fin de course).
            var sleeve = torsoMat;
            var glove = Mats.Lit(new Color(0.16f, 0.16f, 0.18f), 0.35f);
            rig.Shoulders = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.3f : 0.3f;
                var rot = Quaternion.Euler(70, 0, i == 0 ? 15 : -15);
                // Même bras qu'avant (capsule centrée en (±0,3 ; 0,42 ; 0,33)), mais pendu à son extrémité côté buste.
                Vector3 axis = rot * Vector3.up, center = new Vector3(x, 0.42f, 0.33f);
                var shoulder = new GameObject(i == 0 ? "ShoulderL" : "ShoulderR").transform;
                shoulder.SetParent(d, false);
                shoulder.localPosition = center - axis * 0.24f;
                MeshKit.Prim(PrimitiveType.Capsule, shoulder, axis * 0.24f, new Vector3(0.17f, 0.24f, 0.17f), sleeve, rot);
                MeshKit.Prim(PrimitiveType.Sphere, shoulder, axis * 0.5f, new Vector3(0.17f, 0.15f, 0.17f), glove);
                rig.Shoulders[i] = shoulder;
            }

            var head = new GameObject("Head").transform;
            head.SetParent(d, false);
            head.localPosition = new Vector3(0, 1.0f, 0.02f);
            rig.Head = head;
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

            if (spec.Kind == AnimalKind.Frog)
            {
                // Les yeux de la grenouille sont sur le dessus : simple bandeau aux couleurs du kart.
                MeshKit.Prim(PrimitiveType.Cylinder, head, new Vector3(0, 0.2f, -0.02f), new Vector3(0.84f, 0.05f, 0.8f),
                    Mats.Lit(spec.Kart, 0.5f), Quaternion.Euler(-8, 0, 0));
            }
            else
            {
                // Casque ouvert aux couleurs du kart : coque au-dessus des yeux, oreilles qui dépassent, liseré et visière.
                MeshKit.Shape(MeshKit.Hemisphere, head, new Vector3(0, 0.15f, -0.01f), new Vector3(0.9f, 0.7f, 0.86f), Mats.Lit(spec.Kart, 0.6f, 0.1f),
                    null, "Helmet");
                MeshKit.Prim(PrimitiveType.Cylinder, head, new Vector3(0, 0.15f, -0.01f), new Vector3(0.92f, 0.025f, 0.88f), Mats.Lit(spec.KartTrim, 0.5f));
                MeshKit.Prim(PrimitiveType.Cube, head, new Vector3(0, 0.18f, 0.37f), new Vector3(0.5f, 0.06f, 0.1f),
                    Mats.Lit(new Color(0.08f, 0.1f, 0.12f), 0.85f), Quaternion.Euler(-12, 0, 0), "Visor");
            }
            return d;
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
                MeshKit.Prim(PrimitiveType.Sphere, head, new Vector3(x * 1.07f, 0.38f, -0.04f), new Vector3(size, size, size * 0.6f), mat);
        }
    }
}
