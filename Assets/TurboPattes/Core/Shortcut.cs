using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Raccourci risqué à l'intérieur d'un virage : le muret intérieur recule de <see cref="Extra"/> mètres et laisse
    /// une bande de sable, plus courte que la piste mais plus lente que l'herbe. Avec un turbo (qui annule le
    /// ralentissement hors piste), la coupe devient payante. Un petit tremplin posé dans le sable permet d'en
    /// survoler une partie : en l'air, le sable ne freine pas.
    /// Distances le long du tour ; <see cref="Side"/> = côté du virage (-1 à gauche, +1 à droite).
    /// </summary>
    public sealed class Shortcut
    {
        /// <summary>Vitesse de pointe dans le sable (fraction de la vitesse normale ; l'herbe est à 0,55).</summary>
        public const float SandFactor = 0.5f;
        /// <summary>Longueur sur laquelle le muret recule ou revient, de part et d'autre de la zone.</summary>
        public const float Fade = 25f;

        public readonly float CenterS, HalfLength, Extra, TrackLength;
        public readonly int Side;

        /// <summary>Tremplin dans le sable : bord, longueur de rampe, hauteur, et bande latérale (distances au centre, côté <see cref="Side"/>).</summary>
        public readonly float KickLipS, KickLength, KickRise, KickInner, KickOuter;
        public bool HasKicker => KickRise > 0f && KickLength > 0f;

        public Shortcut(float centerS, float halfLength, int side, float extra, float trackLength,
            float kickLipS = 0f, float kickLength = 0f, float kickRise = 0f, float kickInner = 0f, float kickOuter = 0f)
        {
            if (trackLength <= 0f) throw new ArgumentOutOfRangeException(nameof(trackLength));
            if (halfLength <= 0f) throw new ArgumentOutOfRangeException(nameof(halfLength));
            if (side != -1 && side != 1) throw new ArgumentOutOfRangeException(nameof(side));
            if (extra <= 0f) throw new ArgumentOutOfRangeException(nameof(extra));
            if (kickOuter < kickInner) throw new ArgumentException("bande du tremplin inversée");
            TrackLength = trackLength;
            CenterS = Wrap(centerS);
            HalfLength = halfLength;
            Side = side;
            Extra = extra;
            KickLipS = Wrap(kickLipS);
            KickLength = kickLength;
            KickRise = kickRise;
            KickInner = kickInner;
            KickOuter = kickOuter;
        }

        float Wrap(float s)
        {
            s %= TrackLength;
            return s < 0f ? s + TrackLength : s;
        }

        /// <summary>Écart signé (le plus court, en tenant compte du bouclage) entre <paramref name="s"/> et le centre.</summary>
        float FromCenter(float s)
        {
            float d = Wrap(s - CenterS);
            return d > TrackLength * 0.5f ? d - TrackLength : d;
        }

        /// <summary>Part du recul du muret à cette distance : 1 dans la zone, 0 loin, transition douce sur <see cref="Fade"/>.</summary>
        public float Amount(float s)
        {
            float d = Math.Abs(FromCenter(s));
            if (d <= HalfLength) return 1f;
            if (d >= HalfLength + Fade) return 0f;
            float u = 1f - (d - HalfLength) / Fade;
            return u * u * (3f - 2f * u);
        }

        /// <summary>Distance du muret à la ligne médiane, du côté <paramref name="side"/>.</summary>
        public float BarrierOffset(float s, int side, float baseOffset) => side == Side ? baseOffset + Extra * Amount(s) : baseOffset;

        /// <summary>Le point (<paramref name="s"/>, <paramref name="lateral"/>) est-il dans le sable (au-delà du bord <paramref name="roadEdge"/>) ?</summary>
        public bool InSand(float s, float lateral, float roadEdge) =>
            Math.Sign(lateral) == Side && Math.Abs(lateral) > roadEdge && Amount(s) > 0f;

        /// <summary>Surélévation du tremplin du sable (même arc que les tremplins de piste) et sa pente dh/ds.</summary>
        public float Kick(float s, float lateral, out float slope)
        {
            slope = 0f;
            if (!HasKicker || Math.Sign(lateral) != Side) return 0f;
            float a = Math.Abs(lateral);
            if (a < KickInner || a > KickOuter) return 0f;
            float before = Wrap(KickLipS - Wrap(s));
            if (before <= 0f || before > KickLength) return 0f;
            float u = 1f - before / KickLength;
            slope = 2f * KickRise * u / KickLength;
            return KickRise * u * u;
        }

        /// <summary>
        /// Rapport des temps de passage (raccourci / trajectoire normale) dans un virage de rayon <paramref name="radius"/> :
        /// la longueur d'un arc décalé de d vers l'intérieur vaut (R - d) / R. Supérieur à 1 = le raccourci fait perdre du temps.
        /// </summary>
        /// <param name="raceLine">Décalage vers l'intérieur de la trajectoire normale (corde).</param>
        /// <param name="cutLine">Décalage vers l'intérieur dans le sable.</param>
        /// <param name="speedFactor">Vitesse dans le sable relative à la vitesse sur la piste (1 sous turbo).</param>
        public static float TimeRatio(float radius, float raceLine, float cutLine, float speedFactor)
        {
            if (radius <= cutLine || radius <= raceLine) throw new ArgumentOutOfRangeException(nameof(radius));
            if (speedFactor <= 0f) throw new ArgumentOutOfRangeException(nameof(speedFactor));
            return (radius - cutLine) / (radius - raceLine) / speedFactor;
        }
    }
}
