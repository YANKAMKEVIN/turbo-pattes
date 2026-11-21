using System.Collections.Generic;

namespace TurboPattes.Core
{
    /// <summary>
    /// Police pixel 5 × 7 (capitales, chiffres, tiret) pour écrire les panneaux sponsors fictifs dans des textures
    /// générées par code, sans fichier de police. Les accents sont rabattus sur la lettre de base.
    /// </summary>
    public static class PixelFont
    {
        public const int Width = 5, Height = 7;
        /// <summary>Colonnes vides entre deux lettres.</summary>
        public const int Spacing = 1;

        static readonly Dictionary<char, string[]> Glyphs = new()
        {
            { 'A', new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
            { 'B', new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." } },
            { 'C', new[] { ".####", "#....", "#....", "#....", "#....", "#....", ".####" } },
            { 'D', new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." } },
            { 'E', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" } },
            { 'F', new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." } },
            { 'G', new[] { ".####", "#....", "#....", "#.###", "#...#", "#...#", ".###." } },
            { 'H', new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" } },
            { 'I', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" } },
            { 'J', new[] { "..###", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." } },
            { 'K', new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" } },
            { 'L', new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" } },
            { 'M', new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" } },
            { 'N', new[] { "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#", "#...#" } },
            { 'O', new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
            { 'P', new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." } },
            { 'Q', new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" } },
            { 'R', new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" } },
            { 'S', new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." } },
            { 'T', new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." } },
            { 'U', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." } },
            { 'V', new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." } },
            { 'W', new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "#...#" } },
            { 'X', new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" } },
            { 'Y', new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." } },
            { 'Z', new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" } },
            { '0', new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." } },
            { '1', new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." } },
            { '2', new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" } },
            { '3', new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." } },
            { '4', new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." } },
            { '5', new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." } },
            { '6', new[] { ".###.", "#....", "#....", "####.", "#...#", "#...#", ".###." } },
            { '7', new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." } },
            { '8', new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." } },
            { '9', new[] { ".###.", "#...#", "#...#", ".####", "....#", "....#", ".###." } },
            { '-', new[] { ".....", ".....", ".....", "#####", ".....", ".....", "....." } },
        };

        /// <summary>Lettre rendue (capitale sans accent) ; espace pour un caractère inconnu.</summary>
        public static char Normalize(char c)
        {
            c = char.ToUpperInvariant(c);
            switch (c)
            {
                case 'É': case 'È': case 'Ê': case 'Ë': return 'E';
                case 'À': case 'Â': case 'Ä': return 'A';
                case 'Î': case 'Ï': return 'I';
                case 'Ô': case 'Ö': return 'O';
                case 'Ù': case 'Û': case 'Ü': return 'U';
                case 'Ç': return 'C';
            }
            return Glyphs.ContainsKey(c) ? c : ' ';
        }

        /// <summary>Pixel (x, y) de la lettre, y = 0 en haut ; faux hors de la case ou pour un espace.</summary>
        public static bool Pixel(char c, int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return false;
            return Glyphs.TryGetValue(Normalize(c), out var rows) && rows[y][x] == '#';
        }

        /// <summary>Largeur du texte en pixels (lettres et espacements, sans marge).</summary>
        public static int TextWidth(string text) =>
            string.IsNullOrEmpty(text) ? 0 : text.Length * (Width + Spacing) - Spacing;

        /// <summary>Pixel (x, y) du texte entier posé à partir de (0, 0).</summary>
        public static bool TextPixel(string text, int x, int y)
        {
            if (string.IsNullOrEmpty(text) || x < 0) return false;
            int cell = Width + Spacing, i = x / cell;
            return i < text.Length && Pixel(text[i], x % cell, y);
        }
    }

    /// <summary>Sponsors fictifs des panneaux du circuit (inventés, sans logo réel ; à vérifier avant publication).</summary>
    public static class Sponsors
    {
        public static readonly string[] Names = { "VELTRA", "KORVEX", "ORLAN", "TAVIK", "MIRALTO" };
    }
}
