using System;
using TurboPattes.Game.App;
using TurboPattes.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Réglages (depuis le titre ou la pause) : volumes, sensibilité tactile, vibrations, qualité graphique.
    /// Chaque changement est enregistré tout de suite. Pendant qu'il est ouvert, <see cref="IsOpen"/> dit aux autres écrans
    /// d'ignorer les commandes. Marche aussi en pause (temps arrêté).
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        static readonly string[] Rows = { "Musique", "Effets", "Sensibilité tactile", "Vibrations", "Qualité graphique", "Retour" };
        static readonly string[] QualityNames = { "Auto", "Maximale", "Haute", "Moyenne", "Basse", "Minimale" };

        public static bool IsOpen { get; private set; }

        readonly UiKit _ui = new();
        Action _closed;
        int _row;

        /// <summary>Ouvre l'écran ; <paramref name="closed"/> est appelé au retour.</summary>
        public static SettingsScreen Open(Action closed = null)
        {
            var screen = new GameObject("Settings").AddComponent<SettingsScreen>();
            screen._closed = closed;
            IsOpen = true;
            return screen;
        }

        void OnDestroy() => IsOpen = false;

        // ------------------------------------------------------------------ disposition

        static float S => Screen.height / 720f;

        static Rect RowRect(int i)
        {
            float s = S, w = 640 * s;
            return new Rect(Screen.width * 0.5f - w * 0.5f, Screen.height * 0.2f + i * 74 * s, w, 62 * s);
        }

        static Rect Minus(int i) { var r = RowRect(i); return new Rect(r.xMax - 250 * S, r.y, 62 * S, r.height); }
        static Rect Plus(int i) { var r = RowRect(i); return new Rect(r.xMax - 62 * S, r.y, 62 * S, r.height); }

        // ------------------------------------------------------------------ logique

        void Update()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            int dx = 0, dy = 0;
            if (kb != null)
            {
                if (kb.leftArrowKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.qKey.wasPressedThisFrame) dx--;
                if (kb.rightArrowKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame) dx++;
                if (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.zKey.wasPressedThisFrame) dy--;
                if (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame) dy++;
            }
            if (pad != null)
            {
                if (pad.dpad.left.wasPressedThisFrame) dx--;
                if (pad.dpad.right.wasPressedThisFrame) dx++;
                if (pad.dpad.up.wasPressedThisFrame) dy--;
                if (pad.dpad.down.wasPressedThisFrame) dy++;
            }
            bool confirm = kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) || pad != null && pad.buttonSouth.wasPressedThisFrame;
            bool back = kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame) || pad != null && pad.buttonEast.wasPressedThisFrame;

            if (back || confirm && _row == Rows.Length - 1) { Close(); return; }
            if (dy != 0) { _row = Mathf.Clamp(_row + dy, 0, Rows.Length - 1); Jukebox.Instance.Move(); }
            if (dx != 0) Change(_row, dx);
            else if (confirm && _row == 3) Change(3, 1);

            var tap = UiKit.Tap();
            if (!tap.HasValue) return;
            for (int i = 0; i < Rows.Length; i++)
            {
                if (i == Rows.Length - 1)
                {
                    if (RowRect(i).Contains(tap.Value)) { Close(); return; }
                    continue;
                }
                if (Minus(i).Contains(tap.Value)) { _row = i; Change(i, -1); }
                else if (Plus(i).Contains(tap.Value) || i == 3 && RowRect(i).Contains(tap.Value)) { _row = i; Change(i, 1); }
            }
        }

        static void Change(int row, int d)
        {
            switch (row)
            {
                case 0: GameSession.MusicVolume = Step(GameSession.MusicVolume, d * 0.1f, 0f, 1f); break;
                case 1: GameSession.SfxVolume = Step(GameSession.SfxVolume, d * 0.1f, 0f, 1f); break;
                case 2: GameSession.TouchSensitivity = Step(GameSession.TouchSensitivity, d * 0.1f, 0.5f, 1.5f); break;
                case 3:
                    GameSession.Vibration = !GameSession.Vibration;
                    Haptics.Pulse(60);
                    break;
                case 4:
                    GameSession.QualityMode = Mathf.Clamp(GameSession.QualityMode + d, -1, QualityTuner.MaxLevel);
                    QualityTuner.Apply(QualityTuner.Effective);
                    break;
                default: return;
            }
            Jukebox.Instance.Move();
        }

        // Pas réguliers sans dérive des flottants (0,1 + 0,1 + 0,1…).
        static float Step(float v, float d, float min, float max) => Mathf.Clamp(Mathf.Round((v + d) * 10f) / 10f, min, max);

        void Close()
        {
            Jukebox.Instance.Back();
            Destroy(gameObject);
            _closed?.Invoke();
        }

        // ------------------------------------------------------------------ dessin

        void OnGUI()
        {
            GUI.depth = -10;
            _ui.EnsureStyles();
            float s = S, w = Screen.width, h = Screen.height;
            UiKit.Box(new Rect(0, 0, w, h), UiKit.Alpha(UiKit.Ink, 0.94f));
            _ui.Label(new Rect(0, h * 0.06f, w, 80 * s), "Réglages", _ui.Big, UiKit.Cream, TextAnchor.MiddleCenter);
            UiKit.Slash(new Rect(w * 0.5f - 70 * s, h * 0.06f + 76 * s, 140 * s, 8 * s), UiKit.Vermilion, 1.2f);
            for (int i = 0; i < Rows.Length; i++)
            {
                var r = RowRect(i);
                bool focus = i == _row;
                if (i == Rows.Length - 1)
                {
                    _ui.Button(new Rect(r.center.x - 150 * s, r.y + 10 * s, 300 * s, r.height), "Retour", true, focus);
                    continue;
                }
                // Ligne en fiche crème ; la ligne active est surlignée en vermillon.
                UiKit.Card(r, focus ? UiKit.Vermilion : UiKit.Cream, 16 * s, 4 * s);
                var ink = focus ? UiKit.Cream : UiKit.Ink;
                if (focus) UiKit.Slash(new Rect(r.x + 10 * s, r.y + r.height * 0.22f, 7 * s, r.height * 0.56f), UiKit.Ochre);
                _ui.Label(new Rect(r.x + 28 * s, r.y, 300 * s, r.height), Rows[i], _ui.Medium, ink, TextAnchor.MiddleLeft);
                var valueRect = new Rect(Minus(i).xMax, r.y, Plus(i).x - Minus(i).xMax, r.height);
                _ui.Label(valueRect, Value(i), _ui.Medium, focus ? UiKit.Ochre : UiKit.Vermilion, TextAnchor.MiddleCenter);
                if (i != 3)
                {
                    var minus = Minus(i); var plus = Plus(i);
                    float m = 7 * s;
                    _ui.Button(new Rect(minus.x + m, minus.y + m, minus.width - 2 * m, minus.height - 2 * m), "-", false);
                    _ui.Button(new Rect(plus.x + m, plus.y + m, plus.width - 2 * m, plus.height - 2 * m), "+", false);
                }
            }
        }

        static string Value(int row) => row switch
        {
            0 => $"{Mathf.RoundToInt(GameSession.MusicVolume * 100)} %",
            1 => $"{Mathf.RoundToInt(GameSession.SfxVolume * 100)} %",
            2 => $"{GameSession.TouchSensitivity:0.0}",
            3 => GameSession.Vibration ? "Oui" : "Non",
            _ => QualityNames[GameSession.QualityMode + 1],
        };
    }
}
