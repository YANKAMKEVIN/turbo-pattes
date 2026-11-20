using UnityEngine;
using UnityEngine.InputSystem;

namespace TurboPattes.Game.Kart
{
    /// <summary>
    /// Commandes du joueur : clavier, manette et écran tactile, fusionnés.
    /// Tactile : glisser à gauche pour tourner (joystick horizontal), boutons Dérapage / Objet / Frein à droite,
    /// accélération automatique ; côtés échangés pour les gauchers (réglages).
    /// </summary>
    public sealed class PlayerInput
    {
        /// <summary>Disposition des commandes tactiles, en pixels écran (origine en bas à gauche).</summary>
        public struct TouchLayout
        {
            public float SteerZoneMinX, SteerZoneMaxX;
            public float SteerRange;
            /// <summary>Abscisse du repère « glisser » (côté de la direction).</summary>
            public float SteerHintX;
            public bool LeftHanded;
            public Vector2 Drift, Item, Brake;
            public float DriftR, ItemR, BrakeR;
        }

        public bool TouchMode { get; private set; } = Application.isMobilePlatform;
        public TouchLayout Layout { get; private set; }
        public bool SteerActive { get; private set; }
        public Vector2 SteerAnchor { get; private set; }
        public Vector2 SteerFinger { get; private set; }
        public bool DriftHeld { get; private set; }
        public bool ItemHeld { get; private set; }
        public bool BrakeHeld { get; private set; }
        /// <summary>Relancer la course (R ou Start).</summary>
        public bool RestartPressed { get; private set; }
        /// <summary>Pause (Échap, P ou Start).</summary>
        public bool PausePressed { get; private set; }

        int _steerTouch = -1;
        bool _prevItemTouch;

        public KartInput Read()
        {
            var input = new KartInput();
            RestartPressed = PausePressed = false;
            ComputeLayout();

            var kb = Keyboard.current;
            if (kb != null)
            {
                float steer = 0f;
                if (kb.leftArrowKey.isPressed || kb.aKey.isPressed || kb.qKey.isPressed) steer -= 1f;
                if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) steer += 1f;
                input.Steer += steer;
                if (kb.upArrowKey.isPressed || kb.wKey.isPressed || kb.zKey.isPressed) input.Throttle = 1f;
                if (kb.downArrowKey.isPressed || kb.sKey.isPressed) input.Brake = 1f;
                input.Drift |= kb.spaceKey.isPressed || kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed;
                input.UseItem |= kb.eKey.wasPressedThisFrame || kb.leftCtrlKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
                RestartPressed |= kb.rKey.wasPressedThisFrame;
                PausePressed |= kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame;
                // Sur téléphone, un événement clavier système (réveil, volume) ne doit pas couper l'accélération auto.
                if (kb.anyKey.wasPressedThisFrame && !Application.isMobilePlatform) TouchMode = false;
            }

            var pad = Gamepad.current;
            if (pad != null)
            {
                float stick = pad.leftStick.ReadValue().x;
                if (Mathf.Abs(stick) > 0.15f) input.Steer += stick;
                if (pad.dpad.left.isPressed) input.Steer -= 1f;
                if (pad.dpad.right.isPressed) input.Steer += 1f;
                input.Throttle = Mathf.Max(input.Throttle, pad.rightTrigger.ReadValue(), pad.buttonSouth.isPressed ? 1f : 0f);
                input.Brake = Mathf.Max(input.Brake, pad.leftTrigger.ReadValue(), pad.buttonEast.isPressed ? 1f : 0f);
                input.Drift |= pad.rightShoulder.isPressed || pad.leftShoulder.isPressed;
                input.UseItem |= pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame;
                PausePressed |= pad.startButton.wasPressedThisFrame;
                if (pad.wasUpdatedThisFrame && (pad.buttonSouth.wasPressedThisFrame || Mathf.Abs(stick) > 0.5f)) TouchMode = false;
            }

            ReadTouch(ref input);
            // Hors de ReadTouch : sur Android, Touchscreen.current peut rester nul tant qu'on n'a pas touché l'écran.
            if (TouchMode)
            {
                input.Drift |= DriftHeld;
                if (BrakeHeld) input.Brake = 1f;
                else input.Throttle = Mathf.Max(input.Throttle, 1f);
            }
            input.Steer = Mathf.Clamp(input.Steer, -1f, 1f);
            return input;
        }

        void ComputeLayout()
        {
            var safe = Screen.safeArea;
            // Droitier ou gaucher (réglages) : la disposition est calculée par Core/TouchZones.
            var z = Core.TouchZones.Compute(Screen.width, Screen.height, safe.xMin, safe.xMax, safe.yMin,
                App.GameSession.TouchSensitivity, App.GameSession.LeftHanded);
            Layout = new TouchLayout
            {
                SteerZoneMinX = z.SteerMinX,
                SteerZoneMaxX = z.SteerMaxX,
                SteerRange = z.SteerRange,
                SteerHintX = z.SteerHintX,
                LeftHanded = z.LeftHanded,
                Drift = new Vector2(z.Drift.X, z.Drift.Y),
                DriftR = z.Drift.R,
                Item = new Vector2(z.Item.X, z.Item.Y),
                ItemR = z.Item.R,
                Brake = new Vector2(z.Brake.X, z.Brake.Y),
                BrakeR = z.Brake.R,
            };
        }

        void ReadTouch(ref KartInput input)
        {
            var ts = Touchscreen.current;
            SteerActive = false;
            DriftHeld = ItemHeld = BrakeHeld = false;
            if (ts == null) { _steerTouch = -1; return; }

            bool steerSeen = false, itemTouch = false;
            foreach (var touch in ts.touches)
            {
                if (!touch.press.isPressed) continue;
                TouchMode = true;
                int id = touch.touchId.ReadValue();
                Vector2 pos = touch.position.ReadValue();
                var l = Layout;

                if (id == _steerTouch)
                {
                    steerSeen = true;
                    SteerFinger = pos;
                    continue;
                }
                if (Inside(pos, l.Drift, l.DriftR * 1.15f)) { DriftHeld = true; continue; }
                if (Inside(pos, l.Item, l.ItemR * 1.2f)) { itemTouch = true; continue; }
                if (Inside(pos, l.Brake, l.BrakeR * 1.2f)) { BrakeHeld = true; continue; }
                if (pos.x >= l.SteerZoneMinX && pos.x <= l.SteerZoneMaxX && _steerTouch < 0)
                {
                    _steerTouch = id;
                    SteerAnchor = pos;
                    SteerFinger = pos;
                    steerSeen = true;
                }
            }
            if (!steerSeen) _steerTouch = -1;

            if (_steerTouch >= 0)
            {
                SteerActive = true;
                float dx = SteerFinger.x - SteerAnchor.x;
                // L'ancre suit le doigt s'il dépasse la course, pour pouvoir contre-braquer vite.
                float range = Layout.SteerRange;
                if (Mathf.Abs(dx) > range) SteerAnchor = new Vector2(SteerFinger.x - Mathf.Sign(dx) * range, SteerAnchor.y);
                input.Steer += Mathf.Clamp((SteerFinger.x - SteerAnchor.x) / range, -1f, 1f);
            }

            ItemHeld = itemTouch;
            if (itemTouch && !_prevItemTouch) input.UseItem = true;
            _prevItemTouch = itemTouch;
        }

        static bool Inside(Vector2 p, Vector2 c, float r) => (p - c).sqrMagnitude <= r * r;
    }
}
