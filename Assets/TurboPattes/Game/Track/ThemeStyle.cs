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

        public static ThemeStyle For(Theme theme) => theme switch
        {
            Theme.Canyon => new ThemeStyle
            {
                SunColor = new(1f, 0.88f, 0.72f), SunIntensity = 1.45f, SunAngles = new(40f, -60f, 0f),
                AmbientSky = new(0.72f, 0.72f, 0.85f), AmbientEquator = new(0.85f, 0.66f, 0.5f), AmbientGround = new(0.5f, 0.35f, 0.25f),
                Fog = new(0.94f, 0.8f, 0.64f), FogStart = 180f, FogEnd = 580f,
                BarrierA = new(0.85f, 0.35f, 0.2f), BarrierB = new(0.98f, 0.92f, 0.8f), BarrierTop = new(0.2f, 0.75f, 0.75f),
            },
            Theme.Jungle => new ThemeStyle
            {
                SunColor = new(1f, 0.95f, 0.8f), SunIntensity = 1.25f, SunAngles = new(55f, 20f, 0f),
                AmbientSky = new(0.55f, 0.72f, 0.6f), AmbientEquator = new(0.45f, 0.62f, 0.4f), AmbientGround = new(0.25f, 0.35f, 0.2f),
                Fog = new(0.6f, 0.78f, 0.62f), FogStart = 90f, FogEnd = 420f, SkyTint = new(0.45f, 0.6f, 0.5f),
                GroundTint = new(0.45f, 0.62f, 0.3f), Dunes = 4f,
                BarrierA = new(0.95f, 0.75f, 0.15f), BarrierB = new(0.2f, 0.5f, 0.25f), BarrierTop = new(0.95f, 0.4f, 0.2f),
            },
            Theme.Lagoon => new ThemeStyle
            {
                SunColor = new(1f, 0.97f, 0.9f), SunIntensity = 1.5f, SunAngles = new(50f, 150f, 0f),
                AmbientSky = new(0.65f, 0.8f, 0.95f), AmbientEquator = new(0.7f, 0.8f, 0.8f), AmbientGround = new(0.5f, 0.5f, 0.4f),
                Fog = new(0.7f, 0.88f, 0.98f), FogStart = 200f, FogEnd = 650f, SkyTint = new(0.4f, 0.55f, 0.65f),
                GroundTint = new(1f, 0.92f, 0.72f), FarLevel = -4f, FarFrom = 16f, FarTo = 42f, Dunes = 1.2f, WaterLevel = -0.6f,
                BarrierA = new(0.15f, 0.7f, 0.85f), BarrierB = new(1f, 1f, 1f), BarrierTop = new(1f, 0.55f, 0.45f),
            },
            Theme.Reef => new ThemeStyle
            {
                SunColor = new(0.7f, 0.9f, 1f), SunIntensity = 1.1f, SunAngles = new(70f, 0f, 0f),
                AmbientSky = new(0.3f, 0.6f, 0.85f), AmbientEquator = new(0.2f, 0.5f, 0.7f), AmbientGround = new(0.15f, 0.3f, 0.45f),
                Fog = new(0.12f, 0.45f, 0.65f), FogStart = 25f, FogEnd = 230f, SkyTint = new(0.1f, 0.4f, 0.7f), SkyGround = new(0.05f, 0.2f, 0.35f),
                SkyExposure = 0.8f, GroundTint = new(0.85f, 0.85f, 0.7f), Dunes = 5f, RoadTint = new(0.85f, 0.95f, 1f),
                BarrierA = new(1f, 0.45f, 0.55f), BarrierB = new(1f, 0.9f, 0.6f), BarrierTop = new(0.3f, 0.95f, 0.85f),
            },
            Theme.Sky => new ThemeStyle
            {
                SunColor = new(1f, 0.95f, 0.85f), SunIntensity = 1.5f, SunAngles = new(35f, 60f, 0f),
                AmbientSky = new(0.75f, 0.82f, 1f), AmbientEquator = new(0.85f, 0.85f, 0.95f), AmbientGround = new(0.8f, 0.8f, 0.85f),
                Fog = new(0.82f, 0.88f, 1f), FogStart = 220f, FogEnd = 700f, SkyTint = new(0.35f, 0.5f, 0.75f), SkyGround = new(0.9f, 0.9f, 0.95f),
                RoadTint = new(0.95f, 0.9f, 1f),
                BarrierA = new(1f, 0.6f, 0.8f), BarrierB = new(1f, 1f, 1f), BarrierTop = new(0.55f, 0.75f, 1f),
            },
            Theme.Space => new ThemeStyle
            {
                SunColor = new(1f, 0.95f, 0.95f), SunIntensity = 1.6f, SunAngles = new(30f, -120f, 0f),
                AmbientSky = new(0.3f, 0.3f, 0.5f), AmbientEquator = new(0.25f, 0.2f, 0.4f), AmbientGround = new(0.1f, 0.1f, 0.2f),
                Fog = new(0.03f, 0.02f, 0.08f), FogStart = 500f, FogEnd = 1400f, SkyTint = new(0f, 0f, 0f), SkyGround = new(0f, 0f, 0f),
                SkyExposure = 0.02f, SkyAtmosphere = 0f, RoadTint = new(0.6f, 0.55f, 0.85f),
                BarrierA = new(0.6f, 0.2f, 1f), BarrierB = new(0.1f, 0.1f, 0.2f), BarrierTop = new(0.2f, 1f, 1f),
            },
            Theme.Ice => new ThemeStyle
            {
                SunColor = new(0.95f, 0.97f, 1f), SunIntensity = 1.3f, SunAngles = new(30f, -20f, 0f),
                AmbientSky = new(0.7f, 0.8f, 1f), AmbientEquator = new(0.75f, 0.8f, 0.9f), AmbientGround = new(0.7f, 0.75f, 0.85f),
                Fog = new(0.85f, 0.9f, 0.98f), FogStart = 120f, FogEnd = 480f, SkyTint = new(0.45f, 0.55f, 0.7f),
                GroundTint = new(0.95f, 0.97f, 1f), Dunes = 3f,
                BarrierA = new(0.2f, 0.45f, 0.85f), BarrierB = new(0.85f, 0.95f, 1f), BarrierTop = new(0.95f, 0.3f, 0.35f),
            },
            Theme.Volcano => new ThemeStyle
            {
                SunColor = new(1f, 0.7f, 0.5f), SunIntensity = 1.1f, SunAngles = new(25f, 80f, 0f),
                AmbientSky = new(0.55f, 0.35f, 0.3f), AmbientEquator = new(0.5f, 0.3f, 0.2f), AmbientGround = new(0.3f, 0.15f, 0.1f),
                Fog = new(0.45f, 0.22f, 0.15f), FogStart = 70f, FogEnd = 380f, SkyTint = new(0.7f, 0.3f, 0.2f), SkyGround = new(0.2f, 0.08f, 0.05f),
                SkyExposure = 0.7f, SkyAtmosphere = 2.2f, GroundTint = new(0.3f, 0.26f, 0.26f), Dunes = 6f,
                BarrierA = new(0.15f, 0.15f, 0.15f), BarrierB = new(1f, 0.5f, 0.1f), BarrierTop = new(1f, 0.85f, 0.2f),
            },
            Theme.City => new ThemeStyle
            {
                SunColor = new(0.55f, 0.6f, 0.9f), SunIntensity = 0.6f, SunAngles = new(50f, 30f, 0f),
                AmbientSky = new(0.3f, 0.3f, 0.55f), AmbientEquator = new(0.3f, 0.25f, 0.45f), AmbientGround = new(0.15f, 0.15f, 0.25f),
                Fog = new(0.1f, 0.08f, 0.2f), FogStart = 120f, FogEnd = 600f, SkyTint = new(0.1f, 0.1f, 0.3f), SkyGround = new(0.05f, 0.05f, 0.1f),
                SkyExposure = 0.25f, GroundTint = new(0.3f, 0.3f, 0.35f), Dunes = 0f,
                BarrierA = new(1f, 0.2f, 0.7f), BarrierB = new(0.15f, 0.15f, 0.25f), BarrierTop = new(0.2f, 0.9f, 1f),
            },
            _ => new ThemeStyle(),
        };
    }
}
