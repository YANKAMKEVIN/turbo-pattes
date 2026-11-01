using System.IO;
using TurboPattes.Core;
using UnityEngine;

namespace TurboPattes.Game.App
{
    public enum Difficulty { Easy, Normal, Hard }

    /// <summary>Course simple contre les IA, contre-la-montre seul avec fantôme, ou coupe de 4 circuits.</summary>
    public enum GameMode { Race, TimeTrial, Cup }

    /// <summary>Coupe en cours : course n (0 à 3), classement cumulé, ordre d'arrivée de la dernière course.</summary>
    public sealed class CupRun
    {
        public int CupIndex;
        public int Race;
        /// <summary>Numéro du pilote du joueur.</summary>
        public int Player;
        /// <summary>Pilotes désignés par leur numéro dans <see cref="Kart.AnimalSpec.Roster"/>.</summary>
        public readonly CupStandings Standings = new(Kart.AnimalSpec.Roster.Length);
        public int[] LastOrder;
        /// <summary>Points de la course <see cref="Race"/> déjà comptés.</summary>
        public bool Recorded;

        public CupDef Def => Cups.All[CupIndex];
        public bool IsLastRace => Race == Cups.RacesPerCup - 1;
        /// <summary>Les quatre courses sont finies : place au podium.</summary>
        public bool Over => Race >= Cups.RacesPerCup;
        public int CircuitIndex => Def.Circuits[Mathf.Min(Race, Cups.RacesPerCup - 1)];

        public void Record(int[] finishOrder)
        {
            if (Recorded) return;
            Standings.AddRace(finishOrder);
            LastOrder = finishOrder;
            Recorded = true;
        }

        public void Next()
        {
            Race++;
            Recorded = false;
        }
    }

    /// <summary>
    /// Réglages choisis dans le menu. Ils survivent au rechargement de la scène (Rejouer) et sont
    /// mémorisés d'une partie à l'autre.
    /// </summary>
    public static class GameSession
    {
        public static readonly int[] LapChoices = { 1, 3, 5 };
        public static readonly string[] DifficultyNames = { "Facile", "Normal", "Difficile" };
        public static readonly string[] ModeNames = { "Course", "Chrono", "Coupe" };

        /// <summary>Coupe en cours (null hors coupe) ; survit aux rechargements de scène, pas à la fermeture du jeu.</summary>
        public static CupRun Cup;

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

        /// <summary>Index du circuit dans <see cref="Track.Circuit.All"/>.</summary>
        public static int Circuit
        {
            get => Mathf.Clamp(Load("tp.circuit", 0), 0, CircuitCount - 1);
            set => Save("tp.circuit", value);
        }

        public const int CircuitCount = Track.Circuit.Count;

        public static Difficulty Difficulty
        {
            get => (Difficulty)Mathf.Clamp(Load("tp.difficulty", 1), 0, 2);
            set => Save("tp.difficulty", (int)value);
        }

        public static GameMode Mode
        {
            get => (GameMode)Mathf.Clamp(Load("tp.mode", 0), 0, 2);
            set => Save("tp.mode", (int)value);
        }

        /// <summary>Coupe choisie dans le menu (index dans <see cref="Cups.All"/>).</summary>
        public static int CupChoice
        {
            get => Mathf.Clamp(Load("tp.cup", 0), 0, Cups.All.Length - 1);
            set => Save("tp.cup", value);
        }

        // ------------------------------------------------------------------ records du contre-la-montre

        /// <summary>Meilleur temps total sur ce circuit et ce nombre de tours (0 = aucun).</summary>
        public static float BestTime(int circuit, int laps) => LoadF($"tp.best.{circuit}.{laps}");
        public static void SetBestTime(int circuit, int laps, float t) => SaveF($"tp.best.{circuit}.{laps}", t);

        /// <summary>Meilleur tour sur ce circuit (0 = aucun) ; le fantôme rejoue ce tour-là.</summary>
        public static float BestLap(int circuit) => LoadF($"tp.bestlap.{circuit}");
        public static void SetBestLap(int circuit, float t) => SaveF($"tp.bestlap.{circuit}", t);

        static string GhostPath(int circuit) => Path.Combine(Application.persistentDataPath, $"fantome-{circuit + 1}.txt");

        public static GhostLap LoadGhost(int circuit)
        {
            try
            {
                string path = GhostPath(circuit);
                return File.Exists(path) ? GhostLap.Parse(File.ReadAllText(path)) : null;
            }
            catch (IOException) { return null; }
        }

        public static void SaveGhost(int circuit, GhostLap ghost)
        {
            try { File.WriteAllText(GhostPath(circuit), ghost.Serialize()); }
            catch (IOException e) { Debug.LogWarning($"[TurboPattes] Fantôme non sauvegardé : {e.Message}"); }
        }

        /// <summary>Efface records et fantôme d'un circuit (autotest).</summary>
        public static void ClearRecords(int circuit)
        {
            PlayerPrefs.DeleteKey($"tp.bestlap.{circuit}");
            for (int laps = 1; laps <= 9; laps++) PlayerPrefs.DeleteKey($"tp.best.{circuit}.{laps}");
            PlayerPrefs.Save();
            try { File.Delete(GhostPath(circuit)); }
            catch (IOException) { }
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

        static float LoadF(string key)
        {
            try { return PlayerPrefs.GetFloat(key, 0f); }
            catch { return 0f; }
        }

        static void SaveF(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
    }
}
