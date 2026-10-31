using System;
using System.Collections.Generic;

namespace TurboPattes.Core
{
    /// <summary>Une note jouée : hauteur MIDI (60 = do 4), début et durée en pas (croches).</summary>
    public readonly struct NoteEvent
    {
        public readonly int Midi;
        public readonly int Start;
        public readonly int Length;

        public NoteEvent(int midi, int start, int length)
        {
            Midi = midi;
            Start = start;
            Length = length;
        }
    }

    /// <summary>
    /// Morceau écrit à la main : une grille d'accords (un par mesure) et une mélodie notée en croches,
    /// 8 pas par mesure, mesures séparées par « | ». Un pas vaut une note (« E5 », « F#4 », « Bb3 »),
    /// « - » (prolonge la note précédente) ou « . » (silence).
    /// </summary>
    public sealed class Song
    {
        public const int StepsPerBar = 8;

        public string Name;
        public float Bpm;
        /// <summary>Un accord par mesure : « C », « Am », « F#m »…</summary>
        public string[] Chords;
        public string Melody;
        /// <summary>Style d'accompagnement : true = course (basse pompée, arpège rapide, batterie pleine).</summary>
        public bool Driving;

        public int Bars => Chords.Length;
        public int Steps => Bars * StepsPerBar;
        public float StepSeconds => 30f / Bpm;
    }

    public static class Tune
    {
        static readonly int[] Semitones = { 9, 11, 0, 2, 4, 5, 7 }; // A B C D E F G

        /// <summary>« C4 » → 60, « A4 » → 69, « Bb3 » → 58, « C#5 » → 73.</summary>
        public static int ParseNote(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 2) throw new FormatException($"Note invalide : « {s} »");
            char letter = char.ToUpperInvariant(s[0]);
            if (letter < 'A' || letter > 'G') throw new FormatException($"Note invalide : « {s} »");
            int semi = Semitones[letter - 'A'];
            int i = 1;
            if (s[i] == '#') { semi++; i++; }
            else if (s[i] == 'b') { semi--; i++; }
            if (!int.TryParse(s.Substring(i), out int octave)) throw new FormatException($"Note invalide : « {s} »");
            return 12 * (octave + 1) + semi;
        }

        /// <summary>Découpe une mélodie en notes. Lève une exception si une mesure n'a pas 8 pas.</summary>
        public static List<NoteEvent> ParseMelody(string melody)
        {
            var events = new List<NoteEvent>();
            string[] bars = melody.Split('|');
            int step = 0, current = -1, start = 0, length = 0;

            void Flush()
            {
                if (current >= 0) events.Add(new NoteEvent(current, start, length));
                current = -1;
            }

            for (int b = 0; b < bars.Length; b++)
            {
                string[] tokens = bars[b].Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length != Song.StepsPerBar)
                    throw new FormatException($"Mesure {b + 1} : {tokens.Length} pas au lieu de {Song.StepsPerBar}");
                foreach (string t in tokens)
                {
                    if (t == "-")
                    {
                        if (current >= 0) length++;
                    }
                    else if (t == ".")
                    {
                        Flush();
                    }
                    else
                    {
                        Flush();
                        current = ParseNote(t);
                        start = step;
                        length = 1;
                    }
                    step++;
                }
            }
            Flush();
            return events;
        }

        /// <summary>Notes de l'accord (fondamentale à l'octave 3) : « Am » → la, do, mi.</summary>
        public static int[] ChordNotes(string chord)
        {
            bool minor = chord.EndsWith("m");
            string root = minor ? chord.Substring(0, chord.Length - 1) : chord;
            int r = ParseNote(root + "3");
            return new[] { r, r + (minor ? 3 : 4), r + 7 };
        }
    }

    /// <summary>Les morceaux du jeu (compositions originales).</summary>
    public static class Songs
    {
        /// <summary>Menu : tranquille et souriant.</summary>
        public static readonly Song Menu = new()
        {
            Name = "Menu",
            Bpm = 100f,
            Driving = false,
            Chords = new[] { "C", "Am", "F", "G", "C", "Em", "F", "G", "Am", "F", "C", "G", "Am", "F", "Dm", "G" },
            Melody =
                "E5 - - - G5 - E5 - | C5 - - - A4 - C5 - | A4 - C5 - F5 - E5 - | D5 - - - B4 - - - |" +
                "E5 - G5 - C6 - B5 - | G5 - - - E5 - B4 - | A4 - C5 - F5 - A5 - | G5 - - - - - . . |" +
                "C5 - - - E5 - A5 - | A5 - G5 - F5 - C5 - | E5 - - - D5 - C5 - | B4 - - - D5 - G5 - |" +
                "A5 - - - G5 - E5 - | F5 - - - A5 - C6 - | A5 - - - F5 - D5 - | B4 - D5 - G5 - . . ",
        };

        /// <summary>Course : rapide et entraînant.</summary>
        public static readonly Song Race = new()
        {
            Name = "Course",
            Bpm = 150f,
            Driving = true,
            Chords = new[] { "F", "G", "Em", "Am", "F", "G", "C", "C", "Am", "F", "C", "G", "Am", "F", "G", "G" },
            Melody =
                "A4 . C5 . F5 - E5 C5 | D5 - B4 G4 D5 . E5 F5 | G5 - E5 . B4 . E5 G5 | A5 - - . G5 E5 C5 E5 |" +
                "F5 . A5 . F5 C5 A4 C5 | B4 D5 G5 - F5 E5 D5 B4 | C5 - E5 G5 C6 - G5 E5 | C5 - - - . . G4 B4 |" +
                "A4 C5 E5 A5 - G5 E5 C5 | F5 - E5 F5 A5 - F5 C5 | E5 - G5 - C6 - B5 G5 | D5 - G5 F5 E5 D5 B4 G4 |" +
                "E5 . A5 . E5 C5 A4 C5 | C5 F5 A5 C6 A5 F5 C5 A4 | B4 D5 G5 B5 A5 G5 F5 D5 | G5 - - - D5 - G4 . ",
        };
    }
}
