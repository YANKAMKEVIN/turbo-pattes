using System;
using TurboPattes.Core;
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
    /// DA « arcade sportif » : fiches blanc piste rangées par section (Son, Commandes, Affichage), jauges en crans obliques avec
    /// la valeur en chiffres fixes, bascules en deux segments, ligne active surlignée en rouge (<see cref="SettingsRows"/>).
    /// </summary>
    public sealed class SettingsScreen : MonoBehaviour
    {
        static readonly string[] Rows = SettingsRows.Labels;
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

        /// <summary>Bascules : segment de gauche (premier choix) ou de droite, sur la place des boutons - / +.</summary>
        static Rect ToggleHalf(int i, int half)
        {
            var r = RowRect(i);
            float s = S, x0 = r.xMax - 250 * s, w = (250 * s - 14 * s) * 0.5f;
            return new Rect(x0 + half * (w + 6 * s), r.y + 9 * s, w, r.height - 18 * s);
        }

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
            else if (confirm && Toggle(_row)) Change(_row, 1);

            var tap = UiKit.Tap();
            if (!tap.HasValue) return;
            for (int i = 0; i < Rows.Length; i++)
            {
                if (i == Rows.Length - 1)
                {
                    if (RowRect(i).Contains(tap.Value)) { Close(); return; }
                    continue;
                }
                if (Toggle(i))
                {
                    // Toucher un segment le choisit ; toucher ailleurs sur la ligne bascule.
                    int half = ToggleHalf(i, 0).Contains(tap.Value) ? 0 : ToggleHalf(i, 1).Contains(tap.Value) ? 1 : -1;
                    if (half >= 0) { _row = i; if (half != ToggleSide(i)) Change(i, 1); }
                    else if (RowRect(i).Contains(tap.Value)) { _row = i; Change(i, 1); }
                }
                else if (Minus(i).Contains(tap.Value)) { _row = i; Change(i, -1); }
                else if (Plus(i).Contains(tap.Value)) { _row = i; Change(i, 1); }
            }
        }

        /// <summary>Lignes à deux états (commandes, vibrations) : un appui bascule, pas de boutons - / +.</summary>
        static bool Toggle(int row) => SettingsRows.Kinds[row] == SettingKind.Toggle;

        /// <summary>Segment retenu d'une bascule : 0 = premier choix (Droitier, Oui), 1 = second.</summary>
        static int ToggleSide(int row) => row == 3 ? (GameSession.LeftHanded ? 1 : 0) : GameSession.Vibration ? 0 : 1;

        static void Change(int row, int d)
        {
            switch (row)
            {
                case 0: GameSession.MusicVolume = Step(GameSession.MusicVolume, d * 0.1f, 0f, 1f); break;
                case 1: GameSession.SfxVolume = Step(GameSession.SfxVolume, d * 0.1f, 0f, 1f); break;
                case 2: GameSession.TouchSensitivity = Step(GameSession.TouchSensitivity, d * 0.1f, 0.5f, 1.5f); break;
                case 3: GameSession.LeftHanded = !GameSession.LeftHanded; break;
                case 4:
                    GameSession.Vibration = !GameSession.Vibration;
                    Haptics.Pulse(60);
                    break;
                case 5:
                    GameSession.QualityMode = Mathf.Clamp(GameSession.QualityMode + d, -1, QualityTuner.MaxLevel);
                    QualityTuner.Apply(QualityTuner.Effective);
                    break;
                default: return;
            }
            Jukebox.Instance.Move();
        }

        static float Step(float v, float d, float min, float max) => SettingsRows.Step(v, d, min, max);

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
            UiKit.Box(new Rect(0, 0, w, h), UiKit.Alpha(UiKit.Carbon, 0.94f));
            _ui.Label(new Rect(0, h * 0.06f, w, 80 * s), "Réglages", _ui.Big, UiKit.Chalk, TextAnchor.MiddleCenter);
            UiKit.Slash(new Rect(w * 0.5f - 70 * s, h * 0.06f + 76 * s, 140 * s, 8 * s), UiKit.Red, 1.2f);
            for (int i = 0; i < Rows.Length; i++)
            {
                var r = RowRect(i);
                bool focus = i == _row;
                // Section : petit titre jaune dans la marge gauche, à hauteur de sa première ligne.
                string section = SettingsRows.SectionStart(i);
                if (section != null && r.x > 190 * s)
                {
                    UiKit.Slash(new Rect(r.x - 22 * s, r.y + r.height * 0.3f, 6 * s, r.height * 0.4f), UiKit.Signal);
                    _ui.Label(new Rect(r.x - 200 * s, r.y, 170 * s, r.height), section.ToUpperInvariant(), _ui.Small, UiKit.Signal, TextAnchor.MiddleRight);
                }
                if (i == Rows.Length - 1)
                {
                    _ui.Button(new Rect(r.center.x - 150 * s, r.y + 10 * s, 300 * s, r.height), "Retour", true, focus);
                    continue;
                }
                // Ligne en fiche blanc piste ; la ligne active est surlignée en rouge.
                UiKit.Card(r, focus ? UiKit.Red : UiKit.Chalk, 16 * s, 4 * s);
                var ink = focus ? UiKit.Chalk : UiKit.Carbon;
                if (focus) UiKit.Slash(new Rect(r.x + 10 * s, r.y + r.height * 0.22f, 7 * s, r.height * 0.56f), UiKit.Signal);
                _ui.Label(new Rect(r.x + 28 * s, r.y, 300 * s, r.height), Rows[i], _ui.Medium, ink, TextAnchor.MiddleLeft);
                if (i == 3) DrawZones(r, s);
                var valueRect = new Rect(Minus(i).xMax, r.y, Plus(i).x - Minus(i).xMax, r.height);
                switch (SettingsRows.Kinds[i])
                {
                    case SettingKind.Toggle:
                        for (int half = 0; half < 2; half++)
                        {
                            bool on = half == ToggleSide(i);
                            var seg = ToggleHalf(i, half);
                            // Choix retenu : jaune sur la ligne active (rouge), rouge sinon ; l'autre reste en retrait.
                            UiKit.Card(seg, on ? (focus ? UiKit.Signal : UiKit.Red) : UiKit.Alpha(focus ? UiKit.Chalk : UiKit.Carbon, 0.16f), 9 * s);
                            _ui.Label(seg, SettingsRows.ToggleNames[i][half], _ui.Small, on ? (focus ? UiKit.Carbon : UiKit.Chalk) : UiKit.Alpha(ink, 0.6f), TextAnchor.MiddleCenter);
                        }
                        continue;
                    case SettingKind.Gauge:
                        DrawGauge(i, valueRect, focus, s);
                        break;
                    default:
                        _ui.Label(valueRect, Value(i), _ui.Medium, focus ? UiKit.Signal : UiKit.Red, TextAnchor.MiddleCenter);
                        break;
                }
                var minus = Minus(i); var plus = Plus(i);
                float m = 7 * s;
                _ui.Button(new Rect(minus.x + m, minus.y + m, minus.width - 2 * m, minus.height - 2 * m), "-", false);
                _ui.Button(new Rect(plus.x + m, plus.y + m, plus.width - 2 * m, plus.height - 2 * m), "+", false);
            }
            // Rappel des commandes, discret, au pied de l'écran.
            _ui.Label(new Rect(0, h - 44 * s, w, 30 * s), "Flèches : choisir et régler   ·   Entrée : basculer   ·   Échap : retour", _ui.Small,
                UiKit.Alpha(UiKit.Chalk, 0.55f), TextAnchor.MiddleCenter);
        }

        /// <summary>Jauge : valeur en chiffres fixes en haut, dix crans obliques en dessous (allumés jusqu'à la valeur).</summary>
        void DrawGauge(int row, Rect area, bool focus, float s)
        {
            var (value, min, max) = row switch
            {
                0 => (GameSession.MusicVolume, 0f, 1f),
                1 => (GameSession.SfxVolume, 0f, 1f),
                _ => (GameSession.TouchSensitivity, 0.5f, 1.5f),
            };
            _ui.Digits(new Rect(area.x, area.y + 3 * s, area.width, area.height * 0.5f), Value(row), _ui.Small,
                focus ? UiKit.Signal : UiKit.Red, TextAnchor.MiddleCenter);
            int lit = SettingsRows.Lit(value, min, max);
            float pad = 8 * s, step = (area.width - 2 * pad) / SettingsRows.Notches;
            for (int k = 0; k < SettingsRows.Notches; k++)
            {
                var notch = new Rect(area.x + pad + k * step + step * 0.25f, area.y + area.height * 0.56f, step * 0.5f, area.height * 0.28f);
                var on = focus ? UiKit.Signal : UiKit.Red;
                UiKit.Slash(notch, k < lit ? on : UiKit.Alpha(focus ? UiKit.Chalk : UiKit.Carbon, 0.22f));
            }
        }

        /// <summary>
        /// Schéma des zones tactiles à droite de la ligne « Commandes tactiles » : écran en paysage, zone de glissement
        /// d'un côté, les trois boutons de l'autre (calculés comme en course, par Core/TouchZones).
        /// </summary>
        void DrawZones(Rect row, float s)
        {
            float space = Screen.width - row.xMax - 24 * s;
            if (space < 150 * s) return;
            float pw = Mathf.Min(220 * s, space - 16 * s), ph = pw * 9f / 16f;
            var phone = new Rect(row.xMax + 20 * s, row.center.y - ph * 0.5f, pw, ph);
            UiKit.Card(phone, UiKit.Chalk, 10 * s, 3 * s);
            var z = TurboPattes.Core.TouchZones.Compute(pw, ph, 0f, pw, 0f, 1f, GameSession.LeftHanded);
            // Zone de glissement, hachurée de repères obliques.
            var zone = new Rect(phone.x + z.SteerMinX, phone.y, z.SteerMaxX - z.SteerMinX, ph);
            UiKit.Box(zone, UiKit.Alpha(UiKit.Telemetry, 0.35f));
            for (int k = 0; k < 4; k++)
                UiKit.Slash(new Rect(zone.x + zone.width * (0.2f + k * 0.18f), zone.y + ph * 0.4f, 4 * s, ph * 0.2f), UiKit.Alpha(UiKit.Carbon, 0.5f));
            void Spot(TurboPattes.Core.TouchSpot b, Color c) => UiKit.Disc(new Vector2(phone.x + b.X, phone.yMax - b.Y), b.R, c);
            Spot(z.Drift, UiKit.Signal);
            Spot(z.Item, UiKit.Telemetry);
            Spot(z.Brake, UiKit.Red);
        }

        static string Value(int row) => row switch
        {
            0 => $"{Mathf.RoundToInt(GameSession.MusicVolume * 100)} %",
            1 => $"{Mathf.RoundToInt(GameSession.SfxVolume * 100)} %",
            2 => $"{GameSession.TouchSensitivity:0.0}",
            3 => GameSession.LeftHanded ? "Gaucher" : "Droitier",
            4 => GameSession.Vibration ? "Oui" : "Non",
            _ => QualityNames[GameSession.QualityMode + 1],
        };
    }
}
