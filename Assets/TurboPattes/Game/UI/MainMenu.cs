using System;
using TurboPattes.Core;
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
    /// (kart sur un plateau tournant), du circuit, du nombre de tours et de la difficulté. Changer de circuit
    /// reconstruit la piste derrière le menu (<see cref="CircuitChanged"/>). Souris, tactile, clavier et manette.
    /// </summary>
    public sealed class MainMenu : MonoBehaviour
    {
        enum Screen { Title, Pilot }
        // Lignes navigables de l'écran pilote, au clavier ou à la manette.
        enum Row { Pilot, Mode, Circuit, Laps, Difficulty, Start }

        public Action Play;
        /// <summary>Le joueur a choisi un autre circuit : l'application reconstruit la piste puis appelle <see cref="SetTrack"/>.</summary>
        public Action<int> CircuitChanged;

        static readonly string[] Species =
            { "la renarde", "le panda", "la grenouille", "la lapine", "l'ours", "la chatte", "le pingouin", "la cochonne" };

        readonly UiKit _ui = new();
        Screen _screen;
        Row _row = Row.Start;
        int _pilot, _laps, _circuit, _cup, _shownTrack;
        string[] _circuitNames;
        Difficulty _difficulty;
        GameMode _mode;

        Camera _cam;
        TrackPath _path;
        SoundBank _sound;
        Transform _stage, _turntable;
        GameObject _kart;
        Vector3 _trackCenter;
        float _trackRadius, _titleAngle, _camBlend, _spin, _pilotChangedAt = -10f;

        public void Setup(TrackPath path, Camera cam, SoundBank sound, int circuit)
        {
            _cam = cam;
            _sound = sound;
            _pilot = GameSession.Pilot;
            _laps = Array.IndexOf(GameSession.LapChoices, GameSession.Laps);
            if (_laps < 0) _laps = 1;
            _difficulty = GameSession.Difficulty;
            _circuit = circuit;
            _shownTrack = circuit;
            _mode = GameSession.Mode;
            _cup = GameSession.CupChoice;
            _circuitNames = Array.ConvertAll(Circuit.All(), c => c.ShortName);

            BuildStage();
            SetTrack(path);
            ShowPilot(_pilot);
            if (_mode == GameMode.Cup) ShowTrack(Cups.All[_cup].Circuits[0]);
        }

        /// <summary>Recadre le survol et repose le plateau sur la (nouvelle) piste.</summary>
        public void SetTrack(TrackPath path)
        {
            _path = path;
            _trackCenter = Vector3.zero;
            _trackRadius = 0f;
            foreach (var p in path.Points) _trackCenter += p;
            _trackCenter /= path.Points.Length;
            foreach (var p in path.Points) _trackRadius = Mathf.Max(_trackRadius, Vector3.Distance(p, _trackCenter));
            // Plateau posé sur la piste, juste avant la ligne de départ.
            _path.Sample(_path.Length - 20f, out var pos, out var tangent, out _);
            _stage.position = pos;
            _stage.rotation = Quaternion.LookRotation(tangent);
        }

        void BuildStage()
        {
            _stage = new GameObject("MenuStage").transform;
            _stage.SetParent(transform, false);
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
            public Rect PlayButton, Back, Go, Prev, Next, Settings;
            public Rect[] Cards, ModeButtons, CircuitButtons, LapButtons, DifficultyButtons;
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
            l.Settings = new Rect(safe.x, safe.yMax - 60 * s, 190 * s, 56 * s);

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
            // Sélecteur de circuit : flèche précédente, nom (touche = suivant), flèche suivante.
            float aw = 56 * s, bh = 52 * s;
            l.CircuitButtons = new[]
            {
                new Rect(px, safe.y + 196 * s, aw, bh),
                new Rect(px + aw + 8 * s, safe.y + 196 * s, pw - 2 * (aw + 8 * s), bh),
                new Rect(px + pw - aw, safe.y + 196 * s, aw, bh),
            };
            l.ModeButtons = new Rect[3];
            l.LapButtons = new Rect[3];
            l.DifficultyButtons = new Rect[3];
            float bw = (pw - 2 * 8 * s) / 3f;
            for (int i = 0; i < 3; i++)
            {
                l.ModeButtons[i] = new Rect(px + i * (bw + 8 * s), safe.y + 106 * s, bw, bh);
                l.LapButtons[i] = new Rect(px + i * (bw + 8 * s), safe.y + 318 * s, bw, bh);
                l.DifficultyButtons[i] = new Rect(px + i * (bw + 8 * s), safe.y + 408 * s, bw, bh);
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
            if (SettingsScreen.IsOpen) return;
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
                // Réglages : bouton en bas à gauche, Tab ou Y à la manette.
                if (kb != null && kb.tabKey.wasPressedThisFrame || pad != null && pad.buttonNorth.wasPressedThisFrame
                    || tap.HasValue && l.Settings.Contains(tap.Value))
                {
                    Jukebox.Instance.Confirm();
                    SettingsScreen.Open();
                    return;
                }
                if (confirm || tap.HasValue && (l.PlayButton.Contains(tap.Value) || tap.Value.y > l.Safe.y + l.Safe.height * 0.2f))
                {
                    _screen = Screen.Pilot;
                    _row = Row.Start;
                    Jukebox.Instance.Confirm();
                }
                return;
            }

            if (back || tap.HasValue && l.Back.Contains(tap.Value))
            {
                _screen = Screen.Title;
                Jukebox.Instance.Back();
                return;
            }
            if (confirm || tap.HasValue && l.Go.Contains(tap.Value))
            {
                // Coupe encore fermée : il faut d'abord monter sur le podium de la précédente.
                if (_mode == GameMode.Cup && !GameSession.CupUnlocked(_cup)) { Jukebox.Instance.Back(); return; }
                StartRace();
                return;
            }

            if (dy != 0)
            {
                _row = (Row)Mathf.Clamp((int)_row + dy, 0, (int)Row.Start);
                // Pas de difficulté en contre-la-montre : la ligne est sautée.
                if (_row == Row.Difficulty && _mode == GameMode.TimeTrial) _row += dy;
                Jukebox.Instance.Move();
            }
            if (dx != 0)
            {
                switch (_row)
                {
                    case Row.Pilot: case Row.Start: ChangePilot(_pilot + dx); break;
                    case Row.Mode: ChangeMode((GameMode)Mathf.Clamp((int)_mode + dx, 0, 2)); break;
                    case Row.Circuit: if (_mode == GameMode.Cup) ChangeCup(_cup + dx); else ChangeCircuit(_circuit + dx); break;
                    case Row.Laps: _laps = Mathf.Clamp(_laps + dx, 0, 2); Jukebox.Instance.Move(); break;
                    case Row.Difficulty: _difficulty = (Difficulty)Mathf.Clamp((int)_difficulty + dx, 0, 2); Jukebox.Instance.Move(); break;
                }
            }

            if (!tap.HasValue) return;
            var p = tap.Value;
            if (l.Prev.Contains(p)) ChangePilot(_pilot - 1);
            else if (l.Next.Contains(p)) ChangePilot(_pilot + 1);
            for (int i = 0; i < l.Cards.Length; i++)
                if (l.Cards[i].Contains(p)) { ChangePilot(i); _row = Row.Pilot; }
            int step = l.CircuitButtons[0].Contains(p) ? -1 : l.CircuitButtons[1].Contains(p) || l.CircuitButtons[2].Contains(p) ? 1 : 0;
            if (step != 0)
            {
                if (_mode == GameMode.Cup) ChangeCup(_cup + step);
                else ChangeCircuit(_circuit + step);
                _row = Row.Circuit;
            }
            for (int i = 0; i < 3; i++)
            {
                if (l.ModeButtons[i].Contains(p)) { ChangeMode((GameMode)i); _row = Row.Mode; }
                if (l.LapButtons[i].Contains(p)) { _laps = i; _row = Row.Laps; Jukebox.Instance.Move(); }
                if (l.DifficultyButtons[i].Contains(p) && _mode != GameMode.TimeTrial) { _difficulty = (Difficulty)i; _row = Row.Difficulty; Jukebox.Instance.Move(); }
            }
        }

        /// <summary>Pour le mode -tp-menutest.</summary>
        public void TestGoToPilots() { _screen = Screen.Pilot; _row = Row.Start; }
        public void TestSelect(int pilot, int laps, Difficulty difficulty, int circuit)
        {
            ChangePilot(pilot);
            _laps = laps;
            _difficulty = difficulty;
            ChangeCircuit(circuit);
            _row = Row.Circuit;
        }
        public void TestStart() => StartRace();
        public void TestMode(GameMode mode) { ChangeMode(mode); _row = Row.Mode; }

        void ChangePilot(int index)
        {
            int before = _pilot;
            ShowPilot(index);
            if (_pilot != before) _sound.Pop();
        }

        void ChangeCircuit(int index)
        {
            index = (index % GameSession.CircuitCount + GameSession.CircuitCount) % GameSession.CircuitCount;
            if (index == _circuit) return;
            _circuit = index;
            Jukebox.Instance.Move();
            ShowTrack(index);
        }

        void ChangeCup(int index)
        {
            index = (index % Cups.All.Length + Cups.All.Length) % Cups.All.Length;
            if (index == _cup) return;
            _cup = index;
            Jukebox.Instance.Move();
            ShowTrack(Cups.All[index].Circuits[0]);
        }

        void ChangeMode(GameMode mode)
        {
            if (mode == _mode) return;
            _mode = mode;
            Jukebox.Instance.Move();
            // En coupe, la piste derrière le menu est la première de la coupe.
            ShowTrack(mode == GameMode.Cup ? Cups.All[_cup].Circuits[0] : _circuit);
        }

        /// <summary>Reconstruit la piste derrière le menu si elle change.</summary>
        void ShowTrack(int index)
        {
            if (index == _shownTrack) return;
            _shownTrack = index;
            CircuitChanged?.Invoke(index);
        }

        void StartRace()
        {
            GameSession.Pilot = _pilot;
            GameSession.Circuit = _circuit;
            GameSession.Laps = GameSession.LapChoices[_laps];
            GameSession.Difficulty = _difficulty;
            GameSession.Mode = _mode;
            GameSession.CupChoice = _cup;
            GameSession.Cup = _mode == GameMode.Cup ? new CupRun { CupIndex = _cup, Player = _pilot } : null;
            Jukebox.Instance.Confirm();
            Play?.Invoke();
            Destroy(gameObject);
        }

        // ------------------------------------------------------------------ dessin

        void OnGUI()
        {
            _ui.EnsureStyles();
            if (SettingsScreen.IsOpen) return;
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
            _ui.Button(l.Settings, "Réglages", new Color(0.2f, 0.25f, 0.4f, 0.85f * a), false, _ui.Small);
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
            DrawStats(new Vector2(midX, l.Safe.y + 180 * s), s, Drivers.For(_pilot));

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
                UiKit.Portrait(c, rad, a, 3 * s);
                _ui.Label(new Rect(r.x, r.yMax - 32 * s, r.width, 26 * s), a.Name, _ui.Small, sel ? gold : Color.white, TextAnchor.MiddleCenter);
            }

            // Réglages.
            var panel = new Rect(l.LapButtons[0].x - 16 * s, l.Safe.y + 60 * s, l.Go.width + 32 * s, 416 * s);
            UiKit.Box(panel, new Color(0.08f, 0.1f, 0.2f, 0.6f), 18 * s);
            Caption(l.ModeButtons[0], "Mode");
            bool cup = _mode == GameMode.Cup, chrono = _mode == GameMode.TimeTrial;
            Caption(l.CircuitButtons[0], cup ? "Coupe" : "Circuit");
            bool circuitRow = _row == Row.Circuit;
            Segment(l.CircuitButtons[0], "<", false, circuitRow);
            Segment(l.CircuitButtons[1], cup ? $"{Cups.All[_cup].Name.Replace("Coupe ", "")}  {_cup + 1}/{Cups.All.Length}"
                : $"{_circuitNames[_circuit]}  {_circuit + 1}/{_circuitNames.Length}", true, circuitRow);
            Segment(l.CircuitButtons[2], ">", false, circuitRow);
            // Sous le sélecteur : record en contre-la-montre, circuits de la coupe.
            string info = cup ? CupText() : chrono ? RecordText() : null;
            if (info != null)
                _ui.Label(new Rect(l.CircuitButtons[0].x, l.CircuitButtons[0].yMax + 2 * s, l.Go.width, 26 * s), info, _ui.Small,
                    new Color(1f, 0.85f, 0.25f), TextAnchor.MiddleCenter);
            Caption(l.LapButtons[0], "Tours");
            Caption(l.DifficultyButtons[0], "Difficulté", chrono ? 0.35f : 1f);
            for (int i = 0; i < 3; i++)
            {
                Segment(l.ModeButtons[i], GameSession.ModeNames[i], i == (int)_mode, _row == Row.Mode && i == (int)_mode);
                Segment(l.LapButtons[i], GameSession.LapChoices[i].ToString(), i == _laps, _row == Row.Laps && i == _laps);
                if (chrono) _ui.Button(l.DifficultyButtons[i], GameSession.DifficultyNames[i], new Color(0.25f, 0.3f, 0.45f, 0.3f), false, _ui.Small);
                else Segment(l.DifficultyButtons[i], GameSession.DifficultyNames[i], i == (int)_difficulty, _row == Row.Difficulty && i == (int)_difficulty);
            }

            float pulse = Mathf.Sin(Time.time * 4f) * 0.04f;
            var go = l.Go;
            _ui.Button(new Rect(go.x - go.width * pulse * 0.5f, go.y - go.height * pulse * 0.5f, go.width * (1f + pulse), go.height * (1f + pulse)),
                _mode == GameMode.Cup && !GameSession.CupUnlocked(_cup) ? "Coupe fermée" : "C'est parti !",
                _mode == GameMode.Cup && !GameSession.CupUnlocked(_cup) ? new Color(0.35f, 0.35f, 0.4f) : new Color(1f, 0.5f, 0.15f), _row == Row.Start, _ui.Medium);
        }

        void Caption(Rect under, string text, float alpha = 1f) =>
            _ui.Label(new Rect(under.x, under.y - 36 * _ui.Scale, 300 * _ui.Scale, 34 * _ui.Scale), text, _ui.Medium, new Color(1f, 1f, 1f, alpha));

        static readonly string[] TrophyNames = { "", "bronze", "argent", "or" };

        string CupText()
        {
            if (!GameSession.CupUnlocked(_cup)) return "Podium de la coupe d'avant requis";
            string circuits = string.Join(", ", Array.ConvertAll(Cups.All[_cup].Circuits, c => _circuitNames[c]));
            int trophy = GameSession.CupTrophy(_cup);
            return trophy > 0 ? $"Trophée {TrophyNames[trophy]} — {circuits}" : circuits;
        }

        string RecordText()
        {
            int laps = GameSession.LapChoices[_laps];
            float best = GameSession.BestTime(_circuit, laps), lap = GameSession.BestLap(_circuit);
            if (best <= 0f && lap <= 0f) return "Pas encore de record";
            string Fmt(float t) => t <= 0f ? "--" : $"{(int)(t / 60f)}:{t % 60f:00.00}";
            var medal = Medals.For(_circuit, lap);
            return $"Record {Fmt(best)}   Tour {Fmt(lap)}" + (medal != Medal.None ? $"   {Medals.Name(medal)}" : "");
        }

        static readonly string[] StatNames = { "Vitesse", "Accélération", "Adhérence", "Poids" };

        /// <summary>Caractère du pilote : quatre jauges de 5 crans, côte à côte.</summary>
        void DrawStats(Vector2 top, float s, DriverStats st)
        {
            int[] values = { st.Speed, st.Accel, st.Grip, st.Weight };
            float cw = 150 * s, x0 = top.x - cw * 2f;
            UiKit.Box(new Rect(x0 - 10 * s, top.y - 4 * s, cw * 4f + 20 * s, 60 * s), new Color(0.06f, 0.08f, 0.18f, 0.7f), 14 * s);
            for (int i = 0; i < 4; i++)
            {
                float x = x0 + i * cw;
                _ui.Label(new Rect(x, top.y, cw, 24 * s), StatNames[i], _ui.Small, Color.white, TextAnchor.MiddleCenter);
                for (int n = 0; n < 5; n++)
                    UiKit.Box(new Rect(x + 17 * s + n * 24 * s, top.y + 30 * s, 20 * s, 14 * s),
                        n < values[i] ? new Color(1f, 0.6f, 0.15f) : new Color(1f, 1f, 1f, 0.22f), 4 * s);
            }
        }

        void Segment(Rect r, string text, bool selected, bool focused)
        {
            _ui.Button(r, text, selected ? new Color(0.25f, 0.65f, 1f) : new Color(0.25f, 0.3f, 0.45f, 0.8f), focused, _ui.Small);
        }
    }
}
