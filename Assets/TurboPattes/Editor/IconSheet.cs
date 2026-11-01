using System.IO;
using TurboPattes.Core;
using TurboPattes.Game.Kart;
using TurboPattes.Game.Visuals;
using UnityEditor;
using UnityEngine;

namespace TurboPattes.EditorTools
{
    /// <summary>
    /// Planche PNG de toutes les icônes, pour les relire sans lancer le jeu :
    /// Unity -batchmode -projectPath . -executeMethod TurboPattes.EditorTools.IconSheet.Export -tp-out docs/icones.png -quit
    /// </summary>
    public static class IconSheet
    {
        [MenuItem("Turbo Pattes/Exporter la planche d'icônes")]
        public static void Export()
        {
            string output = "docs/icones.png";
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == "-tp-out") output = args[i + 1];

            const int cell = 160, cols = 6;
            var icons = new System.Collections.Generic.List<(Texture2D tex, Color bg)>();
            foreach (ItemKind k in new[] { ItemKind.Carrot, ItemKind.Coconut, ItemKind.Honey, ItemKind.Shield, ItemKind.Feather, ItemKind.Yarn })
                icons.Add((Icons.Item(k), new Color(0.12f, 0.15f, 0.25f)));
            foreach (var spec in AnimalSpec.Roster) icons.Add((Icons.Portrait(spec), spec.Kart));

            int rows = (icons.Count + cols - 1) / cols;
            var sheet = new Texture2D(cols * cell, rows * cell, TextureFormat.RGBA32, false);
            var bgPx = new Color[sheet.width * sheet.height];
            for (int i = 0; i < bgPx.Length; i++) bgPx[i] = new Color(0.9f, 0.9f, 0.92f);
            sheet.SetPixels(bgPx);
            for (int i = 0; i < icons.Count; i++)
            {
                int ox = i % cols * cell, oy = (rows - 1 - i / cols) * cell;
                var src = icons[i].tex.GetPixels(0);
                for (int y = 0; y < cell; y++)
                for (int x = 0; x < cell; x++)
                {
                    float dx = x - cell * 0.5f + 0.5f, dy = y - cell * 0.5f + 0.5f;
                    var bg = dx * dx + dy * dy < cell * cell * 0.25f ? icons[i].bg : new Color(0.9f, 0.9f, 0.92f);
                    var s = src[y * cell + x];
                    // Les icônes stockent des valeurs linéaires (voir IconCanvas.ToTexture).
                    if (QualitySettings.activeColorSpace == ColorSpace.Linear) s = new Color(s.gamma.r, s.gamma.g, s.gamma.b, s.a);
                    sheet.SetPixel(ox + x, oy + y, Color.Lerp(bg, new Color(s.r, s.g, s.b), s.a));
                }
            }
            sheet.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));
            File.WriteAllBytes(output, sheet.EncodeToPNG());
            Debug.Log($"[IconSheet] {icons.Count} icônes -> {output}");
        }
    }
}
