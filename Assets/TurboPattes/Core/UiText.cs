using System;
using System.Globalization;

namespace TurboPattes.Core
{
    /// <summary>Couleur RVB (0 à 1), lue depuis une notation « #RRGGBB ».</summary>
    public readonly struct Rgb
    {
        public readonly float R, G, B;

        public Rgb(float r, float g, float b)
        {
            R = r;
            G = g;
            B = b;
        }

        public static Rgb Hex(string hex)
        {
            if (hex == null) throw new ArgumentNullException(nameof(hex));
            string h = hex.StartsWith("#") ? hex.Substring(1) : hex;
            if (h.Length != 6) throw new FormatException($"Couleur attendue au format #RRGGBB : {hex}");
            int v = int.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Rgb(((v >> 16) & 0xff) / 255f, ((v >> 8) & 0xff) / 255f, (v & 0xff) / 255f);
        }

        /// <summary>Luminance relative (WCAG 2.1) : composantes sRVB ramenées en linéaire puis pondérées.</summary>
        public float Luminance => 0.2126f * Lin(R) + 0.7152f * Lin(G) + 0.0722f * Lin(B);

        static float Lin(float c) => c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);

        /// <summary>Rapport de contraste WCAG entre deux couleurs opaques (1 à 21).</summary>
        public static float Contrast(Rgb a, Rgb b)
        {
            float la = a.Luminance, lb = b.Luminance;
            return (Math.Max(la, lb) + 0.05f) / (Math.Min(la, lb) + 0.05f);
        }
    }

    /// <summary>
    /// Chiffres à largeur fixe pour le chrono, la vitesse et les écarts : Saira Condensed a des chiffres proportionnels
    /// (le « 1 » est étroit), donc chaque caractère est posé dans une case de largeur fixe, en fraction de la
    /// taille de police. Le texte ne bouge plus quand les chiffres défilent.
    /// </summary>
    public static class FixedDigits
    {
        /// <summary>Case d'un chiffre : un peu plus large que le chiffre le plus large de Saira Condensed Bold (0,49 em).</summary>
        public const float Digit = 0.5f;
        /// <summary>Case des séparateurs « : », « , » et « . ».</summary>
        public const float Separator = 0.25f;
        public const float Space = 0.19f;
        public const float Sign = 0.48f;
        /// <summary>Lettres (« km/h », « e ») : case moyenne, juste pour que le total tombe à peu près juste.</summary>
        public const float Other = 0.45f;

        public static float Cell(char c)
        {
            if (c >= '0' && c <= '9') return Digit;
            switch (c)
            {
                case ':': case ',': case '.': return Separator;
                case ' ': return Space;
                case '+': case '-': return Sign;
                default: return Other;
            }
        }

        /// <summary>Largeur totale du texte, en fraction de la taille de police.</summary>
        public static float Width(string text)
        {
            float w = 0f;
            if (text == null) return w;
            foreach (char c in text) w += Cell(c);
            return w;
        }
    }

    /// <summary>Textes de course : temps, places, écarts (format français, virgule décimale).</summary>
    public static class RaceText
    {
        /// <summary>« 1:05,32 » : minutes, secondes et centièmes, arrondis au centième sans « 0:60,00 ».</summary>
        public static string Time(float seconds)
        {
            if (seconds < 0f || float.IsNaN(seconds)) seconds = 0f;
            long cs = (long)Math.Round(seconds * 100.0, MidpointRounding.AwayFromZero);
            long m = cs / 6000, s = cs / 100 % 60, c = cs % 100;
            return $"{m}:{s:00},{c:00}";
        }

        /// <summary>Durée positive en secondes et centièmes : « 1,24 ».</summary>
        public static string Seconds(float seconds)
        {
            long cs = (long)Math.Round(Math.Max(0f, seconds) * 100.0, MidpointRounding.AwayFromZero);
            return $"{cs / 100},{cs % 100:00}";
        }

        /// <summary>« 1er », « 2e »…</summary>
        public static string Ordinal(int rank) => rank == 1 ? "1er" : rank + "e";

        /// <summary>Écart signé en secondes : « -0,42 », « +1,80 ».</summary>
        public static string Gap(float seconds)
        {
            long cs = (long)Math.Round(Math.Abs(seconds) * 100.0, MidpointRounding.AwayFromZero);
            string sign = seconds < 0f && cs > 0 ? "-" : "+";
            return $"{sign}{cs / 100},{cs % 100:00}";
        }
    }
}
