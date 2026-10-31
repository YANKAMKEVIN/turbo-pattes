using System;
using System.Collections.Generic;

namespace TurboPattes.Core
{
    /// <summary>Tremplin : la piste monte en arc sur <see cref="Length"/> mètres puis retombe d'un coup au bord (<see cref="LipS"/>).</summary>
    public readonly struct Kicker
    {
        public readonly float LipS;
        public readonly float Length;
        public readonly float Rise;

        public Kicker(float lipS, float length, float rise)
        {
            LipS = lipS;
            Length = length;
            Rise = rise;
        }
    }

    /// <summary>
    /// Hauteur de la piste le long du tour (circuit fermé). Les clés (distance, hauteur) sont reliées par une cubique
    /// monotone (Fritsch-Carlson) : pas de bosse ni de creux en dehors des clés, et pente continue partout.
    /// Les tremplins s'ajoutent par-dessus, seulement sur le bitume et les vibreurs.
    /// </summary>
    public sealed class HeightProfile
    {
        public float Length { get; }
        public IReadOnlyList<Kicker> Kickers => _kickers;

        readonly float[] _s, _h, _m;
        readonly Kicker[] _kickers;

        /// <param name="keyS">Distances croissantes dans [0, length).</param>
        public HeightProfile(float[] keyS, float[] keyH, float length, IList<Kicker> kickers = null)
        {
            if (keyS == null || keyH == null || keyS.Length != keyH.Length || keyS.Length < 2) throw new ArgumentException("clés invalides");
            if (length <= 0f) throw new ArgumentOutOfRangeException(nameof(length));
            for (int i = 0; i < keyS.Length; i++)
            {
                if (keyS[i] < 0f || keyS[i] >= length) throw new ArgumentOutOfRangeException(nameof(keyS));
                if (i > 0 && keyS[i] <= keyS[i - 1]) throw new ArgumentException("distances non croissantes");
            }
            Length = length;
            _s = (float[])keyS.Clone();
            _h = (float[])keyH.Clone();
            _kickers = kickers == null ? Array.Empty<Kicker>() : new List<Kicker>(kickers).ToArray();
            int n = _s.Length;
            _m = new float[n];
            for (int i = 0; i < n; i++)
            {
                float d0 = Secant(i - 1), d1 = Secant(i);
                // Tangente nulle aux extremums : la courbe ne dépasse jamais les clés.
                _m[i] = d0 * d1 <= 0f ? 0f : 2f / (1f / d0 + 1f / d1);
            }
        }

        int Wrap(int i) => (i % _s.Length + _s.Length) % _s.Length;

        /// <summary>Longueur de l'intervalle qui part de la clé <paramref name="i"/> (le dernier rejoint la première).</summary>
        float Span(int i)
        {
            i = Wrap(i);
            float next = i + 1 < _s.Length ? _s[i + 1] : _s[0] + Length;
            return next - _s[i];
        }

        float Secant(int i) => (_h[Wrap(i + 1)] - _h[Wrap(i)]) / Span(i);

        public float WrapS(float s)
        {
            s %= Length;
            return s < 0f ? s + Length : s;
        }

        /// <summary>Hauteur sans les tremplins (talus, piles de pont, murets).</summary>
        public float BaseHeight(float s) => Base(s, out _);

        /// <summary>Pente (dh/ds) sans les tremplins.</summary>
        public float BaseSlope(float s)
        {
            Base(s, out float slope);
            return slope;
        }

        float Base(float s, out float slope)
        {
            s = WrapS(s);
            int n = _s.Length;
            int i = n - 1;
            if (s >= _s[0])
            {
                int lo = 0, hi = n - 1;
                while (lo < hi)
                {
                    int mid = (lo + hi + 1) >> 1;
                    if (_s[mid] <= s) lo = mid; else hi = mid - 1;
                }
                i = lo;
            }
            float start = _s[i];
            if (s < start) start -= Length; // avant la première clé : intervalle qui boucle depuis la dernière
            float span = Span(i);
            float t = (s - start) / span;
            int j = Wrap(i + 1);
            float t2 = t * t, t3 = t2 * t;
            float h00 = 2f * t3 - 3f * t2 + 1f, h10 = t3 - 2f * t2 + t, h01 = -2f * t3 + 3f * t2, h11 = t3 - t2;
            float d00 = 6f * t2 - 6f * t, d10 = 3f * t2 - 4f * t + 1f, d01 = -6f * t2 + 6f * t, d11 = 3f * t2 - 2f * t;
            slope = (d00 * _h[i] + d01 * _h[j]) / span + d10 * _m[i] + d11 * _m[j];
            return h00 * _h[i] + h10 * span * _m[i] + h01 * _h[j] + h11 * span * _m[j];
        }

        /// <summary>Surélévation due aux tremplins à la distance <paramref name="s"/>.</summary>
        public float Kick(float s, out float slope)
        {
            slope = 0f;
            float total = 0f;
            foreach (var k in _kickers)
            {
                float before = WrapS(k.LipS - WrapS(s));
                if (before <= 0f || before > k.Length) continue;
                float u = 1f - before / k.Length;
                total += k.Rise * u * u;
                slope += 2f * k.Rise * u / k.Length;
            }
            return total;
        }

        /// <summary>Hauteur du sol ; <paramref name="onRamp"/> = le kart est sur la largeur du tremplin.</summary>
        public float Height(float s, bool onRamp = true)
        {
            float h = BaseHeight(s);
            return onRamp ? h + Kick(s, out _) : h;
        }

        public float Slope(float s, bool onRamp = true)
        {
            float slope = BaseSlope(s);
            if (onRamp)
            {
                Kick(s, out float k);
                slope += k;
            }
            return slope;
        }
    }
}
