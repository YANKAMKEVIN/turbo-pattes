using System;

namespace TurboPattes.Core
{
    /// <summary>
    /// Alerte d'un projectile qui arrive par l'arrière (noix de coco, pelote) : plus il est près, plus l'icône est
    /// grosse et plus elle clignote et bipe vite, pour laisser le temps de sortir le bouclier ou la plume.
    /// </summary>
    public static class ThreatAlert
    {
        /// <summary>Distance (m) à partir de laquelle on prévient.</summary>
        public const float Range = 45f;

        /// <summary>0 au bord de la portée, 1 au contact.</summary>
        public static float Urgency(float distance) => Math.Max(0f, Math.Min(1f, 1f - distance / Range));

        /// <summary>Intervalle entre deux bips (et période du clignotement) : 0,5 s au loin, 0,1 s au contact.</summary>
        public static float Interval(float urgency) => 0.5f - 0.4f * Clamp01(urgency);

        /// <summary>Échelle de l'icône : 0,8 au loin, 1,4 au contact.</summary>
        public static float Scale(float urgency) => 0.8f + 0.6f * Clamp01(urgency);

        /// <summary>Phase allumée du clignotement (60 % de chaque période).</summary>
        public static bool BlinkOn(float time, float urgency)
        {
            float period = Interval(urgency);
            float phase = time / period;
            return phase - (float)Math.Floor(phase) < 0.6f;
        }

        static float Clamp01(float v) => Math.Max(0f, Math.Min(1f, v));
    }

    /// <summary>Cadence des bips d'alerte : un bip tout de suite, puis au rythme de l'urgence.</summary>
    public sealed class ThreatBeeper
    {
        float _wait;

        /// <summary>Avance le temps ; vrai quand il faut biper.</summary>
        public bool Tick(float dt, bool active, float urgency)
        {
            if (!active) { _wait = 0f; return false; }
            _wait -= dt;
            if (_wait > 0f) return false;
            _wait = ThreatAlert.Interval(urgency);
            return true;
        }
    }
}
