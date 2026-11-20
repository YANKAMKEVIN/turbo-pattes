using UnityEngine;

namespace TurboPattes.Game.Audio
{
    /// <summary>Sons synthétisés au lancement : moteur, bips du départ, turbo, chocs, objets.</summary>
    public sealed class SoundBank : MonoBehaviour
    {
        const int Rate = 44100;
        AudioSource _engine, _drift, _oneShots;
        AudioClip _beep, _go, _boost, _bump, _pop, _hit, _tick, _levelUp, _finish, _throw, _warn;

        public static SoundBank Create(Transform parent)
        {
            var go = new GameObject("Sound");
            go.transform.SetParent(parent, false);
            var bank = go.AddComponent<SoundBank>();
            bank.Build();
            return bank;
        }

        void Build()
        {
            _engine = gameObject.AddComponent<AudioSource>();
            _engine.clip = Make("Engine", 1f, (t, i) =>
            {
                float f = 70f;
                float saw = 2f * (t * f - Mathf.Floor(t * f + 0.5f));
                float saw2 = 2f * (t * f * 2.01f - Mathf.Floor(t * f * 2.01f + 0.5f));
                return (saw * 0.5f + saw2 * 0.25f + Mathf.Sin(t * f * Mathf.PI * 2f) * 0.4f) * 0.35f;
            });
            _engine.loop = true;
            _engine.volume = 0f;
            _engine.Play();

            _drift = gameObject.AddComponent<AudioSource>();
            var noise = new System.Random(7);
            float lp = 0f;
            _drift.clip = Make("Drift", 1f, (t, i) =>
            {
                lp += ((float)noise.NextDouble() * 2f - 1f - lp) * 0.25f;
                return lp * 0.5f;
            });
            _drift.loop = true;
            _drift.volume = 0f;
            _drift.Play();

            _oneShots = gameObject.AddComponent<AudioSource>();

            _beep = Tone("Beep", 0.18f, 523f, 523f);
            _go = Tone("Go", 0.5f, 1046f, 1046f);
            _tick = Tone("Tick", 0.04f, 1500f, 1500f, 0.3f);
            _levelUp = Tone("LevelUp", 0.12f, 880f, 1320f, 0.35f);
            _pop = Tone("Pop", 0.12f, 400f, 1400f, 0.6f);
            _throw = Tone("Throw", 0.18f, 700f, 250f, 0.5f);
            _warn = Tone("Warn", 0.07f, 1760f, 1760f, 0.4f);
            _finish = Make("Finish", 1.2f, (t, i) =>
            {
                float[] notes = { 523f, 659f, 784f, 1046f };
                int n = Mathf.Min(3, (int)(t / 0.2f));
                float env = Mathf.Clamp01(1f - (t - n * 0.2f) / (n == 3 ? 0.6f : 0.2f));
                return Mathf.Sin(t * notes[n] * Mathf.PI * 2f) * 0.4f * env;
            });
            var rng = new System.Random(3);
            _boost = Make("Boost", 0.6f, (t, i) =>
            {
                float env = Mathf.Clamp01(1f - t / 0.6f);
                float n = (float)rng.NextDouble() * 2f - 1f;
                return (n * 0.3f + Mathf.Sin(t * (300f + t * 900f) * Mathf.PI * 2f) * 0.3f) * env;
            });
            _bump = Make("Bump", 0.2f, (t, i) =>
            {
                float env = Mathf.Clamp01(1f - t / 0.2f);
                return (Mathf.Sin(t * 90f * Mathf.PI * 2f) * 0.7f + ((float)rng.NextDouble() - 0.5f) * 0.4f) * env;
            });
            _hit = Make("Hit", 0.5f, (t, i) =>
            {
                float env = Mathf.Clamp01(1f - t / 0.5f);
                return (Mathf.Sin(t * (600f - t * 900f) * Mathf.PI * 2f) * 0.4f + ((float)rng.NextDouble() - 0.5f) * 0.3f) * env;
            });
        }

        static AudioClip Make(string name, float seconds, System.Func<float, int, float> sample)
        {
            int n = Mathf.CeilToInt(seconds * Rate);
            var data = new float[n];
            for (int i = 0; i < n; i++) data[i] = sample(i / (float)Rate, i);
            var clip = AudioClip.Create(name, n, 1, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static AudioClip Tone(string name, float seconds, float f0, float f1, float volume = 0.45f)
        {
            float phase = 0f;
            return Make(name, seconds, (t, i) =>
            {
                float k = t / seconds;
                phase += Mathf.Lerp(f0, f1, k) / Rate;
                float env = Mathf.Clamp01(1f - k) * Mathf.Clamp01(t * 200f);
                return (phase % 1f < 0.5f ? 1f : -1f) * 0.35f * volume * env + Mathf.Sin(phase * Mathf.PI * 2f) * 0.5f * volume * env;
            });
        }

        /// <param name="speed01">Vitesse rapportée à la vitesse maximale.</param>
        public void SetEngine(float speed01, bool boosting, bool active)
        {
            float targetVol = active ? 0.28f * App.GameSession.SfxVolume : 0f;
            _engine.volume = Mathf.MoveTowards(_engine.volume, targetVol, Time.deltaTime);
            float pitch = 0.55f + speed01 * 1.1f + (boosting ? 0.25f : 0f);
            _engine.pitch = Mathf.Lerp(_engine.pitch, pitch, 1f - Mathf.Exp(-8f * Time.deltaTime));
        }

        public void SetDrift(bool drifting)
        {
            _drift.volume = Mathf.MoveTowards(_drift.volume, drifting ? 0.18f * App.GameSession.SfxVolume : 0f, Time.deltaTime * 2f);
        }

        void Play(AudioClip c, float volume = 1f) => _oneShots.PlayOneShot(c, volume * App.GameSession.SfxVolume);

        public void Beep() => Play(_beep);
        public void Go() => Play(_go);
        public void Boost() => Play(_boost, 0.8f);
        public void Bump() => Play(_bump, 0.8f);
        public void Pop() => Play(_pop, 0.7f);
        public void Hit() => Play(_hit);
        public void Tick() => Play(_tick, 0.5f);
        public void LevelUp() => Play(_levelUp, 0.6f);
        public void Finish() => Play(_finish);
        public void Throw() => Play(_throw, 0.7f);
        /// <summary>Bip d'alerte : projectile qui arrive par l'arrière.</summary>
        public void Warn() => Play(_warn, 0.55f);
    }
}
