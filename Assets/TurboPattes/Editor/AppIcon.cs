using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace TurboPattes.EditorTools
{
    /// <summary>
    /// Icône de l'application dessinée par code (aucune image à la main), direction « arcade sportif » : deux chevrons
    /// rouge course et un drapeau à damier sur un fond carbone, avec des traits de vitesse acier. Écrite dans Assets/TurboPattes/Generated/AppIcon.png
    /// puis assignée comme icône par défaut (Mac, Android legacy et rond). Règle aussi la version de l'application.
    /// </summary>
    public static class AppIcon
    {
        const int Size = 1024;
        const int Samples = 3; // sur-échantillonnage 3x3 par pixel
        const string Dir = "Assets/TurboPattes/Generated";
        const string Path_ = Dir + "/AppIcon.png";

        static readonly Color Carbon = new Color(0.078f, 0.09f, 0.106f);   // #14171B
        static readonly Color Graphite = new Color(0.153f, 0.173f, 0.2f);  // #272C33
        static readonly Color Red = new Color(0.784f, 0.129f, 0.106f);     // #C8211B
        static readonly Color Chalk = new Color(0.949f, 0.957f, 0.965f);   // #F2F4F6
        static readonly Color Steel = new Color(0.541f, 0.576f, 0.62f);    // #8A939E
        static readonly Color Shade = new Color(0f, 0f, 0f);

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
            // reçoivent la même texture, le dessin est centré dans la zone que le masque garde.
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

        /// <summary>Couleur en un point (x, y) de [0,1]² (y vers le haut) : fond, traits de vitesse, damier, ombre puis chevrons.</summary>
        static Color Shade_(float x, float y)
        {
            float t = Mathf.Clamp01((x + (1f - y)) * 0.5f);
            var c = Color.Lerp(Graphite, Carbon, t);

            // Dessin réduit vers le centre : le masque des icônes adaptatives Android rogne les bords.
            x = 0.5f + (x - 0.5f) / 0.8f;
            y = 0.5f + (y - 0.5f) / 0.8f;
            // Traits de vitesse horizontaux, à gauche des chevrons.
            float lines = Mathf.Max(Capsule(x, y, 0.06f, 0.42f, 0.3f, 0.42f, 0.022f), Capsule(x, y, 0.1f, 0.58f, 0.32f, 0.58f, 0.022f));
            if (lines > 0) c = Steel;
            // Drapeau à damier en haut à gauche (4 × 3 cases).
            if (x >= 0.12f && x < 0.4f && y >= 0.7f && y < 0.91f)
                c = ((int)((x - 0.12f) / 0.07f) + (int)((y - 0.7f) / 0.07f)) % 2 == 0 ? Chalk : Carbon;
            if (Chevrons(x - 0.012f, y + 0.014f)) c = Color.Lerp(c, Shade, 0.35f);
            if (Chevrons(x, y)) c = Red;
            return c;
        }

        /// <summary>Deux chevrons « > » côte à côte, pointe à droite, centrés en hauteur.</summary>
        static bool Chevrons(float x, float y)
        {
            float dy = Mathf.Abs(y - 0.48f);
            if (dy > 0.26f) return false;
            foreach (float x0 in new[] { 0.36f, 0.58f })
            {
                float left = x0 + 0.26f - dy * 0.9f;
                if (x >= left && x <= left + 0.13f) return true;
            }
            return false;
        }

        /// <summary>1 si le point est dans la capsule (segment a-b épaissi de r), sinon 0.</summary>
        static float Capsule(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float dx = bx - ax, dy = by - ay;
            float t = Mathf.Clamp01(((x - ax) * dx + (y - ay) * dy) / (dx * dx + dy * dy));
            float ex = x - (ax + dx * t), ey = y - (ay + dy * t);
            return ex * ex + ey * ey <= r * r ? 1f : 0f;
        }
    }
}
