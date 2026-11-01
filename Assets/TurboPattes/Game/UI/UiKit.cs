using TurboPattes.Game.Kart;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Outils IMGUI partagés par le HUD et les menus, dans la DA « Atelier plein gaz » : plaques obliques façon
    /// fiche de chronométrage, ombres franches décalées, police Saira, palette d'atelier. Styles mis à l'échelle
    /// de l'écran ; clics lus via l'Input System (souris et tactile). Les clics se lisent dans Update, le dessin dans OnGUI.
    /// </summary>
    public sealed class UiKit
    {
        // Palette « Atelier plein gaz ».
        public static readonly Color Charcoal = new(0.141f, 0.188f, 0.176f);
        public static readonly Color Vermilion = new(0.851f, 0.302f, 0.247f);
        public static readonly Color Ochre = new(0.914f, 0.725f, 0.286f);
        public static readonly Color Teal = new(0.235f, 0.561f, 0.569f);
        public static readonly Color Cream = new(0.949f, 0.906f, 0.808f);

        public enum Kind { Primary, Secondary, Dark }

        /// <summary>Titres (Saira Black italique), chiffres (ExtraBold), textes (SemiBold).</summary>
        public GUIStyle Title, Huge, Big, Medium, Small, Tiny;
        public float Scale { get; private set; }

        static Font _black, _extra, _semi;
        static Texture2D _slashL, _slashR;

        /// <summary>À appeler dans OnGUI avant de dessiner.</summary>
        public void EnsureStyles()
        {
            float scale = Screen.height / 720f;
            if (Big != null && Mathf.Approximately(scale, Scale)) return;
            Scale = scale;
            if (_semi == null)
            {
                _black = Resources.Load<Font>("Fonts/SairaBlackItalic");
                _extra = Resources.Load<Font>("Fonts/SairaExtraBold");
                _semi = Resources.Load<Font>("Fonts/SairaSemiBold");
            }
            GUIStyle Make(Font f, float size) => new()
            {
                font = f,
                fontSize = Mathf.RoundToInt(size * scale),
                normal = { textColor = Color.white },
                clipping = TextClipping.Overflow,
            };
            Title = Make(_black, 96);
            Huge = Make(_black, 130);
            Big = Make(_extra, 60);
            Medium = Make(_extra, 30);
            Small = Make(_semi, 21);
            Tiny = Make(_semi, 16);
        }

        /// <summary>Texte ; <paramref name="shadow"/> &gt; 0 ajoute une ombre franche couleur encre.</summary>
        public void Label(Rect r, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.UpperLeft, float shadow = 0f)
        {
            var s = new GUIStyle(style) { alignment = anchor };
            if (shadow > 0f)
            {
                float o = Mathf.Max(1f, shadow * Scale);
                s.normal.textColor = Lin(new Color(Charcoal.r, Charcoal.g, Charcoal.b, 0.85f * color.a));
                GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, s);
            }
            s.normal.textColor = Lin(color);
            GUI.Label(r, text, s);
        }

        public static void Disc(Vector2 center, float radius, Color color)
        {
            // Boîte aux coins arrondis à 100 % : même chemin que les plaques, couleurs justes.
            Box(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), color, radius);
        }

        public static void Box(Rect r, Color color, float radius = 0f)
        {
            GUI.DrawTexture(r, ProceduralTextures.White, ScaleMode.StretchToFill, true, 0f, Linear(color), 0f, radius);
        }

        /// <summary>
        /// Plaque en parallélogramme (bords en « / ») : <paramref name="slant"/> est le décalage horizontal
        /// entre le haut et le bas du bord, en pixels. 0 donne un rectangle.
        /// </summary>
        public static void Plate(Rect r, Color color, float slant)
        {
            if (slant <= 0.5f) { Box(r, color); return; }
            EnsureSlashes();
            var c = Linear(color);
            GUI.DrawTexture(new Rect(r.x + slant, r.y, r.width - 2f * slant, r.height), ProceduralTextures.White, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
            GUI.DrawTexture(new Rect(r.x, r.y, slant, r.height), _slashL, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
            GUI.DrawTexture(new Rect(r.xMax - slant, r.y, slant, r.height), _slashR, ScaleMode.StretchToFill, true, 0f, c, 0f, 0f);
        }

        /// <summary>Plaque avec ombre franche décalée et contour encre (style affiche).</summary>
        public void Card(Rect r, Color fill, float slant, float shadow = 5f, float outline = 3f)
        {
            float o = shadow * Scale, b = outline * Scale;
            if (o > 0f) Plate(new Rect(r.x + o, r.y + o, r.width, r.height), new Color(0.06f, 0.08f, 0.07f, 0.55f), slant);
            if (b > 0f) Plate(new Rect(r.x - b, r.y - b, r.width + 2f * b, r.height + 2f * b), Charcoal, slant);
            Plate(r, fill, slant);
        }

        /// <summary>Deux barres obliques d'accent « // » (repère de fiche de course).</summary>
        public void Stripes(Vector2 topLeft, float height, Color a, Color b)
        {
            float w = height * 0.32f, slant = height * 0.35f, gap = w * 0.55f;
            Plate(new Rect(topLeft.x, topLeft.y, w + slant, height), a, slant);
            Plate(new Rect(topLeft.x + w + gap, topLeft.y, w + slant, height), b, slant);
        }

        /// <summary>Bouton-plaque : primaire vermillon, secondaire crème, sombre encre. <paramref name="lit"/> = choisi (ocre).</summary>
        public void Button(Rect r, string text, Kind kind, bool focused = false, bool lit = false, GUIStyle style = null)
        {
            float slant = r.height * 0.28f;
            Color fill = lit ? Ochre : kind switch { Kind.Primary => Vermilion, Kind.Secondary => Cream, _ => Charcoal };
            Color ink = lit || kind == Kind.Secondary ? Charcoal : Cream;
            if (focused)
            {
                float f = 5f * Scale;
                Plate(new Rect(r.x - f - 3f * Scale, r.y - f, r.width + 2f * f + 6f * Scale, r.height + 2f * f), Ochre, slant);
            }
            Card(r, fill, slant, focused ? 6f : 4f, 2.5f);
            // Tranche basse plus sombre : relief imprimé (bas du parallélogramme).
            float th = r.height * 0.12f, st = slant * th / r.height;
            Plate(new Rect(r.x, r.yMax - th, r.width - slant + st, th), new Color(0f, 0f, 0f, 0.16f), st);
            Label(r, text, style ?? Medium, ink, TextAnchor.MiddleCenter);
        }

        /// <summary>Numéro de course du pilote sur une plaque à sa livrée.</summary>
        public void Badge(Rect r, AnimalSpec spec)
        {
            float slant = r.height * 0.3f;
            Plate(new Rect(r.x - 2f * Scale, r.y - 2f * Scale, r.width + 4f * Scale, r.height + 4f * Scale), Charcoal, slant);
            Plate(r, spec.Kart, slant);
            Plate(new Rect(r.x + r.width * 0.62f, r.y, r.width * 0.38f, r.height), spec.KartTrim, slant);
            var st = new GUIStyle(Medium) { fontSize = Mathf.RoundToInt(r.height * 0.72f) };
            Label(new Rect(r.x, r.y - r.height * 0.04f, r.width, r.height), spec.Number.ToString("00"), st, InkOn(spec.Kart), TextAnchor.MiddleCenter);
        }

        /// <summary>Encre lisible sur cette couleur de fond (crème sur foncé, encre sur clair).</summary>
        public static Color InkOn(Color background) =>
            background.r * 0.3f + background.g * 0.59f + background.b * 0.11f > 0.55f ? Charcoal : Cream;

        static void EnsureSlashes()
        {
            if (_slashL != null) return;
            _slashL = Slash("SlashL", left: true);
            _slashR = Slash("SlashR", left: false);
        }

        // Triangle anticrénelé : moitié d'un bord « / ». La texture compte ses lignes depuis le bas.
        static Texture2D Slash(string name, bool left)
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false, true) { name = name, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                float u = (i + 0.5f) / n, yTop = 1f - (j + 0.5f) / n;
                // Distance signée à la diagonale qui va du coin haut droit au coin bas gauche.
                float d = (u - (1f - yTop)) * 0.7071f * n;
                float a = Mathf.Clamp01((left ? d : -d) + 0.5f);
                px[j * n + i] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            return tex;
        }

        // IMGUI ne convertit pas les couleurs en espace linéaire : sans ça, tout sort délavé.
        public static Color Linear(Color c) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? new Color(c.linear.r, c.linear.g, c.linear.b, c.a) : c;

        static Color Lin(Color c) => Linear(c);

        /// <summary>Position du clic (souris ou doigt) relâché à cette image, en coordonnées GUI ; null sinon.</summary>
        public static Vector2? Tap()
        {
            // EnhancedTouch garde les touchers appuyés et relâchés dans la même image (tapes très brèves),
            // que Touchscreen.wasReleasedThisFrame laisse passer.
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();
            foreach (var touch in UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches)
                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended) return ToGui(touch.screenPosition);
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasReleasedThisFrame) return ToGui(mouse.position.ReadValue());
            return null;
        }

        static Vector2 ToGui(Vector2 screen) => new(screen.x, Screen.height - screen.y);
    }
}
