using System;
using System.Collections;
using System.IO;
using TurboPattes.Core;
using TurboPattes.Game.Audio;
using TurboPattes.Game.Race;
using TurboPattes.Game.Track;
using TurboPattes.Game.UI;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TurboPattes.Game.App
{
    /// <summary>
    /// Point d'entrée : la scène ne contient que cet objet, tout le reste est construit au lancement.
    /// Argument <c>-tp-autotest &lt;dossier&gt;</c> : le joueur est conduit par l'IA, captures d'écran
    /// et journal dans le dossier, puis fermeture. <c>-tp-laps N</c> change le nombre de tours,
    /// <c>-tp-circuit N</c> choisit le circuit (1 = Prairie, 2 = Canyon…). <c>-tp-mode chrono</c> : contre-la-montre
    /// (avec <c>-tp-fresh</c>, records et fantôme du circuit effacés d'abord) ; <c>-tp-mode coupe -tp-cup N</c> : la coupe N
    /// entière, une course après l'autre (scène rechargée), jusqu'au podium.
    /// </summary>
    public sealed class KartApp : MonoBehaviour
    {
        public const int PlayerSlot = 5;

        string _autotestDir, _menuTestDir;
        bool _diag;
        int _lapsOverride;
        int _circuitOverride = -1;
        int _circuitIndex;
        GameMode _mode;
        GameMode? _modeArg;
        int _cupArg;
        bool _fresh;
        string _shotDir;
        GhostRunner _ghost;
        Transform _world, _trackRoot;
        Light _sun;
        QualityTuner _tuner;
        Material _skyTemplate;
        RaceManager _race;
        Circuit _circuit;
        TrackPath _path;
        System.Collections.Generic.List<BoostPad> _pads;
        ChaseCamera _chase;
        SoundBank _sound;

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            QualitySettings.vSyncCount = 0;

            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-tp-autotest") _autotestDir = args[i + 1];
                if (args[i] == "-tp-menutest") _menuTestDir = args[i + 1];
                if (args[i] == "-tp-laps" && int.TryParse(args[i + 1], out int l)) _lapsOverride = Mathf.Clamp(l, 1, 9);
                if (args[i] == "-tp-quality" && int.TryParse(args[i + 1], out int q)) QualityTuner.Level = Mathf.Clamp(q, 0, QualityTuner.MaxLevel);
                if (args[i] == "-tp-circuit" && int.TryParse(args[i + 1], out int c)) _circuitOverride = Mathf.Clamp(c - 1, 0, GameSession.CircuitCount - 1);
                if (args[i] == "-tp-mode") _modeArg = args[i + 1] == "chrono" ? GameMode.TimeTrial : args[i + 1] == "coupe" ? GameMode.Cup : GameMode.Race;
                if (args[i] == "-tp-cup" && int.TryParse(args[i + 1], out int cup)) _cupArg = Mathf.Clamp(cup - 1, 0, Cups.All.Length - 1);
            }
            _fresh = Array.IndexOf(args, "-tp-fresh") >= 0;

            _diag = Array.IndexOf(args, "-tp-diag") >= 0;
            // Sur Android, un dossier relatif va dans le stockage de l'application.
            if (_autotestDir != null && !Path.IsPathRooted(_autotestDir)) _autotestDir = Path.Combine(Application.persistentDataPath, _autotestDir);
            if (_circuitOverride >= 0 && _autotestDir == null) GameSession.Circuit = _circuitOverride;
            _circuitIndex = _circuitOverride >= 0 ? _circuitOverride : _autotestDir != null ? 0 : GameSession.Circuit;

            // Mode : celui de l'autotest (course par défaut), sinon celui du menu (Rejouer, course suivante de la coupe).
            bool auto = _autotestDir != null;
            _mode = auto ? _modeArg ?? GameMode.Race : GameSession.Mode;
            bool direct = auto || !GameSession.ShowMenu;
            if (auto && _mode == GameMode.Cup && GameSession.Cup == null) GameSession.Cup = new CupRun { CupIndex = _cupArg, Player = 0 };
            if (!direct || _mode != GameMode.Cup) GameSession.Cup = null;
            var cupRun = GameSession.Cup;
            if (cupRun != null) _circuitIndex = cupRun.CircuitIndex;

            BuildWorld();
            if (cupRun != null && cupRun.Over) ShowPodium(cupRun);
            else if (direct) StartRace();
            else ShowMenu();
        }

        void Update()
        {
            if (_tuner != null) _tuner.Measuring = _race != null && _race.State == RaceManager.Phase.Racing && !_race.Paused;
        }

        void ShowMenu()
        {
            var menu = new GameObject("Menu").AddComponent<UI.MainMenu>();
            // Changer de circuit (ou de coupe) dans le menu reconstruit la piste derrière lui.
            menu.CircuitChanged = index =>
            {
                LoadCircuit(index);
                menu.SetTrack(_path);
            };
            menu.Setup(_path, _chase.GetComponent<Camera>(), _sound, _circuitIndex);
            Jukebox.Instance.Play(Songs.Menu, 0.45f);
            menu.Play = StartRace;
            if (_menuTestDir != null) StartCoroutine(MenuTest(menu));
        }

        /// <summary><c>-tp-menutest &lt;dossier&gt;</c> : parcourt menu, choix du pilote, course et pause en prenant des captures.</summary>
        IEnumerator MenuTest(UI.MainMenu menu)
        {
            Directory.CreateDirectory(_menuTestDir);
            string Shot(string name) => Path.Combine(_menuTestDir, name + ".png");
            yield return new WaitForSeconds(2.5f);
            ScreenCapture.CaptureScreenshot(Shot("1_titre"));
            yield return new WaitForSeconds(0.5f);
            menu.TestGoToPilots();
            yield return new WaitForSeconds(2.5f);
            ScreenCapture.CaptureScreenshot(Shot("2_pilote"));
            yield return new WaitForSeconds(0.3f);
            // Modes : contre-la-montre (record affiché, difficulté grisée) puis coupe (piste de la coupe derrière le menu).
            menu.TestMode(GameMode.TimeTrial);
            yield return new WaitForSeconds(1f);
            ScreenCapture.CaptureScreenshot(Shot("2b_chrono"));
            yield return new WaitForSeconds(0.3f);
            menu.TestMode(GameMode.Cup);
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(Shot("2c_coupe"));
            string cupTrack = _circuit.Name;
            yield return new WaitForSeconds(0.3f);
            menu.TestMode(GameMode.Race);
            yield return new WaitForSeconds(0.5f);
            menu.TestSelect(4, 0, Difficulty.Hard, 1);
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(Shot("3_bruno"));
            yield return new WaitForSeconds(0.3f);
            menu.TestStart();
            yield return new WaitForSeconds(7f);
            // Objet en main pour voir son icône sur la capture.
            _race.Player.Item = Core.ItemKind.Honey;
            _race.Player.ItemRoll = 0f;
            yield return null;
            ScreenCapture.CaptureScreenshot(Shot("4_course"));
            yield return new WaitForSeconds(0.3f);
            _race.SetPaused(true);
            yield return null;
            yield return null;
            ScreenCapture.CaptureScreenshot(Shot("5_pause"));
            yield return new WaitForSecondsRealtime(0.5f);
            File.WriteAllText(Path.Combine(_menuTestDir, "menutest.log"),
                $"pilote={_race.Player.Spec.Name} tours={_race.Laps} circuit={_race.Circuit.Name} (memorise={GameSession.Circuit}) " +
                $"mode={GameSession.Mode} musique={_race.Music.Name} pisteCoupe={cupTrack} " +
                $"karts={string.Join(",", Array.ConvertAll(_race.Karts, k => k.Spec.Name))}\n");
            Application.Quit();
        }

        void StartRace()
        {
            // « Rejouer » relance directement la course ; seul le bouton Menu remet ce drapeau.
            GameSession.ShowMenu = false;
            // Silence pendant le compte à rebours ; la musique de course part au « PARTEZ ! ».
            Jukebox.Instance.Play(null);
            bool auto = _autotestDir != null;
            if (!auto) _mode = GameSession.Mode;
            var cup = _mode == GameMode.Cup ? GameSession.Cup : null;
            if (cup == null && _mode == GameMode.Cup) _mode = GameMode.Race;
            bool solo = _mode == GameMode.TimeTrial;
            int laps = _lapsOverride > 0 ? _lapsOverride : auto ? 3 : GameSession.Laps;
            int pilot = auto ? 0 : cup?.Player ?? GameSession.Pilot;
            var difficulty = auto ? Difficulty.Normal : GameSession.Difficulty;
            int seed = auto ? 1234 + (cup?.Race ?? 0) : Environment.TickCount;
            if (solo && auto && _fresh) GameSession.ClearRecords(_circuitIndex);

            _race = new GameObject("Race").AddComponent<RaceManager>();
            _race.Autopilot = auto;
            _race.Solo = solo;
            // Une musique par circuit, calculée pendant le compte à rebours.
            _race.Music = Songs.ForCircuit(_circuitIndex);
            Jukebox.Instance.Prepare(_race.Music);
            _race.Setup(_circuit, _path, _pads, _chase, _sound, PlayerSlot, laps, seed, pilot, GameSession.AiSpeedRange(difficulty));
            if (solo)
            {
                _ghost = new GameObject("Ghost").AddComponent<GhostRunner>();
                _ghost.Setup(_race, _circuitIndex);
            }
            if (cup != null)
                _race.RaceFinished += () => cup.Record(Array.ConvertAll(_race.Order, i => RaceManager.SpecIndex(_race.Karts[i])));
            var hud = new GameObject("Hud").AddComponent<Hud>();
            hud.Race = _race;
            hud.Ghost = _ghost;
            hud.Cup = cup;
            if (auto) StartCoroutine(Autotest());
            if (_diag) StartCoroutine(Diagnostics());
        }

        void ShowPodium(CupRun cup)
        {
            var podium = new GameObject("Podium").AddComponent<Podium>();
            podium.Setup(cup, _path, _chase.GetComponent<Camera>());
            if (_autotestDir != null) StartCoroutine(PodiumTest(cup));
        }

        /// <summary>Fin de la coupe en autotest : capture du podium, classement final dans podium.log, fermeture.</summary>
        IEnumerator PodiumTest(CupRun cup)
        {
            Directory.CreateDirectory(_autotestDir);
            yield return new WaitForSeconds(3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(_autotestDir, "podium.png"));
            yield return new WaitForSeconds(0.5f);
            var ranking = cup.Standings.Ranking();
            File.WriteAllText(Path.Combine(_autotestDir, "podium.log"),
                $"{cup.Def.Name} courses={cup.Standings.Races}\n" +
                string.Join("\n", Array.ConvertAll(ranking, id => $"  {Array.IndexOf(ranking, id) + 1}. {Kart.AnimalSpec.Roster[id].Name} {cup.Standings.Total(id)} pts")) + "\n");
            yield return new WaitForSeconds(0.3f);
            Application.Quit();
        }

        /// <summary><c>-tp-diag</c> : état du rendu dans le journal (logcat sur Android).</summary>
        IEnumerator Diagnostics()
        {
            yield return new WaitForSeconds(5f);
            Debug.Log($"[TPDiag] API={SystemInfo.graphicsDeviceType} GPU={SystemInfo.graphicsDeviceName} quality={QualitySettings.names[QualitySettings.GetQualityLevel()]} " +
                      $"pipeline={GraphicsSettings.currentRenderPipeline?.name}");
            var shaders = new System.Collections.Generic.HashSet<Shader>();
            int renderers = 0, visible = 0, noMat = 0, noMesh = 0;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                renderers++;
                if (r.isVisible) visible++;
                if (r.sharedMaterial == null) noMat++;
                else shaders.Add(r.sharedMaterial.shader);
                if (r is MeshRenderer && r.GetComponent<MeshFilter>()?.sharedMesh == null) noMesh++;
            }
            foreach (var sh in shaders) Debug.Log($"[TPDiag] shader {sh.name} supported={sh.isSupported} passes={sh.passCount}");
            var cam = Camera.main;
            Debug.Log($"[TPDiag] renderers={renderers} visibles={visible} sansMateriau={noMat} sansMaillage={noMesh} " +
                      $"camera={cam.transform.position} joueur={_race.Player.transform.position}");
            Debug.Log("[TPDiag] devices: " + string.Join(", ", System.Linq.Enumerable.Select(UnityEngine.InputSystem.InputSystem.devices, d => $"{d.GetType().Name}:{d.displayName}")));
            for (int i = 0; i < 40; i++)
            {
                var p = _race.Player;
                var input = _race.LastPlayerInput;
                FrameTimingManager.CaptureFrameTimings();
                var timings = new FrameTiming[1];
                if (FrameTimingManager.GetLatestTimings(1, timings) > 0)
                    Debug.Log($"[TPDiag] cpu={timings[0].cpuFrameTime:0.0}ms (principal {timings[0].cpuMainThreadFrameTime:0.0}, rendu {timings[0].cpuRenderThreadFrameTime:0.0}) gpu={timings[0].gpuFrameTime:0.0}ms " +
                              $"echelle={QualityTuner.Level}");
                Debug.Log($"[TPDiag] t={Time.timeSinceLevelLoad:0} touch={_race.Input.TouchMode} gaz={input.Throttle:0.0} frein={input.Brake:0.0} volant={input.Steer:0.0} " +
                          $"drift={input.Drift} vF={p.ForwardSpeed:0.0} lat={p.Lateral:0.0} horsPiste={p.Offroad} controles={p.ControlsEnabled} spin={p.SpinTime:0.0} etat={_race.State}");
                yield return new WaitForSeconds(1f);
            }
        }

        void BuildWorld()
        {
            var world = _world = new GameObject("World").transform;

            // Lumière du soleil (couleur et angle selon le circuit, voir ApplyTheme)
            var sun = _sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;

            var sky = Mats.Sky();
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.sun = sun;

            // Caméra
            var camGo = new GameObject("Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 900f;
            cam.clearFlags = sky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            _chase = camGo.AddComponent<ChaseCamera>();
            _tuner = gameObject.AddComponent<QualityTuner>();
            BuildPostProcessing(world);

            _sound = SoundBank.Create(world);
            LoadCircuit(_circuitIndex);
        }

        /// <summary>Construit (ou reconstruit, depuis le menu) la piste et son décor.</summary>
        void LoadCircuit(int index)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            bool rebuild = _trackRoot != null;
            if (rebuild) Destroy(_trackRoot.gameObject);
            _circuitIndex = index;
            _circuit = Circuit.All()[index];
            _path = new TrackPath(_circuit);
            _trackRoot = new GameObject("Track " + _circuit.Name).transform;
            _trackRoot.SetParent(_world, false);
            _pads = TrackBuilder.Build(_circuit, _path, _trackRoot, 42);
            ApplyTheme(_circuit.Theme);
            if (rebuild) Resources.UnloadUnusedAssets();
            Debug.Log($"[TurboPattes] Circuit {_circuit.Name} construit en {watch.ElapsedMilliseconds} ms");
        }

        void ApplyTheme(Theme theme)
        {
            var st = ThemeStyle.For(theme);
            _sun.color = st.SunColor;
            _sun.intensity = st.SunIntensity;
            _sun.transform.rotation = Quaternion.Euler(st.SunAngles);
            RenderSettings.ambientSkyColor = st.AmbientSky;
            RenderSettings.ambientEquatorColor = st.AmbientEquator;
            RenderSettings.ambientGroundColor = st.AmbientGround;
            RenderSettings.fogColor = st.Fog;
            RenderSettings.fogStartDistance = st.FogStart;
            RenderSettings.fogEndDistance = st.FogEnd;
            var cam = _chase.GetComponent<Camera>();
            cam.backgroundColor = st.Fog;
            cam.farClipPlane = theme == Theme.Space ? 1500f : 900f;
            // Ciel procédural réglé par thème (copie du gabarit, qui reste intact).
            if (_skyTemplate == null) _skyTemplate = RenderSettings.skybox;
            if (_skyTemplate != null)
            {
                var sky = new Material(_skyTemplate);
                void Set(string prop, float v) { if (sky.HasProperty(prop)) sky.SetFloat(prop, v); }
                if (sky.HasProperty("_SkyTint")) sky.SetColor("_SkyTint", st.SkyTint);
                if (sky.HasProperty("_GroundColor")) sky.SetColor("_GroundColor", st.SkyGround);
                Set("_Exposure", st.SkyExposure);
                Set("_AtmosphereThickness", st.SkyAtmosphere);
                RenderSettings.skybox = sky;
            }
        }

        static void BuildPostProcessing(Transform parent)
        {
            var go = new GameObject("PostProcess");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.05f);
            bloom.intensity.Override(0.5f);
            bloom.scatter.Override(0.6f);
            var color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(12f);
            color.contrast.Override(6f);
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
            volume.sharedProfile = profile;
        }

        /// <summary>Un tronçon de pont passe-t-il juste au-dessus de <paramref name="p"/> ?</summary>
        bool UnderBridge(Vector3 p)
        {
            for (int i = 0; i < _path.Count; i++)
            {
                if (!_path.Elevated[i] || _path.BaseHeights[i] < p.y + 5f) continue;
                float dx = _path.Points[i].x - p.x, dz = _path.Points[i].z - p.z;
                if (dx * dx + dz * dz < 36f) return true;
            }
            return false;
        }

        /// <summary>Vues fixes du pont et du tremplin (caméra de poursuite suspendue le temps de deux captures).</summary>
        IEnumerator ReliefViews()
        {
            var cam = _chase.transform;
            _chase.enabled = false;
            if (_path.Bridges.Count > 0)
            {
                var bridge = _path.Bridges[0];
                float mid = _path.Wrap(bridge.from + _path.Wrap(bridge.to - bridge.from) * 0.5f);
                Vector3 deck = _path.PointAt(mid);
                _path.Sample(mid, out _, out var bt, out var br);
                cam.position = deck + br * 45f - bt * 30f + Vector3.up * 2f;
                cam.rotation = Quaternion.LookRotation(deck + Vector3.down * 4f - cam.position);
                yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, "vue_pont.png"));
                yield return null;
            }
            if (_path.Profile.Kickers.Count > 0)
            {
                var kicker = _path.Profile.Kickers[0];
                _path.Sample(kicker.LipS, out var lip, out var kt, out var kr);
                cam.position = lip - kt * 22f + kr * 16f + Vector3.up * 6f;
                cam.rotation = Quaternion.LookRotation(lip - cam.position);
                yield return null;
                ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, "vue_tremplin.png"));
                yield return null;
            }
            // Vue d'ensemble, de haut.
            Vector3 centre = Vector3.zero;
            foreach (var p in _path.Points) centre += p;
            centre /= _path.Count;
            cam.position = centre + new Vector3(0f, 170f, -260f);
            cam.rotation = Quaternion.LookRotation(centre - cam.position);
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, "vue_ensemble.png"));
            yield return null;
            _chase.enabled = true;
        }

        IEnumerator Autotest()
        {
            var cup = GameSession.Cup;
            // En coupe, un dossier par course.
            _shotDir = cup != null ? Path.Combine(_autotestDir, $"course{cup.Race + 1}") : _autotestDir;
            Directory.CreateDirectory(_shotDir);
            var log = new StreamWriter(Path.Combine(_shotDir, "autotest.log")) { AutoFlush = true };
            log.WriteLine($"Turbo Pattes autotest {DateTime.Now:s} {Screen.width}x{Screen.height} circuit={_circuit.Name} " +
                          $"longueur={_path.Length:0}m relief={_path.HasRelief} mode={_mode} musique={_race.Music.Name} karts={_race.Karts.Length}" +
                          (cup != null ? $" coupe={cup.Def.Name} course={cup.Race + 1}" : "") +
                          (_ghost != null ? $" fantome={_ghost.GhostSamples} meilleurTour={_ghost.BestLapBefore:0.00}" : ""));
            bool shotGhost = false;
            float ghostSeenAt = -1f, ghostDistSum = 0f;
            int ghostFrames = 0;
            bool shotJump = false, shotBridge = false, shotUnder = false;
            float maxY = 0f;
            if (_path.HasRelief && cup == null && _ghost == null) yield return ReliefViews();
            float start = Time.time;
            float[] shots = { 1.5f, 4.2f, 9f, 16f, 28f, 45f, 70f };
            int next = 0;
            float lastLog = 0f;
            float deadline = 60f * 6f;
            while (Time.time - start < deadline)
            {
                float t = Time.time - start;
                if (next < shots.Length && t >= shots[next])
                {
                    ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, $"shot_{next:00}_{t:000}s.png"));
                    next++;
                }
                var pl = _race.Player;
                maxY = Mathf.Max(maxY, pl.Position.y);
                // Captures du relief : premier saut, passage sur le pont, passage dessous.
                if (_path.HasRelief && _race.State == RaceManager.Phase.Racing)
                {
                    string extra = null;
                    if (!shotJump && pl.Airborne && pl.Vertical.AirTime > 0.25f) { shotJump = true; extra = "saut"; }
                    else if (!shotBridge && _path.OnBridge(pl.Laps.S) && pl.Position.y > 5f) { shotBridge = true; extra = "pont"; }
                    else if (!shotUnder && UnderBridge(pl.Position)) { shotUnder = true; extra = "dessous"; }
                    if (extra != null)
                    {
                        ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, $"shot_{extra}_{t:000}s.png"));
                        log.WriteLine($"  capture {extra} t={t:0.0} y={pl.Position.y:0.0} s={pl.Laps.S:0}");
                    }
                }
                // Fantôme : distance au joueur, capture quand il roule depuis 2 s.
                if (_ghost != null && _ghost.GhostVisible)
                {
                    if (ghostSeenAt < 0f)
                    {
                        ghostSeenAt = t;
                        log.WriteLine($"  fantome visible t={t:0.0} tour={pl.Laps.Lap}");
                    }
                    ghostDistSum += Vector3.Distance(_ghost.GhostPosition, pl.Position);
                    ghostFrames++;
                    // Capture quand le fantôme roule tout près (moins de 12 m) : bien visible à l'écran.
                    float gap = Vector3.Distance(_ghost.GhostPosition, pl.Position);
                    if (!shotGhost && t - ghostSeenAt > 2f && gap < 12f)
                    {
                        shotGhost = true;
                        ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, $"shot_fantome_{t:000}s.png"));
                        log.WriteLine($"  capture fantome t={t:0.0} ecart={Vector3.Distance(_ghost.GhostPosition, pl.Position):0.0}m");
                    }
                }
                foreach (var k in _race.Karts)
                {
                    float ground = _path.GroundHeight(k.Laps.S, k.Lateral);
                    if (k.Position.y < ground - 0.5f || float.IsNaN(k.Position.y))
                        log.WriteLine($"  ANOMALIE {k.Spec.Name} sous la piste y={k.Position.y:0.0} sol={ground:0.0} s={k.Laps.S:0}");
                }
                if (t - lastLog >= 5f)
                {
                    lastLog = t;
                    var p = _race.Player;
                    log.WriteLine($"t={t:0} etat={_race.State} tour={p.Laps.Lap} place={p.Rank + 1} vitesse={p.Speed * 3.6f:0}km/h " +
                                  $"lat={p.Lateral:0.0} y={p.Position.y:0.0}{(p.Airborne ? " (en l'air)" : "")} s={p.Laps.S:0} objet={p.Item} drift={p.Drift.Level} " +
                                  $"fps={1f / Mathf.Max(1e-4f, Time.smoothDeltaTime):0}");
                    foreach (var k in _race.Karts)
                        if (!k.IsPlayer && Mathf.Abs(k.Lateral) > _race.Path.BarrierOffset) log.WriteLine($"  ANOMALIE {k.Spec.Name} hors barrière");
                }
                if (_race.State == RaceManager.Phase.Results) break;
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(_shotDir, "shot_99_results.png"));
            yield return null;
            log.WriteLine($"Fin t={Time.time - start:0}s etat={_race.State}");
            for (int r = 0; r < _race.Order.Length; r++)
            {
                var k = _race.Karts[_race.Order[r]];
                log.WriteLine($"  {r + 1}. {k.Spec.Name}{(k.IsPlayer ? " (joueur)" : "")} {(k.Finished ? k.FinishTime.ToString("0.0") + "s" : "en course, tour " + k.Laps.Lap)}");
            }
            log.WriteLine("Tours joueur : " + string.Join(", ", Array.ConvertAll(_race.LapTimes, x => x.ToString("0.0"))));
            log.WriteLine($"Relief : hauteur max joueur {maxY:0.0} m ; sauts " +
                          string.Join(", ", Array.ConvertAll(_race.Karts, k => $"{k.Spec.Name} {k.Jumps} (plus long {k.LongestJump:0.00}s)")));
            if (_ghost != null)
                log.WriteLine($"Chrono : record tour {GameSession.BestLap(_circuitIndex):0.00}s (avant {_ghost.BestLapBefore:0.00}) nouveauTour={_ghost.NewLapRecord} " +
                              $"record total {GameSession.BestTime(_circuitIndex, _race.Laps):0.00}s nouveauTotal={_ghost.NewTimeRecord} " +
                              $"fantome images={ghostFrames} ecartMoyen={(ghostFrames > 0 ? ghostDistSum / ghostFrames : 0f):0.0}m points={GameSession.LoadGhost(_circuitIndex)?.Count ?? 0}");
            if (cup != null)
            {
                var st = cup.Standings;
                log.WriteLine($"Coupe {cup.Def.Name} apres course {cup.Race + 1} : " +
                              string.Join(", ", Array.ConvertAll(st.Ranking(), id => $"{Kart.AnimalSpec.Roster[id].Name} {st.Total(id)} (+{st.LastPoints(id)})")));
                log.Close();
                // Course suivante (ou podium) : la scène se recharge, l'autotest reprend dans le nouveau KartApp.
                cup.Next();
                GameSession.ShowMenu = false;
                yield return new WaitForSeconds(0.3f);
                _race.Restart();
                yield break;
            }
            log.Close();
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }
}
