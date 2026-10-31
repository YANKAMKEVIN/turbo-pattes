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

        public static readonly AnimalSpec[] Roster =
        {
            new() { Name = "Roxy", Kind = AnimalKind.Fox, Fur = new Color(1f, 0.52f, 0.16f), Accent = Color.white,
                Kart = new Color(0.15f, 0.65f, 1f), KartTrim = new Color(1f, 0.85f, 0.25f) },
            new() { Name = "Bambou", Kind = AnimalKind.Panda, Fur = Color.white, Accent = new Color(0.12f, 0.12f, 0.14f),
                Kart = new Color(0.35f, 0.85f, 0.35f), KartTrim = Color.white },
            new() { Name = "Flip", Kind = AnimalKind.Frog, Fur = new Color(0.4f, 0.85f, 0.25f), Accent = new Color(0.95f, 0.95f, 0.6f),
                Kart = new Color(1f, 0.75f, 0.1f), KartTrim = new Color(0.9f, 0.2f, 0.2f) },
            new() { Name = "Lili", Kind = AnimalKind.Rabbit, Fur = new Color(0.98f, 0.92f, 0.95f), Accent = new Color(1f, 0.6f, 0.75f),
                Kart = new Color(1f, 0.45f, 0.7f), KartTrim = Color.white },
            new() { Name = "Bruno", Kind = AnimalKind.Bear, Fur = new Color(0.55f, 0.35f, 0.2f), Accent = new Color(0.85f, 0.7f, 0.5f),
                Kart = new Color(0.9f, 0.2f, 0.18f), KartTrim = new Color(0.2f, 0.2f, 0.25f) },
            new() { Name = "Mimi", Kind = AnimalKind.Cat, Fur = new Color(0.6f, 0.58f, 0.68f), Accent = Color.white,
                Kart = new Color(0.6f, 0.35f, 0.95f), KartTrim = new Color(0.3f, 1f, 0.9f) },
            new() { Name = "Pingo", Kind = AnimalKind.Penguin, Fur = new Color(0.12f, 0.14f, 0.2f), Accent = Color.white,
                Kart = new Color(0.2f, 0.85f, 0.9f), KartTrim = new Color(1f, 0.55f, 0.1f) },
            new() { Name = "Rosa", Kind = AnimalKind.Pig, Fur = new Color(1f, 0.68f, 0.72f), Accent = new Color(0.95f, 0.5f, 0.55f),
                Kart = new Color(0.98f, 0.98f, 0.98f), KartTrim = new Color(1f, 0.4f, 0.55f) },
        };
    }
}
