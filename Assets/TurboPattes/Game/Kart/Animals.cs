using UnityEngine;

namespace TurboPattes.Game.Kart
{
    public enum AnimalKind { Fox, Panda, Frog, Rabbit, Bear, Cat, Penguin, Pig }

    /// <summary>Un pilote : son animal, ses couleurs et celles de son kart.</summary>
    public sealed class AnimalSpec
    {
        public string Name;
        public AnimalKind Kind;
        public Color Fur;
        public Color Accent;
        public Color Kart;
        public Color KartTrim;
        /// <summary>Numéro de course (plaques du HUD et du kart).</summary>
        public int Number;
        /// <summary>Devise affichée au choix du pilote.</summary>
        public string Motto;

        public static readonly AnimalSpec[] Roster =
        {
            new() { Name = "Roxy", Kind = AnimalKind.Fox, Fur = new Color(1f, 0.52f, 0.16f), Accent = Color.white,
                Kart = new Color(0.235f, 0.561f, 0.569f), KartTrim = new Color(0.851f, 0.302f, 0.247f),
                Number = 26, Motto = "Audacieuse et stratège" },
            new() { Name = "Bambou", Kind = AnimalKind.Panda, Fur = Color.white, Accent = new Color(0.12f, 0.12f, 0.14f),
                Kart = new Color(0.949f, 0.906f, 0.808f), KartTrim = new Color(0.141f, 0.188f, 0.176f),
                Number = 73, Motto = "Méthodique, fin technicien" },
            new() { Name = "Flip", Kind = AnimalKind.Frog, Fur = new Color(0.4f, 0.85f, 0.25f), Accent = new Color(0.95f, 0.95f, 0.6f),
                Kart = new Color(0.914f, 0.725f, 0.286f), KartTrim = new Color(0.235f, 0.561f, 0.569f),
                Number = 9, Motto = "Inventif et imprévisible" },
            new() { Name = "Lili", Kind = AnimalKind.Rabbit, Fur = new Color(0.98f, 0.92f, 0.95f), Accent = new Color(1f, 0.6f, 0.75f),
                Kart = new Color(0.561f, 0.651f, 0.478f), KartTrim = new Color(0.949f, 0.906f, 0.808f),
                Number = 22, Motto = "Explosive au freinage" },
            new() { Name = "Bruno", Kind = AnimalKind.Bear, Fur = new Color(0.55f, 0.35f, 0.2f), Accent = new Color(0.85f, 0.7f, 0.5f),
                Kart = new Color(0.788f, 0.561f, 0.180f), KartTrim = new Color(0.141f, 0.188f, 0.176f),
                Number = 7, Motto = "Tenace, ne lâche rien" },
            new() { Name = "Mimi", Kind = AnimalKind.Cat, Fur = new Color(0.6f, 0.58f, 0.68f), Accent = Color.white,
                Kart = new Color(0.420f, 0.306f, 0.443f), KartTrim = new Color(0.914f, 0.725f, 0.286f),
                Number = 14, Motto = "Précise, trajectoires propres" },
            new() { Name = "Pingo", Kind = AnimalKind.Penguin, Fur = new Color(0.12f, 0.14f, 0.2f), Accent = Color.white,
                Kart = new Color(0.369f, 0.478f, 0.549f), KartTrim = new Color(0.851f, 0.302f, 0.247f),
                Number = 31, Motto = "Calme sous pression" },
            new() { Name = "Rosa", Kind = AnimalKind.Pig, Fur = new Color(1f, 0.68f, 0.72f), Accent = new Color(0.95f, 0.5f, 0.55f),
                Kart = new Color(0.851f, 0.302f, 0.247f), KartTrim = new Color(0.949f, 0.906f, 0.808f),
                Number = 12, Motto = "Cheffe d'atelier, franche" },
        };
    }
}
