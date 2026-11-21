using System;

namespace TurboPattes.Core
{
    /// <summary>Mouvements du décor animé, calculés à partir du temps (aucun état) : lave qui palpite…</summary>
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
    }
}
