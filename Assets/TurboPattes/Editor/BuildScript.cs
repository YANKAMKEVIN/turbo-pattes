using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TurboPattes.Game.Visuals;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TurboPattes.EditorTools
{
    /// <summary>
    /// Configuration du projet et builds en ligne de commande :
    ///   Unity -batchmode -quit -projectPath . -buildTarget Android -executeMethod TurboPattes.EditorTools.BuildScript.BuildAndroid
    ///   Unity -batchmode -quit -projectPath . -buildTarget OSXUniversal -executeMethod TurboPattes.EditorTools.BuildScript.BuildMac
    /// </summary>
    public static class BuildScript
    {
        const string ScenePath = "Assets/Scenes/Main.unity";
        const string ResourcesDir = "Assets/TurboPattes/Resources";
        const string BundleId = "com.kevinyankam.turbopattes";

        [MenuItem("Turbo Pattes/Configurer le projet")]
        public static void Setup()
        {
            ConfigureInputHandling();
            CreateMaterialTemplates();
            EnsureScene();
            ConfigurePlayer();
            ConfigureRendering();
            AssetDatabase.SaveAssets();
            Debug.Log("[TurboPattes] Projet configuré");
        }

        [MenuItem("Turbo Pattes/Build Android (APK)")]
        public static void BuildAndroid()
        {
            if (!RequireActiveTarget(BuildTarget.Android, "Android")) return;
            Setup();
            EditorUserBuildSettings.buildAppBundle = false;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.enableFrameTimingStats = true; // temps CPU/GPU dans le diagnostic -tp-diag
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            Build(BuildTarget.Android, "Builds/Android/TurboPattes.apk");
        }

        [MenuItem("Turbo Pattes/Build macOS")]
        public static void BuildMac()
        {
            if (!RequireActiveTarget(BuildTarget.StandaloneOSX, "OSXUniversal")) return;
            Setup();
            // Seul le module Mac Mono est installé (pas IL2CPP).
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            Build(BuildTarget.StandaloneOSX, "Builds/macOS/TurboPattes.app");
        }

        /// <summary>
        /// En ligne de commande, l'éditeur doit démarrer sur la bonne cible (<c>-buildTarget Android</c>) : si le build
        /// change de cible en cours de route, URP filtre les shaders avec les réglages de l'ancienne plateforme et
        /// retire des passes entières (monde invisible sur Android après un build Mac).
        /// </summary>
        static bool RequireActiveTarget(BuildTarget target, string argName)
        {
            if (!Application.isBatchMode || EditorUserBuildSettings.activeBuildTarget == target) return true;
            Debug.LogError($"[TurboPattes] Relancer avec -buildTarget {argName} (cible active : {EditorUserBuildSettings.activeBuildTarget}).");
            EditorApplication.Exit(3);
            return false;
        }

        static void Build(BuildTarget target, string output)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = output,
                target = target,
                options = BuildOptions.None,
            });
            var s = report.summary;
            Debug.Log($"[TurboPattes] Build {target} : {s.result}, {s.totalErrors} erreur(s), {s.totalSize / (1024 * 1024)} Mo -> {output}");
            if (Application.isBatchMode && s.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>
        /// Gabarits de matériaux dans Resources : ils embarquent les shaders URP et leurs mots-clés dans le build
        /// (les matériaux du jeu sont ensuite clonés depuis eux, voir <see cref="Mats"/>).
        /// </summary>
        static void CreateMaterialTemplates()
        {
            Directory.CreateDirectory(ResourcesDir);
            AssetDatabase.Refresh();
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            var particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var sky = Shader.Find("Skybox/Procedural");

            SaveMaterial(Mats.LitTemplate, lit, m =>
            {
                m.SetFloat("_Smoothness", 0.25f);
                m.enableInstancing = true;
            });
            SaveMaterial(Mats.EmissiveTemplate, lit, m =>
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", Color.white);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            });
            SaveMaterial(Mats.TransparentTemplate, lit, m =>
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
                m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
                m.SetFloat("_ZWrite", 0f);
                m.SetFloat("_Smoothness", 0.9f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
                m.SetShaderPassEnabled("DepthOnly", false);
                m.SetShaderPassEnabled("ShadowCaster", false);
            });
            SaveMaterial(Mats.ParticleTemplate, particles, m =>
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 2f); // additif
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                m.SetFloat("_DstBlend", (float)BlendMode.One);
                m.SetFloat("_ZWrite", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            });
            SaveMaterial(Mats.SkyTemplate, sky, m =>
            {
                m.SetFloat("_SunSize", 0.035f);
                m.SetFloat("_AtmosphereThickness", 0.75f);
                m.SetColor("_SkyTint", new Color(0.45f, 0.62f, 0.95f));
                m.SetColor("_GroundColor", new Color(0.55f, 0.7f, 0.5f));
                m.SetFloat("_Exposure", 1.35f);
            });
        }

        static void SaveMaterial(string name, Shader shader, Action<Material> configure)
        {
            if (shader == null)
            {
                Debug.LogError($"[TurboPattes] Shader introuvable pour {name}");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }
            string path = $"{ResourcesDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            bool created = m == null;
            if (created) m = new Material(shader) { name = name };
            else m.shader = shader;
            configure(m);
            if (created) AssetDatabase.CreateAsset(m, path);
            else EditorUtility.SetDirty(m);
        }

        /// <summary>La scène ne contient que KartApp (qui construit tout) ; le brouillard est activé pour garder ses variantes de shader.</summary>
        static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("TurboPattes").AddComponent<Game.App.KartApp>();
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>($"{ResourcesDir}/{Mats.SkyTemplate}.mat");
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = "Kevin Yankam";
            PlayerSettings.productName = "Turbo Pattes";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, BundleId);
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;
            PlayerSettings.defaultScreenWidth = 1600;
            PlayerSettings.defaultScreenHeight = 900;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.SplashScreen.backgroundColor = new Color(0.45f, 0.7f, 1f);
        }

        /// <summary>Active Input Handling = Input System Package (prend effet au prochain lancement de l'éditeur).</summary>
        static void ConfigureInputHandling()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets == null || assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            var prop = so.FindProperty("activeInputHandler");
            if (prop == null || prop.intValue == 1) return;
            prop.intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();
            Debug.LogWarning("[TurboPattes] Active Input Handling passé à Input System : relancer l'éditeur avant de builder.");
        }

        static void ConfigureRendering()
        {
            var assets = new List<UniversalRenderPipelineAsset>();
            if (GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset main) assets.Add(main);
            for (int i = 0; i < QualitySettings.names.Length; i++)
                if (QualitySettings.GetRenderPipelineAssetAt(i) is UniversalRenderPipelineAsset q && !assets.Contains(q)) assets.Add(q);
            if (assets.Count == 0)
            {
                Debug.LogError("[TurboPattes] Aucun asset URP.");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }
            foreach (var a in assets)
            {
                a.supportsHDR = true;
                a.msaaSampleCount = 4;
                a.shadowDistance = 70f;
                a.renderScale = 1f;
                var so = new SerializedObject(a);
                var soft = so.FindProperty("m_SoftShadowsSupported");
                if (soft != null) soft.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(a);
            }
        }
    }
}
