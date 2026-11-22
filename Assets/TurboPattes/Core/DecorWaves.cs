using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Mouvements du décor animé, calculés à partir du temps (aucun état) : lave qui palpite, houle du lagon,
    /// nuages qui dérivent.
    /// </summary>
    public static class DecorWaves
    {
        /// <summary>
        /// Intensité de la lueur de la lave (multiplie son émission) : une lente respiration de 2,6 s et un frémissement
        /// plus rapide. Toujours entre 0,65 et 1,35, moyenne 1.
        /// </summary>
        public static float LavaGlow(float time)
        {
            double t = time;
            return (float)(1.0 + 0.25 * Math.Sin(2.0 * Math.PI * t / 2.6) + 0.1 * Math.Sin(2.0 * Math.PI * t / 0.9 + 1.3));
        }

        // ------------------------------------------------------------------ houle du lagon

        /// <summary>
        /// Trois trains de houle (direction, longueur d'onde en m, période en s, amplitude en m, phase). La plus courte
        /// longueur d'onde (24 m) vaut quatre mailles de <see cref="WaterCell"/> : pas de crénelage.
        /// </summary>
        static readonly (double dx, double dz, double length, double period, double amp, double phase)[] Swells =
        {
            (0.8, 0.6, 62.0, 7.0, 0.09, 0.0),
            (-0.5, 0.866, 38.0, 5.0, 0.06, 1.7),
            (0.28, -0.96, 24.0, 3.4, 0.03, 4.2),
        };

        /// <summary>Somme des amplitudes : l'eau ne monte ni ne descend jamais de plus de 0,18 m.</summary>
        public const float WaterAmplitude = 0.18f;

        /// <summary>Taille d'une maille du plan d'eau animé (m).</summary>
        public const float WaterCell = 6f;

        /// <summary>Largeur de la bordure où la houle s'éteint, au bord du plan d'eau animé (m).</summary>
        public const float WaterFade = 60f;

        /// <summary>Hauteur de l'eau autour de son niveau de repos au point (x, z) et à l'instant <paramref name="time"/>.</summary>
        public static float WaterHeight(float x, float z, float time)
        {
            double h = 0.0;
            foreach (var w in Swells) h += w.amp * Math.Sin(Phase(w, x, z, time));
            return (float)h;
        }

        /// <summary>Pente de l'eau (dh/dx, dh/dz) : donne la normale (-dx ; 1 ; -dz) sans recalculer le maillage.</summary>
        public static (float dx, float dz) WaterSlope(float x, float z, float time)
        {
            double sx = 0.0, sz = 0.0;
            foreach (var w in Swells)
            {
                double k = 2.0 * Math.PI / w.length;
                double c = w.amp * k * Math.Cos(Phase(w, x, z, time));
                sx += c * w.dx;
                sz += c * w.dz;
            }
            return ((float)sx, (float)sz);
        }

        static double Phase((double dx, double dz, double length, double period, double amp, double phase) w, float x, float z, float time) =>
            2.0 * Math.PI * ((w.dx * x + w.dz * z) / w.length - time / w.period) + w.phase;

        /// <summary>
        /// Part de la houle gardée à <paramref name="edgeDistance"/> mètres du bord du plan d'eau animé : 0 au bord,
        /// 1 au-delà de <see cref="WaterFade"/>, transition douce. Au bord, le plan animé rejoint son niveau de repos.
        /// </summary>
        public static float WaterEdge(float edgeDistance)
        {
            float u = Math.Max(0f, Math.Min(1f, edgeDistance / WaterFade));
            return u * u * (3f - 2f * u);
        }

        // ------------------------------------------------------------------ nuages qui dérivent

        /// <summary>Période d'un aller-retour d'un nuage poussé par le vent (s).</summary>
        public const float CloudPeriod = 80f;

        /// <summary>Plus grande dérive d'un nuage de part et d'autre de sa place (m).</summary>
        public const float CloudReach = 10f;

        /// <summary>
        /// Distance à la ligne médiane en deçà de laquelle un groupe de nuages ne doit jamais venir (m) : demi-largeur
        /// de la piste et des murets, plus le rayon d'un groupe (15 m) et une marge.
        /// </summary>
        public const float CloudClearance = 25f;

        /// <summary>
        /// Dérive permise à un groupe de nuages posé à <paramref name="distanceToTrack"/> mètres de la piste : jamais
        /// plus près que <see cref="CloudClearance"/> (un nuage déjà plus près ne bouge pas), au plus <see cref="CloudReach"/>.
        /// </summary>
        public static float CloudDriftReach(float distanceToTrack) =>
            Math.Max(0f, Math.Min(CloudReach, distanceToTrack - CloudClearance));

        /// <summary>Position du nuage dans son aller-retour, entre -1 et 1 (lent, déphasé d'un groupe à l'autre).</summary>
        public static float CloudDrift(float time, float phase) =>
            (float)Math.Sin(2.0 * Math.PI * time / CloudPeriod + phase);

        /// <summary>Lent flottement vertical d'un groupe de nuages (m), au plus 0,6 m.</summary>
        public static float CloudBob(float time, float phase) =>
            (float)(0.6 * Math.Sin(2.0 * Math.PI * time / 11.0 + phase * 1.7));
    }
}
