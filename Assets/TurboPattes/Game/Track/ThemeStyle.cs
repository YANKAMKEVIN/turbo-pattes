using UnityEngine;

namespace TurboPattes.Game.Track
{
    /// <summary>Ambiance d'un thème : lumière, ciel, brouillard, sol, murets, eau.</summary>
    public sealed class ThemeStyle
    {
        public Color SunColor = new(1f, 0.96f, 0.88f);
        public float SunIntensity = 1.35f;
        public Vector3 SunAngles = new(48f, -35f, 0f);
        public Color AmbientSky = new(0.62f, 0.75f, 0.95f), AmbientEquator = new(0.62f, 0.7f, 0.62f), AmbientGround = new(0.35f, 0.42f, 0.3f);
        public Color Fog = new(0.82f, 0.86f, 0.86f);
        public float FogStart = 160f, FogEnd = 520f;
        /// <summary>Ciel procédural : teinte, sol, exposition, épaisseur d'atmosphère.</summary>
        public Color SkyTint = new(0.52f, 0.52f, 0.5f), SkyGround = new(0.42f, 0.4f, 0.36f);
        public float SkyExposure = 1.3f, SkyAtmosphere = 1f;
        /// <summary>Teinte appliquée à la texture neutre du sol (terrain en relief).</summary>
        public Color GroundTint = Color.white;
        // Murets aux couleurs de l'atelier par défaut : sarcelle et crème, lisse ocre.
        public Color BarrierA = new(0.235f, 0.561f, 0.569f), BarrierB = new(0.949f, 0.906f, 0.808f), BarrierTop = new(0.914f, 0.725f, 0.286f);
        public Color RoadTint = Color.white;
        /// <summary>Sol loin de la piste (sous l'eau pour le lagon), distances du fondu, amplitude des dunes.</summary>
        public float FarLevel, FarFrom = 50f, FarTo = 140f, Dunes = 2.6f;
        /// <summary>Niveau de l'eau (NaN = pas d'eau visible).</summary>
        public float WaterLevel = float.NaN;
        public Color Water = new(0.15f, 0.65f, 0.75f);
        /// <summary>
        /// Rivage (port) : au nord de <see cref="ShoreZ"/>, le sol lointain remonte de <see cref="LandRise"/> m (collines),
        /// au sud il reste au niveau <see cref="FarLevel"/>, sous l'eau. NaN = pas de rivage.
        /// </summary>
        public float ShoreZ = float.NaN, LandRise;

        /// <summary>Surélévation du sol lointain due au rivage, en (x, z).</summary>
        public float Land(float x, float z)
        {
            if (float.IsNaN(ShoreZ)) return 0f;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(ShoreZ - 10f, ShoreZ + 40f, z));
            return t * LandRise * (1f + 0.35f * Mathf.Sin(x * 0.019f) * Mathf.Cos(z * 0.023f));
        }

        public static ThemeStyle For(Theme theme) => theme switch
        {
            Theme.Canyon => new ThemeStyle
            {
                SunColor = new(1f, 0.88f, 0.72f), SunIntensity = 1.45f, SunAngles = new(40f, -60f, 0f),
                AmbientSky = new(0.72f, 0.72f, 0.85f), AmbientEquator = new(0.85f, 0.66f, 0.5f), AmbientGround = new(0.5f, 0.35f, 0.25f),
                Fog = new(0.94f, 0.8f, 0.64f), FogStart = 180f, FogEnd = 580f,
                BarrierA = new(0.85f, 0.35f, 0.2f), BarrierB = new(0.98f, 0.92f, 0.8f), BarrierTop = new(0.2f, 0.75f, 0.75f),
            },
            // 2e passe « Atelier plein gaz » : chaque thème garde sa signature (lumière, brouillard, ciel) mais ses murets
            // et ses accents puisent dans la palette (encre, vermillon, ocre, sarcelle, crème), comme une affiche peinte.
            Theme.Jungle => new ThemeStyle
            {
                SunColor = new(1f, 0.9f, 0.72f), SunIntensity = 1.3f, SunAngles = new(42f, 20f, 0f),
                AmbientSky = new(0.55f, 0.66f, 0.56f), AmbientEquator = new(0.46f, 0.55f, 0.4f), AmbientGround = new(0.26f, 0.3f, 0.2f),
                Fog = new(0.62f, 0.7f, 0.56f), FogStart = 80f, FogEnd = 400f, SkyTint = new(0.5f, 0.58f, 0.46f), SkyGround = new(0.36f, 0.38f, 0.3f),
                GroundTint = new(0.5f, 0.58f, 0.32f), Dunes = 4f,
                BarrierA = new(0.914f, 0.725f, 0.286f), BarrierB = new(0.141f, 0.188f, 0.176f), BarrierTop = new(0.851f, 0.302f, 0.247f),
            },
            Theme.Lagoon => new ThemeStyle
            {
                SunColor = new(1f, 0.94f, 0.82f), SunIntensity = 1.45f, SunAngles = new(44f, 150f, 0f),
                AmbientSky = new(0.66f, 0.76f, 0.82f), AmbientEquator = new(0.74f, 0.76f, 0.7f), AmbientGround = new(0.52f, 0.48f, 0.38f),
                Fog = new(0.8f, 0.85f, 0.84f), FogStart = 200f, FogEnd = 650f, SkyTint = new(0.42f, 0.55f, 0.6f),
                GroundTint = new(1f, 0.9f, 0.72f), FarLevel = -4f, FarFrom = 16f, FarTo = 42f, Dunes = 1.2f, WaterLevel = -0.6f,
                Water = new(0.18f, 0.55f, 0.58f),
                BarrierA = new(0.235f, 0.561f, 0.569f), BarrierB = new(0.949f, 0.906f, 0.808f), BarrierTop = new(0.851f, 0.302f, 0.247f),
            },
            Theme.Reef => new ThemeStyle
            {
                SunColor = new(0.78f, 0.92f, 0.9f), SunIntensity = 1.1f, SunAngles = new(70f, 0f, 0f),
                AmbientSky = new(0.3f, 0.56f, 0.6f), AmbientEquator = new(0.22f, 0.45f, 0.5f), AmbientGround = new(0.14f, 0.26f, 0.3f),
                Fog = new(0.13f, 0.38f, 0.42f), FogStart = 25f, FogEnd = 230f, SkyTint = new(0.12f, 0.38f, 0.45f), SkyGround = new(0.06f, 0.18f, 0.22f),
                SkyExposure = 0.8f, GroundTint = new(0.88f, 0.84f, 0.7f), Dunes = 5f, RoadTint = new(0.88f, 0.95f, 0.95f),
                BarrierA = new(0.851f, 0.302f, 0.247f), BarrierB = new(0.949f, 0.906f, 0.808f), BarrierTop = new(0.914f, 0.725f, 0.286f),
            },
            Theme.Sky => new ThemeStyle
            {
                // Fin d'après-midi : soleil bas et doré sur la mer de nuages.
                SunColor = new(1f, 0.86f, 0.66f), SunIntensity = 1.5f, SunAngles = new(24f, 60f, 0f),
                AmbientSky = new(0.72f, 0.76f, 0.86f), AmbientEquator = new(0.9f, 0.82f, 0.74f), AmbientGround = new(0.82f, 0.76f, 0.7f),
                Fog = new(0.9f, 0.85f, 0.78f), FogStart = 220f, FogEnd = 700f, SkyTint = new(0.44f, 0.5f, 0.62f), SkyGround = new(0.92f, 0.86f, 0.78f),
                SkyAtmosphere = 1.4f, RoadTint = new(0.95f, 0.92f, 0.9f),
                BarrierA = new(0.851f, 0.302f, 0.247f), BarrierB = new(0.949f, 0.906f, 0.808f), BarrierTop = new(0.235f, 0.561f, 0.569f),
            },
            Theme.Space => new ThemeStyle
            {
                SunColor = new(1f, 0.93f, 0.84f), SunIntensity = 1.6f, SunAngles = new(30f, -120f, 0f),
                AmbientSky = new(0.3f, 0.36f, 0.42f), AmbientEquator = new(0.24f, 0.26f, 0.3f), AmbientGround = new(0.1f, 0.12f, 0.14f),
                Fog = new(0.04f, 0.06f, 0.07f), FogStart = 500f, FogEnd = 1400f, SkyTint = new(0f, 0f, 0f), SkyGround = new(0f, 0f, 0f),
                SkyExposure = 0.02f, SkyAtmosphere = 0f, RoadTint = new(0.7f, 0.72f, 0.78f),
                BarrierA = new(0.914f, 0.725f, 0.286f), BarrierB = new(0.141f, 0.188f, 0.176f), BarrierTop = new(0.235f, 0.561f, 0.569f),
            },
            Theme.Ice => new ThemeStyle
            {
                // Lumière rasante et chaude sur la neige, ombres bleutées : affiche de station de ski.
                SunColor = new(1f, 0.9f, 0.76f), SunIntensity = 1.35f, SunAngles = new(22f, -20f, 0f),
                AmbientSky = new(0.62f, 0.72f, 0.86f), AmbientEquator = new(0.74f, 0.76f, 0.8f), AmbientGround = new(0.7f, 0.72f, 0.78f),
                Fog = new(0.86f, 0.87f, 0.88f), FogStart = 120f, FogEnd = 480f, SkyTint = new(0.46f, 0.55f, 0.66f),
                GroundTint = new(0.96f, 0.96f, 0.97f), Dunes = 3f,
                BarrierA = new(0.235f, 0.561f, 0.569f), BarrierB = new(0.949f, 0.906f, 0.808f), BarrierTop = new(0.851f, 0.302f, 0.247f),
            },
            Theme.Volcano => new ThemeStyle
            {
                SunColor = new(1f, 0.72f, 0.5f), SunIntensity = 1.15f, SunAngles = new(25f, 80f, 0f),
                AmbientSky = new(0.5f, 0.36f, 0.32f), AmbientEquator = new(0.46f, 0.3f, 0.22f), AmbientGround = new(0.26f, 0.15f, 0.1f),
                Fog = new(0.38f, 0.22f, 0.17f), FogStart = 70f, FogEnd = 380f, SkyTint = new(0.62f, 0.32f, 0.22f), SkyGround = new(0.16f, 0.09f, 0.07f),
                SkyExposure = 0.7f, SkyAtmosphere = 2.2f, GroundTint = new(0.32f, 0.28f, 0.27f), Dunes = 6f,
                BarrierA = new(0.141f, 0.188f, 0.176f), BarrierB = new(0.914f, 0.725f, 0.286f), BarrierTop = new(0.851f, 0.302f, 0.247f),
            },
            Theme.City => new ThemeStyle
            {
                // Crépuscule : ciel sarcelle profond, fenêtres et lampadaires chauds, enseignes aux couleurs de la palette.
                SunColor = new(1f, 0.7f, 0.5f), SunIntensity = 0.65f, SunAngles = new(12f, 30f, 0f),
                AmbientSky = new(0.3f, 0.4f, 0.46f), AmbientEquator = new(0.42f, 0.34f, 0.32f), AmbientGround = new(0.16f, 0.17f, 0.18f),
                Fog = new(0.16f, 0.2f, 0.22f), FogStart = 120f, FogEnd = 600f, SkyTint = new(0.16f, 0.28f, 0.32f), SkyGround = new(0.08f, 0.09f, 0.1f),
                SkyExposure = 0.35f, GroundTint = new(0.32f, 0.32f, 0.33f), Dunes = 0f,
                BarrierA = new(0.851f, 0.302f, 0.247f), BarrierB = new(0.141f, 0.188f, 0.176f), BarrierTop = new(0.914f, 0.725f, 0.286f),
            },
            Theme.Port => new ThemeStyle
            {
                // Port méditerranéen en fin de matinée : soleil chaud, mer sarcelle, ville crème et ocre sur la colline.
                SunColor = new(1f, 0.92f, 0.78f), SunIntensity = 1.45f, SunAngles = new(40f, -150f, 0f),
                AmbientSky = new(0.66f, 0.74f, 0.8f), AmbientEquator = new(0.78f, 0.74f, 0.66f), AmbientGround = new(0.5f, 0.44f, 0.36f),
                Fog = new(0.86f, 0.84f, 0.78f), FogStart = 180f, FogEnd = 620f, SkyTint = new(0.44f, 0.56f, 0.62f), SkyGround = new(0.6f, 0.56f, 0.5f),
                GroundTint = new(0.74f, 0.74f, 0.56f), FarLevel = -4f, FarFrom = 14f, FarTo = 30f, Dunes = 0.8f, WaterLevel = -0.6f,
                Water = new(0.18f, 0.52f, 0.55f), ShoreZ = 10f, LandRise = 11f,
                BarrierA = new(0.949f, 0.906f, 0.808f), BarrierB = new(0.235f, 0.561f, 0.569f), BarrierTop = new(0.851f, 0.302f, 0.247f),
            },
            _ => new ThemeStyle(),
        };
    }
}
