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
        /// <summary>Style d'accompagnement (basse, accords, batterie).</summary>
        public SongStyle Style;
        /// <summary>Timbre de la mélodie.</summary>
        public LeadVoice Lead;
        /// <summary>Tonalité (« C », « F#m »…) : la mélodie et les accords n'en sortent pas.</summary>
        public string Key;

        public int Bars => Chords.Length;
        public int Steps => Bars * StepsPerBar;
        public float StepSeconds => 30f / Bpm;
    }

    /// <summary>Accompagnements : chacun a sa basse, ses accords et sa batterie.</summary>
    public enum SongStyle { Calm, Driving, Gallop, Tropical, Calypso, Aquatic, Airy, Space, Sleigh, Heavy, Disco }

    /// <summary>Timbres de mélodie.</summary>
    public enum LeadVoice { Square, Twang, Mallet, Steel, Bubble, Flute, Bell, Saw, Fuzz, Sync }

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

        static readonly int[] MajorScale = { 0, 2, 4, 5, 7, 9, 11 };
        static readonly int[] MinorScale = { 0, 2, 3, 5, 7, 8, 10 };

        /// <summary>La note appartient-elle à la gamme de la tonalité (« G » majeur, « Em » mineur naturel) ?</summary>
        public static bool InKey(int midi, string key)
        {
            bool minor = key.EndsWith("m");
            int tonic = ParseNote((minor ? key.Substring(0, key.Length - 1) : key) + "4");
            int degree = ((midi - tonic) % 12 + 12) % 12;
            return Array.IndexOf(minor ? MinorScale : MajorScale, degree) >= 0;
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
            Style = SongStyle.Calm,
            Lead = LeadVoice.Square,
            Key = "C",
            Chords = new[] { "C", "Am", "F", "G", "C", "Em", "F", "G", "Am", "F", "C", "G", "Am", "F", "Dm", "G" },
            Melody =
                "E5 - - - G5 - E5 - | C5 - - - A4 - C5 - | A4 - C5 - F5 - E5 - | D5 - - - B4 - - - |" +
                "E5 - G5 - C6 - B5 - | G5 - - - E5 - B4 - | A4 - C5 - F5 - A5 - | G5 - - - - - . . |" +
                "C5 - - - E5 - A5 - | A5 - G5 - F5 - C5 - | E5 - - - D5 - C5 - | B4 - - - D5 - G5 - |" +
                "A5 - - - G5 - E5 - | F5 - - - A5 - C6 - | A5 - - - F5 - D5 - | B4 - D5 - G5 - . . ",
        };

        /// <summary>Circuit des Prés (ex-Prairie des Pattes) : rapide et entraînant.</summary>
        public static readonly Song Race = new()
        {
            Name = "Prairie",
            Bpm = 150f,
            Style = SongStyle.Driving,
            Lead = LeadVoice.Square,
            Key = "C",
            Chords = new[] { "F", "G", "Em", "Am", "F", "G", "C", "C", "Am", "F", "C", "G", "Am", "F", "G", "G" },
            Melody =
                "A4 . C5 . F5 - E5 C5 | D5 - B4 G4 D5 . E5 F5 | G5 - E5 . B4 . E5 G5 | A5 - - . G5 E5 C5 E5 |" +
                "F5 . A5 . F5 C5 A4 C5 | B4 D5 G5 - F5 E5 D5 B4 | C5 - E5 G5 C6 - G5 E5 | C5 - - - . . G4 B4 |" +
                "A4 C5 E5 A5 - G5 E5 C5 | F5 - E5 F5 A5 - F5 C5 | E5 - G5 - C6 - B5 G5 | D5 - G5 F5 E5 D5 B4 G4 |" +
                "E5 . A5 . E5 C5 A4 C5 | C5 F5 A5 C6 A5 F5 C5 A4 | B4 D5 G5 B5 A5 G5 F5 D5 | G5 - - - D5 - G4 . ",
        };
            /// <summary>Canyon Rouge (ex-Canyon Caramel) : galop de western en ré mineur, guitare pincée.</summary>
        public static readonly Song Canyon = new()
        {
            Name = "Canyon",
            Bpm = 140f,
            Style = SongStyle.Gallop,
            Lead = LeadVoice.Twang,
            Key = "Dm",
            Chords = new[] { "Dm", "Dm", "C", "Dm", "Bb", "C", "Dm", "Dm", "F", "C", "Dm", "Am", "Bb", "C", "Dm", "Dm" },
            Melody =
                "D5 - A4 - D5 E5 F5 - | E5 - D5 - C5 - A4 - | G4 - C5 - E5 - G5 - | F5 - E5 D5 - - . . |" +
                "F5 - D5 - Bb4 - D5 F5 | E5 - C5 - G4 - C5 E5 | D5 - - F5 A5 - G5 F5 | D5 - - - . . A4 C5 |" +
                "F5 - A5 - C6 - A5 F5 | G5 - E5 - C5 - E5 G5 | F5 - D5 - A4 - D5 F5 | E5 - - - C5 - A4 - |" +
                "Bb4 D5 F5 - Bb5 - A5 G5 | C5 E5 G5 - C6 - Bb5 G5 | A5 - F5 - D5 - E5 F5 | D5 - - - - - . . ",
        };

        /// <summary>Jungle Papaye : marimba bondissant et congas.</summary>
        public static readonly Song Jungle = new()
        {
            Name = "Jungle",
            Bpm = 145f,
            Style = SongStyle.Tropical,
            Lead = LeadVoice.Mallet,
            Key = "F",
            Chords = new[] { "F", "F", "Bb", "C", "F", "Dm", "Bb", "C", "Dm", "Bb", "F", "C", "Dm", "Bb", "C", "F" },
            Melody =
                "C5 . A4 C5 . F5 . A5 | G5 - F5 . C5 . A4 . | Bb4 . D5 F5 . D5 . Bb4 | C5 - E5 - G5 - . . |" +
                "A5 . F5 A5 . C6 . A5 | F5 - D5 . A4 . D5 . | D5 . F5 Bb5 . F5 . D5 | E5 - G5 - C6 - . . |" +
                "D5 - F5 - A5 - F5 D5 | Bb4 - D5 - F5 - D5 Bb4 | A4 - C5 - F5 - A5 - | G5 - E5 - C5 - . . |" +
                "F5 . D5 F5 . A5 . F5 | D5 . Bb4 D5 . F5 . D5 | E5 . G5 C6 . G5 . E5 | F5 - - - . . . . ",
        };

        /// <summary>Lagon Coco : calypso décontracté, tambour d'acier.</summary>
        public static readonly Song Lagoon = new()
        {
            Name = "Lagon",
            Bpm = 128f,
            Style = SongStyle.Calypso,
            Lead = LeadVoice.Steel,
            Key = "G",
            Chords = new[] { "G", "C", "D", "G", "G", "Em", "C", "D", "C", "D", "Bm", "Em", "C", "D", "G", "G" },
            Melody =
                "B4 - D5 . G5 - D5 . | E5 - G5 . C6 - G5 E5 | F#5 - A5 . D5 - F#5 . | G5 - - - . . D5 . |" +
                "B4 . D5 G5 - . B5 . | G5 . E5 B4 - . E5 . | E5 - G5 - C6 - B5 A5 | F#5 - - - A5 - . . |" +
                "G5 - E5 - C5 - E5 G5 | A5 - F#5 - D5 - F#5 A5 | B5 - F#5 - D5 - B4 D5 | E5 - - - G5 - B5 - |" +
                "C6 - B5 A5 G5 - E5 . | D5 - F#5 A5 D6 - C6 A5 | B5 - G5 . D5 . B4 . | G4 - - - . . . . ",
        };

        /// <summary>Récif Bulle : rêveur et mouillé, notes qui remontent comme des bulles, échos.</summary>
        public static readonly Song Reef = new()
        {
            Name = "Recif",
            Bpm = 120f,
            Style = SongStyle.Aquatic,
            Lead = LeadVoice.Bubble,
            Key = "Am",
            Chords = new[] { "Am", "F", "C", "G", "Am", "F", "G", "Em", "F", "G", "Am", "Am", "F", "G", "Em", "Am" },
            Melody =
                "E5 - - - A5 - - - | C6 - - - A5 - F5 - | G5 - - - E5 - C5 - | D5 - - - B4 - - - |" +
                "C5 - E5 - A5 - B5 - | C6 - - - F5 - - - | D5 - - - G5 - B5 - | G5 - - - - - . . |" +
                "A5 - - - C6 - A5 - | B5 - - - D6 - B5 - | C6 - - - E5 - A5 - | A5 - - - - - . . |" +
                "F5 - A5 - C6 - A5 - | G5 - B5 - D6 - B5 - | E5 - G5 - B5 - G5 - | A5 - - - - - - - ",
        };

        /// <summary>Route des Nuages : aérien, flûte et arpèges qui montent.</summary>
        public static readonly Song Sky = new()
        {
            Name = "Nuages",
            Bpm = 138f,
            Style = SongStyle.Airy,
            Lead = LeadVoice.Flute,
            Key = "D",
            Chords = new[] { "D", "A", "Bm", "G", "D", "A", "G", "A", "Bm", "G", "D", "A", "Bm", "G", "A", "D" },
            Melody =
                "F#5 - A5 - D6 - A5 - | E5 - A5 - C#6 - A5 - | D5 - F#5 - B5 - F#5 - | G5 - B5 - D6 - B5 - |" +
                "A5 - F#5 - D5 - F#5 A5 | C#6 - A5 - E5 - A5 C#6 | B5 - G5 - D5 - G5 B5 | A5 - - - E5 - C#5 - |" +
                "D6 - - B5 F#5 - B5 - | D6 - - B5 G5 - B5 - | A5 - - F#5 D5 - F#5 - | E5 - - - C#6 - A5 - |" +
                "B5 - D6 - B5 - F#5 - | G5 - B5 - D6 - B5 - | A5 - C#6 - E6 - C#6 - | D6 - - - - - . . ",
        };

        /// <summary>Orbite Comète : synthé spatial, arpèges en doubles croches, grande caisse claire.</summary>
        public static readonly Song Space = new()
        {
            Name = "Espace",
            Bpm = 132f,
            Style = SongStyle.Space,
            Lead = LeadVoice.Saw,
            Key = "Em",
            Chords = new[] { "Em", "C", "G", "D", "Em", "C", "D", "D", "C", "D", "Bm", "Em", "C", "D", "Em", "Em" },
            Melody =
                "E5 . . B4 E5 . G5 . | E5 . C5 . G5 - - . | D5 . . B4 D5 . G5 . | F#5 - - - A5 - . . |" +
                "B5 . . G5 B5 . E6 . | C6 - - . G5 . E5 . | F#5 . A5 . D6 - - . | A5 - - - F#5 - D5 - |" +
                "E5 - G5 - C6 - - - | D6 - - - A5 - F#5 - | F#5 - B5 - D6 - - - | E6 - - - B5 - G5 - |" +
                "C6 - B5 - G5 - E5 - | D6 - C6 - A5 - F#5 - | E5 - G5 - B5 - E6 - | E6 - - - - - . . ",
        };

        /// <summary>Banquise Givrée : clochettes, grelots et cordes pincées.</summary>
        public static readonly Song Ice = new()
        {
            Name = "Banquise",
            Bpm = 144f,
            Style = SongStyle.Sleigh,
            Lead = LeadVoice.Bell,
            Key = "Bb",
            Chords = new[] { "Bb", "Gm", "Eb", "F", "Bb", "Gm", "Eb", "F", "Gm", "Eb", "Bb", "F", "Eb", "F", "Bb", "Bb" },
            Melody =
                "D5 F5 Bb5 . F5 . D5 . | G5 Bb5 D6 . Bb5 . G5 . | Eb5 G5 Bb5 . G5 . Eb5 . | F5 - A5 - C6 - . . |" +
                "Bb5 . A5 . Bb5 . F5 . | G5 . F5 . G5 . D5 . | Eb5 . D5 . Eb5 . G5 . | F5 - - - . . . . |" +
                "G5 - Bb5 - D6 - Bb5 - | G5 - Eb5 - Bb4 - Eb5 G5 | F5 - D5 - Bb4 - D5 F5 | A5 - - - C6 - A5 - |" +
                "G5 . Bb5 . Eb6 . Bb5 . | A5 . C6 . F6 . C6 . | D6 - Bb5 - F5 - D5 - | Bb4 - - - . . . . ",
        };

        /// <summary>Volcan Grondant : lourd et grondant, guitare saturée, double grosse caisse.</summary>
        public static readonly Song Volcano = new()
        {
            Name = "Volcan",
            Bpm = 156f,
            Style = SongStyle.Heavy,
            Lead = LeadVoice.Fuzz,
            Key = "Cm",
            Chords = new[] { "Cm", "Ab", "Bb", "Cm", "Cm", "Ab", "Fm", "Gm", "Fm", "Gm", "Cm", "Ab", "Ab", "Bb", "Cm", "Cm" },
            Melody =
                "C5 C5 . Eb5 . G5 . C6 | Ab5 - G5 - Eb5 - C5 . | Bb4 - D5 - F5 - Bb5 . | C6 - - - G5 - Eb5 - |" +
                "C5 C5 . Eb5 . G5 . Bb5 | C6 - Bb5 - Ab5 - Eb5 . | F5 - Ab5 - C6 - Ab5 F5 | G5 - - - D5 - Bb4 - |" +
                "F5 . F5 Ab5 . C6 . Ab5 | G5 . G5 Bb5 . D6 . Bb5 | C6 - G5 - Eb5 - G5 C6 | Eb6 - C6 - Ab5 - Eb5 - |" +
                "Ab5 - C6 - Eb6 - C6 - | Bb5 - D6 - F6 - D6 - | C6 - G5 - Eb5 - C5 - | C5 - - - . . . . ",
        };

        /// <summary>Ville Néon : disco de nuit, basse en octaves, grosse caisse sur chaque temps.</summary>
        public static readonly Song City = new()
        {
            Name = "Neon",
            Bpm = 124f,
            Style = SongStyle.Disco,
            Lead = LeadVoice.Sync,
            Key = "F#m",
            Chords = new[] { "F#m", "D", "A", "E", "F#m", "D", "A", "E", "Bm", "D", "F#m", "C#m", "Bm", "D", "E", "C#m" },
            Melody =
                "F#5 - . F#5 A5 - C#6 - | D6 - . A5 F#5 - D5 - | E5 - . A5 C#6 - A5 - | B5 - - - G#5 - E5 - |" +
                "C#6 - . C#6 B5 - A5 - | F#5 - . A5 D6 - A5 - | C#6 - E6 - C#6 - A5 - | B5 - - - . . E5 G#5 |" +
                "B5 - D6 - F#6 - D6 - | A5 - - D6 - - F#5 - | C#6 - - A5 - - F#5 - | G#5 - - - E5 - C#5 - |" +
                "D5 - F#5 - B5 - D6 - | F#5 - A5 - D6 - F#6 - | E6 - D6 - C#6 - B5 - | G#5 - - - E5 - - - ",
        };

        /// <summary>Podium de fin de coupe : fanfare festive en do majeur, 8 mesures qui bouclent.</summary>
        public static readonly Song Podium = new()
        {
            Name = "Podium",
            Bpm = 120f,
            Style = SongStyle.Driving,
            Lead = LeadVoice.Bell,
            Key = "C",
            Chords = new[] { "C", "F", "C", "G", "C", "F", "G", "C" },
            Melody =
                "G5 - E5 G5 C6 - - - | A5 - F5 A5 C6 - A5 - | G5 - E5 G5 E6 - D6 C6 | D6 - - - B5 G5 - . |" +
                "E6 - C6 E6 G6 - E6 C6 | F6 - A5 C6 F6 - E6 D6 | D6 - B5 G5 B5 D6 G6 - | C6 - - E6 C6 - - - ",
        };

        /// <summary>Jingle d'arrivée : deux mesures qui montent et finissent sur la tonique tenue.</summary>
        public static readonly Song Finish = new()
        {
            Name = "Arrivee",
            Bpm = 140f,
            Style = SongStyle.Driving,
            Lead = LeadVoice.Square,
            Key = "C",
            Chords = new[] { "C", "C" },
            Melody = "C5 E5 G5 C6 E6 G5 C6 E6 | C6 - - - - - - - ",
        };

        /// <summary>Un morceau par circuit, dans l'ordre de <c>Circuit.All()</c>.</summary>
        public static readonly Song[] Circuits = { Race, Canyon, Jungle, Lagoon, Reef, Sky, Space, Ice, Volcano, City };

        public static readonly Song[] All = { Menu, Race, Canyon, Jungle, Lagoon, Reef, Sky, Space, Ice, Volcano, City, Podium, Finish };

        /// <summary>Morceau du circuit n (0 = Prairie) ; retombe sur la Prairie hors limites.</summary>
        public static Song ForCircuit(int index) => index >= 0 && index < Circuits.Length ? Circuits[index] : Race;

        /// <summary>Tempo du dernier tour, relevé d'environ 12 %.</summary>
        public const float SprintTempo = 1.12f;

        static readonly Dictionary<Song, Song> Sprints = new();

        /// <summary>
        /// La même partition au tempo du dernier tour. Toujours la même instance pour un morceau donné
        /// (le lecteur range les morceaux calculés par instance).
        /// </summary>
        public static Song Sprint(Song song)
        {
            if (song == null) return null;
            lock (Sprints)
            {
                if (Sprints.TryGetValue(song, out var fast)) return fast;
                fast = new Song
                {
                    Name = song.Name + " (sprint)", Bpm = song.Bpm * SprintTempo, Chords = song.Chords, Melody = song.Melody,
                    Style = song.Style, Lead = song.Lead, Key = song.Key,
                };
                Sprints[song] = fast;
                return fast;
            }
        }
    }
}
