using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Nom affiché du jeu, choisi par Kevin le 2026-11-21 : « Turbo Grid » (docs/design/da-arcade-sportif.md, § 9).
    /// Seul endroit à modifier : logo, nom de l'application (BuildScript) et menus de l'éditeur en dépendent.
    /// Le dépôt, les espaces de noms et l'identifiant Android gardent « turbopattes » (changer l'identifiant
    /// créerait une nouvelle application sur le store).
    /// </summary>
    public static class Brand
    {
        public const string Title = "Turbo Grid";
        public const string Tagline = "Championnat de karting arcade";

        /// <summary>
        /// Les deux lignes du logo de l'écran titre, en capitales : le titre coupé à l'espace le plus proche du milieu
        /// (un seul mot : tout sur la première ligne).
        /// </summary>
        public static string[] LogoLines(string title = Title)
        {
            string t = (title ?? "").Trim().ToUpperInvariant();
            int best = -1;
            for (int i = 0; i < t.Length; i++)
                if (t[i] == ' ' && (best < 0 || Math.Abs(i - t.Length / 2) < Math.Abs(best - t.Length / 2))) best = i;
            return best < 0 ? new[] { t, "" } : new[] { t.Substring(0, best).Trim(), t.Substring(best + 1).Trim() };
        }
    }

    /// <summary>
    /// Direction « arcade sportif » (docs/design/da-arcade-sportif.md) : fond carbone, blanc piste, rouge course pour
    /// l'action, jaune signal pour l'information, bleu télémétrie pour le froid, acier pour le métal du décor.
    /// </summary>
    public static class Palette
    {
        public static readonly Rgb Carbon = Rgb.Hex("#14171B");
        public static readonly Rgb Graphite = Rgb.Hex("#272C33");
        public static readonly Rgb Chalk = Rgb.Hex("#F2F4F6");
        public static readonly Rgb Red = Rgb.Hex("#C8211B");
        public static readonly Rgb Signal = Rgb.Hex("#F2C200");
        public static readonly Rgb Telemetry = Rgb.Hex("#1A62C0");
        public static readonly Rgb Steel = Rgb.Hex("#8A939E");

        /// <summary>Contraste minimal (WCAG 2.1) du texte courant, et des gros titres ou bandeaux (≥ 48 px à 720 p).</summary>
        public const float MinTextContrast = 4.5f;
        public const float MinLargeTextContrast = 3f;

        /// <summary>Un couple « texte sur fond » employé par le HUD ou les menus.</summary>
        public readonly struct TextPair
        {
            public readonly string Use;
            public readonly Rgb Text, Background;
            public readonly bool Large;

            public TextPair(string use, Rgb text, Rgb background, bool large = false)
            {
                Use = use;
                Text = text;
                Background = background;
                Large = large;
            }

            public float Contrast => Rgb.Contrast(Text, Background);
            public float Required => Large ? MinLargeTextContrast : MinTextContrast;
        }

        /// <summary>Tous les couples texte / fond réellement dessinés (testés : contraste suffisant).</summary>
        public static readonly TextPair[] TextPairs =
        {
            new("Chiffres et noms sur plaque carbone", Chalk, Carbon),
            new("Libellés TOUR / DEVANT / km/h sur plaque carbone", Signal, Carbon),
            new("Numéros du classement sur plaque carbone", Steel, Carbon),
            new("Texte sur fiche blanc piste", Carbon, Chalk),
            new("Texte sur bouton principal, ligne du joueur", Chalk, Red),
            new("Libellés rouges sur fiche (Meilleur tour, Total)", Red, Chalk),
            new("Fantôme, points gagnés sur fiche", Telemetry, Chalk),
            new("Écart favorable, bouton OBJET", Chalk, Telemetry),
            new("Écart et repères sur bandeau graphite", Chalk, Graphite),
            new("Libellés jaunes sur bandeau graphite", Signal, Graphite),
            new("Texte sur repère jaune (meilleur tour)", Carbon, Signal),
            new("Bandeau « Mauvais sens ! »", Red, Carbon, large: true),
            new("Compte à rebours", Signal, Carbon, large: true),
        };
    }

    /// <summary>Polices (Resources/Fonts) : Saira Condensed pour titres, chiffres et HUD, Saira pour les phrases (OFL).</summary>
    public static class Typography
    {
        public const string Bold = "Fonts/SairaCondensed-Bold";
        public const string SemiBold = "Fonts/SairaCondensed-SemiBold";
        public const string Regular = "Fonts/Saira-Regular";
    }
}
