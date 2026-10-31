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

    /// <summary>Données d'un circuit. Points de contrôle en mètres (x, z), dans le sens de la course.</summary>
    public sealed class Circuit
    {
        public string Name;
        public Vector2[] Control;
        public float HalfWidth = 7f;
        public float CurbWidth = 1.2f;
        public float BarrierOffset = 12f;
        /// <summary>Rangées de bulles surprise, en fraction du tour.</summary>
        public float[] ItemRows;
        /// <summary>Tapis turbo, en fraction du tour et décalage latéral.</summary>
        public (float fraction, float lateral)[] Boosts;

        /// <summary>
        /// « Prairie des Pattes » : une longue ligne droite, une boucle serrée, des S en montée vers le nord,
        /// une épingle au fond et un grand retour rapide. Géométrie vérifiée : ~1,1 km, rayon mini ~15 m,
        /// tronçons voisins séparés d'au moins 40 m.
        /// </summary>
        public static Circuit Meadow() => new()
        {
            Name = "Prairie des Pattes",
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
    }
}
