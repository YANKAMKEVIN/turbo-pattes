using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Posture du pilote (angles en degrés) : bras levés depuis le volant, bras écartés vers l'extérieur,
    /// tête baissée (positif) ou relevée, tête penchée sur le côté, et petit saut du buste (m).
    /// </summary>
    public readonly struct DriverPose
    {
        public readonly float ArmLift, ArmSpread, HeadPitch, HeadRoll, Bounce;

        public DriverPose(float armLift, float armSpread, float headPitch, float headRoll, float bounce)
        {
            ArmLift = armLift;
            ArmSpread = armSpread;
            HeadPitch = headPitch;
            HeadRoll = headRoll;
            Bounce = bounce;
        }

        public static readonly DriverPose Rest = new(0f, 0f, 0f, 0f, 0f);
    }

    public enum Celebration { None, Victory, Defeat }

    /// <summary>
    /// Animations de fin de course jouées par code : victoire (bras levés qui saluent, petits sauts, regard vers le ciel)
    /// pour le podium, défaite (bras qui lâchent le volant, tête basse qui se secoue) pour la seconde moitié du peloton.
    /// Calculées à partir du temps depuis l'arrivée, sans état ; partent de la posture de conduite.
    /// </summary>
    public static class DriverPoses
    {
        /// <summary>Animation selon la place (0 = premier) : podium = victoire, seconde moitié du peloton = défaite.</summary>
        public static Celebration For(int rank, int racers)
        {
            if (racers <= 1 || rank < 3) return Celebration.Victory;
            return rank >= racers / 2 ? Celebration.Defeat : Celebration.None;
        }

        public static DriverPose Pose(Celebration c, float time) => c switch
        {
            Celebration.Victory => Victory(time),
            Celebration.Defeat => Defeat(time),
            _ => DriverPose.Rest,
        };

        static float Ease(float t, float duration)
        {
            float u = Math.Max(0f, Math.Min(1f, t / duration));
            return u * u * (3f - 2f * u);
        }

        static float Wave(float t, float hz) => (float)Math.Sin(2.0 * Math.PI * hz * t);

        /// <summary>Bras levés en 0,35 s puis qui saluent, petits sauts, tête relevée qui dodeline.</summary>
        public static DriverPose Victory(float t)
        {
            float e = Ease(t, 0.35f);
            return new DriverPose(
                115f * e,
                e * (18f + 10f * Wave(t, 1.6f)),
                -12f * e,
                6f * e * Wave(t, 0.8f),
                0.12f * e * Math.Abs(Wave(t, 1.1f)));
        }

        /// <summary>Bras qui retombent en 0,8 s, tête basse, secouée de plus en plus lentement, buste tassé.</summary>
        public static DriverPose Defeat(float t)
        {
            float e = Ease(t, 0.8f);
            float shake = 9f * (0.35f + 0.65f * (float)Math.Exp(-0.5 * Math.Max(0f, t)));
            return new DriverPose(-18f * e, 0f, 28f * e, e * shake * Wave(t, 0.5f), -0.06f * e);
        }
    }
}
