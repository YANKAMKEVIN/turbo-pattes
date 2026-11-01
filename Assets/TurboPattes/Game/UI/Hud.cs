using TurboPattes.Core;
using TurboPattes.Game.App;
using TurboPattes.Game.Kart;
using TurboPattes.Game.Race;
using TurboPattes.Game.Visuals;
using TurboPattes.Game.Audio;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Interface de course en IMGUI, DA « Atelier plein gaz » : tour, place, objet, vitesse, classement, compte à rebours,
    /// commandes tactiles et écran de résultats. Le tactile est lu par <see cref="PlayerInput"/> ; ici on ne fait que dessiner.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        public RaceManager Race;
        /// <summary>Contre-la-montre : records et fantôme (null sinon).</summary>
        public GhostRunner Ghost;
        /// <summary>Coupe en cours (null sinon).</summary>
        public CupRun Cup;

        readonly UiKit _ui = new();
        GUIStyle _big, _medium, _small, _tiny, _center;
        float _scale;
        int _focus;
        static readonly string[] ItemNames = { "", "Carotte turbo", "Noix de coco", "Pot de miel", "Bulle bouclier", "Plume planante", "Pelote de laine" };
        static readonly string[] PauseChoices = { "Reprendre", "Recommencer", "Réglages", "Menu" };
        static readonly string[] ResultChoices = { "Rejouer", "Menu" };
        static readonly string[] CupChoices = { "Course suivante", "Menu" };
        static readonly string[] CupLastChoices = { "Podium", "Menu" };

        string[] ResultLabels => Cup == null ? ResultChoices : Cup.IsLastRace ? CupLastChoices : CupChoices;

        void EnsureStyles()
        {
            _ui.EnsureStyles();
            _scale = _ui.Scale;
            _big = _ui.Big;
            _medium = _ui.Medium;
            _small = _ui.Small;
            _tiny = _ui.Tiny;
            _center = new GUIStyle(_big) { alignment = TextAnchor.MiddleCenter };
        }

        public static string Ordinal(int rank) => rank == 1 ? "1er" : rank + "e";

        void Label(Rect r, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.UpperLeft, float shadow = 0f) =>
            _ui.Label(r, text, style, color, anchor, shadow);

        static void Disc(Vector2 center, float radius, Color color) => UiKit.Disc(center, radius, color);

        static void Box(Rect r, Color color) => UiKit.Box(r, color);

        // ------------------------------------------------------------------ boutons de pause et de fin (logique)

        static float S => Screen.height / 720f;

        static Rect PauseButton()
        {
            var safe = Screen.safeArea;
            float s = S, r = 30 * s;
            return new Rect(safe.xMax - 24 * s - 2 * r, Screen.height - safe.yMax + 120 * s, 2 * r, 2 * r);
        }

        static Rect[] MenuButtons(int count, float y, bool row)
        {
            float s = S, w = Screen.width;
            var rects = new Rect[count];
            for (int i = 0; i < count; i++)
                rects[i] = row
                    ? new Rect(w * 0.5f - (count * 290 * s - 20 * s) * 0.5f + i * 290 * s, y, 270 * s, 70 * s)
                    : new Rect(w * 0.5f - 160 * s, y + i * 88 * s, 320 * s, 70 * s);
            return rects;
        }

        Rect[] PauseRects() => MenuButtons(PauseChoices.Length, Screen.height * 0.34f, false);

        struct Results { public Rect Card; public float Row; public Rect[] Buttons; }

        // Lignes du tableau de résultats : karts, ou tours + total, record et médailles en contre-la-montre.
        int ResultRows => Race.Solo ? Race.Laps + 4 : Race.Karts.Length;

        // Fiche de classement à droite, boutons dessous ; partagé entre Update (clics) et OnGUI.
        Results ResultLayout()
        {
            float s = S, w = Screen.width;
            var safe = Screen.safeArea;
            float top = Screen.height - safe.yMax + 30 * s;
            float row = 36 * s, cw = Mathf.Min(640 * s, w * 0.52f);
            var card = new Rect(safe.xMax - 40 * s - cw, top, cw, 66 * s + row * ResultRows);
            float bw = (cw - 24 * s) * 0.5f, by = card.yMax + 26 * s;
            return new Results
            {
                Card = card, Row = row,
                Buttons = new[] { new Rect(card.x, by, bw, 66 * s), new Rect(card.x + bw + 24 * s, by, bw, 66 * s) },
            };
        }

        Rect[] ResultRects() => ResultLayout().Buttons;

        void Update()
        {
            if (Race == null || Race.Player == null || SettingsScreen.IsOpen) return;
            var tap = UiKit.Tap();
            if (Race.Paused) Navigate(PauseRects(), tap, ChoosePause, vertical: true);
            else if (Race.State == RaceManager.Phase.Results) Navigate(ResultRects(), tap, ChooseResult, vertical: false);
            else
            {
                _focus = 0;
                if (tap.HasValue && Race.Input.TouchMode && PauseButton().Contains(tap.Value)) { Jukebox.Instance.Confirm(); Race.SetPaused(true); }
            }
        }

        void Navigate(Rect[] buttons, Vector2? tap, System.Action<int> choose, bool vertical)
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            int d = 0;
            if (kb != null)
            {
                if ((vertical ? kb.upArrowKey : kb.leftArrowKey).wasPressedThisFrame) d--;
                if ((vertical ? kb.downArrowKey : kb.rightArrowKey).wasPressedThisFrame) d++;
            }
            if (pad != null)
            {
                if ((vertical ? pad.dpad.up : pad.dpad.left).wasPressedThisFrame) d--;
                if ((vertical ? pad.dpad.down : pad.dpad.right).wasPressedThisFrame) d++;
            }
            int before = _focus;
            _focus = Mathf.Clamp(_focus + d, 0, buttons.Length - 1);
            if (_focus != before) Jukebox.Instance.Move();
            bool confirm = kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                           || pad != null && pad.buttonSouth.wasPressedThisFrame;
            bool back = kb != null && kb.escapeKey.wasPressedThisFrame || pad != null && (pad.buttonEast.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
            if (confirm) { Jukebox.Instance.Confirm(); choose(_focus); return; }
            if (back && Race.Paused) { Jukebox.Instance.Back(); choose(0); return; }
            if (!tap.HasValue) return;
            for (int i = 0; i < buttons.Length; i++)
                if (buttons[i].Contains(tap.Value)) { Jukebox.Instance.Confirm(); choose(i); return; }
        }

        void ChoosePause(int i)
        {
            if (i == 0) Race.SetPaused(false);
            else if (i == 2) SettingsScreen.Open();
            else Leave(toMenu: i == 3);
        }

        void ChooseResult(int i)
        {
            // En coupe, « Course suivante » / « Podium » passe à la suite ; la scène se recharge sur le circuit suivant.
            if (Cup != null && i == 0) Cup.Next();
            Leave(toMenu: i == 1);
        }

        void Leave(bool toMenu)
        {
            GameSession.ShowMenu = toMenu;
            if (toMenu) GameSession.Cup = null;
            Race.Restart();
        }

        void OnGUI()
        {
            if (Race == null || Race.Player == null) return;
            EnsureStyles();
            float s = _scale;
            float w = Screen.width, h = Screen.height;
            var safe = Screen.safeArea;
            float left = safe.xMin + 24 * s, right = safe.xMax - 24 * s, top = h - safe.yMax + 16 * s;
            var p = Race.Player;

            if (Race.State == RaceManager.Phase.Results)
            {
                DrawResults(w, h, s);
                return;
            }
            if (Race.Paused)
            {
                if (!SettingsScreen.IsOpen) DrawPause(w, h, s);
                return;
            }

            // Place : grande plaque encre, chiffre crème, total en ocre (pas de place en contre-la-montre).
            int rank = p.Rank + 1;
            var posRect = new Rect(left, top, 158 * s, 96 * s);
            if (!Race.Solo)
            {
                _ui.Card(posRect, UiKit.Charcoal, 20 * s, 5f, 0f);
                var rankStyle = new GUIStyle(_ui.Big) { fontSize = Mathf.RoundToInt(78 * s) };
                Label(new Rect(posRect.x + 24 * s, posRect.y - 8 * s, 80 * s, posRect.height), rank.ToString(), rankStyle,
                    rank == 1 ? UiKit.Ochre : UiKit.Cream, TextAnchor.MiddleCenter);
                Label(new Rect(posRect.x + 98 * s, posRect.y + 14 * s, 60 * s, 30 * s), rank == 1 ? "er" : "e", _small, UiKit.Cream);
                Label(new Rect(posRect.x + 98 * s, posRect.y + 46 * s, 60 * s, 30 * s), $"/{Race.Karts.Length}", _medium, UiKit.Ochre);
            }

            // Bandeau de chronométrage : tour, temps, vitesse.
            float x = Race.Solo ? left + 8 * s : posRect.xMax + 22 * s, bh = 46 * s;
            _ui.Stripes(new Vector2(x - 8 * s, top), bh, UiKit.Vermilion, UiKit.Ochre);
            x += 30 * s;
            int lap = p.Laps.DisplayLap(Race.Laps);
            var lapRect = new Rect(x, top, 176 * s, bh);
            _ui.Card(lapRect, UiKit.Cream, 12 * s, 4f, 0f);
            Label(new Rect(lapRect.x + 18 * s, lapRect.y, 70 * s, bh), "TOUR", _small, UiKit.Charcoal, TextAnchor.MiddleLeft);
            Label(new Rect(lapRect.x + 74 * s, lapRect.y - 2 * s, 90 * s, bh), $"{lap}/{Race.Laps}", _medium, UiKit.Vermilion, TextAnchor.MiddleCenter);
            var timeRect = new Rect(lapRect.xMax + 10 * s, top, 160 * s, bh);
            _ui.Card(timeRect, UiKit.Charcoal, 12 * s, 4f, 0f);
            Label(timeRect, FormatTime(Race.RaceTime), _medium, UiKit.Cream, TextAnchor.MiddleCenter);
            float kmh = p.Speed * 3.6f;
            var speedRect = new Rect(timeRect.xMax + 10 * s, top, 164 * s, bh);
            _ui.Card(speedRect, p.BoostTime > 0f ? UiKit.Ochre : UiKit.Charcoal, 12 * s, 4f, 0f);
            Label(new Rect(speedRect.x, speedRect.y - 2 * s, speedRect.width - 66 * s, bh), $"{kmh:0}", _medium,
                p.BoostTime > 0f ? UiKit.Charcoal : UiKit.Cream, TextAnchor.MiddleRight);
            Label(new Rect(speedRect.xMax - 60 * s, speedRect.y, 56 * s, bh), "km/h", _tiny,
                p.BoostTime > 0f ? UiKit.Charcoal : UiKit.Ochre, TextAnchor.MiddleLeft);

            if (Race.Solo) DrawChrono(left, right, top, top + bh + 16 * s, s);
            else
            {
                DrawStandings(left, posRect.yMax + 16 * s, s);
                if (!Race.Input.TouchMode) DrawItemSlot(new Rect(right - 104 * s, top, 104 * s, 104 * s), p);
            }

            if (p.Drift.Active && p.Drift.Level > 0)
                Label(new Rect(w * 0.5f - 200 * s, h * 0.66f, 400 * s, 40 * s), new string('/', p.Drift.Level * 2), _ui.Big,
                    KartMotor.DriftColors[p.Drift.Level], TextAnchor.MiddleCenter, 3f);

            if (Race.State == RaceManager.Phase.Countdown)
            {
                int n = Mathf.CeilToInt(Race.CountdownLeft);
                if (n <= 3 && n >= 1)
                {
                    float pulse = 1f + (Mathf.Ceil(Race.CountdownLeft) - Race.CountdownLeft) * 0.4f;
                    var st = new GUIStyle(_ui.Huge) { fontSize = Mathf.RoundToInt(170 * s * pulse) };
                    Label(new Rect(0, h * 0.2f, w, h * 0.34f), n.ToString(), st, UiKit.Cream, TextAnchor.MiddleCenter, 8f);
                }
                var nameRect = new Rect(w * 0.5f - 220 * s, h * 0.6f, 440 * s, 52 * s);
                _ui.Card(nameRect, UiKit.Charcoal, 16 * s, 5f, 0f);
                _ui.Stripes(new Vector2(nameRect.x + 22 * s, nameRect.y + 10 * s), 32 * s, UiKit.Vermilion, UiKit.Ochre);
                Label(new Rect(nameRect.x + 40 * s, nameRect.y, nameRect.width - 50 * s, nameRect.height), Race.Circuit.Name.ToUpperInvariant(),
                    _medium, UiKit.Cream, TextAnchor.MiddleCenter);
                string sub = Cup != null ? $"{Cup.Def.Name} · course {Cup.Race + 1}/{Cups.RacesPerCup}" : Race.Solo ? "Contre-la-montre" : null;
                if (sub != null)
                {
                    var subRect = new Rect(w * 0.5f - 180 * s, nameRect.yMax + 8 * s, 360 * s, 36 * s);
                    _ui.Card(subRect, UiKit.Ochre, 11 * s, 4f, 0f);
                    Label(subRect, sub.ToUpperInvariant(), _small, UiKit.Charcoal, TextAnchor.MiddleCenter);
                }
            }
            if (Race.BannerTime > 0f && !string.IsNullOrEmpty(Race.Banner))
            {
                var st = new GUIStyle(_ui.Title) { fontSize = Mathf.RoundToInt(92 * s) };
                var c = UiKit.Ochre;
                c.a = Mathf.Clamp01(Race.BannerTime * 2f);
                Label(new Rect(0, h * 0.26f, w, h * 0.22f), Race.Banner.ToUpperInvariant(), st, c, TextAnchor.MiddleCenter, 7f);
            }
            if (Race.WrongWay && Mathf.Repeat(Time.time, 0.8f) < 0.55f)
            {
                var r = new Rect(w * 0.5f - 230 * s, h * 0.4f, 460 * s, 70 * s);
                _ui.Card(r, UiKit.Vermilion, 22 * s, 6f, 3f);
                Label(r, "MAUVAIS SENS", _ui.Big, UiKit.Cream, TextAnchor.MiddleCenter);
            }

            if (Race.Input.TouchMode)
            {
                DrawTouchControls(h, s);
                var pb = PauseButton();
                _ui.Card(pb, UiKit.Charcoal, 10 * s, 4f, 0f);
                UiKit.Box(new Rect(pb.center.x - 10 * s, pb.center.y - 12 * s, 7 * s, 24 * s), UiKit.Cream);
                UiKit.Box(new Rect(pb.center.x + 3 * s, pb.center.y - 12 * s, 7 * s, 24 * s), UiKit.Cream);
            }
            else if (Race.State == RaceManager.Phase.Countdown)
            {
                var r = new Rect(w * 0.5f - 420 * s, h - safe.yMin - 64 * s, 840 * s, 38 * s);
                _ui.Card(r, UiKit.Charcoal, 12 * s, 3f, 0f);
                Label(r, "Flèches : conduire    Espace : dérapage    E : objet    Échap : pause    (manette : RT, RB, X, Start)", _tiny,
                    UiKit.Cream, TextAnchor.MiddleCenter);
            }
        }

        void DrawItemSlot(Rect r, KartMotor p)
        {
            _ui.Card(r, UiKit.Charcoal, 14 * _scale, 5f, 0f);
            UiKit.Box(new Rect(r.x + 16 * _scale, r.yMax - 8 * _scale, r.width - 32 * _scale, 3 * _scale), UiKit.Ochre);
            if (p.Item == ItemKind.None)
                Label(r, "OBJET", _tiny, new Color(UiKit.Cream.r, UiKit.Cream.g, UiKit.Cream.b, 0.35f), TextAnchor.MiddleCenter);
            DrawItemIcon(r.center, r.height * 0.36f, p);
        }

        static void DrawItemIcon(Vector2 c, float ir, KartMotor p)
        {
            var kind = p.Item;
            if (kind == ItemKind.None) return;
            // Roulette : fait défiler les objets.
            if (p.ItemRoll > 0f) kind = (ItemKind)(1 + (int)(Time.time * 14f) % ItemRoll.KindCount);
            GUI.DrawTexture(new Rect(c.x - ir, c.y - ir, ir * 2f, ir * 2f), Icons.Item(kind), ScaleMode.ScaleToFit);
        }

        void DrawStandings(float x, float y, float s)
        {
            // Le joueur et ses voisins immédiats (le classement complet est aux résultats).
            var order = Race.Order;
            int me = System.Array.IndexOf(order, System.Array.IndexOf(Race.Karts, Race.Player));
            int first = Mathf.Clamp(me - 1, 0, Mathf.Max(0, order.Length - 3));
            float row = 32 * s, gap = 6 * s;
            for (int i = 0; i < 3 && first + i < order.Length; i++)
            {
                int r = first + i;
                var k = Race.Karts[order[r]];
                var rect = new Rect(x + (k.IsPlayer ? 10 * s : 0f), y + i * (row + gap), 226 * s, row);
                _ui.Card(rect, k.IsPlayer ? UiKit.Ochre : new Color(UiKit.Charcoal.r, UiKit.Charcoal.g, UiKit.Charcoal.b, 0.92f), 9 * s, 3f, 0f);
                var ink = k.IsPlayer ? UiKit.Charcoal : UiKit.Cream;
                Label(new Rect(rect.x + 14 * s, rect.y, 28 * s, row), (r + 1).ToString(), _small, ink, TextAnchor.MiddleCenter);
                _ui.Badge(new Rect(rect.x + 48 * s, rect.y + 5 * s, 44 * s, row - 10 * s), k.Spec);
                Label(new Rect(rect.x + 102 * s, rect.y, 120 * s, row), k.Spec.Name, _small, ink, TextAnchor.MiddleLeft);
            }
        }

        void DrawTouchControls(float h, float s)
        {
            var input = Race.Input;
            var l = input.Layout;
            Vector2 Gui(Vector2 screen) => new(screen.x, h - screen.y);

            var ring = new Color(UiKit.Charcoal.r, UiKit.Charcoal.g, UiKit.Charcoal.b, 0.45f);
            if (input.SteerActive)
            {
                var a = Gui(input.SteerAnchor);
                Disc(a, l.SteerRange * 1.25f, ring);
                float dx = Mathf.Clamp(input.SteerFinger.x - input.SteerAnchor.x, -l.SteerRange, l.SteerRange);
                Disc(a + new Vector2(dx, 3f * s), l.SteerRange * 0.48f, new Color(0f, 0f, 0f, 0.3f));
                Disc(a + new Vector2(dx, 0f), l.SteerRange * 0.48f, UiKit.Cream);
            }
            else
            {
                var hint = new Vector2(Screen.safeArea.xMin + l.SteerRange * 2f, h * 0.78f);
                Disc(hint, l.SteerRange * 1.25f, ring);
                Disc(hint, l.SteerRange * 0.3f, new Color(UiKit.Cream.r, UiKit.Cream.g, UiKit.Cream.b, 0.5f));
                var st = new GUIStyle(_ui.Big) { fontSize = Mathf.RoundToInt(l.SteerRange * 0.8f) };
                Label(new Rect(hint.x - l.SteerRange * 1.15f, hint.y - l.SteerRange * 0.5f, l.SteerRange * 0.8f, l.SteerRange), "‹", st,
                    UiKit.Cream, TextAnchor.MiddleCenter);
                Label(new Rect(hint.x + l.SteerRange * 0.35f, hint.y - l.SteerRange * 0.5f, l.SteerRange * 0.8f, l.SteerRange), "›", st,
                    UiKit.Cream, TextAnchor.MiddleCenter);
            }

            var p = Race.Player;
            var driftColor = p.Drift.Active && p.Drift.Level > 0 ? KartMotor.DriftColors[p.Drift.Level] : UiKit.Vermilion;
            TouchButton(Gui(l.Drift), l.DriftR, "DÉRAPAGE", driftColor, input.DriftHeld);
            TouchButton(Gui(l.Brake), l.BrakeR, "FREIN", UiKit.Teal, input.BrakeHeld);
            if (Race.Solo) return;
            bool ready = p.Item != ItemKind.None && p.ItemRoll <= 0f;
            var ic = Gui(l.Item);
            TouchButton(ic, l.ItemR, p.Item == ItemKind.None ? "OBJET" : "", UiKit.Charcoal, input.ItemHeld);
            if (p.Item != ItemKind.None)
            {
                if (ready) UiKit.Box(new Rect(ic.x - l.ItemR * 0.5f, ic.y + l.ItemR * 0.72f, l.ItemR, 3f * s), UiKit.Ochre);
                DrawItemIcon(ic, l.ItemR * 0.62f, p);
            }
        }

        void TouchButton(Vector2 c, float r, string text, Color color, bool held)
        {
            float o = 4f * _scale;
            Disc(c + new Vector2(o, o), r, new Color(0f, 0f, 0f, 0.35f));
            Disc(c, r, UiKit.Charcoal);
            var fill = held ? UiKit.Ochre : color;
            fill.a = held ? 1f : 0.88f;
            Disc(c, r - 3f * _scale, fill);
            if (text.Length > 0)
            {
                var st = new GUIStyle(_small) { fontSize = Mathf.RoundToInt(Mathf.Min(21f * _scale, r * 0.34f)) };
                Label(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), text, st, held ? UiKit.Charcoal : UiKit.Cream, TextAnchor.MiddleCenter);
            }
        }

        /// <summary>Contre-la-montre : tour en cours et écart sous le bandeau, meilleur tour et record à droite.</summary>
        void DrawChrono(float left, float right, float top, float below, float s)
        {
            // En tactile, le bouton pause occupe le coin : le panneau se range à sa gauche.
            if (Race.Input.TouchMode) right = Mathf.Min(right, PauseButton().xMin - 16 * s);
            float lapTime = Race.State == RaceManager.Phase.Racing && !Race.Player.Finished ? Race.RaceTime - Race.LapStartTime : 0f;
            var lapRect = new Rect(left + 38 * s, below, 210 * s, 40 * s);
            _ui.Card(lapRect, UiKit.Charcoal, 11 * s, 4f, 0f);
            Label(new Rect(lapRect.x + 18 * s, lapRect.y, 80 * s, lapRect.height), "CE TOUR", _tiny, UiKit.Ochre, TextAnchor.MiddleLeft);
            Label(new Rect(lapRect.x + 80 * s, lapRect.y, lapRect.width - 96 * s, lapRect.height), FormatTime(lapTime), _small, UiKit.Cream, TextAnchor.MiddleRight);

            float bestLap = Ghost != null ? Ghost.BestLap : 0f;
            float bestTime = Ghost != null ? Ghost.BestTimeBefore : 0f;
            var panel = new Rect(right - 250 * s, top, 250 * s, 124 * s);
            _ui.Card(panel, UiKit.Cream, 0f, 5f, 3f);
            UiKit.Box(new Rect(panel.x, panel.y, 8 * s, panel.height), UiKit.Ochre);
            Label(new Rect(panel.x + 22 * s, panel.y + 6 * s, 220 * s, 24 * s), "MEILLEUR TOUR", _tiny, UiKit.Teal);
            Label(new Rect(panel.x + 22 * s, panel.y + 24 * s, 210 * s, 36 * s), bestLap > 0f ? FormatTime(bestLap) : "-:--,--", _medium, UiKit.Charcoal);
            Label(new Rect(panel.x + 22 * s, panel.y + 64 * s, 220 * s, 24 * s), $"RECORD {Race.Laps} TOUR{(Race.Laps > 1 ? "S" : "")}", _tiny, UiKit.Teal);
            Label(new Rect(panel.x + 22 * s, panel.y + 82 * s, 210 * s, 36 * s), bestTime > 0f ? FormatTime(bestTime) : "-:--,--", _medium, UiKit.Charcoal);
            if (Ghost != null && Ghost.GhostVisible)
                Label(new Rect(panel.x, panel.yMax + 8 * s, panel.width, 26 * s), "FANTÔME DU MEILLEUR TOUR", _tiny, UiKit.Cream, TextAnchor.MiddleRight, 2f);
            // Écart avec le meilleur tour aux temps intermédiaires et à la ligne : vert en avance, vermillon en retard.
            if (Ghost != null && Ghost.DeltaAge < 2.5f)
            {
                float d = Ghost.Delta;
                float a = Mathf.Clamp01((2.5f - Ghost.DeltaAge) * 2f);
                var col = d <= 0f ? new Color(0.30f, 0.62f, 0.36f, a) : new Color(UiKit.Vermilion.r, UiKit.Vermilion.g, UiKit.Vermilion.b, a);
                var dRect = new Rect(lapRect.x, lapRect.yMax + 10 * s, 150 * s, 44 * s);
                _ui.Card(dRect, col, 12 * s, 0f, 0f);
                Label(dRect, $"{(d <= 0f ? "-" : "+")}{Mathf.Abs(d):0.00}", _medium, new Color(UiKit.Cream.r, UiKit.Cream.g, UiKit.Cream.b, a), TextAnchor.MiddleCenter);
            }
        }

        void DrawResults(float w, float h, float s)
        {
            Box(new Rect(0, 0, w, h), new Color(0.06f, 0.08f, 0.07f, 0.55f));
            if (Race.Solo)
            {
                DrawChronoResults(s);
                return;
            }
            if (Cup != null && Cup.LastOrder != null)
            {
                DrawCupResults(s);
                return;
            }
            var p = Race.Player;
            var lay = ResultLayout();

            // Colonne gauche : fiche encre avec le rang, la mention et le meilleur tour du joueur.
            float lx = lay.Card.x - 340 * s, ly = lay.Card.y;
            var side = new Rect(lx, ly, 300 * s, 340 * s);
            _ui.Card(side, UiKit.Charcoal, 0f, 6f, 0f);
            UiKit.Box(new Rect(side.x, side.y, side.width, 8 * s), p.Rank == 0 ? UiKit.Ochre : UiKit.Vermilion);
            var rankStyle = new GUIStyle(_ui.Huge) { fontSize = Mathf.RoundToInt(140 * s) };
            Label(new Rect(side.x, side.y + 6 * s, side.width - 60 * s, 160 * s), (p.Rank + 1).ToString(), rankStyle,
                p.Rank == 0 ? UiKit.Ochre : UiKit.Cream, TextAnchor.MiddleRight);
            Label(new Rect(side.xMax - 64 * s, side.y + 36 * s, 60 * s, 50 * s), p.Rank == 0 ? "er" : "e", _medium, UiKit.Cream);
            var tag = new Rect(side.x + 24 * s, side.y + 176 * s, side.width - 48 * s, 52 * s);
            _ui.Card(tag, p.Rank == 0 ? UiKit.Ochre : UiKit.Vermilion, 16 * s, 4f, 0f);
            Label(tag, p.Rank == 0 ? "VICTOIRE !" : p.Rank < 3 ? "PODIUM !" : "ARRIVÉE", _medium, p.Rank == 0 ? UiKit.Charcoal : UiKit.Cream,
                TextAnchor.MiddleCenter);
            var laps = Race.LapTimes;
            float best = float.MaxValue;
            foreach (float t in laps) best = Mathf.Min(best, t);
            if (laps.Length > 0)
            {
                Label(new Rect(side.x, tag.yMax + 18 * s, side.width, 26 * s), "MEILLEUR TOUR", _tiny, UiKit.Ochre, TextAnchor.MiddleCenter);
                Label(new Rect(side.x, tag.yMax + 42 * s, side.width, 44 * s), FormatTime(best), _medium, UiKit.Cream, TextAnchor.MiddleCenter);
            }

            // Fiche de classement.
            var card = lay.Card;
            _ui.Card(card, UiKit.Cream, 0f, 6f, 3f);
            _ui.Stripes(new Vector2(card.x + 20 * s, card.y + 16 * s), 28 * s, UiKit.Vermilion, UiKit.Ochre);
            Label(new Rect(card.x + 64 * s, card.y + 8 * s, 300 * s, 44 * s), "CLASSEMENT", _medium, UiKit.Charcoal, TextAnchor.MiddleLeft);
            Label(new Rect(card.xMax - 260 * s, card.y + 8 * s, 240 * s, 44 * s), Race.Circuit.Name, _small, UiKit.Teal, TextAnchor.MiddleRight);
            float row = lay.Row, y = card.y + 58 * s;
            for (int r = 0; r < Race.Order.Length; r++)
            {
                var k = Race.Karts[Race.Order[r]];
                float yy = y + r * row;
                if (k.IsPlayer) UiKit.Plate(new Rect(card.x + 8 * s, yy, card.width - 16 * s, row), UiKit.Ochre, 10 * s);
                else if (r % 2 == 1) UiKit.Box(new Rect(card.x + 8 * s, yy, card.width - 16 * s, row), new Color(0f, 0f, 0f, 0.05f));
                Label(new Rect(card.x + 18 * s, yy, 50 * s, row), Ordinal(r + 1), _small, UiKit.Charcoal, TextAnchor.MiddleLeft);
                _ui.Badge(new Rect(card.x + 76 * s, yy + 5 * s, 48 * s, row - 10 * s), k.Spec);
                Label(new Rect(card.x + 138 * s, yy, 200 * s, row), k.Spec.Name, _small, UiKit.Charcoal, TextAnchor.MiddleLeft);
                string time = k.Finished ? FormatTime(k.FinishTime) : "en course…";
                Label(new Rect(card.xMax - 220 * s, yy, 200 * s, row), time, _small, UiKit.Charcoal, TextAnchor.MiddleRight);
            }

            DrawResultButtons();
        }

        void DrawResultButtons()
        {
            var buttons = ResultRects();
            var labels = ResultLabels;
            for (int i = 0; i < buttons.Length; i++)
                _ui.Button(buttons[i], labels[i].ToUpperInvariant(), i == 0 ? UiKit.Kind.Primary : UiKit.Kind.Secondary, i == _focus);
        }

        /// <summary>Fiche encre de gauche (résultats) : grand chiffre ou mention, avec un bandeau de couleur.</summary>
        Rect SideCard(Results lay, Color band, float s)
        {
            var side = new Rect(lay.Card.x - 340 * s, lay.Card.y, 300 * s, 340 * s);
            _ui.Card(side, UiKit.Charcoal, 0f, 6f, 0f);
            UiKit.Box(new Rect(side.x, side.y, side.width, 8 * s), band);
            return side;
        }

        /// <summary>Fiche crème de droite : repère « // », titre et sous-titre.</summary>
        void ListCard(Rect card, string title, string sub, float s)
        {
            _ui.Card(card, UiKit.Cream, 0f, 6f, 3f);
            _ui.Stripes(new Vector2(card.x + 20 * s, card.y + 16 * s), 28 * s, UiKit.Vermilion, UiKit.Ochre);
            Label(new Rect(card.x + 64 * s, card.y + 8 * s, 300 * s, 44 * s), title, _medium, UiKit.Charcoal, TextAnchor.MiddleLeft);
            Label(new Rect(card.xMax - 300 * s, card.y + 8 * s, 280 * s, 44 * s), sub, _small, UiKit.Teal, TextAnchor.MiddleRight);
        }

        void DrawChronoResults(float s)
        {
            var p = Race.Player;
            var lay = ResultLayout();
            bool record = Ghost != null && Ghost.NewTimeRecord;

            // Fiche de gauche : temps total et mention.
            var side = SideCard(lay, record ? UiKit.Ochre : UiKit.Teal, s);
            Label(new Rect(side.x, side.y + 30 * s, side.width, 30 * s), "TEMPS TOTAL", _small, UiKit.Ochre, TextAnchor.MiddleCenter);
            var big = new GUIStyle(_ui.Big) { fontSize = Mathf.RoundToInt(58 * s) };
            Label(new Rect(side.x, side.y + 64 * s, side.width, 80 * s), FormatTime(p.FinishTime), big, UiKit.Cream, TextAnchor.MiddleCenter);
            var tag = new Rect(side.x + 24 * s, side.y + 176 * s, side.width - 48 * s, 52 * s);
            _ui.Card(tag, record ? UiKit.Ochre : UiKit.Vermilion, 16 * s, 4f, 0f);
            Label(tag, record ? "RECORD !" : "ARRIVÉE", _medium, record ? UiKit.Charcoal : UiKit.Cream, TextAnchor.MiddleCenter);
            float before = Ghost != null ? Ghost.BestTimeBefore : 0f;
            string recordText = before <= 0f ? "Premier temps ici" : record ? $"Ancien record {FormatTime(before)}" : $"Record {FormatTime(before)}";
            Label(new Rect(side.x, tag.yMax + 30 * s, side.width, 30 * s), recordText, _small, UiKit.Cream, TextAnchor.MiddleCenter);

            // Fiche de droite : temps de chaque tour, meilleur tour surligné, puis médailles.
            var card = lay.Card;
            ListCard(card, "TOURS", Race.Circuit.Name, s);
            var laps = Race.LapTimes;
            int bestIndex = -1;
            for (int i = 0; i < laps.Length; i++)
                if (laps[i] > 0f && (bestIndex < 0 || laps[i] < laps[bestIndex])) bestIndex = i;
            float row = lay.Row, y = card.y + 58 * s;
            for (int i = 0; i < laps.Length; i++)
            {
                float yy = y + i * row;
                if (i == bestIndex) UiKit.Plate(new Rect(card.x + 8 * s, yy, card.width - 16 * s, row), UiKit.Ochre, 10 * s);
                else if (i % 2 == 1) UiKit.Box(new Rect(card.x + 8 * s, yy, card.width - 16 * s, row), new Color(0f, 0f, 0f, 0.05f));
                Label(new Rect(card.x + 24 * s, yy, 200 * s, row), $"Tour {i + 1}", _small, UiKit.Charcoal, TextAnchor.MiddleLeft);
                if (i == bestIndex) Label(new Rect(card.x + 150 * s, yy, 200 * s, row), "MEILLEUR", _tiny, UiKit.Vermilion, TextAnchor.MiddleLeft);
                Label(new Rect(card.xMax - 220 * s, yy, 200 * s, row), FormatTime(laps[i]), _small, UiKit.Charcoal, TextAnchor.MiddleRight);
            }
            float ty = y + laps.Length * row;
            UiKit.Box(new Rect(card.x + 16 * s, ty + 4 * s, card.width - 32 * s, 2 * s), UiKit.Charcoal);
            Label(new Rect(card.x + 24 * s, ty + 6 * s, 200 * s, row), "TOTAL", _medium, UiKit.Charcoal, TextAnchor.MiddleLeft);
            Label(new Rect(card.xMax - 220 * s, ty + 6 * s, 200 * s, row), FormatTime(p.FinishTime), _medium, UiKit.Vermilion, TextAnchor.MiddleRight);
            if (Ghost != null) DrawMedals(card.x + 16 * s, ty + row * 1.5f, card.width - 32 * s, s);
            DrawResultButtons();
        }

        static readonly Color[] MedalColors = { Color.clear, new(0.75f, 0.45f, 0.22f), new(0.62f, 0.66f, 0.7f), new(0.914f, 0.725f, 0.286f) };

        /// <summary>Médailles du circuit : temps à battre, celle obtenue en couleur pleine.</summary>
        void DrawMedals(float x, float y, float w, float s)
        {
            var got = Ghost.Medal;
            float cw = w / 3f;
            for (int m = 3; m >= 1; m--)
            {
                var medal = (Medal)m;
                var r = new Rect(x + (3 - m) * cw + 4 * s, y, cw - 8 * s, 40 * s);
                bool earned = got >= medal;
                var col = MedalColors[m];
                UiKit.Plate(r, earned ? col : new Color(0f, 0f, 0f, 0.07f), 10 * s);
                Disc(new Vector2(r.x + 22 * s, r.center.y), 9 * s, earned ? UiKit.Cream : new Color(col.r, col.g, col.b, 0.6f));
                Label(new Rect(r.x + 36 * s, r.y, r.width - 40 * s, r.height), $"{Medals.Name(medal)} {Medals.Target(Ghost.Circuit, medal):0.0}",
                    _tiny, earned ? UiKit.Charcoal : new Color(UiKit.Charcoal.r, UiKit.Charcoal.g, UiKit.Charcoal.b, 0.55f), TextAnchor.MiddleLeft);
            }
            if (got > Ghost.MedalBefore)
                Label(new Rect(x, y + 44 * s, w, 30 * s), $"Médaille {Medals.Name(got).ToLowerInvariant()} gagnée !", _small, UiKit.Vermilion, TextAnchor.MiddleCenter);
        }

        /// <summary>Coupe : place à cette course, points gagnés et classement cumulé.</summary>
        void DrawCupResults(float s)
        {
            var st = Cup.Standings;
            int me = Cup.Player;
            var lay = ResultLayout();

            int place = st.LastPlace(me) + 1;
            var side = SideCard(lay, place == 1 ? UiKit.Ochre : UiKit.Vermilion, s);
            var rankStyle = new GUIStyle(_ui.Huge) { fontSize = Mathf.RoundToInt(140 * s) };
            Label(new Rect(side.x, side.y + 6 * s, side.width - 60 * s, 160 * s), place.ToString(), rankStyle,
                place == 1 ? UiKit.Ochre : UiKit.Cream, TextAnchor.MiddleRight);
            Label(new Rect(side.xMax - 64 * s, side.y + 36 * s, 60 * s, 50 * s), place == 1 ? "er" : "e", _medium, UiKit.Cream);
            var tag = new Rect(side.x + 24 * s, side.y + 176 * s, side.width - 48 * s, 52 * s);
            _ui.Card(tag, UiKit.Ochre, 16 * s, 4f, 0f);
            Label(tag, $"+{st.LastPoints(me)} PTS", _medium, UiKit.Charcoal, TextAnchor.MiddleCenter);
            Label(new Rect(side.x, tag.yMax + 18 * s, side.width, 26 * s), Cup.Def.Name.ToUpperInvariant(), _tiny, UiKit.Ochre, TextAnchor.MiddleCenter);
            Label(new Rect(side.x, tag.yMax + 42 * s, side.width, 40 * s), $"Course {Cup.Race + 1}/{Cups.RacesPerCup}", _medium, UiKit.Cream, TextAnchor.MiddleCenter);

            var card = lay.Card;
            ListCard(card, "COUPE", "classement cumulé", s);
            var ranking = st.Ranking();
            float row = lay.Row, y = card.y + 58 * s;
            for (int r = 0; r < ranking.Length; r++)
            {
                int id = ranking[r];
                var spec = AnimalSpec.Roster[id];
                float yy = y + r * row;
                if (id == me) UiKit.Plate(new Rect(card.x + 8 * s, yy, card.width - 16 * s, row), UiKit.Ochre, 10 * s);
                else if (r % 2 == 1) UiKit.Box(new Rect(card.x + 8 * s, yy, card.width - 16 * s, row), new Color(0f, 0f, 0f, 0.05f));
                Label(new Rect(card.x + 18 * s, yy, 50 * s, row), Ordinal(r + 1), _small, UiKit.Charcoal, TextAnchor.MiddleLeft);
                _ui.Badge(new Rect(card.x + 76 * s, yy + 5 * s, 48 * s, row - 10 * s), spec);
                Label(new Rect(card.x + 138 * s, yy, 160 * s, row), spec.Name, _small, UiKit.Charcoal, TextAnchor.MiddleLeft);
                Label(new Rect(card.x + 300 * s, yy, 120 * s, row), $"{Ordinal(st.LastPlace(id) + 1)} +{st.LastPoints(id)}", _tiny, UiKit.Teal, TextAnchor.MiddleLeft);
                Label(new Rect(card.xMax - 180 * s, yy, 160 * s, row), $"{st.Total(id)} pts", _small, UiKit.Charcoal, TextAnchor.MiddleRight);
            }
            DrawResultButtons();
        }

        void DrawPause(float w, float h, float s)
        {
            Box(new Rect(0, 0, w, h), new Color(0.06f, 0.08f, 0.07f, 0.62f));
            var buttons = PauseRects();
            var card = new Rect(buttons[0].x - 40 * s, buttons[0].y - 110 * s, buttons[0].width + 80 * s,
                buttons[buttons.Length - 1].yMax - buttons[0].y + 150 * s);
            _ui.Card(card, UiKit.Cream, 0f, 7f, 3f);
            _ui.Stripes(new Vector2(card.x + 30 * s, card.y + 30 * s), 40 * s, UiKit.Vermilion, UiKit.Ochre);
            var st = new GUIStyle(_ui.Title) { fontSize = Mathf.RoundToInt(64 * s) };
            Label(new Rect(card.x + 90 * s, card.y + 14 * s, card.width - 100 * s, 76 * s), "PAUSE", st, UiKit.Charcoal, TextAnchor.MiddleLeft);
            for (int i = 0; i < buttons.Length; i++)
                _ui.Button(buttons[i], PauseChoices[i].ToUpperInvariant(), i == 0 ? UiKit.Kind.Primary : UiKit.Kind.Dark, i == _focus);
        }

        static string FormatTime(float t)
        {
            int cs = Mathf.Max(0, Mathf.RoundToInt(t * 100f));
            return $"{cs / 6000}:{cs / 100 % 60:00},{cs % 100:00}";
        }
    }
}
