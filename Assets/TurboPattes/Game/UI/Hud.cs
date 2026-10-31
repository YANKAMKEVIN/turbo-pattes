using TurboPattes.Core;
using TurboPattes.Game.Kart;
using TurboPattes.Game.Race;
using TurboPattes.Game.Visuals;
using UnityEngine;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Interface de course en IMGUI (prototype) : tour, place, objet, vitesse, classement, compte à rebours,
    /// commandes tactiles et écran de résultats. Le tactile est lu par <see cref="PlayerInput"/> ; ici on ne fait que dessiner.
    /// </summary>
    public sealed class Hud : MonoBehaviour
    {
        public RaceManager Race;

        GUIStyle _big, _medium, _small, _center;
        float _scale;
        static readonly string[] ItemNames = { "", "Carotte turbo", "Noix de coco", "Pot de miel", "Bulle bouclier" };
        static readonly Color[] ItemColors =
        {
            Color.clear, new(1f, 0.55f, 0.15f), new(0.55f, 0.35f, 0.2f), new(1f, 0.75f, 0.1f), new(0.5f, 0.85f, 1f),
        };

        void EnsureStyles()
        {
            float scale = Screen.height / 720f;
            if (_big != null && Mathf.Approximately(scale, _scale)) return;
            _scale = scale;
            _big = new GUIStyle { fontSize = Mathf.RoundToInt(64 * scale), fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            _medium = new GUIStyle(_big) { fontSize = Mathf.RoundToInt(30 * scale) };
            _small = new GUIStyle(_big) { fontSize = Mathf.RoundToInt(19 * scale), fontStyle = FontStyle.Bold };
            _center = new GUIStyle(_big) { alignment = TextAnchor.MiddleCenter };
        }

        static string Ordinal(int rank) => rank == 1 ? "1er" : rank + "e";

        void Label(Rect r, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            var s = new GUIStyle(style) { alignment = anchor };
            float o = Mathf.Max(1f, 2.5f * _scale);
            s.normal.textColor = new Color(0f, 0f, 0f, 0.6f * color.a);
            GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, s);
            s.normal.textColor = color;
            GUI.Label(r, text, s);
        }

        static void Disc(Vector2 center, float radius, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), ProceduralTextures.Circle);
            GUI.color = old;
        }

        static void Box(Rect r, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, ProceduralTextures.White);
            GUI.color = old;
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

            // Tour
            int lap = p.Laps.DisplayLap(Race.Laps);
            Label(new Rect(left, top, 400 * s, 50 * s), $"Tour {lap}/{Race.Laps}", _medium, Color.white);
            Label(new Rect(left, top + 38 * s, 400 * s, 40 * s), FormatTime(Race.RaceTime), _small, new Color(1f, 1f, 1f, 0.85f));

            // Place
            int rank = p.Rank + 1;
            var rankColor = rank == 1 ? new Color(1f, 0.85f, 0.2f) : rank <= 3 ? new Color(0.6f, 0.95f, 1f) : Color.white;
            Label(new Rect(right - 300 * s, top - 6 * s, 300 * s, 80 * s), Ordinal(rank), _big, rankColor, TextAnchor.UpperRight);
            Label(new Rect(right - 300 * s, top + 62 * s, 300 * s, 30 * s), $"sur {Race.Karts.Length}", _small, Color.white, TextAnchor.UpperRight);

            DrawItemSlot(new Vector2(w * 0.5f, top + 46 * s), 42 * s, p);
            DrawStandings(left, top + 90 * s, s);

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
            }
            if (Race.BannerTime > 0f && !string.IsNullOrEmpty(Race.Banner))
            {
                var st = new GUIStyle(_center) { fontSize = Mathf.RoundToInt(70 * s) };
                Label(new Rect(0, h * 0.28f, w, h * 0.2f), Race.Banner, st, new Color(1f, 0.85f, 0.25f, Mathf.Clamp01(Race.BannerTime * 2f)),
                    TextAnchor.MiddleCenter);
            }
            if (Race.WrongWay && Mathf.Repeat(Time.time, 0.8f) < 0.55f)
                Label(new Rect(0, h * 0.4f, w, 80 * s), "Mauvais sens !", _big, new Color(1f, 0.3f, 0.3f), TextAnchor.MiddleCenter);

            if (Race.Input.TouchMode) DrawTouchControls(h, s);
            else if (Race.State == RaceManager.Phase.Countdown)
                Label(new Rect(0, h - safe.yMin - 50 * s, w, 30 * s),
                    "Flèches : conduire   Espace : drift   E : objet   R : rejouer   (manette : RT, RB, X)", _small,
                    new Color(1f, 1f, 1f, 0.8f), TextAnchor.MiddleCenter);
        }

        void DrawItemSlot(Vector2 c, float r, KartMotor p)
        {
            Disc(c, r + 5 * _scale, new Color(1f, 1f, 1f, 0.9f));
            Disc(c, r, new Color(0.12f, 0.15f, 0.25f, 0.85f));
            var kind = p.Item;
            if (kind == ItemKind.None) return;
            if (p.ItemRoll > 0f)
            {
                // Roulette : fait défiler les objets.
                kind = (ItemKind)(1 + (int)(Time.time * 14f) % 4);
            }
            Disc(c, r * 0.62f, ItemColors[(int)kind]);
            string letter = kind switch { ItemKind.Carrot => "C", ItemKind.Coconut => "N", ItemKind.Honey => "M", _ => "B" };
            Label(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), letter, _medium, Color.white, TextAnchor.MiddleCenter);
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
                Disc(new Vector2(x + 36 * s, yy + row * 0.5f), 7 * s, k.Spec.Kart);
                Label(new Rect(x + 50 * s, yy, 120 * s, row), k.Spec.Name, _small, k.IsPlayer ? new Color(1f, 0.92f, 0.5f) : Color.white);
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
            Button(Gui(l.Item), l.ItemR, "OBJET", new Color(0.4f, 0.75f, 1f), input.ItemHeld || p.Item != ItemKind.None && p.ItemRoll <= 0f);
            Button(Gui(l.Brake), l.BrakeR, "FREIN", new Color(0.9f, 0.3f, 0.3f), input.BrakeHeld);
        }

        void Button(Vector2 c, float r, string text, Color color, bool lit)
        {
            Disc(c, r, new Color(0f, 0f, 0f, 0.3f));
            Disc(c, r * 0.9f, new Color(color.r, color.g, color.b, lit ? 0.85f : 0.45f));
            Label(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), text, _small, Color.white, TextAnchor.MiddleCenter);
        }

        void DrawResults(float w, float h, float s)
        {
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
                Disc(new Vector2(x + 110 * s, yy + row * 0.5f), 11 * s, k.Spec.Kart);
                Label(new Rect(x + 130 * s, yy, 220 * s, row), k.Spec.Name, _medium, Color.white);
                string time = k.Finished ? FormatTime(k.FinishTime) : "en course…";
                Label(new Rect(x + panelW - 236 * s, yy, 220 * s, row), time, _medium, new Color(1f, 1f, 1f, 0.85f), TextAnchor.UpperRight);
            }

            float by = y + row * Race.Karts.Length + 30 * s;
            var laps = Race.LapTimes;
            string lapsText = "";
            for (int i = 0; i < laps.Length; i++) lapsText += (i > 0 ? "   " : "") + $"T{i + 1} {FormatTime(laps[i])}";
            Label(new Rect(0, by, w, 30 * s), lapsText, _small, Color.white, TextAnchor.MiddleCenter);

            var btn = new Rect(w * 0.5f - 140 * s, by + 44 * s, 280 * s, 70 * s);
            Box(btn, new Color(1f, 0.55f, 0.2f, 0.95f));
            Label(btn, "Rejouer", _medium, Color.white, TextAnchor.MiddleCenter);
            if (GUI.Button(btn, GUIContent.none, GUIStyle.none)) Race.Restart();
            Label(new Rect(0, btn.yMax + 8 * s, w, 30 * s), "ou R / Start", _small, new Color(1f, 1f, 1f, 0.7f), TextAnchor.MiddleCenter);
        }

        static string FormatTime(float t)
        {
            int m = (int)(t / 60f);
            float sec = t - m * 60f;
            return $"{m}:{sec:00.00}";
        }
    }
}
