using System;

namespace TurboPattes.Core
{
    public enum Medal { None, Bronze, Silver, Gold }

    /// <summary>
    /// Médailles du contre-la-montre, d'après le meilleur tour. Les temps de référence sont ceux d'un tour lancé de
    /// l'IA de l'autotest (pilote équilibré) ; l'or demande de battre cette référence, donc de drifter et de couper juste.
    /// </summary>
    public static class Medals
    {
        /// <summary>Tour lancé de référence (s), dans l'ordre de <c>Circuit.All()</c>.</summary>
        public static readonly float[] Reference = { 40.2f, 42.4f, 45.1f, 42.6f, 38.5f, 48.7f, 53.3f, 48.1f, 43.5f, 50.0f, 39.5f };

        public const float GoldFactor = 0.985f;
        public const float SilverFactor = 1.04f;
        public const float BronzeFactor = 1.12f;

        static float Ref(int circuit) => Reference[Math.Max(0, Math.Min(Reference.Length - 1, circuit))];

        /// <summary>Temps à battre pour la médaille (arrondi au dixième).</summary>
        public static float Target(int circuit, Medal medal)
        {
            float f = medal switch { Medal.Gold => GoldFactor, Medal.Silver => SilverFactor, Medal.Bronze => BronzeFactor, _ => 0f };
            return (float)Math.Round(Ref(circuit) * f, 1);
        }

        /// <summary>Médaille obtenue avec ce meilleur tour (0 ou moins = pas de temps).</summary>
        public static Medal For(int circuit, float bestLap)
        {
            if (bestLap <= 0f) return Medal.None;
            if (bestLap <= Target(circuit, Medal.Gold)) return Medal.Gold;
            if (bestLap <= Target(circuit, Medal.Silver)) return Medal.Silver;
            if (bestLap <= Target(circuit, Medal.Bronze)) return Medal.Bronze;
            return Medal.None;
        }

        public static string Name(Medal m) => m switch { Medal.Gold => "Or", Medal.Silver => "Argent", Medal.Bronze => "Bronze", _ => "" };
    }

    /// <summary>
    /// Temps intermédiaires d'un tour, au quart, à la moitié et aux trois quarts, pour comparer avec le fantôme.
    /// Un kart qui démarre juste derrière la ligne (fraction proche de 1) ne déclenche rien avant de l'avoir franchie.
    /// </summary>
    public sealed class LapSplits
    {
        public static readonly float[] Points = { 0.25f, 0.5f, 0.75f };

        readonly float[] _times = new float[Points.Length];
        int _next;

        public float[] Times => _times;
        public int Count => _next;

        public void Reset()
        {
            _next = 0;
            Array.Clear(_times, 0, _times.Length);
        }

        /// <summary>Position dans le tour (0 à 1) et temps depuis le début du tour : index du point franchi, ou -1.</summary>
        public int Update(float lapFraction, float lapTime)
        {
            if (_next >= Points.Length) return -1;
            float p = Points[_next];
            if (lapFraction < p || lapFraction > p + 0.2f) return -1;
            _times[_next] = lapTime;
            return _next++;
        }
    }
}
