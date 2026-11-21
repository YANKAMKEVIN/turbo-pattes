using System.Collections.Generic;
using UnityEngine;

namespace TurboPattes.Game.Visuals
{
    /// <summary>
    /// Matériaux URP créés par code. Les gabarits viennent de Resources (générés par l'outil
    /// d'édition) pour que shaders et mots-clés partent dans le build ; à défaut on retombe sur
    /// <see cref="Shader.Find"/>, qui ne marche de façon sûre que dans l'éditeur.
    /// Les matériaux renvoyés sont partagés : ne pas les modifier, cloner pour animer.
    /// </summary>
    public static class Mats
    {
        public const string LitTemplate = "TP_Lit";
        public const string EmissiveTemplate = "TP_LitEmissive";
        public const string TransparentTemplate = "TP_LitTransparent";
        public const string ParticleTemplate = "TP_Particle";
        public const string SkyTemplate = "TP_Sky";

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");

        public static int BaseColorProperty => BaseColorId;
        public static int EmissionColorProperty => EmissionColorId;

        static readonly Dictionary<string, Material> Templates = new();
        static readonly Dictionary<(Color, float, float), Material> LitCache = new();
        static readonly Dictionary<(Color, Color), Material> EmissiveCache = new();
        static readonly Dictionary<Color, Material> TransparentCache = new();
        static readonly Dictionary<Color, Material> ParticleCache = new();

        static Material Template(string name, string fallbackShader)
        {
            if (Templates.TryGetValue(name, out var m) && m != null) return m;
            m = Resources.Load<Material>(name);
            if (m == null)
            {
                Debug.LogWarning($"[TurboPattes] Gabarit {name} absent de Resources : Shader.Find (lancer « {TurboPattes.Core.Brand.Title}/Configurer le projet »).");
                m = new Material(Shader.Find(fallbackShader));
            }
            Templates[name] = m;
            return m;
        }

        public static Material Lit(Color color, float smoothness = 0.25f, float metallic = 0f)
        {
            var key = (color, smoothness, metallic);
            if (LitCache.TryGetValue(key, out var m)) return m;
            m = new Material(Template(LitTemplate, "Universal Render Pipeline/Lit")) { name = "Lit" };
            m.SetColor(BaseColorId, color);
            m.SetFloat(SmoothnessId, smoothness);
            m.SetFloat(MetallicId, metallic);
            m.enableInstancing = true;
            LitCache[key] = m;
            return m;
        }

        /// <summary>Matériau texturé (non mis en cache : chaque appel crée une instance).</summary>
        public static Material Textured(Texture2D texture, Color tint, Vector2 tiling, float smoothness = 0.2f)
        {
            var m = new Material(Template(LitTemplate, "Universal Render Pipeline/Lit")) { name = texture.name };
            m.SetTexture(BaseMapId, texture);
            m.SetTextureScale(BaseMapId, tiling);
            m.mainTextureScale = tiling;
            m.SetColor(BaseColorId, tint);
            m.SetFloat(SmoothnessId, smoothness);
            m.SetFloat(MetallicId, 0f);
            return m;
        }

        public static Material Emissive(Color color, Color emission)
        {
            var key = (color, emission);
            if (EmissiveCache.TryGetValue(key, out var m)) return m;
            m = NewEmissive(color, emission);
            EmissiveCache[key] = m;
            return m;
        }

        /// <summary>Instance émissive à part, pour l'animer.</summary>
        public static Material NewEmissive(Color color, Color emission)
        {
            var m = new Material(Template(EmissiveTemplate, "Universal Render Pipeline/Lit")) { name = "Emissive" };
            m.EnableKeyword("_EMISSION");
            m.SetColor(BaseColorId, color);
            m.SetColor(EmissionColorId, emission);
            return m;
        }

        public static Material Transparent(Color color)
        {
            if (TransparentCache.TryGetValue(color, out var m)) return m;
            m = NewTransparent(color);
            TransparentCache[color] = m;
            return m;
        }

        public static Material NewTransparent(Color color)
        {
            var m = new Material(Template(TransparentTemplate, "Universal Render Pipeline/Lit")) { name = "Transparent" };
            m.SetColor(BaseColorId, color);
            return m;
        }

        /// <summary>Particules additives (étincelles, flammes, poussière).</summary>
        public static Material Particle(Color color)
        {
            if (ParticleCache.TryGetValue(color, out var m)) return m;
            m = new Material(Template(ParticleTemplate, "Universal Render Pipeline/Particles/Unlit")) { name = "Particle" };
            m.SetTexture(BaseMapId, ProceduralTextures.SoftDot);
            m.SetColor(BaseColorId, color);
            ParticleCache[color] = m;
            return m;
        }

        public static Material Sky()
        {
            var t = Resources.Load<Material>(SkyTemplate);
            if (t != null) return t;
            var s = Shader.Find("Skybox/Procedural");
            return s != null ? new Material(s) : null;
        }
    }
}
