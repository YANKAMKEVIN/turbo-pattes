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
    /// Interface de course en IMGUI (prototype) : tour, place, objet, vitesse, classement, compte à rebours,
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
        GUIStyle _big, _medium, _small, _center;
        float _scale;
        int _focus;
        static readonly string[] ItemNames = { "", "Carotte turbo", "Noix de coco", "Pot de miel", "Bulle bouclier" };
        static readonly string[] PauseChoices = { "Reprendre", "Recommencer", "Menu" };
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
            _center = new GUIStyle(_big) { alignment = TextAnchor.MiddleCenter };
        }

        public static string Ordinal(int rank) => rank == 1 ? "1er" : rank + "e";

        void Label(Rect r, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.UpperLeft) =>
            _ui.Label(r, text, style, color, anchor);

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

        Rect[] PauseRects() => MenuButtons(PauseChoices.Length, Screen.height * 0.36f, false);

        // Lignes du tableau de résultats : karts, ou tours en contre-la-montre.
        int ResultRows => Race.Solo ? Race.Laps + 2 : Race.Karts.Length;

        Rect[] ResultRects()
        {
            float row = 38 * S;
            return MenuButtons(ResultLabels.Length, 130 * S + row * ResultRows + 74 * S, true);
        }

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
                DrawPause(w, h, s);
                return;
            }

            // Tour
            int lap = p.Laps.DisplayLap(Race.Laps);
            Label(new Rect(left, top, 400 * s, 50 * s), $"Tour {lap}/{Race.Laps}", _medium, Color.white);
            Label(new Rect(left, top + 38 * s, 400 * s, 40 * s), FormatTime(Race.RaceTime), _small, new Color(1f, 1f, 1f, 0.85f));

            if (Race.Solo) DrawChrono(left, right, top, s);
            else
            {
                // Place
                int rank = p.Rank + 1;
                var rankColor = rank == 1 ? new Color(1f, 0.85f, 0.2f) : rank <= 3 ? new Color(0.6f, 0.95f, 1f) : Color.white;
                Label(new Rect(right - 300 * s, top - 6 * s, 300 * s, 80 * s), Ordinal(rank), _big, rankColor, TextAnchor.UpperRight);
                Label(new Rect(right - 300 * s, top + 62 * s, 300 * s, 30 * s), $"sur {Race.Karts.Length}", _small, Color.white, TextAnchor.UpperRight);

                DrawItemSlot(new Vector2(w * 0.5f, top + 46 * s), 42 * s, p);
                DrawStandings(left, top + 90 * s, s);
            }

            // Vitesse et charge de drift
            float kmh = p.Speed * 3.6f;
            var speedRect = Race.Input.TouchMode
                ? new Rect(w * 0.5f - 150 * s, h - safe.yMin - 70 * s, 300 * s, 50 * s)
                : new Rect(right - 300 * s, h - safe.yMin - 80 * s, 300 * s, 60 * s);
            Label(speedRect, $"{kmh:0} km/h", _medium, p.BoostTime > 0f ? new Color(1f, 0.7f, 0.2f) : Color.white,
                Race.Input.TouchMode ? TextAnchor.MiddleCenter : TextAnchor.LowerRight);
            if (p.Drift.Active && p.Drift.Level > 0)
                Label(new Rect(w * 0.5f - 200 * s, h * 0.66f, 400 * s, 40 * s), new string('>', p.Drift.Level * 2), _medium,
                    KartMotor.DriftColors[p.Drift.Level], TextAnchor.MiddleCenter);

            if (Race.State == RaceManager.Phase.Countdown)
            {
                int n = Mathf.CeilToInt(Race.CountdownLeft);
                if (n <= 3 && n >= 1)
                {
                    float pulse = 1f + (Mathf.Ceil(Race.CountdownLeft) - Race.CountdownLeft) * 0.5f;
                    var st = new GUIStyle(_center) { fontSize = Mathf.RoundToInt(140 * s * pulse) };
                    Label(new Rect(0, h * 0.25f, w, h * 0.3f), n.ToString(), st, new Color(1f, 0.9f, 0.3f), TextAnchor.MiddleCenter);
                }
                Label(new Rect(0, h * 0.58f, w, 40 * s), Race.Circuit.Name, _medium, Color.white, TextAnchor.MiddleCenter);
                string sub = Cup != null ? $"{Cup.Def.Name} — course {Cup.Race + 1}/{Cups.RacesPerCup}" : Race.Solo ? "Contre-la-montre" : null;
                if (sub != null) Label(new Rect(0, h * 0.58f + 40 * s, w, 34 * s), sub, _small, new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleCenter);
            }
            if (Race.BannerTime > 0f && !string.IsNullOrEmpty(Race.Banner))
            {
                var st = new GUIStyle(_center) { fontSize = Mathf.RoundToInt(70 * s) };
                Label(new Rect(0, h * 0.28f, w, h * 0.2f), Race.Banner, st, new Color(1f, 0.85f, 0.25f, Mathf.Clamp01(Race.BannerTime * 2f)),
                    TextAnchor.MiddleCenter);
            }
            if (Race.WrongWay && Mathf.Repeat(Time.time, 0.8f) < 0.55f)
                Label(new Rect(0, h * 0.4f, w, 80 * s), "Mauvais sens !", _big, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter);

            if (Race.Input.TouchMode)
            {
                DrawTouchControls(h, s);
                var pb = PauseButton();
                Disc(pb.center, pb.width * 0.5f, new Color(0f, 0f, 0f, 0.35f));
                Box(new Rect(pb.center.x - 9 * s, pb.center.y - 11 * s, 6 * s, 22 * s), Color.white);
                Box(new Rect(pb.center.x + 3 * s, pb.center.y - 11 * s, 6 * s, 22 * s), Color.white);
            }
            else if (Race.State == RaceManager.Phase.Countdown)
                Label(new Rect(0, h - safe.yMin - 50 * s, w, 30 * s),
                    "Flèches : conduire   Espace : drift   E : objet   Échap : pause   (manette : RT, RB, X, Start)", _small,
                    new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
        }

        void DrawItemSlot(Vector2 c, float r, KartMotor p)
        {
            Disc(c, r + 5 * _scale, new Color(1f, 1f, 1f, 0.9f));
            Disc(c, r, new Color(0.12f, 0.15f, 0.28f));
            var kind = p.Item;
            if (kind == ItemKind.None) return;
            if (p.ItemRoll > 0f)
            {
                // Roulette : fait défiler les objets.
                kind = (ItemKind)(1 + (int)(Time.time * 14f) % 4);
            }
            float ir = r * 0.9f;
            GUI.DrawTexture(new Rect(c.x - ir, c.y - ir, ir * 2f, ir * 2f), Icons.Item(kind), ScaleMode.ScaleToFit);
            if (p.ItemRoll <= 0f)
                Label(new Rect(c.x - 150 * _scale, c.y + r + 4 * _scale, 300 * _scale, 30 * _scale), ItemNames[(int)kind], _small, Color.white,
                    TextAnchor.UpperCenter);
        }

        void DrawStandings(float x, float y, float s)
        {
            var order = Race.Order;
            float row = 24 * s;
            for (int r = 0; r < order.Length; r++)
            {
                var k = Race.Karts[order[r]];
                float yy = y + r * row;
                if (k.IsPlayer) Box(new Rect(x - 6 * s, yy, 170 * s, row), new Color(1f, 0.85f, 0.2f, 0.35f));
                Label(new Rect(x, yy, 30 * s, row), (r + 1).ToString(), _small, Color.white);
                UiKit.Portrait(new Vector2(x + 38 * s, yy + row * 0.5f), 10 * s, k.Spec);
                Label(new Rect(x + 54 * s, yy, 116 * s, row), k.Spec.Name, _small, k.IsPlayer ? new Color(1f, 0.92f, 0.5f) : Color.white);
            }
        }

        void DrawTouchControls(float h, float s)
        {
            var input = Race.Input;
            var l = input.Layout;
            Vector2 Gui(Vector2 screen) => new(screen.x, h - screen.y);

            if (input.SteerActive)
            {
                var a = Gui(input.SteerAnchor);
                Disc(a, l.SteerRange * 1.2f, new Color(1f, 1f, 1f, 0.15f));
                float dx = Mathf.Clamp(input.SteerFinger.x - input.SteerAnchor.x, -l.SteerRange, l.SteerRange);
                Disc(a + new Vector2(dx, 0f), l.SteerRange * 0.5f, new Color(1f, 1f, 1f, 0.5f));
            }
            else
            {
                var hint = new Vector2(Screen.safeArea.xMin + l.SteerRange * 2f, h * 0.78f);
                Disc(hint, l.SteerRange * 1.2f, new Color(1f, 1f, 1f, 0.1f));
                Label(new Rect(hint.x - 120 * s, hint.y - 15 * s, 240 * s, 30 * s), "< glisser >", _small, new Color(1f, 1f, 1f, 0.6f),
                    TextAnchor.MiddleCenter);
            }

            var p = Race.Player;
            var driftColor = p.Drift.Active && p.Drift.Level > 0 ? KartMotor.DriftColors[p.Drift.Level] : new Color(1f, 0.55f, 0.2f);
            Button(Gui(l.Drift), l.DriftR, "DRIFT", driftColor, input.DriftHeld);
            if (!Race.Solo) Button(Gui(l.Item), l.ItemR, "OBJET", new Color(0.4f, 0.75f, 1f), input.ItemHeld || p.Item != ItemKind.None && p.ItemRoll <= 0f);
            Button(Gui(l.Brake), l.BrakeR, "FREIN", new Color(0.9f, 0.3f, 0.3f), input.BrakeHeld);
        }

        void Button(Vector2 c, float r, string text, Color color, bool lit)
        {
            Disc(c, r, new Color(0f, 0f, 0f, 0.3f));
            Disc(c, r * 0.9f, new Color(color.r, color.g, color.b, lit ? 0.85f : 0.45f));
            Label(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), text, _small, Color.white, TextAnchor.MiddleCenter);
        }

        /// <summary>Contre-la-montre : tour en cours, meilleur tour et record à droite.</summary>
        void DrawChrono(float left, float right, float top, float s)
        {
            var gold = new Color(1f, 0.85f, 0.25f);
            float lapTime = Race.State == RaceManager.Phase.Racing && !Race.Player.Finished ? Race.RaceTime - Race.LapStartTime : 0f;
            Label(new Rect(left, top + 66 * s, 400 * s, 34 * s), $"Tour  {FormatTime(lapTime)}", _small, Color.white);
            float bestLap = Ghost != null ? Ghost.BestLap : 0f;
            float bestTime = Ghost != null ? Ghost.BestTimeBefore : 0f;
            Label(new Rect(right - 360 * s, top, 360 * s, 34 * s), "Meilleur tour", _small, gold, TextAnchor.UpperRight);
            Label(new Rect(right - 360 * s, top + 26 * s, 360 * s, 40 * s), bestLap > 0f ? FormatTime(bestLap) : "--:--.--", _medium, Color.white, TextAnchor.UpperRight);
            Label(new Rect(right - 360 * s, top + 72 * s, 360 * s, 34 * s), $"Record {Race.Laps} tour{(Race.Laps > 1 ? "s" : "")}", _small, gold, TextAnchor.UpperRight);
            Label(new Rect(right - 360 * s, top + 98 * s, 360 * s, 40 * s), bestTime > 0f ? FormatTime(bestTime) : "--:--.--", _medium, Color.white, TextAnchor.UpperRight);
            if (Ghost != null && Ghost.GhostVisible)
                Label(new Rect(right - 360 * s, top + 142 * s, 360 * s, 30 * s), "Fantôme du meilleur tour", _small, new Color(0.75f, 0.9f, 1f), TextAnchor.UpperRight);
        }

        void DrawResults(float w, float h, float s)
        {
            if (Race.Solo)
            {
                DrawChronoResults(w, s);
                return;
            }
            if (Cup != null && Cup.LastOrder != null)
            {
                DrawCupResults(w, s);
                return;
            }
            Box(new Rect(0, 0, w, h), new Color(0.05f, 0.08f, 0.18f, 0.55f));
            var p = Race.Player;
            Label(new Rect(0, 30 * s, w, 80 * s), p.Rank == 0 ? "Victoire !" : $"Tu finis {Ordinal(p.Rank + 1)}", _big,
                new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleCenter);

            float panelW = 560 * s, x = (w - panelW) * 0.5f, y = 130 * s, row = 38 * s;
            Box(new Rect(x, y - 10 * s, panelW, row * Race.Karts.Length + 20 * s), new Color(0f, 0f, 0f, 0.35f));
            for (int r = 0; r < Race.Order.Length; r++)
            {
                var k = Race.Karts[Race.Order[r]];
                float yy = y + r * row;
                if (k.IsPlayer) Box(new Rect(x, yy, panelW, row), new Color(1f, 0.85f, 0.2f, 0.3f));
                Label(new Rect(x + 16 * s, yy, 60 * s, row), Ordinal(r + 1), _medium, Color.white);
                UiKit.Portrait(new Vector2(x + 110 * s, yy + row * 0.5f), 16 * s, k.Spec);
                Label(new Rect(x + 130 * s, yy, 220 * s, row), k.Spec.Name, _medium, Color.white);
                string time = k.Finished ? FormatTime(k.FinishTime) : "en course…";
                Label(new Rect(x + panelW - 236 * s, yy, 220 * s, row), time, _medium, new Color(1f, 1f, 1f, 0.85f), TextAnchor.UpperRight);
            }

            float by = y + row * Race.Karts.Length + 30 * s;
            var laps = Race.LapTimes;
            string lapsText = "";
            for (int i = 0; i < laps.Length; i++) lapsText += (i > 0 ? "   " : "") + $"T{i + 1} {FormatTime(laps[i])}";
            Label(new Rect(0, by, w, 30 * s), lapsText, _small, Color.white, TextAnchor.MiddleCenter);

            DrawResultButtons();
        }

        void DrawResultButtons()
        {
            var buttons = ResultRects();
            var labels = ResultLabels;
            for (int i = 0; i < buttons.Length; i++)
                _ui.Button(buttons[i], labels[i], i == 0 ? new Color(1f, 0.5f, 0.15f) : new Color(0.25f, 0.45f, 0.8f), i == _focus);
        }

        void DrawChronoResults(float w, float s)
        {
            Box(new Rect(0, 0, w, Screen.height), new Color(0.05f, 0.08f, 0.18f, 0.55f));
            var gold = new Color(1f, 0.85f, 0.25f);
            var p = Race.Player;
            bool record = Ghost != null && Ghost.NewTimeRecord;
            Label(new Rect(0, 30 * s, w, 80 * s), record ? "Nouveau record !" : "Contre-la-montre", _big, gold, TextAnchor.MiddleCenter);

            float panelW = 520 * s, x = (w - panelW) * 0.5f, y = 130 * s, row = 38 * s;
            Box(new Rect(x, y - 10 * s, panelW, row * ResultRows + 20 * s), new Color(0f, 0f, 0f, 0.35f));
            var laps = Race.LapTimes;
            int bestIndex = -1;
            for (int i = 0; i < laps.Length; i++)
                if (laps[i] > 0f && (bestIndex < 0 || laps[i] < laps[bestIndex])) bestIndex = i;
            for (int i = 0; i < laps.Length; i++)
            {
                float yy = y + i * row;
                if (i == bestIndex) Box(new Rect(x, yy, panelW, row), new Color(1f, 0.85f, 0.2f, 0.25f));
                Label(new Rect(x + 20 * s, yy, 200 * s, row), $"Tour {i + 1}", _medium, Color.white);
                Label(new Rect(x + panelW - 240 * s, yy, 220 * s, row), FormatTime(laps[i]), _medium, Color.white, TextAnchor.UpperRight);
            }
            float ty = y + laps.Length * row;
            Label(new Rect(x + 20 * s, ty, 200 * s, row), "Total", _medium, gold);
            Label(new Rect(x + panelW - 240 * s, ty, 220 * s, row), FormatTime(p.FinishTime), _medium, gold, TextAnchor.UpperRight);
            float before = Ghost != null ? Ghost.BestTimeBefore : 0f;
            string recordText = before <= 0f ? "Premier temps sur ce circuit" : record ? $"Ancien record {FormatTime(before)}" : $"Record {FormatTime(before)}";
            Label(new Rect(x + 20 * s, ty + row, panelW - 40 * s, row), recordText, _small, new Color(1f, 1f, 1f, 0.85f), TextAnchor.MiddleCenter);
            DrawResultButtons();
        }

        /// <summary>Coupe : place à cette course, points gagnés et classement cumulé.</summary>
        void DrawCupResults(float w, float s)
        {
            Box(new Rect(0, 0, w, Screen.height), new Color(0.05f, 0.08f, 0.18f, 0.55f));
            var gold = new Color(1f, 0.85f, 0.25f);
            var st = Cup.Standings;
            int me = Cup.Player;
            Label(new Rect(0, 24 * s, w, 70 * s), $"Tu finis {Ordinal(st.LastPlace(me) + 1)}", _big, gold, TextAnchor.MiddleCenter);
            Label(new Rect(0, 88 * s, w, 34 * s), $"{Cup.Def.Name} — après la course {Cup.Race + 1}/{Cups.RacesPerCup}", _small, Color.white, TextAnchor.MiddleCenter);

            float panelW = 600 * s, x = (w - panelW) * 0.5f, y = 130 * s, row = 38 * s;
            Box(new Rect(x, y - 10 * s, panelW, row * ResultRows + 20 * s), new Color(0f, 0f, 0f, 0.35f));
            var ranking = st.Ranking();
            for (int r = 0; r < ranking.Length; r++)
            {
                int id = ranking[r];
                var spec = AnimalSpec.Roster[id];
                float yy = y + r * row;
                if (id == me) Box(new Rect(x, yy, panelW, row), new Color(1f, 0.85f, 0.2f, 0.3f));
                Label(new Rect(x + 16 * s, yy, 60 * s, row), Ordinal(r + 1), _medium, Color.white);
                UiKit.Portrait(new Vector2(x + 110 * s, yy + row * 0.5f), 16 * s, spec);
                Label(new Rect(x + 130 * s, yy, 200 * s, row), spec.Name, _medium, Color.white);
                Label(new Rect(x + 320 * s, yy, 120 * s, row), $"{Ordinal(st.LastPlace(id) + 1)} +{st.LastPoints(id)}", _small, new Color(0.7f, 0.95f, 1f), TextAnchor.MiddleLeft);
                Label(new Rect(x + panelW - 176 * s, yy, 160 * s, row), $"{st.Total(id)} pts", _medium, Color.white, TextAnchor.UpperRight);
            }
            DrawResultButtons();
        }

        void DrawPause(float w, float h, float s)
        {
            Box(new Rect(0, 0, w, h), new Color(0.05f, 0.08f, 0.18f, 0.6f));
            Label(new Rect(0, h * 0.14f, w, 90 * s), "Pause", _big, new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleCenter);
            var buttons = PauseRects();
            for (int i = 0; i < buttons.Length; i++)
                _ui.Button(buttons[i], PauseChoices[i], i == 0 ? new Color(1f, 0.5f, 0.15f) : new Color(0.25f, 0.45f, 0.8f), i == _focus);
        }

        static string FormatTime(float t)
        {
            int m = (int)(t / 60f);
            float sec = t - m * 60f;
            return $"{m}:{sec:00.00}";
        }
    }
}
