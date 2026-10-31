using System;
using System.Collections;
using System.IO;
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
    /// et journal dans le dossier, puis fermeture. <c>-tp-laps N</c> change le nombre de tours.
    /// </summary>
    public sealed class KartApp : MonoBehaviour
    {
        public const int PlayerSlot = 5;

        string _autotestDir, _menuTestDir;
        bool _diag;
        int _lapsOverride;
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
            }

            _diag = Array.IndexOf(args, "-tp-diag") >= 0;
            BuildWorld();
            if (_autotestDir != null || !GameSession.ShowMenu) StartRace();
            else ShowMenu();
        }

        void ShowMenu()
        {
            var menu = new GameObject("Menu").AddComponent<UI.MainMenu>();
            menu.Setup(_path, _chase.GetComponent<Camera>(), _sound);
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
            menu.TestSelect(4, 0, Difficulty.Hard);
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
                $"pilote={_race.Player.Spec.Name} tours={_race.Laps} karts={string.Join(",", Array.ConvertAll(_race.Karts, k => k.Spec.Name))}\n");
            Application.Quit();
        }

        void StartRace()
        {
            // « Rejouer » relance directement la course ; seul le bouton Menu remet ce drapeau.
            GameSession.ShowMenu = false;
            bool auto = _autotestDir != null;
            int laps = _lapsOverride > 0 ? _lapsOverride : auto ? 3 : GameSession.Laps;
            int pilot = auto ? 0 : GameSession.Pilot;
            var difficulty = auto ? Difficulty.Normal : GameSession.Difficulty;
            int seed = auto ? 1234 : Environment.TickCount;

            _race = new GameObject("Race").AddComponent<RaceManager>();
            _race.Autopilot = auto;
            _race.Setup(_circuit, _path, _pads, _chase, _sound, PlayerSlot, laps, seed, pilot, GameSession.AiSpeedRange(difficulty));
            var hud = new GameObject("Hud").AddComponent<Hud>();
            hud.Race = _race;
            if (auto) StartCoroutine(Autotest());
            if (_diag) StartCoroutine(Diagnostics());
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
                Debug.Log($"[TPDiag] t={Time.timeSinceLevelLoad:0} touch={_race.Input.TouchMode} gaz={input.Throttle:0.0} frein={input.Brake:0.0} volant={input.Steer:0.0} " +
                          $"drift={input.Drift} vF={p.ForwardSpeed:0.0} lat={p.Lateral:0.0} horsPiste={p.Offroad} controles={p.ControlsEnabled} spin={p.SpinTime:0.0} etat={_race.State}");
                yield return new WaitForSeconds(1f);
            }
        }

        void BuildWorld()
        {
            var world = new GameObject("World").transform;

            // Lumière du soleil
            var sun = new GameObject("Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.96f, 0.88f);
            sun.intensity = 1.35f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            var sky = Mats.Sky();
            if (sky != null) RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.75f, 0.95f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.7f, 0.62f);
            RenderSettings.ambientGroundColor = new Color(0.35f, 0.42f, 0.3f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.84f, 0.97f);
            RenderSettings.fogStartDistance = 160f;
            RenderSettings.fogEndDistance = 520f;
            RenderSettings.sun = sun;

            // Caméra
            var camGo = new GameObject("Camera");
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.2f;
            cam.farClipPlane = 900f;
            cam.clearFlags = sky != null ? CameraClearFlags.Skybox : CameraClearFlags.SolidColor;
            cam.backgroundColor = RenderSettings.fogColor;
            camGo.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            var data = camGo.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            _chase = camGo.AddComponent<ChaseCamera>();
            BuildPostProcessing(world);

            _circuit = Circuit.Meadow();
            _path = new TrackPath(_circuit.Control, 40, _circuit.HalfWidth, _circuit.CurbWidth, _circuit.BarrierOffset);
            _pads = TrackBuilder.Build(_circuit, _path, world, 42);
            _sound = SoundBank.Create(world);
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

        IEnumerator Autotest()
        {
            Directory.CreateDirectory(_autotestDir);
            var log = new StreamWriter(Path.Combine(_autotestDir, "autotest.log")) { AutoFlush = true };
            log.WriteLine($"Turbo Pattes autotest {DateTime.Now:s} {Screen.width}x{Screen.height}");
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
                    ScreenCapture.CaptureScreenshot(Path.Combine(_autotestDir, $"shot_{next:00}_{t:000}s.png"));
                    next++;
                }
                if (t - lastLog >= 5f)
                {
                    lastLog = t;
                    var p = _race.Player;
                    log.WriteLine($"t={t:0} etat={_race.State} tour={p.Laps.Lap} place={p.Rank + 1} vitesse={p.Speed * 3.6f:0}km/h " +
                                  $"lat={p.Lateral:0.0} objet={p.Item} drift={p.Drift.Level} fps={1f / Mathf.Max(1e-4f, Time.smoothDeltaTime):0}");
                    foreach (var k in _race.Karts)
                        if (!k.IsPlayer && Mathf.Abs(k.Lateral) > _race.Path.BarrierOffset) log.WriteLine($"  ANOMALIE {k.Spec.Name} hors barrière");
                }
                if (_race.State == RaceManager.Phase.Results) break;
                yield return null;
            }
            yield return new WaitForSeconds(1.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(_autotestDir, "shot_99_results.png"));
            yield return null;
            log.WriteLine($"Fin t={Time.time - start:0}s etat={_race.State}");
            for (int r = 0; r < _race.Order.Length; r++)
            {
                var k = _race.Karts[_race.Order[r]];
                log.WriteLine($"  {r + 1}. {k.Spec.Name}{(k.IsPlayer ? " (joueur)" : "")} {(k.Finished ? k.FinishTime.ToString("0.0") + "s" : "en course, tour " + k.Laps.Lap)}");
            }
            log.WriteLine("Tours joueur : " + string.Join(", ", Array.ConvertAll(_race.LapTimes, x => x.ToString("0.0"))));
            log.Close();
            yield return new WaitForSeconds(0.5f);
            Application.Quit();
        }
    }
}
