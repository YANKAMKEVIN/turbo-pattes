using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TurboPattes.EditorTools
{
    /// <summary>
    /// Icône de l'application dessinée par code (aucune image à la main) : empreinte de patte crème inclinée sur un
    /// dégradé orange -> vermillon, avec deux traits de vitesse. Écrite dans Assets/TurboPattes/Generated/AppIcon.png
    /// puis assignée comme icône par défaut (Mac, Android legacy et rond). Règle aussi la version de l'application.
    /// </summary>
    public static class AppIcon
    {
        const int Size = 1024;
        const int Samples = 3; // sur-échantillonnage 3x3 par pixel
        const string Dir = "Assets/TurboPattes/Generated";
        const string Path_ = Dir + "/AppIcon.png";

        static readonly Color Orange = new Color(1f, 0.62f, 0.12f);
        static readonly Color Vermilion = new Color(0.9f, 0.2f, 0.1f);
        static readonly Color Cream = new Color(1f, 0.95f, 0.82f);
        static readonly Color Shade = new Color(0.55f, 0.08f, 0.05f);

        [MenuItem("Turbo Pattes/Icône de l'application")]
        public static void Apply()
        {
            Directory.CreateDirectory(Dir);
            File.WriteAllBytes(Path_, Render().EncodeToPNG());
            AssetDatabase.ImportAsset(Path_, ImportAssetOptions.ForceUpdate);

            var importer = (TextureImporter)AssetImporter.GetAtPath(Path_);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = 1024;
            importer.isReadable = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(Path_);
            if (tex == null)
            {
                Debug.LogError("[TurboPattes] Icône introuvable après import : " + Path_);
                return;
            }

            // Icône par défaut (utilisée par le build Mac).
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { tex }, IconKind.Any);

            // Android : icône adaptative (fond + premier plan ; le système découpe la forme). Toutes les tailles
            // reçoivent la même texture, la patte est centrée dans la zone que le masque garde.
            FillAndroid(tex, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);

            AssetDatabase.SaveAssets();
            Debug.Log("[TurboPattes] Icône de l'application appliquée : " + Path_);
        }

        static void FillAndroid(Texture2D tex, PlatformIconKind kind)
        {
            var icons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, kind);
            foreach (var icon in icons)
            {
                if (icon.maxLayerCount >= 2) icon.SetTextures(tex, tex);
                else icon.SetTexture(tex);
            }
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, kind, icons);
        }

        // ---- Dessin ----

        static Texture2D Render()
        {
            var px = new Color32[Size * Size];
            float inv = 1f / (Size * Samples);
            for (int j = 0; j < Size; j++)
            for (int i = 0; i < Size; i++)
            {
                float r = 0, g = 0, b = 0;
                for (int sy = 0; sy < Samples; sy++)
                for (int sx = 0; sx < Samples; sx++)
                {
                    float x = (i * Samples + sx + 0.5f) * inv;
                    float y = (j * Samples + sy + 0.5f) * inv; // y vers le haut
                    var c = Shade_(x, y);
                    r += c.r; g += c.g; b += c.b;
                }
                float n = Samples * Samples;
                px[j * Size + i] = new Color(r / n, g / n, b / n, 1f);
            }
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        /// <summary>Couleur en un point (x, y) de [0,1]² : fond, traits de vitesse, ombre puis patte.</summary>
        static Color Shade_(float x, float y)
        {
            float t = Mathf.Clamp01((x + (1f - y)) * 0.5f);
            var c = Color.Lerp(Orange, Vermilion, t);

            // Dessin réduit vers le centre : le masque des icônes adaptatives Android rogne les bords.
            x = 0.5f + (x - 0.5f) / 0.8f;
            y = 0.5f + (y - 0.5f) / 0.8f;
            // Deux traits de vitesse obliques, derrière la patte (en bas à gauche).
            float lines = Mathf.Max(Capsule(x, y, 0.05f, 0.40f, 0.27f, 0.58f, 0.028f), Capsule(x, y, 0.07f, 0.18f, 0.33f, 0.39f, 0.036f));
            if (lines > 0) c = Color.Lerp(c, Cream, 0.6f);

            if (Paw(x - 0.012f, y + 0.014f)) c = Color.Lerp(c, Shade, 0.3f);
            if (Paw(x, y)) c = Cream;
            return c;
        }

        /// <summary>1 si le point est dans la capsule (segment a-b épaissi de r), sinon 0.</summary>
        static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float dx = bx - ax, dy = by - ay;
            float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
            float ex = x - (ax + dx * t), ey = y - (ay + dy * t);
            return ex * ex + ey * ey <= r * r ? 1f : 0f;
        }

        /// <summary>Patte inclinée de 15 degrés autour de (0,56 ; 0,47) : un coussinet et quatre doigts ovales.</summary>
        static bool Paw(float x, float y)
        {
            const float tilt = -15f * Mathf.Deg2Rad;
            float dx = x - 0.56f, dy = y - 0.47f;
            float cs = Mathf.Cos(-tilt), sn = Mathf.Sin(-tilt);
            float u = dx * cs - dy * sn, v = dx * sn + dy * cs; // repère local de la patte
            return Ellipse(u, v, 0f, -0.07f, 0.23f, 0.17f)
                || Ellipse(u, v, -0.285f, 0.12f, 0.08f, 0.105f)
                || Ellipse(u, v, -0.095f, 0.265f, 0.09f, 0.12f)
                || Ellipse(u, v, 0.095f, 0.265f, 0.09f, 0.12f)
                || Ellipse(u, v, 0.285f, 0.12f, 0.08f, 0.105f);
        }

        static bool Ellipse(float u, float v, float cx, float cy, float rx, float ry)
        {
            float a = (u - cx) / rx, b = (v - cy) / ry;
            return a * a + b * b <= 1f;
        }
    }
}
