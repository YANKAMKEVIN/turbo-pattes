using System;

namespace TurboPattes.Core
{
    /// <summary>Genre d'une ligne de réglage : jauge à crans, bascule à deux choix, liste de valeurs, ou bouton.</summary>
    public enum SettingKind { Gauge, Toggle, Choice, Action }

    /// <summary>
    /// Lignes de l'écran des réglages (DA « Atelier ») : regroupées en sections (Son, Commandes, Affichage),
    /// jauges dessinées en crans obliques, bascules en deux segments. Logique pure, sans Unity.
    /// </summary>
    public static class SettingsRows
    {
        public static readonly string[] Labels = { "Musique", "Effets", "Sensibilité tactile", "Commandes tactiles", "Vibrations", "Qualité graphique", "Retour" };
        public static readonly SettingKind[] Kinds =
            { SettingKind.Gauge, SettingKind.Gauge, SettingKind.Gauge, SettingKind.Toggle, SettingKind.Toggle, SettingKind.Choice, SettingKind.Action };
        /// <summary>Les deux choix des bascules (gauche, droite), null pour les autres lignes.</summary>
        public static readonly string[][] ToggleNames = { null, null, null, new[] { "Droitier", "Gaucher" }, new[] { "Oui", "Non" }, null, null };
        static readonly string[] Sections = { "Son", "Son", "Commandes", "Commandes", "Commandes", "Affichage", "" };

        /// <summary>Crans d'une jauge.</summary>
        public const int Notches = 10;

        public static int Count => Labels.Length;

        /// <summary>Nom de la section si la ligne <paramref name="row"/> en ouvre une, sinon null.</summary>
        public static string SectionStart(int row)
        {
            if (row < 0 || row >= Sections.Length || Sections[row].Length == 0) return null;
            return row == 0 || Sections[row - 1] != Sections[row] ? Sections[row] : null;
        }

        /// <summary>Pas réguliers d'un dixième, sans dérive des flottants (0,1 + 0,1 + 0,1…), bornés.</summary>
        public static float Step(float value, float delta, float min, float max) =>
            Math.Max(min, Math.Min(max, (float)Math.Round((value + delta) * 10f) / 10f));

        /// <summary>Nombre de crans allumés d'une jauge pour une valeur entre <paramref name="min"/> et <paramref name="max"/>.</summary>
        public static int Lit(float value, float min, float max, int notches = Notches)
        {
            if (max <= min) return 0;
            float t = (value - min) / (max - min);
            return Math.Max(0, Math.Min(notches, (int)Math.Round(t * notches)));
        }
    }
}
