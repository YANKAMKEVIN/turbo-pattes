using TurboPattes.Game.Visuals;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Petits outils IMGUI partagés par le HUD et les menus : styles mis à l'échelle de l'écran, texte ombré,
    /// pastilles, boîtes arrondies, et clics lus via l'Input System (souris et tactile).
    /// Les clics se lisent dans Update (une fois par image), le dessin dans OnGUI.
    /// </summary>
    public sealed class UiKit
    {
        public GUIStyle Huge, Big, Medium, Small;
        public float Scale { get; private set; }

        /// <summary>À appeler dans OnGUI avant de dessiner.</summary>
        public void EnsureStyles()
        {
            float scale = Screen.height / 720f;
            if (Big != null && Mathf.Approximately(scale, Scale)) return;
            Scale = scale;
            Big = new GUIStyle { fontSize = Mathf.RoundToInt(64 * scale), fontStyle = FontStyle.Bold, normal = { textColor = Color.white } };
            Huge = new GUIStyle(Big) { fontSize = Mathf.RoundToInt(110 * scale) };
            Medium = new GUIStyle(Big) { fontSize = Mathf.RoundToInt(30 * scale) };
            Small = new GUIStyle(Big) { fontSize = Mathf.RoundToInt(19 * scale) };
        }

        public void Label(Rect r, string text, GUIStyle style, Color color, TextAnchor anchor = TextAnchor.UpperLeft, float shadow = 2.5f)
        {
            var s = new GUIStyle(style) { alignment = anchor };
            float o = Mathf.Max(1f, shadow * Scale);
            s.normal.textColor = new Color(0f, 0f, 0f, 0.6f * color.a);
            GUI.Label(new Rect(r.x + o, r.y + o, r.width, r.height), text, s);
            s.normal.textColor = color;
            GUI.Label(r, text, s);
        }

        public static void Disc(Vector2 center, float radius, Color color)
        {
            var old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), ProceduralTextures.Circle);
            GUI.color = old;
        }

        public static void Box(Rect r, Color color, float radius = 0f)
        {
            // Cette surcharge ne convertit pas la couleur en espace linéaire : sans ça, tout sort délavé.
            if (QualitySettings.activeColorSpace == ColorSpace.Linear) color = new Color(color.linear.r, color.linear.g, color.linear.b, color.a);
            GUI.DrawTexture(r, ProceduralTextures.White, ScaleMode.StretchToFill, true, 0f, color, 0f, radius);
        }

        /// <summary>Bouton arrondi avec ombre portée ; <paramref name="focused"/> l'entoure (navigation clavier/manette).</summary>
        public void Button(Rect r, string text, Color color, bool focused = false, GUIStyle style = null)
        {
            float rad = Mathf.Min(r.height * 0.5f, 22f * Scale);
            Box(new Rect(r.x, r.y + 5f * Scale, r.width, r.height), new Color(0f, 0f, 0f, 0.3f), rad);
            if (focused) Box(new Rect(r.x - 5f * Scale, r.y - 5f * Scale, r.width + 10f * Scale, r.height + 10f * Scale), Color.white, rad + 5f * Scale);
            Box(r, color, rad);
            Box(new Rect(r.x + 6f * Scale, r.y + 4f * Scale, r.width - 12f * Scale, r.height * 0.38f), new Color(1f, 1f, 1f, 0.18f), rad * 0.8f);
            Label(r, text, style ?? Medium, Color.white, TextAnchor.MiddleCenter);
        }

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
