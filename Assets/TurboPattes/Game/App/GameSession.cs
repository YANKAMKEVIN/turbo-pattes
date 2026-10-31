using UnityEngine;

namespace TurboPattes.Game.App
{
    public enum Difficulty { Easy, Normal, Hard }

    /// <summary>
    /// Réglages choisis dans le menu. Ils survivent au rechargement de la scène (Rejouer) et sont
    /// mémorisés d'une partie à l'autre.
    /// </summary>
    public static class GameSession
    {
        public static readonly int[] LapChoices = { 1, 3, 5 };
        public static readonly string[] DifficultyNames = { "Facile", "Normal", "Difficile" };

        /// <summary>Au chargement de la scène : menu (true) ou course directe (Rejouer).</summary>
        public static bool ShowMenu = true;

        public static int Pilot
        {
            get => Mathf.Clamp(Load("tp.pilot", 0), 0, 7);
            set => Save("tp.pilot", value);
        }

        public static int Laps
        {
            get => Load("tp.laps", 3);
            set => Save("tp.laps", value);
        }

        public static Difficulty Difficulty
        {
            get => (Difficulty)Mathf.Clamp(Load("tp.difficulty", 1), 0, 2);
            set => Save("tp.difficulty", (int)value);
        }

        /// <summary>Plage de vitesse des IA (fraction de la vitesse max du joueur).</summary>
        public static Vector2 AiSpeedRange(Difficulty d) => d switch
        {
            Difficulty.Easy => new Vector2(0.86f, 0.92f),
            Difficulty.Hard => new Vector2(0.97f, 1.01f),
            _ => new Vector2(0.93f, 0.99f),
        };

        static int Load(string key, int fallback)
        {
            try { return PlayerPrefs.GetInt(key, fallback); }
            catch { return fallback; }
        }

        static void Save(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }
    }
}
