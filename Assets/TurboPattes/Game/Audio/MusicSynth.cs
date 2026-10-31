using System;
using TurboPattes.Core;

namespace TurboPattes.Game.Audio
{
    /// <summary>
    /// Joue un <see cref="Song"/> en échantillons mono, bouclables sans couture (la fin des notes de la dernière
    /// mesure est repliée sur le début). Pur calcul : peut tourner hors du fil principal.
    /// </summary>
    public static class MusicSynth
    {
        public const int Rate = 22050;
        const float Tau = (float)(Math.PI * 2.0);

        public static float[] Render(Song song)
        {
            float step = song.StepSeconds;
            int loop = (int)(song.Steps * step * Rate);
            var buf = new float[loop + (int)(Song.StepsPerBar * step * Rate)];
            var lead = new float[buf.Length];

            foreach (var n in Tune.ParseMelody(song.Melody))
                Note(lead, n.Start * step, n.Length * step, Hz(n.Midi), 0.2f, Lead);
            Echo(lead, (int)(3 * step * Rate), 0.28f);
            for (int i = 0; i < buf.Length; i++) buf[i] = lead[i];

            var rng = new Random(11);
            for (int bar = 0; bar < song.Bars; bar++)
            {
                int[] chord = Tune.ChordNotes(song.Chords[bar]);
                float t0 = bar * Song.StepsPerBar * step;
                if (song.Driving) DrivingBar(buf, chord, t0, step, rng);
                else CalmBar(buf, chord, t0, step, rng);
            }

            // Replie la queue sur le début, puis adoucit les crêtes.
            var outBuf = new float[loop];
            for (int i = 0; i < buf.Length; i++) outBuf[i % loop] += buf[i];
            for (int i = 0; i < loop; i++) outBuf[i] = (float)Math.Tanh(outBuf[i] * 1.2f) * 0.8f;
            return outBuf;
        }

        static void DrivingBar(float[] buf, int[] chord, float t0, float step, Random rng)
        {
            int root = chord[0] - 12;
            // Basse pompée en croches : fondamentale, octave sur les contretemps.
            for (int s = 0; s < 8; s++)
                Note(buf, t0 + s * step, step * 0.85f, Hz(s % 2 == 1 ? root + 12 : root), 0.26f, Bass);
            // Arpège en doubles croches, à l'octave 5.
            for (int s = 0; s < 16; s++)
            {
                int midi = chord[s % 3] + 24 + (s % 6 >= 3 ? 12 : 0);
                Note(buf, t0 + s * step * 0.5f, step * 0.45f, Hz(midi), 0.045f, Pluck);
            }
            // Batterie : grosse caisse sur 1, 3 et le « et » de 2 ; caisse claire sur 2 et 4 ; charleston en croches.
            Kick(buf, t0, 0.55f);
            Kick(buf, t0 + 3 * step, 0.4f);
            Kick(buf, t0 + 4 * step, 0.55f);
            Snare(buf, t0 + 2 * step, 0.3f, rng);
            Snare(buf, t0 + 6 * step, 0.3f, rng);
            for (int s = 0; s < 8; s++) Hat(buf, t0 + s * step, s % 2 == 1 ? 0.09f : 0.05f, s % 2 == 1 ? 0.06f : 0.03f, rng);
        }

        static void CalmBar(float[] buf, int[] chord, float t0, float step, Random rng)
        {
            int root = chord[0] - 12;
            Note(buf, t0, step * 3.8f, Hz(root), 0.26f, Bass);
            Note(buf, t0 + 4 * step, step * 3.8f, Hz(root + 7), 0.22f, Bass);
            // Nappe douce et arpège en croches.
            foreach (int m in chord) Note(buf, t0, step * 8f, Hz(m + 12), 0.035f, Pad);
            int[] order = { 0, 1, 2, 1, 0, 2, 1, 2 };
            for (int s = 0; s < 8; s++) Note(buf, t0 + s * step, step * 0.9f, Hz(chord[order[s]] + 24), 0.05f, Pluck);
            Kick(buf, t0, 0.35f);
            Snare(buf, t0 + 4 * step, 0.12f, rng);
            for (int s = 0; s < 8; s += 2) Hat(buf, t0 + s * step, 0.035f, 0.03f, rng);
        }

        static float Hz(int midi) => 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0);

        delegate float Voice(float phase, float t, float len);

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

        // Carré 25 % avec vibrato qui arrive après le début de la note.
        static float Lead(float phase, float t, float len)
        {
            float vib = (float)Math.Sin(t * 5.5f * Tau) * 0.004f * Math.Min(1f, Math.Max(0f, (t - 0.15f) * 4f));
            float p = Frac(phase * (1f + vib));
            float decay = 0.65f + 0.35f * (float)Math.Exp(-t * 8f);
            return (p < 0.25f ? 1f : -0.33f) * decay;
        }

        static float Bass(float phase, float t, float len) => (1f - 4f * Math.Abs(Frac(phase) - 0.5f)) * (0.7f + 0.3f * (float)Math.Exp(-t * 6f));

        static float Pluck(float phase, float t, float len) => (Frac(phase) < 0.125f ? 1f : -0.15f) * (float)Math.Exp(-t * 14f);

        static float Pad(float phase, float t, float len)
        {
            float swell = Math.Min(1f, t / 0.4f);
            return (1f - 4f * Math.Abs(Frac(phase) - 0.5f) + 0.3f * (float)Math.Sin(phase * 2.003f * Tau)) * swell;
        }

        static void Kick(float[] buf, float start, float amp)
        {
            int i0 = (int)(start * Rate), n = (int)(0.18f * Rate);
            float phase = 0f;
            for (int k = 0; k < n && i0 + k < buf.Length; k++)
            {
                float t = k / (float)Rate;
                phase += (45f + 120f * (float)Math.Exp(-t * 30f)) / Rate;
                buf[i0 + k] += (float)Math.Sin(phase * Tau) * amp * (float)Math.Exp(-t * 18f);
            }
        }

        static void Snare(float[] buf, float start, float amp, Random rng)
        {
            int i0 = (int)(start * Rate), n = (int)(0.16f * Rate);
            for (int k = 0; k < n && i0 + k < buf.Length; k++)
            {
                float t = k / (float)Rate;
                float noise = (float)rng.NextDouble() * 2f - 1f;
                buf[i0 + k] += (noise * 0.8f + (float)Math.Sin(t * 190f * Tau) * 0.4f) * amp * (float)Math.Exp(-t * 22f);
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
