using System;
using System.Collections.Generic;

namespace TurboPattes.Core
{
    /// <summary>
    /// Horloge de passage d'un kart : le temps de course où il a atteint chaque repère de distance (tous les
    /// <see cref="Step"/> mètres de progression). Sert à mesurer les écarts en secondes, comme au chronométrage :
    /// l'écart avec celui de devant est le temps écoulé depuis qu'il est passé là où l'on est.
    /// </summary>
    public sealed class ProgressClock
    {
        public float Step { get; }
        /// <summary>Progression du premier repère (les karts partent un peu derrière la ligne).</summary>
        public float Origin { get; }

        readonly List<float> _times = new();

        public ProgressClock(float step = 5f, float origin = -100f)
        {
            if (step <= 0f) throw new ArgumentOutOfRangeException(nameof(step));
            Step = step;
            Origin = origin;
        }

        /// <summary>Progression la plus loin enregistrée (repère compris).</summary>
        public float Reached => Origin + (_times.Count - 1) * Step;

        /// <summary>Note la position du kart ; seul le premier passage à chaque repère compte (un recul n'efface rien).</summary>
        public void Record(float progress, float time)
        {
            while (Origin + _times.Count * Step <= progress) _times.Add(time);
        }

        /// <summary>Temps où le kart a atteint cette progression (interpolé entre deux repères).</summary>
        public bool TimeAt(float progress, out float time)
        {
            time = 0f;
            float k = (progress - Origin) / Step;
            if (k < 0f || _times.Count == 0) return false;
            int i = (int)Math.Floor(k);
            if (i >= _times.Count - 1)
            {
                if (i == _times.Count - 1 && Math.Abs(k - i) < 1e-4f) { time = _times[i]; return true; }
                return false;
            }
            float f = k - i;
            time = _times[i] + (_times[i + 1] - _times[i]) * f;
            return true;
        }
    }

    /// <summary>Écarts utiles du HUD : pilote juste devant et juste derrière, en secondes.</summary>
    public static class RaceGaps
    {
        /// <summary>Retard sur le kart de devant : temps écoulé depuis qu'il est passé à notre position.</summary>
        public static bool Ahead(ProgressClock ahead, float myProgress, float now, out float seconds)
        {
            seconds = 0f;
            if (!ahead.TimeAt(myProgress, out float t)) return false;
            seconds = Math.Max(0f, now - t);
            return true;
        }

        /// <summary>Avance sur le kart de derrière : temps écoulé depuis qu'on est passé à sa position.</summary>
        public static bool Behind(ProgressClock mine, float behindProgress, float now, out float seconds)
        {
            seconds = 0f;
            if (!mine.TimeAt(behindProgress, out float t)) return false;
            seconds = Math.Max(0f, now - t);
            return true;
        }

        /// <summary>
        /// Tendance d'un écart d'une mesure à l'autre : -1 il se réduit, +1 il grandit, 0 stable
        /// (variation sous <paramref name="threshold"/> secondes).
        /// </summary>
        public static int Trend(float before, float now, float threshold = 0.05f) =>
            now < before - threshold ? -1 : now > before + threshold ? 1 : 0;
    }
}
