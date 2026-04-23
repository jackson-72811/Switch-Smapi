using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SwitchSMAPI.Framework.Events;

namespace SwitchSMAPI.Framework.Input {

    /// <inheritdoc cref="IInputHelper"/>
    public class InputHelper : IInputHelper {

        private KeyboardState    _lastKeyboard;
        private GamePadState     _lastGamepad;
        private MouseState       _lastMouse;
        private KeyboardState    _curKeyboard;
        private GamePadState     _curGamepad;
        private MouseState       _curMouse;
        private readonly HashSet<SButton> _suppressedThisTick = new HashSet<SButton>();

        // Called by the Harmony UpdateTicking patch before raising UpdateTicking event
        internal void BeginTick() {
            _lastKeyboard = _curKeyboard;
            _lastGamepad  = _curGamepad;
            _lastMouse    = _curMouse;

            _curKeyboard  = Keyboard.GetState();
            _curGamepad   = GamePad.GetState(PlayerIndex.One);
            _curMouse     = Mouse.GetState();
            _suppressedThisTick.Clear();
        }

        public ICursorPosition GetCursorPosition() {
            var screenPos = new Vector2(_curMouse.X, _curMouse.Y);
            return new CursorPosition(screenPos);
        }

        public bool IsDown(SButton button) {
            if (_suppressedThisTick.Contains(button)) return false;

            if (TryGetKey(button, out Keys key))
                return _curKeyboard.IsKeyDown(key);

            if (TryGetButton(button, out Buttons btn))
                return _curGamepad.IsButtonDown(btn);

            if (button == SButton.MouseLeft)   return _curMouse.LeftButton   == ButtonState.Pressed;
            if (button == SButton.MouseRight)  return _curMouse.RightButton  == ButtonState.Pressed;
            if (button == SButton.MouseMiddle) return _curMouse.MiddleButton == ButtonState.Pressed;

            return false;
        }

        public bool IsPressedThisTick(SButton button) {
            if (_suppressedThisTick.Contains(button)) return false;

            if (TryGetKey(button, out Keys key))
                return _curKeyboard.IsKeyDown(key) && !_lastKeyboard.IsKeyDown(key);

            if (TryGetButton(button, out Buttons btn))
                return _curGamepad.IsButtonDown(btn) && !_lastGamepad.IsButtonDown(btn);

            if (button == SButton.MouseLeft)
                return _curMouse.LeftButton == ButtonState.Pressed &&
                       _lastMouse.LeftButton == ButtonState.Released;

            return false;
        }

        public bool IsSuppressed(SButton button) => _suppressedThisTick.Contains(button);

        public void Suppress(SButton button) => _suppressedThisTick.Add(button);

        public IEnumerable<SButton> GetPressedButtons() {
            var result = new List<SButton>();

            // Keyboard
            foreach (Keys key in _curKeyboard.GetPressedKeys()) {
                if (TryGetSButton(key, out SButton sb) && !IsSuppressed(sb))
                    result.Add(sb);
            }

            // Gamepad
            foreach (Buttons btn in GetPressedGamepadButtons()) {
                if (TryGetSButtonFromGamepad(btn, out SButton sb) && !IsSuppressed(sb))
                    result.Add(sb);
            }

            return result;
        }

        // ── Mapping helpers ────────────────────────────────────────────────────

        private static bool TryGetKey(SButton button, out Keys key) {
            int v = (int)button;
            if (v > 0 && v < 0x2000) { key = (Keys)v; return true; }
            key = Keys.None;
            return false;
        }

        private static bool TryGetSButton(Keys key, out SButton button) {
            button = (SButton)(int)key;
            return (int)key > 0 && (int)key < 0x2000;
        }

        private static bool TryGetButton(SButton sb, out Buttons btn) {
            btn = default;
            int v = (int)sb - 0x2001;
            if (v < 0 || v >= s_GamepadMap.Length) return false;
            btn = s_GamepadMap[v];
            return btn != 0;
        }

        private static bool TryGetSButtonFromGamepad(Buttons btn, out SButton sb) {
            for (int i = 0; i < s_GamepadMap.Length; ++i) {
                if (s_GamepadMap[i] == btn) {
                    sb = (SButton)(i + 0x2001);
                    return true;
                }
            }
            sb = SButton.None;
            return false;
        }

        private static readonly Buttons[] s_GamepadMap = {
            Buttons.A, Buttons.B, Buttons.X, Buttons.Y,
            Buttons.Back, Buttons.Start,
            Buttons.LeftTrigger, Buttons.RightTrigger,
            Buttons.LeftShoulder, Buttons.RightShoulder,
            Buttons.DPadLeft, Buttons.DPadRight, Buttons.DPadUp, Buttons.DPadDown,
            Buttons.LeftStick, Buttons.RightStick,
        };

        private static IEnumerable<Buttons> GetPressedGamepadButtons() {
            var result = new List<Buttons>();
            foreach (Buttons b in Enum.GetValues(typeof(Buttons)))
                if (b != 0 && GamePad.GetState(PlayerIndex.One).IsButtonDown(b))
                    result.Add(b);
            return result;
        }

        // ── ICursorPosition implementation ─────────────────────────────────────

        private sealed class CursorPosition : ICursorPosition {
            public Vector2 ScreenPixels   { get; }
            public Vector2 Tile           => new Vector2(
                (float)Math.Floor(ScreenPixels.X / 64f),
                (float)Math.Floor(ScreenPixels.Y / 64f));
            public Vector2 AbsolutePixels => ScreenPixels; // simplified — no viewport offset
            public CursorPosition(Vector2 screen) { ScreenPixels = screen; }
        }
    }
}
