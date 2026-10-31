namespace TurboPattes.Core
{
    public enum VerticalEvent { None, TookOff, Landed }

    /// <summary>
    /// Mouvement vertical d'un kart : collé au sol tant que celui-ci ne se dérobe pas plus vite que la gravité
    /// (bord d'un tremplin), sinon trajectoire balistique jusqu'à la réception.
    /// </summary>
    public sealed class VerticalMotion
    {
        /// <summary>Gravité arcade (m/s²), plus forte que la vraie pour des sauts courts et nerveux.</summary>
        public const float Gravity = 24f;
        /// <summary>Le kart décolle si le sol passe plus bas que sa trajectoire libre d'au moins cette marge.</summary>
        public const float TakeOffMargin = 0.05f;

        public float Y { get; private set; }
        public float Vy { get; private set; }
        public bool Grounded { get; private set; } = true;
        /// <summary>Durée du saut en cours (ou du dernier saut une fois posé).</summary>
        public float AirTime { get; private set; }
        /// <summary>Vitesse d'impact (m/s, positive) de la dernière réception.</summary>
        public float LandingSpeed { get; private set; }

        public void Reset(float groundY)
        {
            Y = groundY;
            Vy = 0f;
            Grounded = true;
            AirTime = 0f;
        }

        /// <param name="groundY">Hauteur du sol sous le kart, après son déplacement horizontal.</param>
        /// <param name="groundVy">Vitesse verticale d'un kart collé à ce sol (pente × vitesse le long de la piste).</param>
        public VerticalEvent Step(float dt, float groundY, float groundVy)
        {
            if (Grounded)
            {
                float free = Y + Vy * dt - 0.5f * Gravity * dt * dt;
                if (groundY < free - TakeOffMargin)
                {
                    Grounded = false;
                    AirTime = 0f;
                    Y = free;
                    Vy -= Gravity * dt;
                    return VerticalEvent.TookOff;
                }
                Y = groundY;
                Vy = groundVy;
                return VerticalEvent.None;
            }

            Vy -= Gravity * dt;
            Y += Vy * dt;
            AirTime += dt;
            if (Y > groundY) return VerticalEvent.None;
            LandingSpeed = groundVy - Vy;
            Y = groundY;
            Vy = groundVy;
            Grounded = true;
            return VerticalEvent.Landed;
        }
    }
}
