using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Écran de marque « Turbo Grid », décidé par Kevin le 2026-11-21 : au lancement, le splash Unity (gardé, réglé dans
    /// ProjectSettings sur fond carbone), puis ce court écran avec le logo du titre et l'accroche, puis l'écran titre.
    /// Montré une seule fois par lancement, jamais pendant les autotests. Un toucher ou une touche passe directement au fondu.
    /// </summary>
    public static class BrandSplash
    {
        /// <summary>Apparition du logo (fondu et léger zoom arrière), en secondes.</summary>
        public const float FadeIn = 0.3f;
        /// <summary>Logo affiché seul.</summary>
        public const float Hold = 1.1f;
        /// <summary>Fondu final : le fond carbone s'efface et laisse voir le survol du circuit de l'écran titre.</summary>
        public const float FadeOut = 0.4f;
        public const float Duration = FadeIn + Hold + FadeOut;
        public const float FadeOutStart = FadeIn + Hold;
        /// <summary>Avant ce délai, un toucher est ignoré (évite qu'un appui resté du splash Unity ne saute tout).</summary>
        public const float MinShown = 0.4f;
        /// <summary>Taille du logo au début de l'apparition (1 = taille de l'écran titre).</summary>
        public const float StartScale = 1.08f;
        /// <summary>Durée du trait rouge qui file sous l'accroche.</summary>
        public const float SweepDuration = 0.5f;

        /// <summary>Montré au premier passage par le menu d'un lancement, sauf en test automatique.</summary>
        public static bool ShouldShow(bool alreadyShown, bool automated) => !alreadyShown && !automated;

        /// <summary>Opacité du logo et de l'accroche : monte, tient, puis s'éteint avec le fond.</summary>
        public static float LogoAlpha(float t)
        {
            if (t <= 0f || Done(t)) return 0f;
            if (t < FadeIn) return t / FadeIn;
            return 1f - Clamp01((t - FadeOutStart) / FadeOut);
        }

        /// <summary>Opacité du fond carbone : plein jusqu'au fondu final.</summary>
        public static float BackdropAlpha(float t) => Done(t) ? 0f : 1f - Clamp01((t - FadeOutStart) / FadeOut);

        /// <summary>Échelle du logo : de <see cref="StartScale"/> à 1 pendant l'apparition (sortie douce, cubique).</summary>
        public static float LogoScale(float t) => 1f + (StartScale - 1f) * Cube(1f - Clamp01(t / FadeIn));

        /// <summary>Avancée (0 à 1) du trait rouge sous l'accroche, sortie douce.</summary>
        public static float Sweep(float t) => 1f - Cube(1f - Clamp01(t / SweepDuration));

        public static bool Done(float t) => t >= Duration;

        /// <summary>
        /// Temps écoulé après un appui pour passer : saute au début du fondu final, sans revenir en arrière s'il a déjà
        /// commencé ; ignoré pendant les <see cref="MinShown"/> premières secondes.
        /// </summary>
        public static float Skip(float t) => t < MinShown ? t : Math.Max(t, FadeOutStart);

        static float Clamp01(float x) => Math.Max(0f, Math.Min(1f, x));
        static float Cube(float x) => x * x * x;
    }
}
