using System;
using TurboPattes.Core;

namespace TurboPattes.Game.Audio
{
    /// <summary>
    /// Joue un <see cref="Song"/> en échantillons mono, bouclables sans couture (la fin des notes de la dernière
    /// mesure est repliée sur le début). Chaque <see cref="SongStyle"/> a sa basse, ses accords et sa batterie ;
    /// chaque <see cref="LeadVoice"/> son timbre. Pur calcul : peut tourner hors du fil principal.
    /// </summary>
    public static class MusicSynth
    {
        public const int Rate = 22050;
        const float Tau = (float)(Math.PI * 2.0);

        public static float[] Render(Song song)
        {
            float step = song.StepSeconds;
            int loop = (int)(song.Steps * step * Rate);
            int length = loop + (int)(Song.StepsPerBar * step * Rate);
            var buf = new float[length];
            var lead = new float[length];
            // Accompagnement noyé dans l'écho (styles aquatique, aérien, spatial).
            var wet = new float[length];

            var voice = LeadOf(song.Lead, out float gain);
            foreach (var n in Tune.ParseMelody(song.Melody))
                Note(lead, n.Start * step, n.Length * step, Hz(n.Midi), gain, voice);
            Echo(lead, (int)(3 * step * Rate), song.Style == SongStyle.Aquatic ? 0.4f : 0.28f);

            var rng = new Random(11);
            for (int bar = 0; bar < song.Bars; bar++)
            {
                var b = new Bar
                {
                    Buf = buf, Wet = wet, Chord = Tune.ChordNotes(song.Chords[bar]), T0 = bar * Song.StepsPerBar * step,
                    Step = step, Rng = rng, Index = bar,
                };
                switch (song.Style)
                {
                    case SongStyle.Driving: DrivingBar(b); break;
                    case SongStyle.Gallop: GallopBar(b); break;
                    case SongStyle.Tropical: TropicalBar(b); break;
                    case SongStyle.Calypso: CalypsoBar(b); break;
                    case SongStyle.Aquatic: AquaticBar(b); break;
                    case SongStyle.Airy: AiryBar(b); break;
                    case SongStyle.Space: SpaceBar(b); break;
                    case SongStyle.Sleigh: SleighBar(b); break;
                    case SongStyle.Heavy: HeavyBar(b); break;
                    case SongStyle.Disco: DiscoBar(b); break;
                    case SongStyle.Riviera: RivieraBar(b); break;
                    default: CalmBar(b); break;
                }
            }
            Echo(wet, (int)(3 * step * Rate), 0.45f);
            for (int i = 0; i < length; i++) buf[i] += lead[i] + wet[i];

            // Replie la queue sur le début, puis adoucit les crêtes.
            var outBuf = new float[loop];
            for (int i = 0; i < length; i++) outBuf[i % loop] += buf[i];
            for (int i = 0; i < loop; i++) outBuf[i] = (float)Math.Tanh(outBuf[i] * 1.2f) * 0.8f;
            return outBuf;
        }

        /// <summary>Une mesure à remplir : accord (fondamentale à l'octave 3), début, durée d'une croche.</summary>
        sealed class Bar
        {
            public float[] Buf, Wet;
            public int[] Chord;
            public float T0, Step;
            public Random Rng;
            public int Index;
            public int Root => Chord[0] - 12;
            public float At(float steps) => T0 + steps * Step;
        }

        // ------------------------------------------------------------------ accompagnements

        static void DrivingBar(Bar b)
        {
            // Basse pompée en croches : fondamentale, octave sur les contretemps.
            for (int s = 0; s < 8; s++)
                Note(b.Buf, b.At(s), b.Step * 0.85f, Hz(s % 2 == 1 ? b.Root + 12 : b.Root), 0.26f, Bass);
            // Arpège en doubles croches, à l'octave 5.
            for (int s = 0; s < 16; s++)
            {
                int midi = b.Chord[s % 3] + 24 + (s % 6 >= 3 ? 12 : 0);
                Note(b.Buf, b.At(s * 0.5f), b.Step * 0.45f, Hz(midi), 0.045f, Pluck);
            }
            // Batterie : grosse caisse sur 1, 3 et le « et » de 2 ; caisse claire sur 2 et 4 ; charleston en croches.
            Kick(b.Buf, b.At(0), 0.55f);
            Kick(b.Buf, b.At(3), 0.4f);
            Kick(b.Buf, b.At(4), 0.55f);
            Snare(b.Buf, b.At(2), 0.3f, b.Rng);
            Snare(b.Buf, b.At(6), 0.3f, b.Rng);
            for (int s = 0; s < 8; s++) Hat(b.Buf, b.At(s), s % 2 == 1 ? 0.09f : 0.05f, s % 2 == 1 ? 0.06f : 0.03f, b.Rng);
        }

        /// <summary>Port : basse « oum-pa » (fondamentale, quinte), accordéon sur les contretemps, mandoline en trémolo, tambourin.</summary>
        static void RivieraBar(Bar b)
        {
            Note(b.Buf, b.At(0), b.Step * 1.8f, Hz(b.Root), 0.26f, Bass);
            Note(b.Buf, b.At(4), b.Step * 1.8f, Hz(b.Root + 7), 0.22f, Bass);
            foreach (int s in new[] { 2, 6 })
                foreach (int m in b.Chord) Note(b.Buf, b.At(s), b.Step * 1.6f, Hz(m + 12), 0.03f, Pad);
            // Trémolo de mandoline : tierce puis quinte, en triples croches.
            for (int s = 0; s < 32; s++)
                Note(b.Buf, b.At(s * 0.25f), b.Step * 0.22f, Hz(b.Chord[1 + s / 16] + 24), 0.022f, Pluck);
            Kick(b.Buf, b.At(0), 0.45f);
            Kick(b.Buf, b.At(4), 0.4f);
            Snare(b.Buf, b.At(2), 0.12f, b.Rng);
            Snare(b.Buf, b.At(6), 0.12f, b.Rng);
            for (int s = 0; s < 8; s++) Hat(b.Buf, b.At(s), s % 2 == 1 ? 0.07f : 0.04f, 0.09f, b.Rng);
        }

        static void CalmBar(Bar b)
        {
            Note(b.Buf, b.T0, b.Step * 3.8f, Hz(b.Root), 0.26f, Bass);
            Note(b.Buf, b.At(4), b.Step * 3.8f, Hz(b.Root + 7), 0.22f, Bass);
            // Nappe douce et arpège en croches.
            foreach (int m in b.Chord) Note(b.Buf, b.T0, b.Step * 8f, Hz(m + 12), 0.035f, Pad);
            int[] order = { 0, 1, 2, 1, 0, 2, 1, 2 };
            for (int s = 0; s < 8; s++) Note(b.Buf, b.At(s), b.Step * 0.9f, Hz(b.Chord[order[s]] + 24), 0.05f, Pluck);
            Kick(b.Buf, b.T0, 0.35f);
            Snare(b.Buf, b.At(4), 0.12f, b.Rng);
            for (int s = 0; s < 8; s += 2) Hat(b.Buf, b.At(s), 0.035f, 0.03f, b.Rng);
        }

        /// <summary>Canyon : basse au galop (longue, brève, brève), accords grattés, sabots en bois.</summary>
        static void GallopBar(Bar b)
        {
            for (int beat = 0; beat < 4; beat++)
            {
                int root = beat % 2 == 0 ? b.Root : b.Root + 7;
                float t = b.At(beat * 2);
                Note(b.Buf, t, b.Step * 0.9f, Hz(root), 0.26f, Bass);
                Note(b.Buf, t + b.Step, b.Step * 0.4f, Hz(root), 0.18f, Bass);
                Note(b.Buf, t + b.Step * 1.5f, b.Step * 0.4f, Hz(root + 12), 0.16f, Bass);
                Block(b.Buf, t, 0.07f, 1500f);
                Block(b.Buf, t + b.Step, 0.05f, 1100f);
                Block(b.Buf, t + b.Step * 1.5f, 0.05f, 1100f);
            }
            // Accord gratté sur 2 et 4 (cordes décalées de quelques millisecondes).
            foreach (int s in new[] { 2, 6 })
                for (int i = 0; i < 3; i++)
                    Note(b.Buf, b.At(s) + i * 0.012f, b.Step * 1.2f, Hz(b.Chord[i] + 12), 0.05f, Pluck);
            Kick(b.Buf, b.At(0), 0.45f);
            Kick(b.Buf, b.At(4), 0.4f);
            Snare(b.Buf, b.At(2), 0.14f, b.Rng);
            Snare(b.Buf, b.At(6), 0.14f, b.Rng);
        }

        /// <summary>Jungle : marimba sur les contretemps, congas, shaker en doubles croches.</summary>
        static void TropicalBar(Bar b)
        {
            foreach (int s in new[] { 0, 3, 4, 6 })
                Note(b.Buf, b.At(s), b.Step * 0.8f, Hz(s == 6 ? b.Root + 7 : b.Root), 0.25f, Bass);
            for (int s = 1; s < 8; s += 2)
                foreach (int m in b.Chord) Note(b.Buf, b.At(s), b.Step * 0.6f, Hz(m + 12), 0.035f, Mallet);
            float[] conga = { 0, 1.5f, 2, 3.5f, 5, 5.5f, 6.5f, 7 };
            for (int i = 0; i < conga.Length; i++) Tom(b.Buf, b.At(conga[i]), i % 3 == 0 ? 210f : 290f, 0.2f);
            Kick(b.Buf, b.At(0), 0.45f);
            Kick(b.Buf, b.At(4), 0.4f);
            for (int s = 0; s < 16; s++) Hat(b.Buf, b.At(s * 0.5f), s % 2 == 1 ? 0.05f : 0.03f, 0.04f, b.Rng);
        }

        /// <summary>Lagon : basse syncopée de calypso, accords piqués, petite caisse claire sur les contretemps.</summary>
        static void CalypsoBar(Bar b)
        {
            Note(b.Buf, b.At(0), b.Step * 1.4f, Hz(b.Root), 0.26f, Bass);
            Note(b.Buf, b.At(3), b.Step * 0.9f, Hz(b.Root + 7), 0.22f, Bass);
            Note(b.Buf, b.At(4), b.Step * 1.4f, Hz(b.Root), 0.24f, Bass);
            Note(b.Buf, b.At(6), b.Step * 1.4f, Hz(b.Chord[1] - 12), 0.2f, Bass);
            foreach (int s in new[] { 1, 3, 5, 6 })
                foreach (int m in b.Chord) Note(b.Buf, b.At(s), b.Step * 0.45f, Hz(m + 12), 0.03f, Pluck);
            Kick(b.Buf, b.At(0), 0.4f);
            Kick(b.Buf, b.At(4), 0.35f);
            Snare(b.Buf, b.At(3), 0.12f, b.Rng);
            Snare(b.Buf, b.At(7), 0.12f, b.Rng);
            for (int s = 0; s < 16; s++) Hat(b.Buf, b.At(s * 0.5f), 0.03f, 0.035f, b.Rng);
        }

        /// <summary>Récif : nappe et basse tenues, arpège lent qui se perd dans l'écho, batterie à mi-temps.</summary>
        static void AquaticBar(Bar b)
        {
            Note(b.Buf, b.T0, b.Step * 7.8f, Hz(b.Root), 0.24f, Bass);
            foreach (int m in b.Chord) Note(b.Buf, b.T0, b.Step * 8f, Hz(m + 12), 0.04f, Pad);
            int[] order = { 0, 2, 1, 2 };
            for (int s = 0; s < 4; s++) Note(b.Wet, b.At(s * 2), b.Step * 1.5f, Hz(b.Chord[order[s]] + 24), 0.05f, Mallet);
            Kick(b.Buf, b.T0, 0.35f);
            Snare(b.Buf, b.At(4), 0.1f, b.Rng);
            for (int s = 1; s < 8; s += 2) Hat(b.Buf, b.At(s), 0.025f, 0.05f, b.Rng);
        }

        /// <summary>Nuages : nappe ample, arpège de clochettes qui monte sur deux octaves, batterie légère.</summary>
        static void AiryBar(Bar b)
        {
            Note(b.Buf, b.T0, b.Step * 3.8f, Hz(b.Root), 0.22f, Bass);
            Note(b.Buf, b.At(4), b.Step * 3.8f, Hz(b.Root + 7), 0.2f, Bass);
            foreach (int m in b.Chord) Note(b.Buf, b.T0, b.Step * 8f, Hz(m + 12), 0.04f, Pad);
            for (int s = 0; s < 8; s++) Note(b.Wet, b.At(s), b.Step * 0.9f, Hz(b.Chord[s % 3] + 24 + (s >= 3 ? 12 : 0)), 0.03f, Bell);
            Kick(b.Buf, b.At(0), 0.4f);
            Kick(b.Buf, b.At(5), 0.25f);
            Snare(b.Buf, b.At(4), 0.16f, b.Rng);
            for (int s = 0; s < 8; s++) Hat(b.Buf, b.At(s), 0.03f, 0.03f, b.Rng);
        }

        /// <summary>Espace : arpège de synthé en doubles croches, basse pulsée, caisse claire qui résonne.</summary>
        static void SpaceBar(Bar b)
        {
            for (int s = 0; s < 8; s++) Note(b.Buf, b.At(s), b.Step * 0.6f, Hz(b.Root - (s % 4 == 3 ? -12 : 0)), 0.22f, Bass);
            int[] arp = { 0, 1, 2, 3, 2, 1 };
            for (int s = 0; s < 16; s++)
            {
                int i = arp[s % arp.Length];
                int midi = i == 3 ? b.Chord[0] + 36 : b.Chord[i] + 24;
                Note(b.Wet, b.At(s * 0.5f), b.Step * 0.4f, Hz(midi), 0.035f, Saw);
            }
            foreach (int m in b.Chord) Note(b.Buf, b.T0, b.Step * 8f, Hz(m + 12), 0.03f, Pad);
            Kick(b.Buf, b.At(0), 0.5f);
            Kick(b.Buf, b.At(4), 0.5f);
            Snare(b.Buf, b.At(2), 0.22f, b.Rng, 9f);
            Snare(b.Buf, b.At(6), 0.22f, b.Rng, 9f);
            for (int s = 1; s < 8; s += 2) Hat(b.Buf, b.At(s), 0.05f, 0.05f, b.Rng);
        }

        /// <summary>Banquise : basse « boum-pa », cordes pincées sur les contretemps, grelots.</summary>
        static void SleighBar(Bar b)
        {
            for (int s = 0; s < 8; s += 2)
                Note(b.Buf, b.At(s), b.Step * 0.7f, Hz(s % 4 == 0 ? b.Root : b.Root + 7), 0.24f, Bass);
            for (int s = 1; s < 8; s += 2)
                foreach (int m in b.Chord) Note(b.Buf, b.At(s), b.Step * 0.4f, Hz(m + 12), 0.035f, Pluck);
            Kick(b.Buf, b.At(0), 0.4f);
            Kick(b.Buf, b.At(4), 0.4f);
            Snare(b.Buf, b.At(2), 0.14f, b.Rng);
            Snare(b.Buf, b.At(6), 0.14f, b.Rng);
            // Grelots : bruit aigu un peu long, en doubles croches.
            for (int s = 0; s < 16; s++) Hat(b.Buf, b.At(s * 0.5f), s % 4 == 0 ? 0.09f : 0.05f, 0.09f, b.Rng);
        }

        /// <summary>Volcan : basse grave martelée, accords de puissance saturés, double grosse caisse, roulement de toms.</summary>
        static void HeavyBar(Bar b)
        {
            for (int s = 0; s < 8; s++) Note(b.Buf, b.At(s), b.Step * 0.8f, Hz(b.Root - 12 + 12 * (s == 7 ? 1 : 0)), 0.28f, Bass);
            foreach (float s in new[] { 0f, 3f, 6f })
            {
                Note(b.Buf, b.At(s), b.Step * (s == 6f ? 2f : 2.8f), Hz(b.Chord[0]), 0.05f, Fuzz);
                Note(b.Buf, b.At(s), b.Step * (s == 6f ? 2f : 2.8f), Hz(b.Chord[0] + 7), 0.04f, Fuzz);
            }
            foreach (float s in new[] { 0f, 1f, 3f, 4f, 5f, 7f }) Kick(b.Buf, b.At(s), 0.42f);
            Snare(b.Buf, b.At(2), 0.32f, b.Rng);
            Snare(b.Buf, b.At(6), 0.32f, b.Rng);
            if (b.Index % 4 == 3)
                for (int s = 4; s < 8; s++) Tom(b.Buf, b.At(s), 200f - (s - 4) * 30f, 0.3f);
            else
                for (int s = 0; s < 8; s++) Hat(b.Buf, b.At(s), 0.05f, 0.03f, b.Rng);
        }

        /// <summary>Ville Néon : grosse caisse sur chaque temps, basse en octaves, charleston ouvert, accords syncopés.</summary>
        static void DiscoBar(Bar b)
        {
            for (int s = 0; s < 8; s++) Note(b.Buf, b.At(s), b.Step * 0.7f, Hz(s % 2 == 1 ? b.Root + 12 : b.Root), 0.25f, Bass);
            foreach (float s in new[] { 1.5f, 3f, 5.5f })
                foreach (int m in b.Chord) Note(b.Buf, b.At(s), b.Step * 0.7f, Hz(m + 12), 0.03f, Sync);
            for (int s = 0; s < 16; s++) Note(b.Wet, b.At(s * 0.5f), b.Step * 0.3f, Hz(b.Chord[s % 3] + 24), 0.02f, Pluck);
            for (int s = 0; s < 8; s += 2) Kick(b.Buf, b.At(s), 0.5f);
            Snare(b.Buf, b.At(2), 0.26f, b.Rng);
            Snare(b.Buf, b.At(6), 0.26f, b.Rng);
            for (int s = 1; s < 8; s += 2) Hat(b.Buf, b.At(s), 0.08f, 0.12f, b.Rng);
        }

        // ------------------------------------------------------------------ timbres

        static float Hz(int midi) => 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0);

        delegate float Voice(float phase, float t, float len);

        static Voice LeadOf(LeadVoice v, out float gain)
        {
            switch (v)
            {
                case LeadVoice.Twang: gain = 0.2f; return Twang;
                case LeadVoice.Mallet: gain = 0.32f; return Mallet;
                case LeadVoice.Steel: gain = 0.2f; return Steel;
                case LeadVoice.Bubble: gain = 0.26f; return Bubble;
                case LeadVoice.Flute: gain = 0.24f; return Flute;
                case LeadVoice.Bell: gain = 0.24f; return Bell;
                case LeadVoice.Saw: gain = 0.17f; return Saw;
                case LeadVoice.Fuzz: gain = 0.13f; return Fuzz;
                case LeadVoice.Sync: gain = 0.14f; return Sync;
                default: gain = 0.2f; return Lead;
            }
        }

        static void Note(float[] buf, float start, float len, float hz, float amp, Voice voice)
        {
            const float release = 0.05f;
            int i0 = (int)(start * Rate), n = (int)((len + release) * Rate);
            for (int k = 0; k < n && i0 + k < buf.Length; k++)
            {
                float t = k / (float)Rate;
                float env = Math.Min(1f, t / 0.006f) * (t > len ? Math.Max(0f, 1f - (t - len) / release) : 1f);
                buf[i0 + k] += voice(t * hz, t, len) * amp * env;
            }
        }

        static float DelayedVibrato(float t, float depth) =>
            (float)Math.Sin(t * 5.5f * Tau) * depth * Math.Min(1f, Math.Max(0f, (t - 0.15f) * 4f));

        static float Sin(float phase) => (float)Math.Sin(phase * Tau);

        // Carré 25 % avec vibrato qui arrive après le début de la note.
        static float Lead(float phase, float t, float len)
        {
            float p = Frac(phase * (1f + DelayedVibrato(t, 0.004f)));
            float decay = 0.65f + 0.35f * (float)Math.Exp(-t * 8f);
            return (p < 0.25f ? 1f : -0.33f) * decay;
        }

        // Corde pincée de guitare sèche : impulsion étroite qui s'éteint vite.
        static float Twang(float phase, float t, float len)
        {
            float p = Frac(phase);
            return ((p < 0.18f ? 1f : -0.2f) * 0.7f + Sin(phase) * 0.5f) * (0.45f + 0.55f * (float)Math.Exp(-t * 5f));
        }

        // Lame de bois frappée : fondamentale et harmonique 4 qui meurt tout de suite.
        static float Mallet(float phase, float t, float len) =>
            Sin(phase) * (float)Math.Exp(-t * 6f) + 0.35f * Sin(phase * 4f) * (float)Math.Exp(-t * 25f);

        // Tambour d'acier : harmoniques légèrement fausses, résonance moyenne.
        static float Steel(float phase, float t, float len) =>
            (Sin(phase) + 0.5f * Sin(phase * 2f) + 0.25f * Sin(phase * 3.01f)) * (float)Math.Exp(-t * 3.5f);

        // Bulle : sinus qui glisse vers sa note par en dessous, vibrato lent.
        static float Bubble(float phase, float t, float len)
        {
            float glide = 1f - 0.06f * (float)Math.Exp(-t * 18f);
            return (Sin(phase * glide * (1f + DelayedVibrato(t, 0.006f))) + 0.2f * Sin(phase * 2f)) * (0.6f + 0.4f * (float)Math.Exp(-t * 3f));
        }

        // Flûte : presque sinus, attaque douce, vibrato marqué.
        static float Flute(float phase, float t, float len)
        {
            float p = phase * (1f + DelayedVibrato(t, 0.006f));
            return (Sin(p) + 0.15f * Sin(p * 2f) + 0.05f * Sin(p * 3f)) * Math.Min(1f, t / 0.04f);
        }

        // Clochette : partiels inharmoniques qui s'éteignent à des vitesses différentes.
        static float Bell(float phase, float t, float len) =>
            (Sin(phase) + 0.45f * Sin(phase * 2.76f) * (float)Math.Exp(-t * 4f) + 0.2f * Sin(phase * 5.4f) * (float)Math.Exp(-t * 9f))
            * (float)Math.Exp(-t * 2.2f);

        // Dent de scie doublée, un peu désaccordée.
        static float Saw(float phase, float t, float len) =>
            (2f * Frac(phase) - 1f + 2f * Frac(phase * 1.006f) - 1f) * 0.5f * (0.75f + 0.25f * (float)Math.Exp(-t * 6f));

        // Guitare saturée : sinus écrasé plus une octave grave.
        static float Fuzz(float phase, float t, float len) =>
            (float)Math.Tanh(4f * (Sin(phase) + 0.5f * Sin(phase * 0.5f))) * 0.8f;

        // Synthé de nuit : deux carrés désaccordés.
        static float Sync(float phase, float t, float len) =>
            ((Frac(phase) < 0.5f ? 0.5f : -0.5f) + (Frac(phase * 1.008f) < 0.5f ? 0.5f : -0.5f)) * (0.7f + 0.3f * (float)Math.Exp(-t * 5f));

        static float Bass(float phase, float t, float len) => (1f - 4f * Math.Abs(Frac(phase) - 0.5f)) * (0.7f + 0.3f * (float)Math.Exp(-t * 6f));

        static float Pluck(float phase, float t, float len) => (Frac(phase) < 0.125f ? 1f : -0.15f) * (float)Math.Exp(-t * 14f);

        static float Pad(float phase, float t, float len)
        {
            float swell = Math.Min(1f, t / 0.4f);
            return (1f - 4f * Math.Abs(Frac(phase) - 0.5f) + 0.3f * Sin(phase * 2.003f)) * swell;
        }

        // ------------------------------------------------------------------ percussions

        static void Kick(float[] buf, float start, float amp)
        {
            int i0 = (int)(start * Rate), n = (int)(0.18f * Rate);
            float phase = 0f;
            for (int k = 0; k < n && i0 + k < buf.Length; k++)
            {
                float t = k / (float)Rate;
                phase += (45f + 120f * (float)Math.Exp(-t * 30f)) / Rate;
                buf[i0 + k] += Sin(phase) * amp * (float)Math.Exp(-t * 18f);
            }
        }

        /// <summary>Tambour accordé (conga, tom) : sinus qui descend un peu.</summary>
        static void Tom(float[] buf, float start, float hz, float amp)
        {
            int i0 = (int)(start * Rate), n = (int)(0.2f * Rate);
            float phase = 0f;
            for (int k = 0; k < n && i0 + k < buf.Length; k++)
            {
                float t = k / (float)Rate;
                phase += hz * (1f + 0.5f * (float)Math.Exp(-t * 25f)) / Rate;
                buf[i0 + k] += Sin(phase) * amp * (float)Math.Exp(-t * 14f);
            }
        }

        /// <summary>Bloc de bois : clic aigu très bref.</summary>
        static void Block(float[] buf, float start, float amp, float hz)
        {
            int i0 = (int)(start * Rate), n = (int)(0.04f * Rate);
            for (int k = 0; k < n && i0 + k < buf.Length; k++)
            {
                float t = k / (float)Rate;
                buf[i0 + k] += Sin(t * hz) * amp * (float)Math.Exp(-t * 90f);
            }
        }

        static void Snare(float[] buf, float start, float amp, Random rng, float decay = 22f)
        {
            int i0 = (int)(start * Rate), n = (int)(Math.Min(0.5f, 3.5f / decay) * Rate);
            for (int k = 0; k < n && i0 + k < buf.Length; k++)
            {
                float t = k / (float)Rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                buf[i0 + k] += (noise * 0.8f + Sin(t * 190f) * 0.4f) * amp * (float)Math.Exp(-t * decay);
            }
        }

        static void Hat(float[] buf, float start, float amp, float len, Random rng)
        {
            int i0 = (int)(start * Rate), n = (int)(len * Rate);
            float prev = 0f;
            for (int k = 0; k < n && i0 + k < buf.Length; k++)
            {
                float t = k / (float)Rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                float high = noise - prev; // passe-haut grossier : garde le grésillement
                prev = noise;
                buf[i0 + k] += high * amp * (1f - t / len);
            }
        }

        static void Echo(float[] buf, int delay, float feedback)
        {
            for (int i = delay; i < buf.Length; i++) buf[i] += buf[i - delay] * feedback;
        }

        static float Frac(float x) => x - (float)Math.Floor(x);
    }
}
