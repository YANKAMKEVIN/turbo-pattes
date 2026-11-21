using UnityEngine;

namespace TurboPattes.Game.Track
{
    /// <summary>Tapis turbo posé sur la piste.</summary>
    public readonly struct BoostPad
    {
        public readonly float S;
        public readonly float Lateral;
        public readonly float Length;
        public readonly float HalfWidth;

        public BoostPad(float s, float lateral, float length = 5f, float halfWidth = 1.8f)
        {
            S = s;
            Lateral = lateral;
            Length = length;
            HalfWidth = halfWidth;
        }
    }

    /// <summary>Décor et ambiance d'un circuit.</summary>
    public enum Theme { Meadow, Canyon, Jungle, Lagoon, Reef, Sky, Space, Ice, Volcano, City }

    /// <summary>Données d'un circuit. Points de contrôle en mètres (x, z), dans le sens de la course.</summary>
    public sealed class Circuit
    {
        public string Name;
        /// <summary>Nom court pour les boutons du menu.</summary>
        public string ShortName;
        public Theme Theme;
        public Vector2[] Control;
        public float HalfWidth = 7f;
        public float CurbWidth = 1.2f;
        public float BarrierOffset = 12f;
        /// <summary>Rangées de bulles surprise, en fraction du tour.</summary>
        public float[] ItemRows;
        /// <summary>Tapis turbo, en fraction du tour et décalage latéral.</summary>
        public (float fraction, float lateral)[] Boosts;
        /// <summary>Hauteur de la piste à chaque point de contrôle (null = circuit plat).</summary>
        public float[] Heights;
        /// <summary>Tremplins : bord au point de contrôle donné, longueur de la rampe et hauteur gagnée (m).</summary>
        public (int control, float length, float rise)[] Kickers;
        /// <summary>Ponts : tronçon porté par des piles (pas de talus dessous), centré sur un point de contrôle.</summary>
        public (int control, float halfLength)[] Bridges;
        /// <summary>
        /// Raccourcis risqués (<see cref="Core.Shortcut"/>) : virage centré sur un point de contrôle, demi-longueur de la zone,
        /// côté intérieur (-1 gauche, +1 droite) et recul du muret (m). Un petit tremplin est posé dans le sable.
        /// </summary>
        public (int control, float halfLength, int side, float extra)[] Shortcuts;

        /// <summary>Gravité relative (espace : sauts plus longs).</summary>
        public float Gravity = 1f;
        /// <summary>Piste suspendue dans le vide (ciel, espace) : pas de sol, tout est porté comme un pont.</summary>
        public bool Floating;

        /// <summary>Tous les circuits, dans l'ordre du menu.</summary>
        public static Circuit[] All() => new[] { Meadow(), Canyon(), Jungle(), Lagoon(), Reef(), Sky(), Space(), Ice(), Volcano(), City() };

        /// <summary>Nombre de circuits, sans les construire.</summary>
        public const int Count = 10;

        /// <summary>
        /// « Prairie des Pattes » : une longue ligne droite, une boucle serrée, des S en montée vers le nord,
        /// une épingle au fond et un grand retour rapide. Géométrie vérifiée : ~1,1 km, rayon mini ~15 m,
        /// tronçons voisins séparés d'au moins 40 m.
        /// </summary>
        public static Circuit Meadow() => new()
        {
            Name = "Prairie des Pattes",
            ShortName = "Prairie",
            Theme = Theme.Meadow,
            Control = new[]
            {
                new Vector2(0, 0), new Vector2(60, 0), new Vector2(120, 0), new Vector2(160, 10), new Vector2(180, 40),
                new Vector2(175, 80), new Vector2(150, 100), new Vector2(120, 92), new Vector2(106, 64), new Vector2(84, 44),
                new Vector2(56, 44), new Vector2(40, 68), new Vector2(40, 100), new Vector2(50, 130), new Vector2(90, 150),
                new Vector2(140, 150), new Vector2(180, 160), new Vector2(200, 190), new Vector2(185, 220), new Vector2(140, 225),
                new Vector2(80, 215), new Vector2(20, 200), new Vector2(-20, 180), new Vector2(-50, 140), new Vector2(-60, 90),
                new Vector2(-50, 40), new Vector2(-30, 10),
            },
            ItemRows = new[] { 0.09f, 0.37f, 0.52f, 0.83f },
            Boosts = new[] { (0.055f, -3.5f), (0.495f, 3f), (0.74f, -2.5f) },
            // Grand virage à gauche du nord-est (rayon ~34 m) : bande de sable à l'intérieur, muret reculé à 21 m
            // (au moins 42 m des autres tronçons, vérifié).
            Shortcuts = new[] { (17, 35f, -1, 9f) },
        };
    
        /// <summary>
        /// « Canyon Caramel » : un huit dans un désert de roches rousses. Départ au sud-est, passage sous le pont,
        /// boucle ouest qui ondule (bosse, creux) en montant, retour sur le pont à 8 m au-dessus du premier passage,
        /// plateau, tremplin au sommet puis longue descente vers la ligne. Géométrie vérifiée : ~1,18 km,
        /// rayon mini ~16 m, tronçons séparés d'au moins 40 m sauf au croisement (8 m de dénivelé).
        /// </summary>
        public static Circuit Canyon() => new()
        {
            Name = "Canyon Caramel",
            ShortName = "Canyon",
            Theme = Theme.Canyon,
            Control = new[]
            {
                new Vector2(140, -90), new Vector2(80, -84), new Vector2(38, -46), new Vector2(0, 0), new Vector2(-45, 46),
                new Vector2(-95, 72), new Vector2(-150, 74), new Vector2(-196, 48), new Vector2(-212, 4), new Vector2(-196, -40),
                new Vector2(-165, -66), new Vector2(-128, -62), new Vector2(-100, -80), new Vector2(-66, -88), new Vector2(-34, -50),
                new Vector2(0, 0), new Vector2(45, 46), new Vector2(100, 66), new Vector2(150, 66), new Vector2(196, 42),
                new Vector2(216, 0), new Vector2(208, -50), new Vector2(190, -82),
            },
            Heights = new[] { 0f, 0f, 0f, 0f, 0f, 2f, 5f, 5f, 3f, 2f, 3f, 5f, 6f, 8f, 8f, 8f, 8f, 8f, 8f, 5f, 2f, 0f, 0f },
            Kickers = new[] { (18, 10f, 1.5f) },
            Bridges = new[] { (15, 42f) },
            ItemRows = new[] { 0.08f, 0.36f, 0.6f, 0.92f },
            Boosts = new[] { (0.035f, 3f), (0.53f, -2.5f), (0.745f, 0f) },
        };

        /// <summary>« Jungle Papaye » : une boucle sinueuse sous les arbres géants : montée vers le belvédère, deux tremplins dans la descente vers le bas-fond.</summary>
        public static Circuit Jungle() => new()
        {
            Name = "Jungle Papaye",
            ShortName = "Jungle",
            Theme = Theme.Jungle,
            Control = new[] { new Vector2(154, 38), new Vector2(114, 57), new Vector2(100, 87), new Vector2(83, 126), new Vector2(42, 148), new Vector2(-7, 145), new Vector2(-51, 132), new Vector2(-96, 122), new Vector2(-140, 104), new Vector2(-174, 73), new Vector2(-200, 36), new Vector2(-225, -7), new Vector2(-232, -58), new Vector2(-193, -97), new Vector2(-119, -103), new Vector2(-56, -85), new Vector2(-23, -81), new Vector2(5, -104), new Vector2(51, -131), new Vector2(111, -141), new Vector2(173, -128), new Vector2(226, -95), new Vector2(245, -45), new Vector2(214, 7) },
            Heights = new[] { 4.5f, 6f, 7f, 7f, 6f, 5f, 4f, 4f, 4.5f, 5f, 4.5f, 3.5f, 1.5f, 0f, 0f, 0f, 0f, 1f, 2f, 2f, 1.5f, 1f, 1.5f, 2.5f },
            Kickers = new[] { (4, 10f, 1.5f), (11, 10f, 1.4f) },
            ItemRows = new[] { 0.1f, 0.33f, 0.58f, 0.84f },
            Boosts = new[] { (0.04f, 0f), (0.47f, -2.5f), (0.7f, 2.5f) },
        };

        /// <summary>« Lagon Coco » : une boucle de sable entre les îlots : pont-ponton au-dessus du lagon, tremplin vers la plage.</summary>
        public static Circuit Lagoon() => new()
        {
            Name = "Lagon Coco",
            ShortName = "Lagon",
            Theme = Theme.Lagoon,
            Control = new[] { new Vector2(68, 125), new Vector2(25, 165), new Vector2(-39, 182), new Vector2(-105, 170), new Vector2(-156, 138), new Vector2(-183, 96), new Vector2(-185, 51), new Vector2(-163, 13), new Vector2(-134, -15), new Vector2(-118, -37), new Vector2(-117, -67), new Vector2(-111, -108), new Vector2(-79, -146), new Vector2(-25, -163), new Vector2(34, -157), new Vector2(86, -139), new Vector2(136, -121), new Vector2(186, -98), new Vector2(218, -61), new Vector2(210, -16), new Vector2(165, 18), new Vector2(115, 36), new Vector2(89, 51), new Vector2(83, 81) },
            Heights = new[] { 3.5f, 3.5f, 3.5f, 2.5f, 1.5f, 1.5f, 2.5f, 2.5f, 2.5f, 2.5f, 1.5f, 0.5f, 1f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 1.5f, 2f, 2.5f, 3f, 3.5f },
            Kickers = new[] { (3, 10f, 1.4f) },
            Bridges = new[] { (8, 45f) },
            ItemRows = new[] { 0.1f, 0.33f, 0.58f, 0.84f },
            Boosts = new[] { (0.04f, 0f), (0.47f, -2.5f), (0.7f, 2.5f) },
        };

        /// <summary>« Récif Bulle » : sous la mer : la piste ondule entre coraux et algues, deux tremplins au-dessus des fosses.</summary>
        public static Circuit Reef() => new()
        {
            Name = "Récif Bulle",
            ShortName = "Récif",
            Theme = Theme.Reef,
            Control = new[] { new Vector2(211, 0), new Vector2(170, 41), new Vector2(113, 59), new Vector2(74, 67), new Vector2(56, 89), new Vector2(37, 127), new Vector2(0, 159), new Vector2(-47, 159), new Vector2(-82, 129), new Vector2(-100, 91), new Vector2(-113, 59), new Vector2(-134, 33), new Vector2(-159, 0), new Vector2(-171, -42), new Vector2(-159, -83), new Vector2(-126, -115), new Vector2(-82, -129), new Vector2(-37, -126), new Vector2(0, -110), new Vector2(28, -94), new Vector2(56, -89), new Vector2(101, -91), new Vector2(159, -83), new Vector2(206, -50) },
            Heights = new[] { 6f, 7f, 7.5f, 7.5f, 6.5f, 5.5f, 4.5f, 3f, 2f, 1.5f, 1.5f, 1.5f, 2f, 2f, 2.5f, 2.5f, 2.5f, 2f, 1.5f, 1.5f, 2f, 2.5f, 3.5f, 5f },
            Kickers = new[] { (3, 10f, 1.5f), (6, 10f, 1.5f) },
            ItemRows = new[] { 0.1f, 0.33f, 0.58f, 0.84f },
            Boosts = new[] { (0.04f, 0f), (0.47f, -2.5f), (0.7f, 2.5f) },
        };

        /// <summary>« Route des Nuages » : piste suspendue entre 30 et 50 m au-dessus d'une mer de nuages, sans sol : deux tremplins, gravité un peu plus légère.</summary>
        public static Circuit Sky() => new()
        {
            Name = "Route des Nuages",
            ShortName = "Nuages",
            Theme = Theme.Sky,
            Control = new[] { new Vector2(-187, 132), new Vector2(-232, 93), new Vector2(-244, 42), new Vector2(-236, -8), new Vector2(-215, -52), new Vector2(-172, -84), new Vector2(-111, -93), new Vector2(-58, -86), new Vector2(-27, -93), new Vector2(7, -126), new Vector2(66, -160), new Vector2(138, -166), new Vector2(201, -142), new Vector2(247, -99), new Vector2(265, -45), new Vector2(238, 8), new Vector2(177, 43), new Vector2(122, 60), new Vector2(96, 80), new Vector2(76, 113), new Vector2(40, 140), new Vector2(-8, 149), new Vector2(-61, 149), new Vector2(-122, 147) },
            Heights = new[] { 44.5f, 47.5f, 48f, 47.5f, 47.5f, 48f, 49.5f, 51.5f, 52.5f, 51.5f, 48f, 43.5f, 39.5f, 36.5f, 36f, 36.5f, 36.5f, 36f, 34.5f, 32.5f, 31.5f, 32.5f, 36f, 40.5f },
            Kickers = new[] { (10, 10f, 1.6f), (18, 10f, 1.5f) },
            Gravity = 0.85f,
            Floating = true,
            ItemRows = new[] { 0.1f, 0.33f, 0.58f, 0.84f },
            Boosts = new[] { (0.04f, 0f), (0.47f, -2.5f), (0.7f, 2.5f) },
        };

        /// <summary>« Orbite Comète » : un huit en apesanteur légère au milieu des étoiles : croisement à 10 m, sauts très longs.</summary>
        public static Circuit Space() => new()
        {
            Name = "Orbite Comète",
            ShortName = "Espace",
            Theme = Theme.Space,
            Control = new[] { new Vector2(160, 118), new Vector2(110, 99), new Vector2(56, 56), new Vector2(0, 0), new Vector2(-57, -58), new Vector2(-113, -103), new Vector2(-166, -122), new Vector2(-208, -108), new Vector2(-235, -64), new Vector2(-243, 0), new Vector2(-231, 62), new Vector2(-202, 105), new Vector2(-160, 118), new Vector2(-110, 99), new Vector2(-56, 56), new Vector2(0, 0), new Vector2(57, -58), new Vector2(113, -103), new Vector2(166, -122), new Vector2(208, -108), new Vector2(235, -64), new Vector2(243, 0), new Vector2(231, 62), new Vector2(202, 105) },
            Heights = new[] { 50f, 50f, 50f, 49.5f, 48.5f, 48f, 47f, 46.5f, 46f, 45.5f, 45f, 44f, 43f, 41.5f, 40.5f, 39.5f, 39f, 39f, 40f, 41.5f, 43.5f, 45.5f, 47.5f, 49f },
            Kickers = new[] { (7, 10f, 1.4f), (12, 10f, 1.4f) },
            Gravity = 0.55f,
            Floating = true,
            ItemRows = new[] { 0.1f, 0.33f, 0.58f, 0.84f },
            Boosts = new[] { (0.04f, 0f), (0.47f, -2.5f), (0.7f, 2.5f) },
        };

        /// <summary>« Banquise Givrée » : un huit sur la glace : pont de glace à 8 m au-dessus du premier passage, descente et tremplin.</summary>
        public static Circuit Ice() => new()
        {
            Name = "Banquise Givrée",
            ShortName = "Banquise",
            Theme = Theme.Ice,
            Control = new[] { new Vector2(135, 102), new Vector2(94, 87), new Vector2(50, 51), new Vector2(0, 0), new Vector2(-53, -54), new Vector2(-101, -93), new Vector2(-137, -103), new Vector2(-160, -85), new Vector2(-174, -48), new Vector2(-186, 0), new Vector2(-193, 53), new Vector2(-187, 100), new Vector2(-162, 122), new Vector2(-116, 107), new Vector2(-59, 61), new Vector2(0, 0), new Vector2(56, -58), new Vector2(109, -101), new Vector2(160, -121), new Vector2(204, -109), new Vector2(231, -64), new Vector2(234, 0), new Vector2(213, 59), new Vector2(177, 94) },
            Heights = new[] { 9f, 9.5f, 9f, 9f, 9f, 9.5f, 9f, 8.5f, 7f, 5f, 3f, 1.5f, 1f, 0.5f, 1f, 1f, 1f, 0.5f, 1f, 1.5f, 3f, 5f, 7f, 8.5f },
            Kickers = new[] { (8, 10f, 1.5f) },
            Bridges = new[] { (3, 42f) },
            ItemRows = new[] { 0.1f, 0.33f, 0.58f, 0.84f },
            Boosts = new[] { (0.04f, 0f), (0.47f, -2.5f), (0.7f, 2.5f) },
        };

        /// <summary>« Volcan Grondant » : on grimpe à 14 m sur le flanc du volcan entre les coulées de lave, puis deux tremplins dans la descente.</summary>
        public static Circuit Volcano() => new()
        {
            Name = "Volcan Grondant",
            ShortName = "Volcan",
            Theme = Theme.Volcano,
            Control = new[] { new Vector2(-118, -119), new Vector2(-68, -123), new Vector2(-25, -115), new Vector2(9, -106), new Vector2(42, -105), new Vector2(87, -111), new Vector2(148, -111), new Vector2(208, -87), new Vector2(235, -39), new Vector2(212, 13), new Vector2(156, 47), new Vector2(105, 62), new Vector2(79, 79), new Vector2(63, 114), new Vector2(34, 153), new Vector2(-14, 171), new Vector2(-62, 155), new Vector2(-93, 119), new Vector2(-114, 86), new Vector2(-140, 59), new Vector2(-175, 29), new Vector2(-200, -13), new Vector2(-198, -60), new Vector2(-167, -99) },
            Heights = new[] { 12f, 13f, 14f, 14f, 13.5f, 13f, 12f, 10.5f, 8.5f, 7f, 5f, 3.5f, 2f, 1f, 0f, 0f, 0.5f, 1f, 2f, 3.5f, 5.5f, 7f, 9f, 10.5f },
            Kickers = new[] { (7, 10f, 1.5f), (11, 10f, 1.5f) },
            ItemRows = new[] { 0.1f, 0.33f, 0.58f, 0.84f },
            Boosts = new[] { (0.04f, 0f), (0.47f, -2.5f), (0.7f, 2.5f) },
        };

        /// <summary>« Ville Néon » : un huit de nuit entre les tours illuminées : échangeur à 9 m, tremplin sur l'avenue.</summary>
        public static Circuit City() => new()
        {
            Name = "Ville Néon",
            ShortName = "Ville",
            Theme = Theme.City,
            Control = new[] { new Vector2(134, 103), new Vector2(102, 96), new Vector2(60, 63), new Vector2(0, 0), new Vector2(-62, -66), new Vector2(-108, -102), new Vector2(-140, -108), new Vector2(-174, -95), new Vector2(-212, -60), new Vector2(-234, 0), new Vector2(-224, 63), new Vector2(-190, 104), new Vector2(-149, 115), new Vector2(-108, 102), new Vector2(-59, 62), new Vector2(0, 0), new Vector2(57, -60), new Vector2(102, -96), new Vector2(143, -110), new Vector2(190, -104), new Vector2(232, -66), new Vector2(246, 0), new Vector2(220, 62), new Vector2(174, 95) },
            Heights = new[] { 7.5f, 8.5f, 9f, 9f, 9f, 8.5f, 7.5f, 7f, 5.5f, 4.5f, 3.5f, 2.5f, 1.5f, 0.5f, 0f, 0f, 0f, 0.5f, 1.5f, 2f, 3.5f, 4.5f, 5.5f, 7f },
            Kickers = new[] { (10, 10f, 1.5f) },
            Bridges = new[] { (3, 42f) },
            ItemRows = new[] { 0.1f, 0.33f, 0.58f, 0.84f },
            Boosts = new[] { (0.04f, 0f), (0.47f, -2.5f), (0.7f, 2.5f) },
        };
    }
}
