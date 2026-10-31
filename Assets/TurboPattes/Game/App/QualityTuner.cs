using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TurboPattes.Game.App
{
    /// <summary>
    /// Qualité adaptative : pendant la course, si la moyenne descend sous 45 i/s, on baisse d'un cran
    /// (échelle de rendu, MSAA, HDR et ombres, post-traitement). Le niveau atteint est mémorisé pour les parties suivantes.
    /// <c>-tp-quality N</c> impose un niveau (diagnostic).
    /// </summary>
    public sealed class QualityTuner : MonoBehaviour
    {
        public const int MaxLevel = 4;
        static readonly float[] Scales = { 1f, 0.85f, 0.75f, 0.75f, 0.62f };
        static readonly int[] Msaa = { 4, 2, 1, 1, 1 };

        /// <summary>Mesure active seulement en course (le menu et le compte à rebours ne comptent pas).</summary>
        public bool Measuring;
        public static int Level
        {
            get { try { return Mathf.Clamp(PlayerPrefs.GetInt("tp.quality", 0), 0, MaxLevel); } catch { return 0; } }
            set { PlayerPrefs.SetInt("tp.quality", value); PlayerPrefs.Save(); }
        }

        float _time;
        int _frames;

        void Start() => Apply(Level);

        void Update()
        {
            if (!Measuring || Time.timeScale == 0f) { _time = 0f; _frames = 0; return; }
            _time += Time.unscaledDeltaTime;
            _frames++;
            if (_time < 4f) return;
            float fps = _frames / _time;
            _time = 0f;
            _frames = 0;
            if (fps >= 45f || Level >= MaxLevel) return;
            Level++;
            Debug.Log($"[TurboPattes] {fps:0} i/s : qualité abaissée au niveau {Level}");
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
