using System;

namespace TurboPattes.Core
{
    public enum ItemKind
    {
        None,
        /// <summary>Carotte turbo : accélération immédiate.</summary>
        Carrot,
        /// <summary>Noix de coco : roule tout droit, rebondit sur les barrières, fait tourner le kart touché.</summary>
        Coconut,
        /// <summary>Pot de miel : flaque posée derrière soi.</summary>
        Honey,
        /// <summary>Bulle bouclier : absorbe un coup pendant quelques secondes.</summary>
        Shield,
    }

    /// <summary>
    /// Tirage d'un objet selon la place : les premiers ont plutôt des objets défensifs,
    /// les derniers plutôt des turbos pour revenir.
    /// </summary>
    public static class ItemRoll
    {
        // Poids : Carotte, Coco, Miel, Bouclier.
        static readonly float[] Front = { 8f, 32f, 42f, 18f };
        static readonly float[] Back = { 50f, 30f, 5f, 15f };
        static readonly ItemKind[] Kinds = { ItemKind.Carrot, ItemKind.Coconut, ItemKind.Honey, ItemKind.Shield };

        /// <param name="rank">Place (0 = premier).</param>
        /// <param name="racers">Nombre de pilotes.</param>
        /// <param name="random01">Valeur aléatoire dans [0, 1).</param>
        public static ItemKind Pick(int rank, int racers, double random01)
        {
            float t = racers <= 1 ? 0f : Math.Max(0f, Math.Min(1f, rank / (float)(racers - 1)));
            float total = 0f;
            var w = new float[Kinds.Length];
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = Front[i] + (Back[i] - Front[i]) * t;
                total += w[i];
            }
            double r = Math.Max(0.0, Math.Min(0.999999, random01)) * total;
            for (int i = 0; i < w.Length; i++)
            {
                if (r < w[i]) return Kinds[i];
                r -= w[i];
            }
            return Kinds[Kinds.Length - 1];
        }
    }
}
