using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Compte les tours à partir de la distance parcourue le long du circuit (0 = ligne d'arrivée).
    /// Un kart placé derrière la ligne au départ commence au tour -1 : il passe au tour 0 (« Tour 1 »)
    /// en franchissant la ligne. Reculer à travers la ligne retire un tour.
    /// </summary>
    public sealed class LapTracker
    {
        public float TrackLength { get; }
        /// <summary>Nombre de passages de la ligne (-1 avant le premier).</summary>
        public int Lap { get; private set; }
        /// <summary>Distance le long du circuit, entre 0 et <see cref="TrackLength"/>.</summary>
        public float S { get; private set; }

        public LapTracker(float trackLength, float startS)
        {
            if (trackLength <= 0f) throw new ArgumentOutOfRangeException(nameof(trackLength));
            TrackLength = trackLength;
            S = startS;
            Lap = startS > trackLength * 0.5f ? -1 : 0;
        }

        /// <summary>Distance totale parcourue depuis la ligne : sert au classement.</summary>
        public float Progress => Lap * TrackLength + S;

        public void Update(float s)
        {
            float ds = s - S;
            if (ds < -TrackLength * 0.5f) Lap++;
            else if (ds > TrackLength * 0.5f) Lap--;
            S = s;
        }

        /// <summary>Tour affiché (1 à <paramref name="laps"/>).</summary>
        public int DisplayLap(int laps) => Math.Max(1, Math.Min(laps, Lap + 1));

        public bool HasFinished(int laps) => Lap >= laps;
    }
}
