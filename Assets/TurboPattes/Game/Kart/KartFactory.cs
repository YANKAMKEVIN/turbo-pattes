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

    /// <summary>Construit un kart et son pilote humain à partir de primitives générées par code. Avant = +z.</summary>
    public static class KartFactory
    {
        public static KartRig Build(DriverSpec spec, Transform parent)
        {
            var rig = new KartRig();
            var root = new GameObject("Kart_" + spec.Name).transform;
            root.SetParent(parent, false);
            rig.Root = root;
            var body = new GameObject("Body").transform;
            body.SetParent(root, false);
            rig.Body = body;

            // Livrée : carénage à la couleur principale, bande et liserés à la couleur secondaire ; plancher, pare-chocs,
            // siège et volant en carbone ; moteur, échappements et jantes en métal.
            var paint = Mats.Lit(spec.Kart, 0.7f, 0.1f);
            var trim = Mats.Lit(spec.KartTrim, 0.55f);
            var carbon = Mats.Lit(new Color(0.07f, 0.08f, 0.09f), 0.75f, 0.2f);
            var metal = Mats.Lit(new Color(0.72f, 0.74f, 0.78f), 0.85f, 0.85f);

            // Châssis : plancher carbone et tubes de pare-chocs avant et arrière.
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.3f, 0), new Vector3(1.35f, 0.08f, 2.5f), carbon, null, "Floor");
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.34f, 1.62f), new Vector3(1.3f, 0.12f, 0.14f), carbon, null, "FrontBumper");
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.36f, -1.34f), new Vector3(1.6f, 0.14f, 0.14f), carbon, null, "RearBumper");
            // Carénage avant enveloppant (nez) et panneau frontal incliné, bande centrale.
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.46f, 1.2f), new Vector3(1.2f, 0.24f, 0.75f), paint, Quaternion.Euler(-10, 0, 0), "Nose");
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.6f, 1.2f), new Vector3(0.18f, 0.02f, 0.72f), trim, Quaternion.Euler(-10, 0, 0), "NoseStripe");
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.66f, 0.82f), new Vector3(0.78f, 0.36f, 0.05f), paint, Quaternion.Euler(-28, 0, 0), "FrontPanel");
            // Pontons latéraux : carénage arrondi et liseré sur le dessus.
            foreach (float side in new[] { -1f, 1f })
            {
                MeshKit.Prim(PrimitiveType.Capsule, body, new Vector3(side * 0.72f, 0.44f, -0.05f), new Vector3(0.3f, 0.68f, 0.28f), paint,
                    Quaternion.Euler(90, 0, 0), "SidePod");
                MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(side * 0.72f, 0.58f, -0.05f), new Vector3(0.16f, 0.02f, 1.05f), trim, null, "SidePodStripe");
            }
            // Siège baquet carbone et moteur sur le côté droit, comme sur un vrai kart.
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.82f, -0.62f), new Vector3(0.62f, 0.6f, 0.12f), carbon, Quaternion.Euler(-10, 0, 0), "Seat");
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.5f, -0.48f), new Vector3(0.6f, 0.08f, 0.4f), carbon, null, "SeatBase");
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0.38f, 0.58f, -1.0f), new Vector3(0.42f, 0.38f, 0.42f), metal, null, "Engine");
            MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0.38f, 0.82f, -1.0f), new Vector3(0.22f, 0.06f, 0.22f), carbon, null, "EngineHead");
            rig.Exhausts = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float x = i == 0 ? -0.3f : 0.3f;
                rig.Exhausts[i] = MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(x, 0.62f, -1.35f), new Vector3(0.14f, 0.16f, 0.14f), metal,
                    Quaternion.Euler(90, 0, 0)).transform;
            }
            // Volant carbone sur sa colonne.
            MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0, 0.95f, 0.35f), new Vector3(0.36f, 0.025f, 0.36f), carbon, Quaternion.Euler(-60, 0, 0), "Wheel");
            MeshKit.Prim(PrimitiveType.Cylinder, body, new Vector3(0, 0.78f, 0.52f), new Vector3(0.05f, 0.22f, 0.05f), metal, Quaternion.Euler(-60, 0, 0), "Column");
            // Becquet arrière bas, dérives carbone.
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 0.98f, -1.25f), new Vector3(1.3f, 0.05f, 0.32f), paint, Quaternion.Euler(8, 0, 0), "Spoiler");
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0, 1.0f, -1.12f), new Vector3(1.3f, 0.015f, 0.05f), trim, null, "SpoilerStripe");
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(0.6f, 0.85f, -1.25f), new Vector3(0.05f, 0.32f, 0.36f), carbon, null, "Endplate");
            MeshKit.Prim(PrimitiveType.Cube, body, new Vector3(-0.6f, 0.85f, -1.25f), new Vector3(0.05f, 0.32f, 0.36f), carbon, null, "Endplate");

            // Numéros de course : plaque blanche à chiffres carbone sur le panneau frontal, les pontons et le pare-chocs arrière.
            var plate = Mats.Textured(Icons.NumberPlate(spec.Number, new Color(0.95f, 0.96f, 0.97f), new Color(0.08f, 0.09f, 0.1f)),
                Color.white, Vector2.one, 0.4f);
            // Un quad Unity se voit depuis -z : on le tourne vers l'extérieur de chaque face (le panneau frontal est
            // incliné de 28° vers l'arrière, sa plaque regarde donc vers l'avant et un peu vers le haut).
            NumberDecal(body, new Vector3(0, 0.675f, 0.848f), Quaternion.Euler(28, 180, 0), 0.34f, plate);
            NumberDecal(body, new Vector3(0.88f, 0.44f, -0.05f), Quaternion.Euler(0, -90, 0), 0.26f, plate);
            NumberDecal(body, new Vector3(-0.88f, 0.44f, -0.05f), Quaternion.Euler(0, 90, 0), 0.26f, plate);
            NumberDecal(body, new Vector3(0, 0.36f, -1.42f), Quaternion.identity, 0.3f, plate);

            // Roues : slicks noirs et jantes métal ; les avant tournent autour d'un pivot pour braquer.
            rig.Wheels = new Transform[4];
            rig.FrontPivots = new Transform[2];
            var tire = Mats.Lit(new Color(0.07f, 0.07f, 0.08f), 0.25f);
            for (int i = 0; i < 4; i++)
            {
                bool front = i < 2;
                float x = (i % 2 == 0 ? -1f : 1f) * 0.85f;
                float z = front ? 0.85f : -0.8f;
                float r = front ? 0.56f : 0.66f;
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
                MeshKit.Prim(PrimitiveType.Cylinder, wheel, Vector3.zero, new Vector3(r, front ? 0.17f : 0.23f, r), tire, Quaternion.Euler(0, 0, 90));
                MeshKit.Prim(PrimitiveType.Cylinder, wheel, new Vector3(x > 0 ? 0.1f : -0.1f, 0, 0), new Vector3(r * 0.62f, 0.1f, r * 0.62f), metal,
                    Quaternion.Euler(0, 0, 90));
                // Branche de jante sombre, pour sentir la rotation.
                MeshKit.Prim(PrimitiveType.Cube, wheel, new Vector3(x > 0 ? 0.12f : -0.12f, 0, 0), new Vector3(0.04f, r * 0.6f, 0.07f), carbon);
                rig.Wheels[i] = wheel;
            }

            rig.Driver = BuildDriver(spec, body, rig);
            rig.DriverRest = rig.Driver.localPosition;

            // Bouclier carbone (masqué par défaut) : enveloppe fumée gris bleuté autour du kart.
            rig.Shield = MeshKit.Prim(PrimitiveType.Sphere, root, new Vector3(0, 0.9f, 0), new Vector3(3.4f, 2.6f, 3.6f),
                Mats.Transparent(new Color(0.55f, 0.62f, 0.72f, 0.3f)), null, "Shield");
            rig.Shield.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rig.Shield.SetActive(false);
            return rig;
        }

        /// <summary>Plaque de numéro carrée (quad texturé de côté <paramref name="width"/>).</summary>
        static void NumberDecal(Transform body, Vector3 pos, Quaternion rot, float width, Material mat)
        {
            var go = MeshKit.Prim(PrimitiveType.Quad, body, pos, new Vector3(width, width, 1f), mat, rot, "Number");
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>
        /// Amplitude des bras dans les postures de fin de course : réduite de 20 % pour rester crédible sur des
        /// proportions adultes (docs/design/da-arcade-sportif.md, § 6).
        /// </summary>
        const float ArmAmplitude = 0.8f;

        /// <summary>Applique une posture (<see cref="DriverPoses"/>) au pilote ; <see cref="DriverPose.Rest"/> le remet au volant.</summary>
        public static void Pose(KartRig rig, DriverPose pose)
        {
            if (rig.Head == null || rig.Shoulders == null) return;
            rig.Driver.localPosition = rig.DriverRest + Vector3.up * pose.Bounce;
            rig.Head.localRotation = Quaternion.Euler(pose.HeadPitch, 0f, pose.HeadRoll);
            // Lever : rotation vers le haut autour de l'axe des épaules ; écarter : vers l'extérieur de chaque côté.
            for (int i = 0; i < rig.Shoulders.Length; i++)
                rig.Shoulders[i].localRotation = Quaternion.Euler(-pose.ArmLift * ArmAmplitude, 0f, (i == 0 ? 1f : -1f) * pose.ArmSpread * ArmAmplitude);
        }

        /// <summary>
        /// Pilote humain assis dans le baquet, proportions adultes : bassin, jambes vers le pédalier, buste en
        /// combinaison (bande de la couleur secondaire), col, bras gantés posés sur le volant et casque intégral à la
        /// couleur de l'équipe (bande, visière fumée, petit becquet). Le visage n'est jamais visible.
        /// Repère local du pilote : origine au fond du baquet, avant = +z, volant vers (0 ; 0,35 ; 0,6).
        /// </summary>
        static Transform BuildDriver(DriverSpec spec, Transform body, KartRig rig)
        {
            var d = new GameObject("Driver").transform;
            d.SetParent(body, false);
            d.localPosition = new Vector3(0, 0.6f, -0.25f);

            var suit = Mats.Lit(spec.Suit, 0.3f);
            var stripe = Mats.Lit(spec.KartTrim, 0.35f);
            var carbon = Mats.Lit(new Color(0.08f, 0.09f, 0.1f), 0.55f);
            var helmet = Mats.Lit(spec.Kart, 0.8f, 0.1f);
            var helmetStripe = Mats.Lit(spec.KartTrim, 0.8f, 0.1f);
            var visor = Mats.Lit(new Color(0.05f, 0.06f, 0.08f), 0.95f, 0.4f);

            // Bassin et jambes : cuisses qui remontent vers les genoux, tibias qui redescendent vers le pédalier.
            MeshKit.Prim(PrimitiveType.Sphere, d, new Vector3(0, 0.08f, -0.14f), new Vector3(0.44f, 0.22f, 0.36f), suit, null, "Pelvis");
            foreach (float side in new[] { -1f, 1f })
            {
                var hip = new Vector3(side * 0.12f, 0.08f, -0.06f);
                var knee = new Vector3(side * 0.15f, 0.26f, 0.36f);
                var foot = new Vector3(side * 0.13f, 0.06f, 0.76f);
                Limb(d, hip, knee, 0.15f, suit, "Thigh");
                Limb(d, knee, foot, 0.12f, suit, "Shin");
                MeshKit.Prim(PrimitiveType.Sphere, d, foot + new Vector3(0, 0.02f, 0.04f), new Vector3(0.11f, 0.1f, 0.2f), carbon, null, "Boot");
            }

            // Buste légèrement renversé contre le dossier, bande verticale de la couleur secondaire sur la poitrine.
            var lean = Quaternion.Euler(-12, 0, 0);
            MeshKit.Prim(PrimitiveType.Capsule, d, new Vector3(0, 0.4f, -0.2f), new Vector3(0.46f, 0.3f, 0.3f), suit, lean, "Torso");
            MeshKit.Prim(PrimitiveType.Cube, d, new Vector3(0, 0.42f, -0.05f), new Vector3(0.12f, 0.46f, 0.02f), stripe, lean, "SuitStripe");
            // Col montant de la combinaison (protège-nuque carbone).
            MeshKit.Prim(PrimitiveType.Cylinder, d, new Vector3(0, 0.72f, -0.22f), new Vector3(0.24f, 0.05f, 0.24f), carbon, null, "Collar");

            // Bras : épaule qui pivote (animations de fin de course), bras tendu vers le volant, gant carbone.
            rig.Shoulders = new Transform[2];
            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? -1f : 1f;
                var shoulder = new GameObject(i == 0 ? "ShoulderL" : "ShoulderR").transform;
                shoulder.SetParent(d, false);
                shoulder.localPosition = new Vector3(side * 0.25f, 0.62f, -0.2f);
                // Main sur la jante du volant, à « dix heures dix ».
                var hand = new Vector3(side * 0.17f, 0.4f, 0.54f) - shoulder.localPosition;
                Limb(shoulder, Vector3.zero, hand * 0.92f, 0.12f, suit, "Arm");
                MeshKit.Prim(PrimitiveType.Sphere, shoulder, Vector3.zero, new Vector3(0.15f, 0.15f, 0.15f), suit, null, "ShoulderPad");
                MeshKit.Prim(PrimitiveType.Sphere, shoulder, hand, new Vector3(0.11f, 0.11f, 0.13f), carbon, null, "Glove");
                rig.Shoulders[i] = shoulder;
            }

            // Tête : pivot à la base du cou, casque intégral.
            var head = new GameObject("Head").transform;
            head.SetParent(d, false);
            head.localPosition = new Vector3(0, 0.76f, -0.2f);
            rig.Head = head;
            var c = new Vector3(0, 0.17f, 0.02f);
            MeshKit.Prim(PrimitiveType.Sphere, head, c, new Vector3(0.34f, 0.36f, 0.38f), helmet, null, "Helmet");
            // Bande centrale d'avant en arrière, de la couleur secondaire.
            MeshKit.Prim(PrimitiveType.Sphere, head, c + new Vector3(0, 0.004f, -0.004f), new Vector3(0.08f, 0.364f, 0.384f), helmetStripe, null, "HelmetStripe");
            // Visière fumée et mentonnière.
            MeshKit.Prim(PrimitiveType.Sphere, head, c + new Vector3(0, 0.015f, 0.105f), new Vector3(0.3f, 0.12f, 0.2f), visor, null, "Visor");
            MeshKit.Prim(PrimitiveType.Sphere, head, c + new Vector3(0, -0.1f, 0.1f), new Vector3(0.24f, 0.12f, 0.18f), helmet, null, "ChinBar");
            // Petit becquet à l'arrière du casque.
            MeshKit.Prim(PrimitiveType.Cube, head, c + new Vector3(0, 0.13f, -0.16f), new Vector3(0.2f, 0.025f, 0.07f), helmetStripe,
                Quaternion.Euler(-15, 0, 0), "HelmetSpoiler");
            return d;
        }

        /// <summary>Membre en capsule de <paramref name="from"/> à <paramref name="to"/> (repère du parent), de diamètre <paramref name="thickness"/>.</summary>
        static void Limb(Transform parent, Vector3 from, Vector3 to, float thickness, Material mat, string name)
        {
            var dir = to - from;
            float len = dir.magnitude;
            // Capsule Unity : hauteur 2 × scale.y (bouts arrondis compris), diamètre scale.x.
            MeshKit.Prim(PrimitiveType.Capsule, parent, (from + to) * 0.5f, new Vector3(thickness, Mathf.Max(len, thickness) * 0.5f, thickness), mat,
                Quaternion.FromToRotation(Vector3.up, dir), name);
        }
    }
}
