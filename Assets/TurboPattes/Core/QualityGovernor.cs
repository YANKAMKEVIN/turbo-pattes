using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Décide du niveau de qualité (0 = le plus beau, <see cref="MaxLevel"/> = le plus léger) d'après la fluidité mesurée
    /// par fenêtres de quelques secondes. Baisse d'un cran dès qu'une fenêtre passe sous <see cref="LowFps"/> ;
    /// remonte d'un cran après <see cref="WindowsToRaise"/> fenêtres d'affilée au-dessus de <see cref="HighFps"/>,
    /// sauf vers un niveau qui a déjà dû être quitté <see cref="MaxFailures"/> fois (pas de va-et-vient).
    /// </summary>
    public sealed class QualityGovernor
    {
        public const float LowFps = 45f;
        public const float HighFps = 57f;
        public const int WindowsToRaise = 3;
        public const int MaxFailures = 2;

        readonly int[] _failures;
        int _good;

        public int MaxLevel { get; }
        public int Level { get; private set; }

        public QualityGovernor(int level, int maxLevel)
        {
            if (maxLevel < 0) throw new ArgumentOutOfRangeException(nameof(maxLevel));
            MaxLevel = maxLevel;
            Level = Math.Max(0, Math.Min(maxLevel, level));
            _failures = new int[maxLevel + 1];
        }

        /// <summary>Nombre de fois où le niveau a été abandonné faute de fluidité.</summary>
        public int Failures(int level) => _failures[level];

        /// <summary>Une fenêtre mesurée : renvoie -1 (plus beau), +1 (plus léger) ou 0.</summary>
        public int Feed(float fps)
        {
            if (fps < LowFps)
            {
                _good = 0;
                if (Level >= MaxLevel) return 0;
                _failures[Level]++;
                Level++;
                return 1;
            }
            if (fps < HighFps || Level == 0)
            {
                _good = 0;
                return 0;
            }
            if (++_good < WindowsToRaise) return 0;
            _good = 0;
            if (_failures[Level - 1] >= MaxFailures) return 0;
            Level--;
            return -1;
        }
    }
}
