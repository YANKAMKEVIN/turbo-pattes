using System.IO;
using TurboPattes.Core;
using TurboPattes.Game.Audio;
using UnityEngine;

namespace TurboPattes.EditorTools
{
    /// <summary>
    /// Exporte les morceaux en WAV pour les écouter hors du jeu :
    /// Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod TurboPattes.EditorTools.MusicExport.Export -tp-out &lt;dossier&gt;
    /// </summary>
    public static class MusicExport
    {
        public static void Export()
        {
            string dir = "Builds/Musique";
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-tp-out") dir = args[i + 1];
            Directory.CreateDirectory(dir);
            foreach (var song in new[] { Songs.Menu, Songs.Race })
            {
                var data = MusicSynth.Render(song);
                // Deux tours de boucle, pour entendre la jointure.
                string path = Path.Combine(dir, $"turbo-pattes-{song.Name.ToLowerInvariant()}.wav");
                WriteWav(path, data, 2, MusicSynth.Rate);
                Debug.Log($"[MusicExport] {path}");
            }
        }

        static void WriteWav(string path, float[] data, int repeats, int rate)
        {
            int samples = data.Length * repeats;
            using var w = new BinaryWriter(File.Create(path));
            w.Write("RIFF".ToCharArray());
            w.Write(36 + samples * 2);
            w.Write("WAVEfmt ".ToCharArray());
            w.Write(16);
            w.Write((short)1);
            w.Write((short)1);
            w.Write(rate);
            w.Write(rate * 2);
            w.Write((short)2);
            w.Write((short)16);
            w.Write("data".ToCharArray());
            w.Write(samples * 2);
            for (int r = 0; r < repeats; r++)
                foreach (float s in data) w.Write((short)(Mathf.Clamp(s, -1f, 1f) * 32767f));
        }
    }
}
