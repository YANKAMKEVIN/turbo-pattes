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
    /// Interface de course en IMGUI, direction « arcade sportif » (plaques carbone, fiches blanc piste, Saira Condensed) : tour, place, objet, vitesse, classement, compte à rebours,
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
        /// <summary>Choix de pause en attente de confirmation (Recommencer ou Menu), -1 sinon.</summary>
        int _confirm = -1;
        // Éclat de la jauge de drift au passage d'un niveau.
        int _driftLevel;
        // Résultats : instant d'arrivée sur l'écran (mise en scène des lignes, confettis).
        float _resultsAt = -1f;
        // Traits de vitesse (turbo, sauts) et éclat du compteur au déclenchement d'un turbo.
        float _speedLines;
        bool _wasBoosting;
        float _boostFlashAt = -10f;
        float _driftFlashAt = -10f;
        // Classement complet affiché deux secondes à chaque passage de ligne ; sinon les deux écarts utiles.
        int _seenLap = int.MinValue;
        float _fullStandingsUntil;
        // Tendance des écarts, mesurée chaque seconde : le joueur rattrape-t-il, est-il rattrapé ?
        float _trendAt;
        readonly float[] _gapBefore = new float[2];
        readonly int[] _gapTrend = new int[2];
        readonly KartMotor[] _gapKart = new KartMotor[2];
        // Mini-carte : cadrage du tracé recalculé seulement si la piste ou la place à l'écran change.
        TurboPattes.Game.Track.TrackPath _mapPath;
        Rect _mapRect;
        MapFit _mapFit;
        int[] _mapDots;
        static readonly string[] ItemNames = { "", "Carotte turbo", "Noix de coco", "Pot de miel", "Bulle bouclier", "Plume planante", "Pelote de laine", "Comète" };
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
            _center = new GUIStyle(_big) { alignment = TextAnchor.MiddleCenter };
        }

        public static string Ordinal(int rank) => RaceText.Ordinal(rank);

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

        Rect[] PauseRects() => MenuButtons(PauseChoices.Length, Screen.height * 0.36f, false);

        Rect ConfirmCard() => new(Screen.width * 0.5f - 320 * S, Screen.height * 0.3f, 640 * S, 240 * S);

        Rect[] ConfirmRects() => MenuButtons(2, ConfirmCard().y + 146 * S, true);

        // Lignes du tableau de résultats : karts, ou tours en contre-la-montre.
        int ResultRows => Race.Solo ? Race.Laps + 4 : Race.Karts.Length;

        Rect[] ResultRects()
        {
            float row = 38 * S;
            return MenuButtons(ResultLabels.Length, 130 * S + row * ResultRows + 74 * S, true);
        }

        void Update()
        {
            if (Race == null || Race.Player == null || SettingsScreen.IsOpen) return;
            UpdateSpeedFeel();
            var tap = UiKit.Tap();
            if (Race.Paused && _confirm >= 0) Navigate(ConfirmRects(), tap, ChooseConfirm, vertical: false);
            else if (Race.Paused) Navigate(PauseRects(), tap, ChoosePause, vertical: true);
            else if (Race.State == RaceManager.Phase.Results)
            {
                if (_resultsAt < 0f) _resultsAt = Time.time;
                Navigate(ResultRects(), tap, ChooseResult, vertical: false);
            }
            else
            {
                _focus = 0;
                _confirm = -1;
                TrackStandings();
                if (tap.HasValue && Race.Input.TouchMode && PauseButton().Contains(tap.Value)) { Jukebox.Instance.Confirm(); Race.SetPaused(true); }
            }
        }

        void TrackStandings()
        {
            int lap = Race.Player.Laps.Lap;
            if (lap != _seenLap)
            {
                if (lap > _seenLap && _seenLap != int.MinValue && Race.State == RaceManager.Phase.Racing) _fullStandingsUntil = Time.time + 2f;
                _seenLap = lap;
            }
            if (Time.time < _trendAt) return;
            _trendAt = Time.time + 1f;
            for (int side = 0; side < 2; side++)
            {
                bool has = Race.GapToNeighbour(side == 0 ? -1 : 1, out var kart, out float gap);
                _gapTrend[side] = has && kart == _gapKart[side] ? RaceGaps.Trend(_gapBefore[side], gap) : 0;
                _gapKart[side] = has ? kart : null;
                _gapBefore[side] = gap;
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
            else
            {
                // Recommencer ou Menu : on demande d'abord, « Annuler » sélectionné.
                _confirm = i;
                _focus = 0;
            }
        }

        void ChooseConfirm(int i)
        {
            int choice = _confirm;
            _confirm = -1;
            _focus = 0; // retour sur « Reprendre »
            if (i == 1) Leave(toMenu: choice == 3);
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

            DrawSpeedLines(w, h, p);

            // Tour et chrono, sur une plaque carbone (contraste stable quel que soit le décor).
            int lap = p.Laps.DisplayLap(Race.Laps);
            _ui.Plate(new Rect(left - 10 * s, top - 6 * s, 210 * s, 84 * s));
            Label(new Rect(left + 4 * s, top - 2 * s, 200 * s, 40 * s), "TOUR", _small, UiKit.Signal, TextAnchor.MiddleLeft);
            _ui.Digits(new Rect(left + 70 * s, top - 4 * s, 120 * s, 40 * s), $"{lap}/{Race.Laps}", _medium, UiKit.Chalk, TextAnchor.MiddleLeft);
            _ui.Digits(new Rect(left + 4 * s, top + 34 * s, 200 * s, 40 * s), FormatTime(Race.RaceTime), _medium, UiKit.Chalk, TextAnchor.MiddleLeft);

            if (Race.Solo) DrawChrono(left, right, top, s);
            else
            {
                // Place : grand chiffre sur fiche blanc piste, rouge quand on mène.
                int rank = p.Rank + 1;
                var place = new Rect(right - 150 * s, top - 6 * s, 150 * s, 100 * s);
                UiKit.Card(place, rank == 1 ? UiKit.Red : UiKit.Chalk, 22 * s, 4 * s);
                var ink = rank == 1 ? UiKit.Chalk : UiKit.Carbon;
                // Dépassement : le chiffre rebondit et une flèche montante (bleue) ou descendante (rouge) s'affiche.
                var pt = Race.Place;
                var placeStyle = pt.Showing ? new GUIStyle(_big) { fontSize = Mathf.RoundToInt(_big.fontSize * pt.Bounce) } : _big;
                Label(new Rect(place.x - 40 * s, place.y - 4 * s, place.width + 80 * s, 76 * s), Ordinal(rank), placeStyle, ink, TextAnchor.MiddleCenter);
                if (pt.Showing && pt.Direction != 0) PlaceArrow(new Vector2(place.x - 30 * s, place.center.y), pt.Direction > 0, pt.Since / PlaceTracker.Show, s);
                Label(new Rect(place.x, place.y + 64 * s, place.width, 30 * s), $"sur {Race.Karts.Length}", _small, ink, TextAnchor.MiddleCenter);

                DrawItemSlot(new Vector2(w * 0.5f, top + 42 * s), 40 * s, p);
                if (Time.time < _fullStandingsUntil || Race.State == RaceManager.Phase.Countdown) DrawStandings(left, top + 96 * s, s);
                else DrawGaps(left, top + 96 * s, s);
            }

            // Mini-carte à droite, sous la place (ou sous le panneau du chrono) ; en tactile, à gauche du bouton pause
            // pour laisser la place au bouton OBJET.
            DrawMiniMap(Race.Solo ? new Rect(right - 170 * s, top + 196 * s, 170 * s, 170 * s)
                : Race.Input.TouchMode ? new Rect(PauseButton().xMin - 166 * s, top + 104 * s, 150 * s, 150 * s)
                : new Rect(right - 170 * s, top + 110 * s, 170 * s, 170 * s), s);

            // Vitesse (chiffres fixes) et charge de drift
            float kmh = p.Speed * 3.6f;
            var speedRect = Race.Input.TouchMode
                ? new Rect(w * 0.5f - 90 * s, h - safe.yMin - 66 * s, 180 * s, 50 * s)
                : new Rect(right - 180 * s, h - safe.yMin - 74 * s, 180 * s, 54 * s);
            _ui.Plate(speedRect);
            float boostFlash = Mathf.Clamp01(1f - (Time.time - _boostFlashAt) / 0.35f);
            if (boostFlash > 0f) UiKit.Card(speedRect, UiKit.Alpha(UiKit.Signal, 0.85f * boostFlash), Mathf.Min(speedRect.height * 0.3f, 18f * s));
            _ui.Digits(new Rect(speedRect.x, speedRect.y, speedRect.width - 64 * s, speedRect.height), $"{kmh:0}", _medium,
                boostFlash > 0.4f ? UiKit.Carbon : p.BoostTime > 0f ? UiKit.Signal : UiKit.Chalk, TextAnchor.MiddleRight);
            Label(new Rect(speedRect.xMax - 60 * s, speedRect.y, 56 * s, speedRect.height), "km/h", _small, UiKit.Alpha(UiKit.Chalk, 0.75f), TextAnchor.MiddleLeft);
            if (p.Drift.Level > _driftLevel) _driftFlashAt = Time.time;
            _driftLevel = p.Drift.Active ? p.Drift.Level : 0;
            // Jauge de charge sous le kart : arc en trois segments, un par palier.
            if (p.Drift.Active) DriftArc(new Vector2(w * 0.5f, h * 0.6f), 74 * s, 25f, 130f, 9 * s, p.Drift.Charge);

            DrawThreat(w, h - safe.yMin - (Race.Input.TouchMode ? 150 : 170) * s, s);

            if (Race.State == RaceManager.Phase.Countdown)
            {
                int n = Mathf.CeilToInt(Race.CountdownLeft);
                if (n <= 3 && n >= 1)
                {
                    float pulse = 1f + (Mathf.Ceil(Race.CountdownLeft) - Race.CountdownLeft) * 0.5f;
                    var st = new GUIStyle(_center) { fontSize = Mathf.RoundToInt(140 * s * pulse) };
                    Label(new Rect(0, h * 0.25f, w, h * 0.3f), n.ToString(), st, UiKit.Signal, TextAnchor.MiddleCenter, 5f);
                }
                var card = new Rect(w * 0.5f - 260 * s, h * 0.58f, 520 * s, 84 * s);
                UiKit.Card(card, UiKit.Chalk, 20 * s, 4 * s);
                Label(new Rect(card.x, card.y + 4 * s, card.width, 44 * s), Race.Circuit.Name, _medium, UiKit.Carbon, TextAnchor.MiddleCenter);
                string sub = Cup != null ? $"{Cup.Def.Name} — course {Cup.Race + 1}/{Cups.RacesPerCup}" : Race.Solo ? "Contre-la-montre" : null;
                if (sub != null) Label(new Rect(card.x, card.y + 44 * s, card.width, 34 * s), sub, _small, UiKit.Red, TextAnchor.MiddleCenter);
            }
            if (Race.BannerTime > 0f && !string.IsNullOrEmpty(Race.Banner))
                Banner(Race.Banner, h * 0.3f, UiKit.Red, UiKit.Chalk, Mathf.Clamp01(Race.BannerTime * 2f), 64f);
            if (Race.WrongWay && Mathf.Repeat(Time.time, 0.8f) < 0.55f)
                Banner("Mauvais sens !", h * 0.42f, UiKit.Carbon, UiKit.Red, 1f, 54f);

            if (Race.Input.TouchMode)
            {
                DrawTouchControls(h, s);
                var pb = PauseButton();
                Disc(pb.center, pb.width * 0.5f, UiKit.Alpha(UiKit.Carbon, 0.8f));
                Box(new Rect(pb.center.x - 9 * s, pb.center.y - 11 * s, 6 * s, 22 * s), UiKit.Chalk);
                Box(new Rect(pb.center.x + 3 * s, pb.center.y - 11 * s, 6 * s, 22 * s), UiKit.Chalk);
            }
            else if (Race.State == RaceManager.Phase.Countdown)
            {
                var hint = new Rect(w * 0.5f - 470 * s, h - safe.yMin - 52 * s, 940 * s, 36 * s);
                _ui.Plate(hint, 0.9f);
                Label(hint, "Flèches : conduire   Espace : dérapage   E : objet   Échap : pause   (manette : RT, RB, X, Start)", _ui.Body,
                    UiKit.Chalk, TextAnchor.MiddleCenter);
            }
        }

        /// <summary>
        /// Jauge de drift : arc de <paramref name="sweep"/> degrés à partir de <paramref name="start"/> (sens horaire à l'écran),
        /// en trois segments qui se remplissent en continu, chacun à la couleur des étincelles de son niveau, avec un
        /// éclat au passage d'un palier.
        /// </summary>
        void DriftArc(Vector2 c, float radius, float start, float sweep, float thickness, float charge)
        {
            const int segments = 3, dotsPerSegment = 12;
            float gap = sweep >= 360f ? 8f : 5f;
            float segSweep = (sweep - gap * (sweep >= 360f ? segments : segments - 1)) / segments;
            float flash = Mathf.Clamp01(1f - (Time.time - _driftFlashAt) / 0.3f);
            for (int seg = 0; seg < segments; seg++)
            {
                float fill = DriftCharge.SegmentFill(charge, seg);
                var col = KartMotor.DriftColors[seg + 1];
                float a0 = start + seg * (segSweep + gap);
                for (int i = 0; i < dotsPerSegment; i++)
                {
                    float t = (i + 0.5f) / dotsPerSegment;
                    float a = (a0 + t * segSweep) * Mathf.Deg2Rad;
                    var pos = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius;
                    Disc(pos, thickness * 0.5f + 1.5f * _scale, UiKit.Alpha(UiKit.Carbon, 0.7f));
                    if (t <= fill) Disc(pos, thickness * 0.5f, col);
                }
            }
            if (flash > 0f) UiKit.Ring(c, radius + (1f - flash) * 14f * _scale, thickness * 0.6f, UiKit.Alpha(UiKit.Chalk, flash), 36);
        }

        /// <summary>Projectile qui arrive par l'arrière : son icône en bas de l'écran, qui grossit et clignote en approchant.</summary>
        void DrawThreat(float w, float y, float s)
        {
            if (Race.Threat == ItemKind.None || !ThreatAlert.BlinkOn(Time.time, Race.ThreatUrgency)) return;
            float r = 34 * s * ThreatAlert.Scale(Race.ThreatUrgency);
            var c = new Vector2(w * 0.5f, y - r);
            Disc(c, r + 4 * s, UiKit.Chalk);
            Disc(c, r, UiKit.Red);
            float ir = r * 0.78f;
            GUI.DrawTexture(new Rect(c.x - ir, c.y - ir, ir * 2f, ir * 2f), Icons.Item(Race.Threat), ScaleMode.ScaleToFit);
            var tag = new Rect(c.x - 80 * s, c.y + r + 6 * s, 160 * s, 26 * s);
            _ui.Plate(tag);
            Label(tag, "DERRIÈRE !", _small, UiKit.Red, TextAnchor.MiddleCenter);
        }

        /// <summary>Traits de vitesse radiaux sur les bords pendant le turbo et les sauts, plus ou moins nombreux selon la qualité.</summary>
        /// <summary>Intensité des traits (lissée) et éclat du compteur ; une fois par image, dans Update.</summary>
        void UpdateSpeedFeel()
        {
            var p = Race.Player;
            bool boosting = p.BoostTime > 0f;
            if (boosting && !_wasBoosting) _boostFlashAt = Time.time;
            _wasBoosting = boosting;
            float target = Race.State == RaceManager.Phase.Racing && !Race.Paused ? SpeedLines.Target(boosting, p.Airborne, p.Speed / KartMotor.MaxSpeed) : 0f;
            _speedLines = Mathf.MoveTowards(_speedLines, target, Time.deltaTime * 4f);
        }

        void DrawSpeedLines(float w, float h, KartMotor p)
        {
            if (_speedLines <= 0.01f) return;
            var center = new Vector2(w * 0.5f, h * 0.5f);
            float half = center.magnitude, thick = 3f * _scale;
            int count = SpeedLines.Count(QualityTuner.Effective);
            var m = GUI.matrix;
            for (int i = 0; i < count; i++)
            {
                SpeedLines.Line(i, count, Time.time, out float deg, out float inner, out float length, out float alpha);
                GUI.matrix = m;
                GUIUtility.RotateAroundPivot(deg, center);
                Box(new Rect(center.x + inner * half, center.y - thick * 0.5f, length * half, thick), UiKit.Alpha(UiKit.Chalk, 0.55f * alpha * _speedLines));
            }
            GUI.matrix = m;
        }

        /// <summary>Chevron de dépassement à gauche de la place : monte (gain) ou descend (perte) en s'effaçant.</summary>
        void PlaceArrow(Vector2 c, bool up, float t, float s)
        {
            float a = 1f - t, dy = (up ? -1f : 1f) * t * 24f * s;
            var col = UiKit.Alpha(up ? UiKit.Telemetry : UiKit.Red, a);
            var ink = UiKit.Alpha(UiKit.Carbon, a);
            float hw = 9 * s, hh = 26 * s;
            // « /\ » pour monter, « \/ » pour descendre, cerné de carbone.
            float k = up ? 0.4f : -0.4f;
            Disc(new Vector2(c.x, c.y + dy), 24 * s, UiKit.Alpha(UiKit.Chalk, a));
            UiKit.Slash(new Rect(c.x - hw - 1 * s, c.y + dy - hh * 0.5f, hw, hh), ink, k);
            UiKit.Slash(new Rect(c.x + 1 * s, c.y + dy - hh * 0.5f, hw, hh), ink, -k);
            UiKit.Slash(new Rect(c.x - hw + 1 * s, c.y + dy - hh * 0.5f + 3 * s, hw - 4 * s, hh - 6 * s), col, k);
            UiKit.Slash(new Rect(c.x + 3 * s, c.y + dy - hh * 0.5f + 3 * s, hw - 4 * s, hh - 6 * s), col, -k);
        }

        /// <summary>Bandeau penché, comme un trait de peinture rapide, pour les annonces de course.</summary>
        void Banner(string text, float y, Color fill, Color ink, float alpha, float size)
        {
            float s = _scale, w = Screen.width;
            var st = new GUIStyle(_center) { fontSize = Mathf.RoundToInt(size * s) };
            float tw = st.CalcSize(new GUIContent(text)).x + 90 * s;
            var r = new Rect(w * 0.5f - tw * 0.5f, y, tw, size * 1.35f * s);
            UiKit.Slash(r, UiKit.Alpha(fill, alpha), 0.25f);
            Label(r, text, st, UiKit.Alpha(ink, alpha), TextAnchor.MiddleCenter);
        }

        void DrawItemSlot(Vector2 c, float r, KartMotor p)
        {
            // En tactile, l'objet est dans le bouton OBJET, sous le pouce : ici, un simple rappel plus petit, sans nom.
            bool touch = Race.Input.TouchMode;
            if (touch) r *= 0.65f;
            Disc(c, r + 4 * _scale, UiKit.Chalk);
            Disc(c, r, UiKit.Carbon);
            var kind = ItemRoll.Shown(p.Item, p.ItemRoll, Time.time);
            if (kind == ItemKind.None) return;
            float ir = r * 0.9f;
            GUI.DrawTexture(new Rect(c.x - ir, c.y - ir, ir * 2f, ir * 2f), Icons.Item(kind), ScaleMode.ScaleToFit);
            if (p.ItemRoll <= 0f && !touch)
            {
                var tag = new Rect(c.x - 100 * _scale, c.y + r + 8 * _scale, 200 * _scale, 28 * _scale);
                _ui.Plate(tag);
                Label(tag, ItemNames[(int)kind], _small, UiKit.Chalk, TextAnchor.MiddleCenter);
            }
        }

        void DrawStandings(float x, float y, float s)
        {
            var order = Race.Order;
            float row = 24 * s;
            _ui.Plate(new Rect(x - 10 * s, y - 6 * s, 190 * s, row * order.Length + 12 * s));
            for (int r = 0; r < order.Length; r++)
            {
                var k = Race.Karts[order[r]];
                float yy = y + r * row;
                if (k.IsPlayer) Box(new Rect(x - 10 * s, yy, 190 * s, row), UiKit.Red);
                _ui.Digits(new Rect(x, yy, 24 * s, row), (r + 1).ToString(), _small, k.IsPlayer ? UiKit.Chalk : UiKit.Signal, TextAnchor.MiddleCenter);
                // Tour de chronométrage : pastille couleur d'équipe, code à trois lettres, numéro.
                Box(new Rect(x + 30 * s, yy + 4 * s, 5 * s, row - 8 * s), k.Spec.Kart);
                Label(new Rect(x + 44 * s, yy, 70 * s, row), k.Spec.Code, _small, UiKit.Chalk, TextAnchor.MiddleLeft);
                _ui.Digits(new Rect(x + 110 * s, yy, 56 * s, row), k.Spec.Number.ToString(), _small,
                    k.IsPlayer ? UiKit.Chalk : UiKit.Steel, TextAnchor.MiddleRight);
            }
        }

        /// <summary>
        /// Pilote juste devant et juste derrière, avec l'écart en secondes. Bleu : l'écart tourne à l'avantage du joueur
        /// (il rattrape celui de devant, ou creuse sur celui de derrière) ; rouge : l'inverse.
        /// </summary>
        void DrawGaps(float x, float y, float s)
        {
            float row = 34 * s, width = 300 * s;
            int rows = 0;
            for (int side = 0; side < 2; side++) if (Race.GapToNeighbour(side == 0 ? -1 : 1, out _, out _)) rows++;
            if (rows == 0) return;
            _ui.Plate(new Rect(x - 10 * s, y - 6 * s, width, row * rows + 12 * s));
            float yy = y;
            for (int side = 0; side < 2; side++)
            {
                if (!Race.GapToNeighbour(side == 0 ? -1 : 1, out var k, out float gap)) continue;
                int trend = _gapKart[side] == k ? _gapTrend[side] : 0;
                // Devant : un écart qui fond est bon signe ; derrière : un écart qui grandit.
                int good = side == 0 ? -trend : trend;
                var col = good > 0 ? UiKit.Telemetry : good < 0 ? UiKit.Red : UiKit.Chalk;
                Label(new Rect(x, yy, 96 * s, row), side == 0 ? "DEVANT" : "DERRIÈRE", _small, UiKit.Signal, TextAnchor.MiddleLeft);
                UiKit.Portrait(new Vector2(x + 110 * s, yy + row * 0.5f), 12 * s, k.Spec);
                Label(new Rect(x + 128 * s, yy, 90 * s, row), k.Spec.Name, _small, UiKit.Chalk, TextAnchor.MiddleLeft);
                if (good != 0) Box(new Rect(x + width - 92 * s, yy + 4 * s, 76 * s, row - 8 * s), col);
                _ui.Digits(new Rect(x + width - 92 * s, yy, 76 * s, row), RaceText.Seconds(gap), _small, UiKit.Chalk, TextAnchor.MiddleCenter);
                yy += row;
            }
        }

        /// <summary>Mini-carte : contour du circuit vu de dessus, un point par kart à la couleur du pilote, le joueur plus gros.</summary>
        void DrawMiniMap(Rect r, float s)
        {
            var path = Race.Path;
            if (path == null) return;
            if (_mapPath != path || _mapRect != r || _mapDots == null)
            {
                var pts = path.Points;
                var xs = new float[pts.Length];
                var zs = new float[pts.Length];
                for (int i = 0; i < pts.Length; i++) { xs[i] = pts[i].x; zs[i] = pts[i].z; }
                _mapFit = MiniMap.Fit(xs, zs, r.x, r.y, r.width, r.height, 16 * s);
                _mapDots = MiniMap.Spread(pts.Length, 120);
                _mapPath = path;
                _mapRect = r;
            }
            _ui.Plate(r, 0.85f);
            foreach (int i in _mapDots)
            {
                _mapFit.Project(path.Points[i].x, path.Points[i].z, out float x, out float y);
                Disc(new Vector2(x, y), 2.4f * s, UiKit.Alpha(UiKit.Chalk, 0.8f));
            }
            // Ligne de départ : petit repère jaune.
            _mapFit.Project(path.Points[0].x, path.Points[0].z, out float lx, out float ly);
            Box(new Rect(lx - 6 * s, ly - 2 * s, 12 * s, 4 * s), UiKit.Signal);
            foreach (var k in Race.Karts)
            {
                if (k.IsPlayer) continue;
                _mapFit.Project(k.Position.x, k.Position.z, out float x, out float y);
                Disc(new Vector2(x, y), 5.5f * s, UiKit.Carbon);
                Disc(new Vector2(x, y), 4f * s, k.Spec.Kart);
            }
            var p = Race.Player;
            _mapFit.Project(p.Position.x, p.Position.z, out float px, out float py);
            Disc(new Vector2(px, py), 9f * s, UiKit.Chalk);
            Disc(new Vector2(px, py), 6.5f * s, p.Spec.Kart);
        }

        void DrawTouchControls(float h, float s)
        {
            var input = Race.Input;
            var l = input.Layout;
            Vector2 Gui(Vector2 screen) => new(screen.x, h - screen.y);

            if (input.SteerActive)
            {
                var a = Gui(input.SteerAnchor);
                Disc(a, l.SteerRange * 1.2f, UiKit.Alpha(UiKit.Carbon, 0.35f));
                float dx = Mathf.Clamp(input.SteerFinger.x - input.SteerAnchor.x, -l.SteerRange, l.SteerRange);
                Disc(a + new Vector2(dx, 0f), l.SteerRange * 0.5f, UiKit.Alpha(UiKit.Chalk, 0.75f));
            }
            else
            {
                var hint = new Vector2(l.SteerHintX, h * 0.78f);
                Disc(hint, l.SteerRange * 1.2f, UiKit.Alpha(UiKit.Carbon, 0.3f));
                Label(new Rect(hint.x - 120 * s, hint.y - 15 * s, 240 * s, 30 * s), "< glisser >", _small, UiKit.Alpha(UiKit.Chalk, 0.8f),
                    TextAnchor.MiddleCenter);
            }

            var p = Race.Player;
            var driftColor = p.Drift.Active && p.Drift.Level > 0 ? KartMotor.DriftColors[p.Drift.Level] : UiKit.Signal;
            TouchButton(Gui(l.Drift), l.DriftR, "DÉRAPAGE", driftColor, input.DriftHeld);
            // Autour du bouton : la même jauge, en couronne.
            if (p.Drift.Active) DriftArc(Gui(l.Drift), l.DriftR + 9 * s, -90f, 360f, 8 * s, p.Drift.Charge);
            if (!Race.Solo) ItemButton(Gui(l.Item), l.ItemR, p, input.ItemHeld);
            TouchButton(Gui(l.Brake), l.BrakeR, "FREIN", UiKit.Red, input.BrakeHeld);
        }

        /// <summary>
        /// Bouton OBJET : discret et vide sans objet ; pendant le tirage, la roulette y défile ; prêt, il montre l'icône
        /// de l'objet en grand sur fond bleu télémétrie.
        /// </summary>
        void ItemButton(Vector2 c, float r, KartMotor p, bool held)
        {
            var kind = ItemRoll.Shown(p.Item, p.ItemRoll, Time.time);
            if (kind == ItemKind.None)
            {
                Disc(c, r, UiKit.Alpha(UiKit.Carbon, 0.35f));
                Disc(c, r * 0.88f, UiKit.Alpha(UiKit.Telemetry, held ? 0.45f : 0.2f));
                Label(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), "OBJET", _small, UiKit.Alpha(UiKit.Chalk, 0.45f), TextAnchor.MiddleCenter);
                return;
            }
            bool ready = p.ItemRoll <= 0f;
            Disc(c, r, UiKit.Alpha(UiKit.Carbon, 0.7f));
            if (ready) UiKit.Ring(c, r * 0.94f, 4f * _scale, UiKit.Chalk, 40);
            Disc(c, r * 0.84f, UiKit.Alpha(UiKit.Telemetry, held ? 1f : ready ? 0.9f : 0.55f));
            float ir = r * (ready ? 0.72f : 0.6f);
            GUI.DrawTexture(new Rect(c.x - ir, c.y - ir, ir * 2f, ir * 2f), Icons.Item(kind), ScaleMode.ScaleToFit);
        }

        void TouchButton(Vector2 c, float r, string text, Color color, bool lit)
        {
            Disc(c, r, UiKit.Alpha(UiKit.Carbon, 0.55f));
            Disc(c, r * 0.88f, UiKit.Alpha(color, lit ? 0.95f : 0.5f));
            Label(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), text, _small, lit ? UiKit.Carbon : UiKit.Chalk, TextAnchor.MiddleCenter);
        }

        /// <summary>Contre-la-montre : tour en cours, meilleur tour et record à droite.</summary>
        void DrawChrono(float left, float right, float top, float s)
        {
            // En tactile, le bouton pause occupe le coin : le panneau se range à sa gauche.
            if (Race.Input.TouchMode) right = Mathf.Min(right, PauseButton().xMin - 16 * s);
            float lapTime = Race.State == RaceManager.Phase.Racing && !Race.Player.Finished ? Race.RaceTime - Race.LapStartTime : 0f;
            _ui.Plate(new Rect(left - 10 * s, top + 84 * s, 210 * s, 44 * s));
            Label(new Rect(left + 4 * s, top + 86 * s, 80 * s, 40 * s), "TOUR", _small, UiKit.Signal, TextAnchor.MiddleLeft);
            _ui.Digits(new Rect(left + 70 * s, top + 86 * s, 130 * s, 40 * s), FormatTime(lapTime), _medium, UiKit.Chalk, TextAnchor.MiddleLeft);
            float bestLap = Ghost != null ? Ghost.BestLap : 0f;
            float bestTime = Ghost != null ? Ghost.BestTimeBefore : 0f;
            bool ghost = Ghost != null && Ghost.GhostVisible;
            var panel = new Rect(right - 250 * s, top - 6 * s, 250 * s, ghost ? 178 * s : 148 * s);
            UiKit.Card(panel, UiKit.Chalk, 20 * s, 4 * s);
            float px = panel.x + 16 * s, pw = panel.width - 32 * s;
            Label(new Rect(px, top, pw, 30 * s), "MEILLEUR TOUR", _small, UiKit.Red, TextAnchor.MiddleLeft);
            _ui.Digits(new Rect(px, top + 26 * s, pw, 40 * s), bestLap > 0f ? FormatTime(bestLap) : "--:--,--", _medium, UiKit.Carbon, TextAnchor.MiddleRight);
            Label(new Rect(px, top + 70 * s, pw, 30 * s), $"RECORD {Race.Laps} TOUR{(Race.Laps > 1 ? "S" : "")}", _small, UiKit.Red, TextAnchor.MiddleLeft);
            _ui.Digits(new Rect(px, top + 96 * s, pw, 40 * s), bestTime > 0f ? FormatTime(bestTime) : "--:--,--", _medium, UiKit.Carbon, TextAnchor.MiddleRight);
            if (ghost)
                Label(new Rect(px, top + 138 * s, pw, 30 * s), "FANTÔME DU MEILLEUR TOUR", _small, UiKit.Telemetry, TextAnchor.MiddleLeft);
            // Écart avec le meilleur tour aux temps intermédiaires et à la ligne : bleu en avance, rouge en retard.
            if (Ghost != null && Ghost.DeltaAge < 2.5f)
            {
                float d = Ghost.Delta;
                float a = Mathf.Clamp01((2.5f - Ghost.DeltaAge) * 2f);
                var plate = new Rect(left - 10 * s, top + 136 * s, 150 * s, 50 * s);
                UiKit.Card(plate, UiKit.Alpha(d <= 0f ? UiKit.Telemetry : UiKit.Red, a), 14 * s);
                _ui.Digits(plate, RaceText.Gap(d), _medium, UiKit.Alpha(UiKit.Chalk, a), TextAnchor.MiddleCenter);
            }
        }

        /// <summary>Fond des écrans de fin et de pause : voile carbone sur la course.</summary>
        static void Veil(float w, float h, float alpha = 0.72f) => Box(new Rect(0, 0, w, h), UiKit.Alpha(UiKit.Carbon, alpha));

        /// <summary>Grand titre blanc avec un repère oblique rouge dessous.</summary>
        void Title(string text, float y, float w)
        {
            float s = _scale;
            Label(new Rect(0, y, w, 80 * s), text, _big, UiKit.Chalk, TextAnchor.MiddleCenter);
            UiKit.Slash(new Rect(w * 0.5f - 70 * s, y + 78 * s, 140 * s, 8 * s), UiKit.Red, 1.2f);
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
            Veil(w, h);
            var p = Race.Player;
            float t = _resultsAt < 0f ? 0f : Time.time - _resultsAt;
            int rows = Race.Order.Length;
            if (ResultReveal.Celebrate(p.Rank)) DrawConfetti(w, h, s, t - ResultReveal.Done(rows));
            Title(p.Rank == 0 ? "Victoire !" : $"Tu finis {Ordinal(p.Rank + 1)}", 22 * s, w);

            float panelW = 560 * s, x = (w - panelW) * 0.5f, y = 130 * s, row = 38 * s;
            UiKit.Card(new Rect(x, y - 10 * s, panelW, row * Race.Karts.Length + 20 * s), UiKit.Chalk, 24 * s, 5 * s);
            // Les lignes entrent une à une, du dernier au premier ; celle du joueur déborde de la fiche.
            for (int r = 0; r < rows; r++)
            {
                float k01 = ResultReveal.RowProgress(r, rows, t);
                if (k01 <= 0f) continue;
                var k = Race.Karts[Race.Order[r]];
                float yy = y + r * row, xx = x - (1f - k01) * 120 * s;
                var ink = UiKit.Alpha(k.IsPlayer ? UiKit.Chalk : UiKit.Carbon, k01);
                if (k.IsPlayer)
                {
                    UiKit.Card(new Rect(xx - 14 * s, yy - 3 * s, panelW + 28 * s, row + 6 * s), UiKit.Alpha(UiKit.Red, k01), 12 * s, 4 * s);
                    UiKit.Slash(new Rect(xx - 4 * s, yy + 7 * s, 7 * s, row - 14 * s), UiKit.Alpha(UiKit.Signal, k01));
                }
                Label(new Rect(xx + 16 * s, yy, 60 * s, row), Ordinal(r + 1), _medium, ink, TextAnchor.MiddleLeft);
                UiKit.Portrait(new Vector2(xx + 110 * s, yy + row * 0.5f), 16 * s, k.Spec);
                Label(new Rect(xx + 130 * s, yy, 220 * s, row), k.Spec.Name, _medium, ink, TextAnchor.MiddleLeft);
                if (k.Finished) _ui.Digits(new Rect(xx + panelW - 236 * s, yy, 220 * s, row), FormatTime(k.FinishTime), _medium, ink, TextAnchor.MiddleRight);
                else Label(new Rect(xx + panelW - 236 * s, yy, 220 * s, row), "en course…", _ui.Body, ink, TextAnchor.MiddleRight);
            }

            // Temps au tour du joueur, une fois le tableau en place ; le meilleur tour sur fiche jaune.
            float lapsAlpha = Mathf.Clamp01((t - ResultReveal.Done(rows)) / 0.3f);
            if (lapsAlpha > 0f)
            {
                float by = y + row * Race.Karts.Length + 24 * s;
                var laps = Race.LapTimes;
                int best = -1;
                for (int i = 0; i < laps.Length; i++)
                    if (laps[i] > 0f && (best < 0 || laps[i] < laps[best])) best = i;
                float chipW = 170 * s, chipGap = 10 * s, cx0 = w * 0.5f - (laps.Length * chipW + (laps.Length - 1) * chipGap) * 0.5f;
                for (int i = 0; i < laps.Length; i++)
                {
                    var chip = new Rect(cx0 + i * (chipW + chipGap), by, chipW, 34 * s);
                    bool isBest = i == best && laps.Length > 1;
                    UiKit.Card(chip, UiKit.Alpha(isBest ? UiKit.Signal : UiKit.Chalk, lapsAlpha), 10 * s);
                    Label(new Rect(chip.x + 10 * s, chip.y, 40 * s, chip.height), $"T{i + 1}", _small, UiKit.Alpha(UiKit.Red, lapsAlpha), TextAnchor.MiddleLeft);
                    _ui.Digits(new Rect(chip.x, chip.y, chip.width - 10 * s, chip.height), FormatTime(laps[i]), _small, UiKit.Alpha(UiKit.Carbon, lapsAlpha), TextAnchor.MiddleRight);
                }
            }

            DrawResultButtons();
        }

        static readonly Color[] ConfettiColors = { UiKit.Red, UiKit.Signal, UiKit.Telemetry, UiKit.Chalk };

        /// <summary>Confettis du podium en 2D : pluie de petits repères obliques aux couleurs des équipes.</summary>
        void DrawConfetti(float w, float h, float s, float t)
        {
            if (t <= 0f) return;
            for (int i = 0; i < 90; i++)
            {
                // Valeurs pseudo-aléatoires fixes par confetti (position, vitesse, balancement).
                float a = Mathf.Repeat(i * 0.618034f, 1f), b = Mathf.Repeat(i * 0.414214f + 0.3f, 1f);
                float fall = h * (0.18f + 0.22f * b);
                // Chaque confetti part d'au-dessus de l'écran, plus ou moins tard, puis retombe en boucle.
                float travel = t * fall - a * h;
                if (travel < 0f) continue;
                float y = Mathf.Repeat(travel, h * 1.1f) - h * 0.05f;
                float x = a * w + Mathf.Sin(t * (1.5f + b * 2f) + i) * 24f * s;
                float size = (7f + 6f * b) * s;
                UiKit.Slash(new Rect(x, y, size, size * 0.55f), ConfettiColors[i % ConfettiColors.Length], Mathf.Sin(t * 3f + i) * 0.9f);
            }
        }

        void DrawResultButtons()
        {
            var buttons = ResultRects();
            var labels = ResultLabels;
            for (int i = 0; i < buttons.Length; i++)
                _ui.Button(buttons[i], labels[i], i == 0, i == _focus);
        }

        void DrawChronoResults(float w, float s)
        {
            Veil(w, Screen.height);
            var p = Race.Player;
            bool record = Ghost != null && Ghost.NewTimeRecord;
            Title(record ? "Nouveau record !" : "Contre-la-montre", 22 * s, w);

            float panelW = 520 * s, x = (w - panelW) * 0.5f, y = 130 * s, row = 38 * s;
            UiKit.Card(new Rect(x, y - 10 * s, panelW, row * ResultRows + 20 * s), UiKit.Chalk, 24 * s, 5 * s);
            var laps = Race.LapTimes;
            int bestIndex = -1;
            for (int i = 0; i < laps.Length; i++)
                if (laps[i] > 0f && (bestIndex < 0 || laps[i] < laps[bestIndex])) bestIndex = i;
            for (int i = 0; i < laps.Length; i++)
            {
                float yy = y + i * row;
                bool best = i == bestIndex;
                if (best) Box(new Rect(x, yy, panelW, row), UiKit.Red);
                var ink = best ? UiKit.Chalk : UiKit.Carbon;
                Label(new Rect(x + 20 * s, yy, 200 * s, row), $"Tour {i + 1}", _medium, ink, TextAnchor.MiddleLeft);
                _ui.Digits(new Rect(x + panelW - 240 * s, yy, 220 * s, row), FormatTime(laps[i]), _medium, ink, TextAnchor.MiddleRight);
            }
            float ty = y + laps.Length * row;
            Box(new Rect(x + 16 * s, ty + 2 * s, panelW - 32 * s, 2 * s), UiKit.Alpha(UiKit.Carbon, 0.4f));
            Label(new Rect(x + 20 * s, ty, 200 * s, row), "Total", _medium, UiKit.Red, TextAnchor.MiddleLeft);
            _ui.Digits(new Rect(x + panelW - 240 * s, ty, 220 * s, row), FormatTime(p.FinishTime), _medium, UiKit.Red, TextAnchor.MiddleRight);
            float before = Ghost != null ? Ghost.BestTimeBefore : 0f;
            string recordText = before <= 0f ? "Premier temps sur ce circuit" : record ? $"Ancien record {FormatTime(before)}" : $"Record {FormatTime(before)}";
            Label(new Rect(x + 20 * s, ty + row, panelW - 40 * s, row), recordText, _ui.Body, UiKit.Carbon, TextAnchor.MiddleCenter);
            if (Ghost != null) DrawMedals(x, ty + row * 1.8f, panelW, s);
            DrawResultButtons();
        }

        static readonly Color[] MedalColors = { Color.clear, new(0.85f, 0.5f, 0.25f), new(0.8f, 0.85f, 0.92f), new(1f, 0.8f, 0.2f) };

        /// <summary>Médailles du circuit : temps à battre, celle obtenue en couleur pleine.</summary>
        void DrawMedals(float x, float y, float w, float s)
        {
            var got = Ghost.Medal;
            float cw = w / 3f;
            for (int m = 3; m >= 1; m--)
            {
                var medal = (Medal)m;
                float cx = x + (3 - m) * cw + cw * 0.5f;
                bool earned = got >= medal;
                var col = MedalColors[m];
                Disc(new Vector2(cx - 50 * s, y + 18 * s), 14 * s, earned ? UiKit.Carbon : UiKit.Alpha(UiKit.Carbon, 0.25f));
                Disc(new Vector2(cx - 50 * s, y + 18 * s), 11 * s, earned ? col : new Color(col.r, col.g, col.b, 0.25f));
                Label(new Rect(cx - 32 * s, y + 4 * s, cw, 30 * s), $"{Medals.Name(medal)} {Medals.Target(Ghost.Circuit, medal):0.0}",
                    _small, earned ? UiKit.Carbon : UiKit.Alpha(UiKit.Carbon, 0.45f), TextAnchor.MiddleLeft);
            }
            if (got > Ghost.MedalBefore)
                Label(new Rect(x, y + 34 * s, w, 30 * s), $"Médaille {Medals.Name(got).ToLowerInvariant()} gagnée !", _small, UiKit.Red, TextAnchor.MiddleCenter);
        }

        /// <summary>Coupe : place à cette course, points gagnés et classement cumulé.</summary>
        void DrawCupResults(float w, float s)
        {
            Veil(w, Screen.height);
            var st = Cup.Standings;
            int me = Cup.Player;
            Label(new Rect(0, 20 * s, w, 70 * s), $"Tu finis {Ordinal(st.LastPlace(me) + 1)}", _big, UiKit.Chalk, TextAnchor.MiddleCenter);
            Label(new Rect(0, 88 * s, w, 34 * s), $"{Cup.Def.Name} — après la course {Cup.Race + 1}/{Cups.RacesPerCup}", _small, UiKit.Signal, TextAnchor.MiddleCenter);

            float panelW = 600 * s, x = (w - panelW) * 0.5f, y = 130 * s, row = 38 * s;
            UiKit.Card(new Rect(x, y - 10 * s, panelW, row * ResultRows + 20 * s), UiKit.Chalk, 24 * s, 5 * s);
            var ranking = st.Ranking();
            for (int r = 0; r < ranking.Length; r++)
            {
                int id = ranking[r];
                var spec = DriverSpec.Roster[id];
                float yy = y + r * row;
                var ink = id == me ? UiKit.Chalk : UiKit.Carbon;
                if (id == me) Box(new Rect(x, yy, panelW, row), UiKit.Red);
                Label(new Rect(x + 16 * s, yy, 60 * s, row), Ordinal(r + 1), _medium, ink, TextAnchor.MiddleLeft);
                UiKit.Portrait(new Vector2(x + 110 * s, yy + row * 0.5f), 16 * s, spec);
                Label(new Rect(x + 130 * s, yy, 200 * s, row), spec.Name, _medium, ink, TextAnchor.MiddleLeft);
                Label(new Rect(x + 320 * s, yy, 120 * s, row), $"{Ordinal(st.LastPlace(id) + 1)} +{st.LastPoints(id)}", _small,
                    id == me ? UiKit.Chalk : UiKit.Telemetry, TextAnchor.MiddleLeft);
                Label(new Rect(x + panelW - 176 * s, yy, 160 * s, row), $"{st.Total(id)} pts", _medium, ink, TextAnchor.MiddleRight);
            }
            DrawResultButtons();
        }

        void DrawPause(float w, float h, float s)
        {
            Veil(w, h);
            Title("Pause", h * 0.14f, w);
            if (_confirm >= 0)
            {
                DrawConfirm(s);
                return;
            }
            var buttons = PauseRects();
            for (int i = 0; i < buttons.Length; i++)
                _ui.Button(buttons[i], PauseChoices[i], i == 0, i == _focus);
        }

        /// <summary>Confirmation de Recommencer / Menu, qui nomme ce qui sera perdu.</summary>
        void DrawConfirm(float s)
        {
            bool toMenu = _confirm == 3;
            string cup = Cup?.Def.Name;
            var card = ConfirmCard();
            UiKit.Card(card, UiKit.Chalk, 28 * s, 6 * s);
            Label(new Rect(card.x + 24 * s, card.y + 22 * s, card.width - 48 * s, 50 * s), LeaveWarning.Question(toMenu, cup), _medium, UiKit.Carbon,
                TextAnchor.MiddleCenter);
            Label(new Rect(card.x + 24 * s, card.y + 76 * s, card.width - 48 * s, 40 * s),
                LeaveWarning.Loss(toMenu, cup, Cup?.Race ?? 0, Race.Solo), _ui.Body, UiKit.Red, TextAnchor.MiddleCenter);
            var buttons = ConfirmRects();
            _ui.Button(buttons[0], "Annuler", false, _focus == 0);
            _ui.Button(buttons[1], LeaveWarning.Confirm(toMenu), true, _focus == 1);
        }

        static string FormatTime(float t) => RaceText.Time(t);
    }
}
