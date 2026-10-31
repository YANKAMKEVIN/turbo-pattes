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
    public enum Theme { Meadow, Canyon }

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

        /// <summary>Tous les circuits, dans l'ordre du menu.</summary>
        public static Circuit[] All() => new[] { Meadow(), Canyon() };

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
    }
}
