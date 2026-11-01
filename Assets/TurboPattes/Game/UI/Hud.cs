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

        readonly UiKit _ui = new();
        GUIStyle _big, _medium, _small, _tiny, _center;
        float _scale;
        int _focus;
        static readonly string[] ItemNames = { "", "Carotte turbo", "Noix de coco", "Pot de miel", "Bulle bouclier" };
        static readonly string[] PauseChoices = { "Reprendre", "Recommencer", "Menu" };
        static readonly string[] ResultChoices = { "Rejouer", "Menu" };

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

        static string Ordinal(int rank) => rank == 1 ? "1er" : rank + "e";

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

        // Fiche de classement à droite, boutons dessous ; partagé entre Update (clics) et OnGUI.
        Results ResultLayout()
        {
            float s = S, w = Screen.width;
            var safe = Screen.safeArea;
            float top = Screen.height - safe.yMax + 30 * s;
            float row = 36 * s, cw = Mathf.Min(640 * s, w * 0.52f);
            var card = new Rect(safe.xMax - 40 * s - cw, top, cw, 66 * s + row * Race.Karts.Length);
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
            if (Race == null || Race.Player == null) return;
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
            else Leave(toMenu: i == 2);
        }

        void ChooseResult(int i) => Leave(toMenu: i == 1);

        void Leave(bool toMenu)
        {
            GameSession.ShowMenu = toMenu;
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
                DrawPause(w, h, s);
                return;
            }

            // Place : grande plaque encre, chiffre crème, total en ocre.
            int rank = p.Rank + 1;
            var posRect = new Rect(left, top, 158 * s, 96 * s);
            _ui.Card(posRect, UiKit.Charcoal, 20 * s, 5f, 0f);
            var rankStyle = new GUIStyle(_ui.Big) { fontSize = Mathf.RoundToInt(78 * s) };
            Label(new Rect(posRect.x + 24 * s, posRect.y - 8 * s, 80 * s, posRect.height), rank.ToString(), rankStyle,
                rank == 1 ? UiKit.Ochre : UiKit.Cream, TextAnchor.MiddleCenter);
            Label(new Rect(posRect.x + 98 * s, posRect.y + 14 * s, 60 * s, 30 * s), rank == 1 ? "er" : "e", _small, UiKit.Cream);
            Label(new Rect(posRect.x + 98 * s, posRect.y + 46 * s, 60 * s, 30 * s), $"/{Race.Karts.Length}", _medium, UiKit.Ochre);

            // Bandeau de chronométrage : tour, temps, vitesse.
            float x = posRect.xMax + 22 * s, bh = 46 * s;
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

            DrawStandings(left, posRect.yMax + 16 * s, s);
            if (!Race.Input.TouchMode) DrawItemSlot(new Rect(right - 104 * s, top, 104 * s, 104 * s), p);

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
            DrawItemIcon(r.center, r.height * 0.36f, p);
        }

        static void DrawItemIcon(Vector2 c, float ir, KartMotor p)
        {
            var kind = p.Item;
            if (kind == ItemKind.None) return;
            // Roulette : fait défiler les objets.
            if (p.ItemRoll > 0f) kind = (ItemKind)(1 + (int)(Time.time * 14f) % 4);
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

        void DrawResults(float w, float h, float s)
        {
            Box(new Rect(0, 0, w, h), new Color(0.06f, 0.08f, 0.07f, 0.55f));
            var p = Race.Player;
            var lay = ResultLayout();

            // Colonne gauche : rang et temps du joueur.
            float lx = lay.Card.x - 380 * s, ly = lay.Card.y;
            var rankStyle = new GUIStyle(_ui.Huge) { fontSize = Mathf.RoundToInt(150 * s) };
            Label(new Rect(lx, ly - 20 * s, 300 * s, 170 * s), (p.Rank + 1).ToString(), rankStyle, p.Rank == 0 ? UiKit.Ochre : UiKit.Cream,
                TextAnchor.MiddleCenter, 8f);
            Label(new Rect(lx + 196 * s, ly + 22 * s, 90 * s, 50 * s), p.Rank == 0 ? "er" : "e", _medium, UiKit.Cream, TextAnchor.UpperLeft, 3f);
            var tag = new Rect(lx + 20 * s, ly + 160 * s, 260 * s, 54 * s);
            _ui.Card(tag, p.Rank == 0 ? UiKit.Ochre : UiKit.Vermilion, 16 * s, 5f, 3f);
            Label(tag, p.Rank == 0 ? "VICTOIRE !" : p.Rank < 3 ? "PODIUM !" : "ARRIVÉE", _medium, p.Rank == 0 ? UiKit.Charcoal : UiKit.Cream,
                TextAnchor.MiddleCenter);
            var laps = Race.LapTimes;
            float best = float.MaxValue;
            foreach (float t in laps) best = Mathf.Min(best, t);
            if (laps.Length > 0)
            {
                Label(new Rect(lx + 20 * s, tag.yMax + 22 * s, 260 * s, 30 * s), "MEILLEUR TOUR", _tiny, UiKit.Ochre, TextAnchor.MiddleCenter, 2f);
                Label(new Rect(lx + 20 * s, tag.yMax + 46 * s, 260 * s, 40 * s), FormatTime(best), _medium, UiKit.Cream, TextAnchor.MiddleCenter, 3f);
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

            var buttons = ResultRects();
            for (int i = 0; i < buttons.Length; i++)
                _ui.Button(buttons[i], ResultChoices[i].ToUpperInvariant(), i == 0 ? UiKit.Kind.Primary : UiKit.Kind.Secondary, i == _focus);
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
