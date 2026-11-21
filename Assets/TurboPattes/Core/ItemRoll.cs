using System;

namespace TurboPattes.Core
{
    public enum ItemKind
    {
        None,
        /// <summary>Nitro (ex-carotte turbo) : accélération immédiate. Habillage : <see cref="ItemCatalog"/>.</summary>
        Carrot,
        /// <summary>Pneu fou (ex-noix de coco) : roule tout droit, rebondit sur les barrières, fait tourner le kart touché.</summary>
        Coconut,
        /// <summary>Flaque d'huile (ex-pot de miel) : flaque posée derrière soi.</summary>
        Honey,
        /// <summary>Bouclier carbone (ex-bulle bouclier) : absorbe un coup pendant quelques secondes.</summary>
        Shield,
        /// <summary>Aileron (ex-plume planante) : grand bond plané, par-dessus les objets au sol, avec un petit élan.</summary>
        Feather,
        /// <summary>Drone traqueur (ex-pelote de laine) : file le long de la piste vers le kart juste devant et le fait tourner.</summary>
        Yarn,
        /// <summary>
        /// Aspiration (ex-comète, objet rare, réservé aux derniers) : pendant <see cref="CometRun.Duration"/> s, le kart file en turbo
        /// sur la ligne médiane, pilote automatique, intouchable, et fait tourner ceux qu'il percute.
        /// </summary>
        Comet,
    }

    /// <summary>Réglages de la comète.</summary>
    public static class CometRun
    {
        public const float Duration = 3.5f;
        /// <summary>Tête-à-queue infligé aux karts percutés.</summary>
        public const float SpinOnHit = 1.2f;
        /// <summary>Course trop petite pour un objet de rattrapage.</summary>
        public const int MinRacers = 4;
    }

    /// <summary>
    /// Tirage d'un objet selon la place : les premiers ont plutôt des objets défensifs,
    /// les derniers plutôt des turbos pour revenir, et le dernier quart du peloton peut tirer la comète (rare).
    /// </summary>
    public static class ItemRoll
    {
        // Poids : Carotte, Coco, Miel, Bouclier, Plume, Pelote. Le premier n'a personne à viser : pas de pelote.
        static readonly float[] Front = { 8f, 30f, 38f, 16f, 8f, 0f };
        static readonly float[] Back = { 42f, 22f, 4f, 10f, 6f, 16f };
        static readonly ItemKind[] Kinds = { ItemKind.Carrot, ItemKind.Coconut, ItemKind.Honey, ItemKind.Shield, ItemKind.Feather, ItemKind.Yarn };

        /// <summary>Poids de la comète pour le tout dernier (les autres objets pèsent 100 au total) ; elle reste hors de la roulette.</summary>
        public const float CometMaxWeight = 8f;

        /// <summary>Poids de la comète : nul dans les trois premiers quarts du peloton, puis croissant jusqu'au dernier.</summary>
        public static float CometWeight(int rank, int racers)
        {
            if (racers < CometRun.MinRacers) return 0f;
            float t = Math.Max(0f, Math.Min(1f, rank / (float)(racers - 1)));
            return t <= 0.75f ? 0f : CometMaxWeight * (t - 0.75f) / 0.25f;
        }

        /// <summary>Nombre d'objets différents (roulette de l'interface).</summary>
        public static int KindCount => Kinds.Length;

        /// <summary>Objet montré par la roulette de l'interface à cet instant (14 images par seconde).</summary>
        public static ItemKind Reel(float time) => Kinds[(int)Math.Floor(Math.Max(0f, time) * 14f) % Kinds.Length];

        /// <summary>
        /// Objet à dessiner pour un kart : l'objet détenu, ou la roulette pendant le tirage ; <see cref="ItemKind.None"/> si rien.
        /// </summary>
        public static ItemKind Shown(ItemKind held, float rollLeft, float time) =>
            held == ItemKind.None ? ItemKind.None : rollLeft > 0f ? Reel(time) : held;

        /// <param name="rank">Place (0 = premier).</param>
        /// <param name="racers">Nombre de pilotes.</param>
        /// <param name="random01">Valeur aléatoire dans [0, 1).</param>
        public static ItemKind Pick(int rank, int racers, double random01)
        {
            float t = racers <= 1 ? 0f : Math.Max(0f, Math.Min(1f, rank / (float)(racers - 1)));
            // Seul en piste (contre-la-montre, ou premier) : pas de pelote, faute de cible.
            bool alone = racers <= 1 || rank <= 0;
            float total = 0f;
            var w = new float[Kinds.Length];
            for (int i = 0; i < w.Length; i++)
            {
                w[i] = Kinds[i] == ItemKind.Yarn && alone ? 0f : Front[i] + (Back[i] - Front[i]) * t;
                total += w[i];
            }
            float comet = CometWeight(rank, racers);
            double r = Math.Max(0.0, Math.Min(0.999999, random01)) * (total + comet);
            for (int i = 0; i < w.Length; i++)
            {
                if (r < w[i]) return Kinds[i];
                r -= w[i];
            }
            return comet > 0f ? ItemKind.Comet : Kinds[Kinds.Length - 1];
        }
    }
}
