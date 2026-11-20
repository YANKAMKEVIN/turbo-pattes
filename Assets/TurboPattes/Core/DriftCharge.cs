using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Charge du dérapage : plus on tient le drift (et plus on braque vers l'intérieur), plus le
    /// turbo libéré au lâcher est long. Trois niveaux.
    /// </summary>
    public sealed class DriftCharge
    {
        /// <summary>Charge nécessaire pour atteindre les niveaux 1, 2 et 3.</summary>
        public static readonly float[] Thresholds = { 1.0f, 2.2f, 3.6f };
        /// <summary>Durée du turbo (s) pour les niveaux 0 à 3.</summary>
        public static readonly float[] BoostDurations = { 0f, 0.6f, 1.1f, 1.7f };

        public bool Active { get; private set; }
        /// <summary>-1 = gauche, +1 = droite, 0 hors drift.</summary>
        public int Direction { get; private set; }
        public float Charge { get; private set; }
        public float Duration { get; private set; }

        public int Level
        {
            get
            {
                for (int i = Thresholds.Length - 1; i >= 0; i--)
                    if (Charge >= Thresholds[i]) return i + 1;
                return 0;
            }
        }

        /// <summary>
        /// Remplissage (0 à 1) du segment <paramref name="segment"/> (0 à 2) de la jauge : le segment n se remplit entre
        /// les paliers n - 1 et n, pour montrer ce qui manque avant le niveau suivant.
        /// </summary>
        public static float SegmentFill(float charge, int segment)
        {
            if (segment < 0 || segment >= Thresholds.Length) return 0f;
            float from = segment == 0 ? 0f : Thresholds[segment - 1], to = Thresholds[segment];
            return Math.Max(0f, Math.Min(1f, (charge - from) / (to - from)));
        }

        public void Start(int direction)
        {
            if (direction == 0) throw new ArgumentException("Direction nulle", nameof(direction));
            Active = true;
            Direction = Math.Sign(direction);
            Charge = 0f;
            Duration = 0f;
        }

        /// <param name="steerIntoDrift">-1 (braque vers l'extérieur) à +1 (vers l'intérieur).</param>
        public void Tick(float dt, float steerIntoDrift)
        {
            if (!Active) return;
            float into = Math.Max(0f, Math.Min(1f, (steerIntoDrift + 1f) * 0.5f));
            Charge += dt * (0.6f + 0.8f * into);
            Duration += dt;
        }

        /// <summary>Termine le drift et renvoie la durée du turbo gagné.</summary>
        public float Release()
        {
            if (!Active) return 0f;
            float boost = BoostDurations[Level];
            Cancel();
            return boost;
        }

        public void Cancel()
        {
            Active = false;
            Direction = 0;
            Charge = 0f;
            Duration = 0f;
        }
    }
}
