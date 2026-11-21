namespace TurboPattes.Core
{
    /// <summary>
    /// Chiffres à sept segments pour les numéros de course peints sur les karts (dessinés par code, sans police).
    /// Segments dans l'ordre a (haut), b (haut droite), c (bas droite), d (bas), e (bas gauche), f (haut gauche), g (milieu).
    /// </summary>
    public static class SevenSegment
    {
        // Bits a..g de 0 à 6.
        static readonly int[] Digits = { 0b0111111, 0b0000110, 0b1011011, 0b1001111, 0b1100110, 0b1101101, 0b1111101, 0b0000111, 0b1111111, 0b1101111 };

        /// <summary>Segments allumés du chiffre (tableau de 7 booléens a..g) ; tout éteint si ce n'est pas un chiffre.</summary>
        public static bool[] Segments(char digit)
        {
            var on = new bool[7];
            if (digit < '0' || digit > '9') return on;
            int bits = Digits[digit - '0'];
            for (int i = 0; i < 7; i++) on[i] = (bits >> i & 1) == 1;
            return on;
        }

        /// <summary>
        /// Extrémités du segment <paramref name="index"/> dans une case de chiffre de largeur 1 et de hauteur 2
        /// (x vers la droite, y vers le bas) : (x0, y0, x1, y1).
        /// </summary>
        public static (float x0, float y0, float x1, float y1) Stroke(int index) => index switch
        {
            0 => (0f, 0f, 1f, 0f),
            1 => (1f, 0f, 1f, 1f),
            2 => (1f, 1f, 1f, 2f),
            3 => (0f, 2f, 1f, 2f),
            4 => (0f, 1f, 0f, 2f),
            5 => (0f, 0f, 0f, 1f),
            _ => (0f, 1f, 1f, 1f),
        };
    }
}
