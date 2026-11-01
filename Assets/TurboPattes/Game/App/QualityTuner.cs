using TurboPattes.Core;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TurboPattes.Game.App
{
    /// <summary>
    /// Qualité adaptative : pendant la course, la fluidité est mesurée par fenêtres de 4 s et <see cref="QualityGovernor"/>
    /// baisse ou remonte d'un cran (échelle de rendu, MSAA, HDR et ombres, post-traitement). Le niveau atteint est mémorisé
    /// pour les parties suivantes. Un niveau imposé dans les réglages (<see cref="GameSession.QualityMode"/>) ou par
    /// <c>-tp-quality N</c> coupe l'adaptation.
    /// </summary>
    public sealed class QualityTuner : MonoBehaviour
    {
        public const int MaxLevel = 4;
        static readonly float[] Scales = { 1f, 0.85f, 0.75f, 0.75f, 0.62f };
        static readonly int[] Msaa = { 4, 2, 1, 1, 1 };

        /// <summary>Mesure active seulement en course (le menu et le compte à rebours ne comptent pas).</summary>
        public bool Measuring;
        /// <summary>Niveau imposé par la ligne de commande (diagnostic) : pas d'adaptation.</summary>
        public static bool Forced;

        public static int Level
        {
            get { try { return Mathf.Clamp(PlayerPrefs.GetInt("tp.quality", 0), 0, MaxLevel); } catch { return 0; } }
            set { PlayerPrefs.SetInt("tp.quality", value); PlayerPrefs.Save(); }
        }

        /// <summary>Niveau appliqué : celui des réglages s'il est imposé, sinon le niveau adaptatif.</summary>
        public static int Effective => !Forced && GameSession.QualityMode >= 0 ? GameSession.QualityMode : Level;

        static bool Adaptive => !Forced && GameSession.QualityMode < 0;

        QualityGovernor _governor;
        float _time;
        int _frames;

        void Start() => Apply(Effective);

        void Update()
        {
            if (!Measuring || Time.timeScale == 0f || !Adaptive) { _time = 0f; _frames = 0; return; }
            _governor ??= new QualityGovernor(Level, MaxLevel);
            _time += Time.unscaledDeltaTime;
            _frames++;
            if (_time < 4f) return;
            float fps = _frames / _time;
            _time = 0f;
            _frames = 0;
            int change = _governor.Feed(fps);
            if (change == 0) return;
            Level = _governor.Level;
            Debug.Log($"[TurboPattes] {fps:0} i/s : qualité {(change > 0 ? "abaissée" : "remontée")} au niveau {Level}");
            Apply(Level);
        }

        public static void Apply(int level)
        {
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
            {
                urp.renderScale = Scales[level];
                urp.msaaSampleCount = Msaa[level];
                urp.supportsHDR = level < 3;
                urp.shadowDistance = level >= 3 ? 0f : 70f;
            }
            var cam = Camera.main;
            if (cam != null && cam.TryGetComponent<UniversalAdditionalCameraData>(out var data))
                data.renderPostProcessing = level < MaxLevel;
        }
    }
}
