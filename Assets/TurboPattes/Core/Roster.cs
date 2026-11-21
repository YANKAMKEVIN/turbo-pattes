namespace TurboPattes.Core
{
    /// <summary>
    /// Identité d'un pilote humain (direction « arcade sportif ») : nom, code de chronométrage, numéro de course,
    /// caractère affiché et couleurs d'équipe (casque, combinaison, kart). Le caractère de jeu reste dans
    /// <see cref="Drivers.Stats"/>, au même index.
    /// </summary>
    public sealed class DriverProfile
    {
        public string FirstName;
        public string LastName;
        /// <summary>Code à trois lettres du classement compact (tour de chronométrage).</summary>
        public string Code;
        /// <summary>Numéro de course, peint sur le kart.</summary>
        public int Number;
        /// <summary>Caractère en quelques mots, sous le nom dans le choix du pilote.</summary>
        public string Trait;
        /// <summary>Couleur principale : casque, carénage du kart.</summary>
        public Rgb Primary;
        /// <summary>Couleur secondaire : bande du casque, liseré, jantes.</summary>
        public Rgb Secondary;
        /// <summary>Combinaison : la couleur principale, ou carbone quand elle est trop claire pour une combinaison.</summary>
        public Rgb Suit;

        public string FullName => FirstName + " " + LastName;
    }

    /// <summary>Les huit pilotes, dans l'ordre de la grille de choix (même ordre que <see cref="Drivers.Stats"/>).</summary>
    public static class Roster
    {
        public static readonly DriverProfile[] All =
        {
            // Ancien Roxy : équilibré.
            new() { FirstName = "Alex", LastName = "Moreau", Code = "MOR", Number = 7, Trait = "Polyvalent",
                Primary = Rgb.Hex("#C8211B"), Secondary = Rgb.Hex("#F2F4F6"), Suit = Rgb.Hex("#C8211B") },
            // Ancien Bambou : lourd, inébranlable.
            new() { FirstName = "Viktor", LastName = "Brandt", Code = "BRA", Number = 12, Trait = "Lourd, inébranlable",
                Primary = Rgb.Hex("#1E7A46"), Secondary = Rgb.Hex("#F2C200"), Suit = Rgb.Hex("#1E7A46") },
            // Ancien Flip : agile.
            new() { FirstName = "Lina", LastName = "Okafor", Code = "OKA", Number = 23, Trait = "Agile",
                Primary = Rgb.Hex("#F2C200"), Secondary = Rgb.Hex("#14171B"), Suit = Rgb.Hex("#14171B") },
            // Ancien Lili : démarrages fulgurants.
            new() { FirstName = "Sami", LastName = "Haddad", Code = "HAD", Number = 31, Trait = "Démarrages fulgurants",
                Primary = Rgb.Hex("#E5711B"), Secondary = Rgb.Hex("#14171B"), Suit = Rgb.Hex("#E5711B") },
            // Ancien Bruno : pointe et poids, lent à lancer.
            new() { FirstName = "Teo", LastName = "Ferraz", Code = "FER", Number = 5, Trait = "Pointe et poids",
                Primary = Rgb.Hex("#1A62C0"), Secondary = Rgb.Hex("#F2F4F6"), Suit = Rgb.Hex("#1A62C0") },
            // Ancien Mimi : précise.
            new() { FirstName = "Hana", LastName = "Mori", Code = "MRI", Number = 18, Trait = "Précise",
                Primary = Rgb.Hex("#E9ECEF"), Secondary = Rgb.Hex("#C8211B"), Suit = Rgb.Hex("#272C33") },
            // Ancien Pingo : glisse vite.
            new() { FirstName = "Nora", LastName = "Lindqvist", Code = "LIN", Number = 27, Trait = "Glisse vite",
                Primary = Rgb.Hex("#1497A8"), Secondary = Rgb.Hex("#F2F4F6"), Suit = Rgb.Hex("#1497A8") },
            // Ancien Rosa : solide.
            new() { FirstName = "Inès", LastName = "Duval", Code = "DUV", Number = 64, Trait = "Solide",
                Primary = Rgb.Hex("#5B3A9C"), Secondary = Rgb.Hex("#F2C200"), Suit = Rgb.Hex("#5B3A9C") },
        };

        public static DriverProfile For(int index) => index >= 0 && index < All.Length ? All[index] : All[0];
    }
}
