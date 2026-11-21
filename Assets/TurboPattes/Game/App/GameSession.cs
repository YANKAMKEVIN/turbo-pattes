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
        /// <summary>Pilotes désignés par leur numéro dans <see cref="Kart.DriverSpec.Roster"/>.</summary>
        public readonly CupStandings Standings = new(Kart.DriverSpec.Roster.Length);
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

        /// <summary>Fantôme du contre-la-montre : son record, le meilleur tour d'un autre pilote, ou aucun.</summary>
        public static GhostMode GhostMode
        {
            get => (GhostMode)Mathf.Clamp(Load("tp.ghost", 0), 0, 2);
            set => Save("tp.ghost", (int)value);
        }

        /// <summary>Coupe choisie dans le menu (index dans <see cref="Cups.All"/>).</summary>
        public static int CupChoice
        {
            get => Mathf.Clamp(Load("tp.cup", 0), 0, Cups.All.Length - 1);
            set => Save("tp.cup", value);
        }

        // ------------------------------------------------------------------ réglages

        /// <summary>Volume de la musique (0 à 1).</summary>
        public static float MusicVolume
        {
            get => Mathf.Clamp01(LoadF("tp.vol.music", 0.8f));
            set => SaveF("tp.vol.music", Mathf.Clamp01(value));
        }

        /// <summary>Volume des effets sonores (moteur, chocs, interface), 0 à 1.</summary>
        public static float SfxVolume
        {
            get => Mathf.Clamp01(LoadF("tp.vol.sfx", 1f));
            set => SaveF("tp.vol.sfx", Mathf.Clamp01(value));
        }

        /// <summary>Sensibilité du glisser tactile pour tourner (0,5 à 1,5).</summary>
        public static float TouchSensitivity
        {
            get => Mathf.Clamp(LoadF("tp.touch", 1f), 0.5f, 1.5f);
            set => SaveF("tp.touch", Mathf.Clamp(value, 0.5f, 1.5f));
        }

        /// <summary>Commandes tactiles pour gaucher : direction à droite, boutons à gauche.</summary>
        public static bool LeftHanded
        {
            get => Load("tp.lefthanded", 0) != 0;
            set => Save("tp.lefthanded", value ? 1 : 0);
        }

        public static bool Vibration
        {
            get => Load("tp.vibration", 1) != 0;
            set => Save("tp.vibration", value ? 1 : 0);
        }

        /// <summary>Qualité graphique : -1 = automatique, sinon niveau imposé (0 = le plus beau).</summary>
        public static int QualityMode
        {
            get => Mathf.Clamp(Load("tp.quality.mode", -1), -1, App.QualityTuner.MaxLevel);
            set => Save("tp.quality.mode", value);
        }

        /// <summary>Le tutoriel de la première course a été vu jusqu'au bout.</summary>
        public static bool TutorialDone
        {
            get => Load("tp.tutorial", 0) != 0;
            set => Save("tp.tutorial", value ? 1 : 0);
        }

        // ------------------------------------------------------------------ trophées des coupes

        /// <summary>Meilleur trophée gagné dans cette coupe : 3 = or, 2 = argent, 1 = bronze, 0 = aucun.</summary>
        public static int CupTrophy(int cup) => Mathf.Clamp(Load($"tp.trophy.{cup}", 0), 0, 3);

        /// <summary>Garde le meilleur des deux trophées ; renvoie vrai s'il est nouveau.</summary>
        public static bool AwardTrophy(int cup, int trophy)
        {
            if (trophy <= CupTrophy(cup)) return false;
            Save($"tp.trophy.{cup}", trophy);
            return true;
        }

        public static bool CupUnlocked(int cup) => Cups.IsUnlocked(cup, CupTrophy);

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

        /// <summary>Meilleur tour de ce pilote sur ce circuit (0 = aucun) : son fantôme sert de rival aux autres pilotes.</summary>
        public static float DriverBestLap(int circuit, int driver) => LoadF($"tp.bestlap.{circuit}.p{driver}");
        public static void SetDriverBestLap(int circuit, int driver, float t) => SaveF($"tp.bestlap.{circuit}.p{driver}", t);

        static string DriverGhostPath(int circuit, int driver) => Path.Combine(Application.persistentDataPath, $"fantome-{circuit + 1}-p{driver + 1}.txt");

        public static GhostLap LoadDriverGhost(int circuit, int driver)
        {
            try
            {
                string path = DriverGhostPath(circuit, driver);
                return File.Exists(path) ? GhostLap.Parse(File.ReadAllText(path)) : null;
            }
            catch (IOException) { return null; }
        }

        public static void SaveDriverGhost(int circuit, int driver, GhostLap ghost)
        {
            try { File.WriteAllText(DriverGhostPath(circuit, driver), ghost.Serialize()); }
            catch (IOException e) { Debug.LogWarning($"[TurboPattes] Fantôme du pilote non sauvegardé : {e.Message}"); }
        }

        /// <summary>Efface records et fantômes d'un circuit (autotest).</summary>
        public static void ClearRecords(int circuit)
        {
            PlayerPrefs.DeleteKey($"tp.bestlap.{circuit}");
            for (int laps = 1; laps <= 9; laps++) PlayerPrefs.DeleteKey($"tp.best.{circuit}.{laps}");
            for (int d = 0; d < Kart.DriverSpec.Roster.Length; d++) PlayerPrefs.DeleteKey($"tp.bestlap.{circuit}.p{d}");
            PlayerPrefs.Save();
            try
            {
                File.Delete(GhostPath(circuit));
                for (int d = 0; d < Kart.DriverSpec.Roster.Length; d++) File.Delete(DriverGhostPath(circuit, d));
            }
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

        static float LoadF(string key, float fallback = 0f)
        {
            try { return PlayerPrefs.GetFloat(key, fallback); }
            catch { return fallback; }
        }

        static void SaveF(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
    }
}
