namespace TurboPattes.Core
{
    /// <summary>
    /// Figure en l'air : appuyer sur DRIFT pendant un saut assez long fait tourner le pilote ; une figure finie avant
    /// la réception donne un petit turbo, une figure ratée (pas finie au contact du sol) ne donne rien.
    /// </summary>
    public sealed class AirTrick
    {
        public const float MinAirTime = 0.25f;
        public const float Duration = 0.35f;
        public const float Boost = 0.7f;

        public bool Active { get; private set; }
        public bool Done { get; private set; }
        public float Progress { get; private set; }

        /// <summary>Appui sur le bouton pendant le saut : lance la figure si le kart vole depuis assez longtemps.</summary>
        public bool TryStart(bool airborne, float airTime)
        {
            if (!airborne || Active || Done || airTime < MinAirTime) return false;
            Active = true;
            Progress = 0f;
            return true;
        }

        public void Tick(float dt)
        {
            if (!Active) return;
            Progress += dt / Duration;
            if (Progress < 1f) return;
            Progress = 1f;
            Active = false;
            Done = true;
        }

        /// <summary>À la réception : durée du turbo gagné (0 si pas de figure ou figure pas finie). Remet à zéro.</summary>
        public float Land()
        {
            float boost = Done ? Boost : 0f;
            Active = Done = false;
            Progress = 0f;
            return boost;
        }
    }
}
