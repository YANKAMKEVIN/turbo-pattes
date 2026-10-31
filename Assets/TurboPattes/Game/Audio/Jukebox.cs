using System.Collections.Generic;
using System.Threading.Tasks;
using TurboPattes.Core;
using UnityEngine;

namespace TurboPattes.Game.Audio
{
    /// <summary>
    /// Musique et sons d'interface. Survit aux rechargements de scène (Rejouer, Menu) : la musique enchaîne
    /// sans repartir de zéro et le son d'un bouton n'est pas coupé. Les morceaux sont calculés en arrière-plan
    /// au premier lancement.
    /// </summary>
    public sealed class Jukebox : MonoBehaviour
    {
        const float FadeSpeed = 0.8f;

        static Jukebox _instance;
        static readonly Dictionary<Song, Task<float[]>> Renders = new();

        AudioSource _music, _ui;
        readonly Dictionary<Song, AudioClip> _clips = new();
        Song _wanted;
        float _level = 0.5f;
        AudioClip _move, _confirm, _back;

        public static Jukebox Instance
        {
            get
            {
                if (_instance != null) return _instance;
                var go = new GameObject("Jukebox");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<Jukebox>();
                _instance.Build();
                return _instance;
            }
        }

        void Build()
        {
            _music = gameObject.AddComponent<AudioSource>();
            _music.loop = true;
            _music.volume = 0f;
            // La pause coupe tous les sons (AudioListener.pause) ; la musique continue, plus bas.
            _music.ignoreListenerPause = true;
            _ui = gameObject.AddComponent<AudioSource>();
            _ui.ignoreListenerPause = true;

            _move = Blip("UiMove", new[] { 1180f }, 0.05f, 0.25f);
            _confirm = Blip("UiConfirm", new[] { 784f, 1175f }, 0.07f, 0.35f);
            _back = Blip("UiBack", new[] { 880f, 587f }, 0.07f, 0.3f);

            foreach (var song in new[] { Songs.Menu, Songs.Race })
                if (!Renders.ContainsKey(song)) Renders[song] = Task.Run(() => MusicSynth.Render(song));
        }

        /// <summary>Enchaîne sur ce morceau (fondu) ; null coupe la musique.</summary>
        public void Play(Song song, float level = 0.5f)
        {
            _wanted = song;
            _level = level;
        }

        /// <summary>Volume du morceau en cours (baisse pendant la pause, à l'arrivée…).</summary>
        public void SetLevel(float level) => _level = level;

        public void Move() => _ui.PlayOneShot(_move);
        public void Confirm() => _ui.PlayOneShot(_confirm);
        public void Back() => _ui.PlayOneShot(_back);

        void Update()
        {
            float step = FadeSpeed * Time.unscaledDeltaTime;
            bool switching = _wanted == null ? _music.clip != null : _music.clip == null || _music.clip.name != _wanted.Name;
            if (!switching)
            {
                _music.volume = Mathf.MoveTowards(_music.volume, _level, step);
                return;
            }

            // Fondu de sortie, puis départ du nouveau morceau dès qu'il est calculé.
            if (_music.isPlaying && _music.volume > 0f)
            {
                _music.volume = Mathf.MoveTowards(_music.volume, 0f, step * 2f);
                return;
            }
            _music.Stop();
            _music.clip = null;
            if (_wanted == null || !TryClip(_wanted, out var clip)) return;
            _music.clip = clip;
            _music.volume = 0f;
            _music.Play();
        }

        bool TryClip(Song song, out AudioClip clip)
        {
            if (_clips.TryGetValue(song, out clip)) return true;
            if (!Renders.TryGetValue(song, out var task)) Renders[song] = task = Task.Run(() => MusicSynth.Render(song));
            if (!task.IsCompleted) return false;
            if (task.IsFaulted)
            {
                Debug.LogError($"[Jukebox] {song.Name} : {task.Exception?.GetBaseException().Message}");
                _wanted = null;
                return false;
            }
            var data = task.Result;
            clip = AudioClip.Create(song.Name, data.Length, 1, MusicSynth.Rate, false);
            clip.SetData(data, 0);
            _clips[song] = clip;
            Debug.Log($"[Jukebox] {song.Name} : {data.Length / (float)MusicSynth.Rate:0.0} s en boucle");
            return true;
        }

        static AudioClip Blip(string name, float[] notes, float noteLen, float volume)
        {
            const int rate = 44100;
            int per = (int)(noteLen * rate), n = per * notes.Length;
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = (i % per) / (float)rate;
                float f = notes[i / per];
                float env = Mathf.Clamp01(t * 400f) * Mathf.Exp(-t * 30f);
                data[i] = (Mathf.Sin(t * f * Mathf.PI * 2f) * 0.8f + (Mathf.Repeat(t * f, 1f) < 0.5f ? 0.2f : -0.2f)) * env * volume;
            }
            var clip = AudioClip.Create(name, n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
