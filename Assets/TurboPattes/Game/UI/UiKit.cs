using TurboPattes.Core;
using TurboPattes.Game.Kart;
using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Composants IMGUI partagés par le HUD et les menus, dans la direction « Atelier plein gaz » : police Saira,
    /// fiches crème à coin oblique, plaques vert charbon derrière les informations de course, surlignage vermillon,
    /// repères obliques ocre, chiffres à largeur fixe. Clics lus via l'Input System (souris et tactile).
    /// Les clics se lisent dans Update (une fois par image), le dessin dans OnGUI.
    /// </summary>
    public sealed class UiKit
    {
        public static readonly Color Ink = Of(Atelier.Charcoal);
        public static readonly Color Cream = Of(Atelier.Cream);
        public static readonly Color Vermilion = Of(Atelier.Vermilion);
        public static readonly Color Ochre = Of(Atelier.Ochre);
        public static readonly Color Blue = Of(Atelier.Workshop);

        /// <summary>Titres, positions (Bold) ; commandes (SemiBold) ; descriptions (Regular).</summary>
        public GUIStyle Huge, Big, Medium, Small, Body;
        public float Scale { get; private set; }

        static Font _bold, _semiBold, _regular;
        static bool _fontsLoaded;

        public static Color Of(Rgb c, float a = 1f) => new(c.R, c.G, c.B, a);
        public static Color Alpha(Color c, float a) => new(c.r, c.g, c.b, c.a * a);

        static void LoadFonts()
        {
            if (_fontsLoaded) return;
            _fontsLoaded = true;
            // Sans le fichier (import raté), la police intégrée prend le relais : GUIStyle.font = null.
            _bold = Resources.Load<Font>(Atelier.FontBold);
            _semiBold = Resources.Load<Font>(Atelier.FontSemiBold) ?? _bold;
            _regular = Resources.Load<Font>(Atelier.FontRegular) ?? _semiBold;
            if (_bold == null) Debug.LogWarning("[TurboPattes] Police Saira introuvable dans Resources/Fonts : police par défaut.");
        }

        /// <summary>À appeler dans OnGUI avant de dessiner.</summary>
        public void EnsureStyles()
        {
            float scale = Screen.height / 720f;
            if (Big != null && Mathf.Approximately(scale, Scale)) return;
            Scale = scale;
            LoadFonts();
            Big = new GUIStyle { font = _bold, fontSize = Mathf.RoundToInt(60 * scale), fontStyle = FontStyle.Normal, normal = { textColor = Color.white } };
            Huge = new GUIStyle(Big) { fontSize = Mathf.RoundToInt(110 * scale) };
            Medium = new GUIStyle(Big) { font = _semiBold, fontSize = Mathf.RoundToInt(29 * scale) };
            Small = new GUIStyle(Big) { font = _semiBold, fontSize = Mathf.RoundToInt(20 * scale) };
            Body = new GUIStyle(Big) { font = _regular, fontSize = Mathf.RoundToInt(24 * scale) };
        }

        /// <summary>Texte net (sans ombre par défaut : les informations reposent sur une plaque ou une fiche).</summary>
        public void Label(Rect r, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.UpperLeft, float shadow = 0f)
        {
            var s = new GUIStyle(style) { alignment = anchor };
            if (shadow > 0f)
            {
                float o = Mathf.Max(1f, shadow * Scale);
                s.normal.textColor = Linear(new Color(Ink.r, Ink.g, Ink.b, 0.75f * color.a));
                GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, s);
            }
            s.normal.textColor = Linear(color);
            GUI.Label(r, text, s);
        }

        /// <summary>
        /// Chiffres à largeur fixe (chrono, vitesse, écarts) : chaque caractère centré dans sa case
        /// (<see cref="FixedDigits"/>), pour que le texte ne tremble pas quand les chiffres changent.
        /// </summary>
        public void Digits(Rect r, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.UpperLeft)
        {
            float size = style.fontSize;
            float total = FixedDigits.Width(text) * size;
            float x = anchor switch
            {
                TextAnchor.UpperCenter or TextAnchor.MiddleCenter or TextAnchor.LowerCenter => r.center.x - total * 0.5f,
                TextAnchor.UpperRight or TextAnchor.MiddleRight or TextAnchor.LowerRight => r.xMax - total,
                _ => r.x,
            };
            var vertical = anchor switch
            {
                TextAnchor.MiddleLeft or TextAnchor.MiddleCenter or TextAnchor.MiddleRight => TextAnchor.MiddleCenter,
                TextAnchor.LowerLeft or TextAnchor.LowerCenter or TextAnchor.LowerRight => TextAnchor.LowerCenter,
                _ => TextAnchor.UpperCenter,
            };
            var s = new GUIStyle(style) { alignment = vertical, clipping = TextClipping.Overflow };
            s.normal.textColor = Linear(color);
            foreach (char c in text)
            {
                float cw = FixedDigits.Cell(c) * size;
                GUI.Label(new Rect(x, r.y, cw, r.height), c.ToString(), s);
                x += cw;
            }
        }

        public static void Disc(Vector2 center, float radius, Color color)
        {
            // Boîte aux coins arrondis à 100 % : même chemin que les boutons, couleurs justes.
            Box(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), color, radius);
        }

        /// <summary>Anneau (cercle évidé, approché par une couronne de pastilles) : jauges et repères tactiles.</summary>
        public static void Ring(Vector2 center, float radius, float thickness, Color color, int dots = 48)
        {
            for (int i = 0; i < dots; i++)
            {
                float a = i * Mathf.PI * 2f / dots;
                Disc(center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius, thickness * 0.5f, color);
            }
        }

        /// <summary>Portrait du pilote sur une pastille de la couleur de son kart.</summary>
        public static void Portrait(Vector2 center, float radius, AnimalSpec spec, float ring = 0f)
        {
            if (ring > 0f) Disc(center, radius + ring, spec.KartTrim);
            Disc(center, radius, spec.Kart);
            float r = radius * 1.12f;
            GUI.DrawTexture(new Rect(center.x - r, center.y - r * 1.06f, r * 2f, r * 2f), Icons.Portrait(spec), ScaleMode.ScaleToFit);
        }

        public static void Box(Rect r, Color color, float radius = 0f)
        {
            GUI.DrawTexture(r, ProceduralTextures.White, ScaleMode.StretchToFill, true, 0f, Linear(color), 0f, radius);
        }

        /// <summary>
        /// Fiche à coin oblique (coin haut droit coupé de <paramref name="cut"/> pixels), avec une ombre courte et
        /// nette décalée vers le bas, comme une fiche de chronométrage posée sur l'établi.
        /// </summary>
        public static void Card(Rect r, Color fill, float cut, float shadow = 0f)
        {
            if (shadow > 0f) CardShape(new Rect(r.x + shadow * 0.5f, r.y + shadow, r.width, r.height), new Color(Ink.r, Ink.g, Ink.b, 0.4f * fill.a), cut);
            CardShape(r, fill, cut);
        }

        static void CardShape(Rect r, Color fill, float cut)
        {
            cut = Mathf.Clamp(cut, 0f, Mathf.Min(r.width, r.height));
            if (cut < 1f) { Box(r, fill); return; }
            Box(new Rect(r.x, r.y + cut, r.width, r.height - cut), fill);
            Box(new Rect(r.x, r.y, r.width - cut, cut), fill);
            GUI.DrawTexture(new Rect(r.xMax - cut, r.y, cut, cut), ProceduralTextures.Wedge, ScaleMode.StretchToFill, true, 0f, Linear(fill), 0f, 0f);
        }

        /// <summary>Plaque vert charbon presque opaque : contraste stable derrière les informations de course.</summary>
        public void Plate(Rect r, float alpha = 1f) => Card(r, new Color(Ink.r, Ink.g, Ink.b, 0.86f * alpha), Mathf.Min(r.height * 0.3f, 18f * Scale));

        /// <summary>Repère oblique : parallélogramme penché vers la droite (pente <paramref name="slant"/>).</summary>
        public static void Slash(Rect r, Color color, float slant = 0.35f)
        {
            var m = GUI.matrix;
            var shear = Matrix4x4.identity;
            // x' = x - slant * (y - centre) : le haut part vers la droite, le bas vers la gauche.
            shear.m01 = -slant;
            shear.m03 = slant * r.center.y;
            GUI.matrix = m * shear;
            Box(r, color);
            GUI.matrix = m;
        }

        /// <summary>Titre de bloc : repère oblique ocre puis texte (couleur au choix selon le fond).</summary>
        public void Caption(Rect r, string text, Color textColor, GUIStyle style = null)
        {
            float s = Scale;
            Slash(new Rect(r.x + 4 * s, r.y + r.height * 0.2f, 7 * s, r.height * 0.6f), Ochre);
            Label(new Rect(r.x + 20 * s, r.y, r.width - 20 * s, r.height), text, style ?? Medium, textColor, TextAnchor.MiddleLeft);
        }

        // IMGUI ne convertit pas les couleurs en espace linéaire : sans ça, tout sort délavé.
        public static Color Linear(Color c) =>
            QualitySettings.activeColorSpace == ColorSpace.Linear ? new Color(c.linear.r, c.linear.g, c.linear.b, c.a) : c;

        /// <summary>
        /// Bouton en fiche à coin oblique. Action principale : vermillon, texte crème ; secondaire : crème, texte encre.
        /// <paramref name="focused"/> (navigation clavier/manette) l'encadre d'encre et ajoute un repère oblique ocre.
        /// </summary>
        public void Button(Rect r, string text, bool primary, bool focused = false, GUIStyle style = null, float alpha = 1f, bool disabled = false)
        {
            float s = Scale, cut = Mathf.Min(r.height * 0.3f, 16f * s);
            if (focused)
            {
                float f = 4f * s;
                CardShape(new Rect(r.x - f, r.y - f, r.width + 2 * f, r.height + 2 * f), Alpha(Ink, alpha), cut + f);
            }
            var fill = disabled ? new Color(Cream.r, Cream.g, Cream.b, 0.45f) : primary ? Vermilion : Cream;
            Card(r, Alpha(fill, alpha), cut, focused ? 0f : 4f * s);
            if (focused) Slash(new Rect(r.x + 10 * s, r.y + r.height * 0.22f, 7 * s, r.height * 0.56f), Alpha(primary ? Ochre : Vermilion, alpha));
            var textColor = disabled ? Alpha(Ink, 0.45f) : primary ? Cream : Ink;
            Label(r, text, style ?? Medium, Alpha(textColor, alpha), TextAnchor.MiddleCenter);
        }

        /// <summary>Choix d'un sélecteur : surligné en vermillon quand il est retenu.</summary>
        public void Segment(Rect r, string text, bool selected, bool focused, bool disabled = false) =>
            Button(r, text, selected, focused, Small, 1f, disabled);

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
