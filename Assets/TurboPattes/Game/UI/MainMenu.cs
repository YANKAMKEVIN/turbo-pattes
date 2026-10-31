using System;
using TurboPattes.Game.App;
using TurboPattes.Game.Audio;
using TurboPattes.Game.Kart;
using TurboPattes.Game.Track;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Menu de départ, sur fond du vrai circuit : écran titre (caméra qui survole la piste), puis choix du pilote
    /// (kart sur un plateau tournant), du nombre de tours et de la difficulté. Souris, tactile, clavier et manette.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        enum Screen { Title, Pilot }
        // Lignes navigables de l'écran pilote, au clavier ou à la manette.
        enum Row { Pilot, Laps, Difficulty, Start }

        public Action Play;

        static readonly string[] Species =
            { "la renarde", "le panda", "la grenouille", "la lapine", "l'ours", "la chatte", "le pingouin", "la cochonne" };

        readonly UiKit _ui = new();
        Screen _screen;
        Row _row = Row.Start;
        int _pilot, _laps;
        Difficulty _difficulty;

        Camera _cam;
        TrackPath _path;
        SoundBank _sound;
        Transform _stage, _turntable;
        GameObject _kart;
        Vector3 _trackCenter;
        float _trackRadius, _titleAngle, _camBlend, _spin, _pilotChangedAt = -10f;

        public void Setup(TrackPath path, Camera cam, SoundBank sound)
        {
            _path = path;
            _cam = cam;
            _sound = sound;
            _pilot = GameSession.Pilot;
            _laps = Array.IndexOf(GameSession.LapChoices, GameSession.Laps);
            if (_laps < 0) _laps = 1;
            _difficulty = GameSession.Difficulty;

            foreach (var p in path.Points) _trackCenter += p;
            _trackCenter /= path.Points.Length;
            foreach (var p in path.Points) _trackRadius = Mathf.Max(_trackRadius, Vector3.Distance(p, _trackCenter));

            BuildStage();
            ShowPilot(_pilot);
        }

        void BuildStage()
        {
            // Plateau posé sur la piste, juste avant la ligne de départ.
            _path.Sample(_path.Length - 20f, out var pos, out var tangent, out _);
            _stage = new GameObject("MenuStage").transform;
            _stage.SetParent(transform, false);
            _stage.position = pos;
            _stage.rotation = Quaternion.LookRotation(tangent);
            MeshKit.Prim(PrimitiveType.Cylinder, _stage, new Vector3(0f, 0.05f, 0f), new Vector3(5.2f, 0.05f, 5.2f), Mats.Lit(new Color(1f, 0.85f, 0.3f)));
            MeshKit.Prim(PrimitiveType.Cylinder, _stage, new Vector3(0f, 0.08f, 0f), new Vector3(4.6f, 0.05f, 4.6f), Mats.Lit(new Color(0.2f, 0.25f, 0.4f)));
            _turntable = new GameObject("Turntable").transform;
            _turntable.SetParent(_stage, false);
            _turntable.localPosition = new Vector3(0f, 0.13f, 0f);
        }

        void ShowPilot(int index)
        {
            _pilot = (index + AnimalSpec.Roster.Length) % AnimalSpec.Roster.Length;
            if (_kart != null) Destroy(_kart);
            var rig = KartFactory.Build(AnimalSpec.Roster[_pilot], _turntable);
            rig.Root.localPosition = Vector3.zero;
            rig.Root.localRotation = Quaternion.identity;
            _kart = rig.Root.gameObject;
            _pilotChangedAt = Time.time;
        }

        // ------------------------------------------------------------------ disposition (partagée Update / OnGUI)

        struct Layout
        {
            public float S;
            public Rect Safe;
            public Rect PlayButton, Back, Go, Prev, Next;
            public Rect[] Cards, LapButtons, DifficultyButtons;
        }

        Layout ComputeLayout()
        {
            float s = UnityEngine.Screen.height / 720f;
            float h = UnityEngine.Screen.height;
            var sa = UnityEngine.Screen.safeArea;
            var safe = new Rect(sa.xMin + 24 * s, h - sa.yMax + 16 * s, sa.width - 48 * s, sa.height - 32 * s);
            var l = new Layout { S = s, Safe = safe };
            float cx = safe.center.x;
            l.PlayButton = new Rect(cx - 170 * s, safe.y + safe.height * 0.66f, 340 * s, 86 * s);
            l.Back = new Rect(safe.x, safe.y, 150 * s, 54 * s);

            // Cartes des pilotes, en bas.
            int n = AnimalSpec.Roster.Length;
            float cardW = Mathf.Min(118 * s, (safe.width - 360 * s) / n - 10 * s), gap = 10 * s;
            float rowW = n * cardW + (n - 1) * gap;
            float x0 = safe.x + (safe.width - 330 * s - rowW) * 0.5f;
            l.Cards = new Rect[n];
            for (int i = 0; i < n; i++) l.Cards[i] = new Rect(x0 + i * (cardW + gap), safe.yMax - 104 * s, cardW, 104 * s);

            // Flèches de part et d'autre du kart (centre de la zone libre).
            float midX = x0 + rowW * 0.5f, midY = safe.y + safe.height * 0.45f;
            l.Prev = new Rect(midX - 290 * s, midY - 40 * s, 80 * s, 80 * s);
            l.Next = new Rect(midX + 210 * s, midY - 40 * s, 80 * s, 80 * s);

            // Panneau de réglages à droite.
            float px = safe.xMax - 310 * s, pw = 310 * s;
            l.LapButtons = new Rect[3];
            l.DifficultyButtons = new Rect[3];
            float bw = (pw - 2 * 8 * s) / 3f;
            for (int i = 0; i < 3; i++)
            {
                l.LapButtons[i] = new Rect(px + i * (bw + 8 * s), safe.y + 150 * s, bw, 56 * s);
                l.DifficultyButtons[i] = new Rect(px + i * (bw + 8 * s), safe.y + 272 * s, bw, 56 * s);
            }
            l.Go = new Rect(px, safe.yMax - 104 * s, pw, 104 * s);
            return l;
        }

        // ------------------------------------------------------------------ logique

        void Update()
        {
            float dt = Time.deltaTime;
            _sound.SetEngine(0f, false, false);
            HandleInput();
            UpdateCamera(dt);

            // Plateau qui tourne, petit saut quand on change de pilote.
            _spin += dt * 40f;
            _turntable.localRotation = Quaternion.Euler(0f, 200f + _spin, 0f);
            float since = Time.time - _pilotChangedAt;
            _turntable.localPosition = new Vector3(0f, 0.13f + (since < 0.35f ? Mathf.Sin(since / 0.35f * Mathf.PI) * 0.6f : 0f), 0f);
        }

        void UpdateCamera(float dt)
        {
            _camBlend = Mathf.MoveTowards(_camBlend, _screen == Screen.Pilot ? 1f : 0f, dt * 1.4f);
            float t = Mathf.SmoothStep(0f, 1f, _camBlend);

            _titleAngle += dt * 6f;
            var titleRot = Quaternion.Euler(0f, _titleAngle, 0f);
            Vector3 titlePos = _trackCenter + titleRot * new Vector3(0f, _trackRadius * 0.45f, -_trackRadius * 1.05f);
            var titleLook = Quaternion.LookRotation(_trackCenter - titlePos);

            // Vue du pilote : de trois quarts avant, kart décalé vers la gauche de l'écran (réglages à droite).
            Vector3 focus = _stage.position + Vector3.up * 1.1f;
            Vector3 pilotPos = _stage.TransformPoint(new Vector3(-2.2f, 2.4f, 7.2f));
            var pilotLook = Quaternion.LookRotation(focus + _stage.right * -1.6f - pilotPos);

            _cam.transform.SetPositionAndRotation(Vector3.Lerp(titlePos, pilotPos, t), Quaternion.Slerp(titleLook, pilotLook, t));
            _cam.fieldOfView = Mathf.Lerp(55f, 42f, t);
        }

        void HandleInput()
        {
            var l = ComputeLayout();
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            bool confirm = kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame)
                           || pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame);
            bool back = kb != null && (kb.escapeKey.wasPressedThisFrame || kb.backspaceKey.wasPressedThisFrame)
                        || pad != null && pad.buttonEast.wasPressedThisFrame;
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
                if (pad.dpad.left.wasPressedThisFrame || pad.leftShoulder.wasPressedThisFrame) dx--;
                if (pad.dpad.right.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame) dx++;
                if (pad.dpad.up.wasPressedThisFrame) dy--;
                if (pad.dpad.down.wasPressedThisFrame) dy++;
            }
            var tap = UiKit.Tap();

            if (_screen == Screen.Title)
            {
                if (confirm || tap.HasValue && (l.PlayButton.Contains(tap.Value) || tap.Value.y > l.Safe.y + l.Safe.height * 0.2f))
                {
                    _screen = Screen.Pilot;
                    _row = Row.Start;
                    _sound.Pop();
                }
                return;
            }

            if (back || tap.HasValue && l.Back.Contains(tap.Value))
            {
                _screen = Screen.Title;
                _sound.Tick();
                return;
            }
            if (confirm || tap.HasValue && l.Go.Contains(tap.Value))
            {
                StartRace();
                return;
            }

            if (dy != 0) { _row = (Row)Mathf.Clamp((int)_row + dy, 0, 3); _sound.Tick(); }
            if (dx != 0)
            {
                switch (_row)
                {
                    case Row.Pilot: case Row.Start: ChangePilot(_pilot + dx); break;
                    case Row.Laps: _laps = Mathf.Clamp(_laps + dx, 0, 2); _sound.Tick(); break;
                    case Row.Difficulty: _difficulty = (Difficulty)Mathf.Clamp((int)_difficulty + dx, 0, 2); _sound.Tick(); break;
                }
            }

            if (!tap.HasValue) return;
            var p = tap.Value;
            if (l.Prev.Contains(p)) ChangePilot(_pilot - 1);
            else if (l.Next.Contains(p)) ChangePilot(_pilot + 1);
            for (int i = 0; i < l.Cards.Length; i++)
                if (l.Cards[i].Contains(p)) { ChangePilot(i); _row = Row.Pilot; }
            for (int i = 0; i < 3; i++)
            {
                if (l.LapButtons[i].Contains(p)) { _laps = i; _row = Row.Laps; _sound.Tick(); }
                if (l.DifficultyButtons[i].Contains(p)) { _difficulty = (Difficulty)i; _row = Row.Difficulty; _sound.Tick(); }
            }
        }

        /// <summary>Pour le mode -tp-menutest.</summary>
        public void TestGoToPilots() { _screen = Screen.Pilot; _row = Row.Start; }
        public void TestSelect(int pilot, int laps, Difficulty difficulty) { ChangePilot(pilot); _laps = laps; _difficulty = difficulty; }
        public void TestStart() => StartRace();

        void ChangePilot(int index)
        {
            int before = _pilot;
            ShowPilot(index);
            if (_pilot != before) _sound.Pop();
        }

        void StartRace()
        {
            GameSession.Pilot = _pilot;
            GameSession.Laps = GameSession.LapChoices[_laps];
            GameSession.Difficulty = _difficulty;
            _sound.Go();
            Play?.Invoke();
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ dessin

        void OnGUI()
        {
            _ui.EnsureStyles();
            var l = ComputeLayout();
            if (_screen == Screen.Title && _camBlend < 0.5f) DrawTitle(l);
            else if (_screen == Screen.Pilot && _camBlend > 0.5f) DrawPilot(l);
        }

        void DrawTitle(Layout l)
        {
            float s = l.S, w = UnityEngine.Screen.width, h = UnityEngine.Screen.height;
            float a = 1f - _camBlend * 2f;
            UiKit.Box(new Rect(0, 0, w, h), new Color(0.05f, 0.1f, 0.25f, 0.25f * a));

            // Logo qui se balance doucement.
            var pivot = new Vector2(w * 0.5f, l.Safe.y + l.Safe.height * 0.3f);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Sin(Time.time * 1.3f) * 2.5f, pivot);
            var logo = new GUIStyle(_ui.Huge) { fontSize = Mathf.RoundToInt(120 * s) };
            _ui.Label(new Rect(0, pivot.y - 130 * s, w, 140 * s), "TURBO", logo, new Color(1f, 0.55f, 0.15f, a), TextAnchor.MiddleCenter, 6f);
            _ui.Label(new Rect(0, pivot.y - 10 * s, w, 140 * s), "PATTES", logo, new Color(1f, 0.85f, 0.2f, a), TextAnchor.MiddleCenter, 6f);
            GUI.matrix = m;
            _ui.Label(new Rect(0, pivot.y + 120 * s, w, 40 * s), "Courses de karts à poils et à plumes", _ui.Medium,
                new Color(1f, 1f, 1f, 0.95f * a), TextAnchor.MiddleCenter);

            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.03f;
            var b = l.PlayButton;
            var pb = new Rect(b.center.x - b.width * pulse * 0.5f, b.center.y - b.height * pulse * 0.5f, b.width * pulse, b.height * pulse);
            _ui.Button(pb, "Jouer", new Color(1f, 0.5f, 0.15f, a), true, _ui.Big);
            _ui.Label(new Rect(0, b.yMax + 18 * s, w, 30 * s), "Touche l'écran, Entrée ou A", _ui.Small,
                new Color(1f, 1f, 1f, 0.75f * a), TextAnchor.MiddleCenter);
        }

        void DrawPilot(Layout l)
        {
            float s = l.S;
            var spec = AnimalSpec.Roster[_pilot];
            var gold = new Color(1f, 0.85f, 0.25f);

            _ui.Button(l.Back, "< Retour", new Color(0.2f, 0.25f, 0.4f, 0.85f), false, _ui.Small);
            _ui.Label(new Rect(l.Back.xMax + 20 * s, l.Safe.y, 600 * s, 54 * s), "Choisis ton pilote", _ui.Medium, Color.white, TextAnchor.MiddleLeft);

            // Nom du pilote au-dessus du kart.
            float midX = (l.Prev.center.x + l.Next.center.x) * 0.5f;
            var nameStyle = new GUIStyle(_ui.Big) { fontSize = Mathf.RoundToInt(58 * s) };
            _ui.Label(new Rect(midX - 300 * s, l.Safe.y + 66 * s, 600 * s, 70 * s), spec.Name, nameStyle, Color.white, TextAnchor.MiddleCenter, 4f);
            _ui.Label(new Rect(midX - 300 * s, l.Safe.y + 130 * s, 600 * s, 34 * s), Species[_pilot], _ui.Medium, gold, TextAnchor.MiddleCenter);

            bool pilotRow = _row == Row.Pilot;
            _ui.Button(l.Prev, "<", new Color(0.2f, 0.25f, 0.4f, 0.75f), pilotRow, _ui.Big);
            _ui.Button(l.Next, ">", new Color(0.2f, 0.25f, 0.4f, 0.75f), pilotRow, _ui.Big);

            for (int i = 0; i < l.Cards.Length; i++)
            {
                var r = l.Cards[i];
                var a = AnimalSpec.Roster[i];
                bool sel = i == _pilot;
                if (sel) UiKit.Box(new Rect(r.x - 5 * s, r.y - 5 * s, r.width + 10 * s, r.height + 10 * s), pilotRow ? Color.white : gold, 18 * s);
                UiKit.Box(r, sel ? new Color(0.16f, 0.2f, 0.36f, 0.95f) : new Color(0.1f, 0.12f, 0.22f, 0.7f), 14 * s);
                float rad = Mathf.Min(r.width * 0.3f, 30 * s);
                var c = new Vector2(r.center.x, r.y + 12 * s + rad);
                UiKit.Disc(c, rad + 3 * s, a.KartTrim);
                UiKit.Disc(c, rad, a.Fur);
                _ui.Label(new Rect(c.x - rad, c.y - rad, rad * 2f, rad * 2f), a.Name.Substring(0, 1), _ui.Medium,
                    a.Fur.grayscale > 0.6f ? new Color(0.15f, 0.15f, 0.2f) : Color.white, TextAnchor.MiddleCenter, 0f);
                _ui.Label(new Rect(r.x, r.yMax - 32 * s, r.width, 26 * s), a.Name, _ui.Small, sel ? gold : Color.white, TextAnchor.MiddleCenter);
            }

            // Réglages.
            var panel = new Rect(l.LapButtons[0].x - 16 * s, l.Safe.y + 90 * s, l.Go.width + 32 * s, 260 * s);
            UiKit.Box(panel, new Color(0.08f, 0.1f, 0.2f, 0.6f), 18 * s);
            _ui.Label(new Rect(l.LapButtons[0].x, l.LapButtons[0].y - 42 * s, 300 * s, 36 * s), "Tours", _ui.Medium, Color.white);
            _ui.Label(new Rect(l.DifficultyButtons[0].x, l.DifficultyButtons[0].y - 42 * s, 300 * s, 36 * s), "Difficulté", _ui.Medium, Color.white);
            for (int i = 0; i < 3; i++)
            {
                Segment(l.LapButtons[i], GameSession.LapChoices[i].ToString(), i == _laps, _row == Row.Laps && i == _laps);
                Segment(l.DifficultyButtons[i], GameSession.DifficultyNames[i], i == (int)_difficulty, _row == Row.Difficulty && i == (int)_difficulty);
            }

            float pulse = Mathf.Sin(Time.time * 4f) * 0.04f;
            var go = l.Go;
            _ui.Button(new Rect(go.x - go.width * pulse * 0.5f, go.y - go.height * pulse * 0.5f, go.width * (1f + pulse), go.height * (1f + pulse)),
                "C'est parti !", new Color(1f, 0.5f, 0.15f), _row == Row.Start, _ui.Medium);
        }

        void Segment(Rect r, string text, bool selected, bool focused)
        {
            _ui.Button(r, text, selected ? new Color(0.25f, 0.65f, 1f) : new Color(0.25f, 0.3f, 0.45f, 0.8f), focused, _ui.Small);
        }
    }
}
