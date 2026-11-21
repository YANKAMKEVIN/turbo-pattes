using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace TurboPattes.Core
{
    /// <summary>Position et orientation (degrés) du fantôme à un instant.</summary>
    public readonly struct GhostPose
    {
        public readonly float X, Y, Z, Yaw, Pitch;

        public GhostPose(float x, float y, float z, float yaw, float pitch)
        {
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Pitch = pitch;
        }
    }

    /// <summary>
    /// Un tour enregistré pour le fantôme du contre-la-montre : une pose tous les <see cref="Interval"/> secondes
    /// depuis le début du tour, relue par interpolation. Se sauvegarde en texte.
    /// </summary>
    public sealed class GhostLap
    {
        public const float Interval = 0.1f;
        const int Stride = 5;
        const string Header = "fantome3";
        const string Header2 = "fantome2";
        const string OldHeader = "fantome1";

        readonly List<float> _data = new();

        /// <summary>Pilote qui a roulé ce tour (numéro dans la liste des pilotes), -1 si inconnu (ancien fichier).</summary>
        public int Driver { get; set; } = -1;
        /// <summary>Temps du tour (0 tant qu'il n'est pas fini).</summary>
        public float LapTime { get; set; }
        /// <summary>Temps intermédiaires (<see cref="LapSplits.Points"/>), vide si inconnus (ancien fichier).</summary>
        public float[] Splits { get; set; } = Array.Empty<float>();
        public int Count => _data.Count / Stride;
        public float Duration => Math.Max(0, Count - 1) * Interval;

        /// <summary>Enregistre la pose à l'instant <paramref name="t"/> (secondes depuis le début du tour), à pas réguliers.</summary>
        public void Record(float t, GhostPose pose)
        {
            while (Count * Interval <= t + 1e-4f)
            {
                _data.Add(pose.X);
                _data.Add(pose.Y);
                _data.Add(pose.Z);
                _data.Add(pose.Yaw);
                _data.Add(pose.Pitch);
            }
        }

        /// <summary>Pose interpolée à l'instant <paramref name="t"/> ; faux s'il n'y a rien d'enregistré ou si le tour est fini.</summary>
        public bool Sample(float t, out GhostPose pose)
        {
            pose = default;
            if (Count == 0 || t < 0f || t > Duration + 1e-4f) return false;
            float f = t / Interval;
            int i = Math.Min((int)f, Count - 1);
            int j = Math.Min(i + 1, Count - 1);
            float u = Math.Min(1f, f - i);
            int a = i * Stride, b = j * Stride;
            pose = new GhostPose(
                Lerp(_data[a], _data[b], u),
                Lerp(_data[a + 1], _data[b + 1], u),
                Lerp(_data[a + 2], _data[b + 2], u),
                LerpAngle(_data[a + 3], _data[b + 3], u),
                Lerp(_data[a + 4], _data[b + 4], u));
            return true;
        }

        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.Append(Header).Append(';').Append(Driver.ToString(CultureInfo.InvariantCulture)).Append(';')
                .Append(LapTime.ToString("0.000", CultureInfo.InvariantCulture)).Append(';');
            for (int i = 0; i < Splits.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(Splits[i].ToString("0.000", CultureInfo.InvariantCulture));
            }
            sb.Append(';').Append(Count).Append(';');
            for (int i = 0; i < _data.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(_data[i].ToString("0.##", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>Relit un fantôme sauvegardé ; null si le texte est abîmé.</summary>
        public static GhostLap Parse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            string[] parts = text.Split(';');
            // Version 1 : pas de temps intermédiaires ; versions 1 et 2 : pilote inconnu.
            if (parts.Length == 4 && parts[0] == OldHeader) parts = new[] { Header, "-1", parts[1], "", parts[2], parts[3] };
            else if (parts.Length == 5 && parts[0] == Header2) parts = new[] { Header, "-1", parts[1], parts[2], parts[3], parts[4] };
            if (parts.Length != 6 || parts[0] != Header) return null;
            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int driver) || driver < -1) return null;
            parts = new[] { parts[0], parts[2], parts[3], parts[4], parts[5] };
            if (!float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float lapTime)) return null;
            var splits = new List<float>();
            if (parts[2].Length > 0)
                foreach (string v in parts[2].Split(','))
                {
                    if (!float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float sp)) return null;
                    splits.Add(sp);
                }
            if (!int.TryParse(parts[3], out int count) || count < 0) return null;
            string[] values = parts[4].Length == 0 ? Array.Empty<string>() : parts[4].Split(',');
            if (values.Length != count * Stride) return null;
            var ghost = new GhostLap { LapTime = lapTime, Splits = splits.ToArray(), Driver = driver };
            foreach (string v in values)
            {
                if (!float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) return null;
                ghost._data.Add(x);
            }
            return ghost;
        }

        static float Lerp(float a, float b, float u) => a + (b - a) * u;

        /// <summary>Interpolation d'angle en degrés par le plus court chemin (350° → 10° passe par 0°).</summary>
        public static float LerpAngle(float a, float b, float u)
        {
            float d = ((b - a) % 360f + 540f) % 360f - 180f;
            return a + d * u;
        }
    }

    /// <summary>Fantôme du contre-la-montre : son propre record, celui d'un autre pilote (rival), ou aucun.</summary>
    public enum GhostMode { Record, Rival, Off }

    /// <summary>Choix du fantôme rival parmi les meilleurs tours enregistrés pilote par pilote.</summary>
    public static class GhostPick
    {
        /// <summary>Libellés du sélecteur du menu, dans l'ordre de <see cref="GhostMode"/>.</summary>
        public static readonly string[] Names = { "Record", "Rival", "Aucun" };

        /// <summary>
        /// Pilote dont on rejoue le fantôme en mode rival : le plus rapide des autres pilotes (meilleur tour &gt; 0),
        /// -1 s'il n'y en a aucun (on n'a encore roulé ce circuit qu'avec son pilote).
        /// </summary>
        /// <param name="bestLaps">Meilleur tour de chaque pilote sur ce circuit (0 = aucun).</param>
        public static int Rival(int player, IReadOnlyList<float> bestLaps)
        {
            int best = -1;
            for (int d = 0; d < bestLaps.Count; d++)
            {
                if (d == player || bestLaps[d] <= 0f) continue;
                if (best < 0 || bestLaps[d] < bestLaps[best]) best = d;
            }
            return best;
        }
    }

    /// <summary>Meilleurs temps : 0 (ou moins) veut dire « pas encore de record ».</summary>
    public static class Records
    {
        public static bool IsBetter(float time, float record) => time > 0f && (record <= 0f || time < record);
    }
}
