using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Posture du pilote (angles en degrés) : bras levés depuis le volant, bras écartés vers l'extérieur,
    /// tête baissée (positif) ou relevée, tête penchée sur le côté, petit saut du buste (m), tête tournée
    /// (positif = vers la droite) et buste penché sur le côté (roulis, même sens que l'inclinaison du kart :
    /// négatif = vers la droite).
    /// </summary>
    public readonly struct DriverPose
    {
        public readonly float ArmLift, ArmSpread, HeadPitch, HeadRoll, Bounce, HeadYaw, Lean;

        public DriverPose(float armLift, float armSpread, float headPitch, float headRoll, float bounce)
            : this(armLift, armSpread, headPitch, headRoll, bounce, 0f, 0f)
        {
        }

        public DriverPose(float armLift, float armSpread, float headPitch, float headRoll, float bounce, float headYaw, float lean)
        {
            ArmLift = armLift;
            ArmSpread = armSpread;
            HeadPitch = headPitch;
            HeadRoll = headRoll;
            Bounce = bounce;
            HeadYaw = headYaw;
            Lean = lean;
        }

        public static readonly DriverPose Rest = new(0f, 0f, 0f, 0f, 0f);
    }

    public enum Celebration { None, Victory, Defeat }

    /// <summary>
    /// Postures de conduite (penché dans les virages, coup d'œil en arrière après un dépassement) et
    /// animations de fin de course jouées par code : victoire (bras levés qui saluent, petits sauts, regard vers le ciel)
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

        // ------------------------------------------------------------------ conduite

        /// <summary>Penché du buste dans un virage pris à fond (degrés).</summary>
        public const float DrivingLean = 7f;
        /// <summary>Tête tournée vers l'intérieur d'un virage braqué à fond (degrés).</summary>
        public const float DrivingLook = 15f;
        /// <summary>Coup d'œil par-dessus l'épaule après un dépassement : durée (s), angle (degrés), délai entre deux (s).</summary>
        public const float GlanceDuration = 0.9f, GlanceYaw = 75f, GlanceCooldown = 4f;

        /// <summary>
        /// Le pilote vient-il de dépasser quelqu'un (place qui s'améliore, <paramref name="previousRank"/> &lt; 0 = inconnue)
        /// en roulant (plus de 30 % de sa vitesse de pointe), assez longtemps après son dernier coup d'œil ?
        /// </summary>
        public static bool StartsGlance(int previousRank, int rank, float sinceLastGlance, float speedRatio) =>
            previousRank >= 0 && rank < previousRank && sinceLastGlance >= GlanceCooldown && speedRatio > 0.3f;

        /// <summary>
        /// Posture de conduite, sans état : buste et tête qui se penchent dans le virage (selon le braquage
        /// <paramref name="steer"/> de -1 à 1 et la vitesse), regard tourné vers la sortie, et coup d'œil en arrière
        /// (côté <paramref name="glanceSide"/>, +1 = droite) pendant <see cref="GlanceDuration"/> après un dépassement
        /// (<paramref name="glanceTime"/> = temps depuis son début). Bras au volant : la posture de repos.
        /// </summary>
        public static DriverPose Driving(float steer, float speedRatio, float glanceTime, int glanceSide)
        {
            steer = Math.Max(-1f, Math.Min(1f, steer));
            float s = Math.Max(0f, Math.Min(1f, speedRatio));
            // Coup d'œil : tourne la tête et revient, en douceur (arche de sinus).
            float g = glanceTime >= 0f && glanceTime < GlanceDuration ? (float)Math.Sin(Math.PI * glanceTime / GlanceDuration) : 0f;
            float side = glanceSide < 0 ? -1f : 1f;
            float yaw = steer * DrivingLook * (1f - g) + side * GlanceYaw * g;
            return new DriverPose(0f, 0f, 0f, -steer * 4f * s * (1f - g), 0f, yaw, -steer * DrivingLean * s);
        }

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
