using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Arrivée du logo sur l'écran titre : il glisse depuis la gauche, dépasse un peu sa place puis s'y pose,
    /// comme un trait de peinture rapide.
    /// </summary>
    public static class LogoIntro
    {
        public const float Duration = 0.7f;
        /// <summary>Pente des lettres du logo (décalage horizontal par pixel de hauteur).</summary>
        public const float Slant = 0.22f;

        /// <summary>Décalage restant, en fraction de la largeur de l'écran : 1 hors champ à gauche, 0 en place (négatif = léger dépassement).</summary>
        public static float Offset(float time)
        {
            float x = Math.Max(0f, Math.Min(1f, time / Duration));
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = x - 1f;
            float eased = 1f + c3 * u * u * u + c1 * u * u; // sortie avec rebond (easeOutBack)
            return 1f - eased;
        }

        /// <summary>Opacité : le logo apparaît sur le premier tiers du glissé.</summary>
        public static float Alpha(float time) => Math.Max(0f, Math.Min(1f, time / (Duration * 0.35f)));
    }
}
