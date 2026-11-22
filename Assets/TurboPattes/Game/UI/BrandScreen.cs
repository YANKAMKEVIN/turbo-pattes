using System;
using TurboPattes.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TurboPattes.Game.UI
{
    /// <summary>
    /// Écran de marque, juste après le splash Unity (<see cref="BrandSplash"/>) : fond carbone plein, logo « Turbo Grid »
    /// du titre (<see cref="UiKit.Logo"/>) qui apparaît en se posant, accroche en Saira Condensed et trait rouge qui file,
    /// puis fondu sur le survol du circuit. Toucher l'écran, Entrée ou A passe au fondu. Pendant qu'il est affiché,
    /// <see cref="IsOpen"/> dit au menu d'ignorer les commandes et de ne pas dessiner l'écran titre.
    /// </summary>
    public sealed class BrandScreen : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        readonly UiKit _ui = new();
        Action _finished;
        float _startedAt;

        /// <summary>Affiche l'écran ; <paramref name="finished"/> est appelé quand il a disparu.</summary>
        public static BrandScreen Show(Action finished = null)
        {
            var screen = new GameObject("BrandScreen").AddComponent<BrandScreen>();
            screen._finished = finished;
            screen._startedAt = Time.unscaledTime;
            IsOpen = true;
            return screen;
        }

        void OnDestroy() => IsOpen = false;

        float Elapsed => Time.unscaledTime - _startedAt;

        void Update()
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            bool skip = kb != null && (kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame)
                        || pad != null && (pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame)
                        || UiKit.Tap().HasValue;
            if (skip) _startedAt = Time.unscaledTime - BrandSplash.Skip(Elapsed);
            if (!BrandSplash.Done(Elapsed)) return;
            _finished?.Invoke();
            _finished = null;
            Destroy(gameObject);
        }

        void OnGUI()
        {
            GUI.depth = -20;
            _ui.EnsureStyles();
            float t = Elapsed, s = _ui.Scale, w = Screen.width, h = Screen.height;
            UiKit.Box(new Rect(0, 0, w, h), UiKit.Alpha(UiKit.Carbon, BrandSplash.BackdropAlpha(t)));

            float a = BrandSplash.LogoAlpha(t);
            if (a <= 0f) return;
            // Même logo et même accroche que l'écran titre, un peu plus haut que le centre (masse visuelle du logo).
            var pivot = new Vector2(w * 0.5f, h * 0.44f);
            var m = GUI.matrix;
            float scale = BrandSplash.LogoScale(t);
            GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), pivot);
            _ui.Logo(pivot, s, a);
            GUI.matrix = m;

            var tagline = new Rect(w * 0.5f - 290 * s, pivot.y + 122 * s, 580 * s, 44 * s);
            _ui.Label(tagline, Brand.Tagline.ToUpperInvariant(), _ui.Medium, UiKit.Alpha(UiKit.Chalk, a), TextAnchor.MiddleCenter);
            // Trait rouge qui file de gauche à droite sous l'accroche, comme une ligne de départ.
            float sweep = BrandSplash.Sweep(t);
            UiKit.Box(new Rect(tagline.x, tagline.yMax + 6 * s, tagline.width * sweep, 5 * s), UiKit.Alpha(UiKit.Red, a));
        }
    }
}
