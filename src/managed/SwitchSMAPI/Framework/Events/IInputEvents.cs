using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace SwitchSMAPI.Framework.Events {

    // ── SButton: abstracted button type ──────────────────────────────────────
    //
    // Mirrors desktop SMAPI's SButton enum.  Switch-specific values are
    // in the range 0xA000–0xA0FF.

    public enum SButton {
        None               = 0,

        // Keyboard (not typically available on Switch handheld/tabletop)
        A = Keys.A, B = Keys.B, C = Keys.C, D = Keys.D, E = Keys.E,
        F = Keys.F, G = Keys.G, H = Keys.H, I = Keys.I, J = Keys.J,
        K = Keys.K, L = Keys.L, M = Keys.M, N = Keys.N, O = Keys.O,
        P = Keys.P, Q = Keys.Q, R = Keys.R, S = Keys.S, T = Keys.T,
        U = Keys.U, V = Keys.V, W = Keys.W, X = Keys.X, Y = Keys.Y,
        Z = Keys.Z,
        Escape = Keys.Escape, Enter = Keys.Enter, Space = Keys.Space,
        Left = Keys.Left, Right = Keys.Right, Up = Keys.Up, Down = Keys.Down,

        // Gamepad (matches XNA Buttons enum offset by 0x2000)
        ControllerA             = 0x2001,
        ControllerB             = 0x2002,
        ControllerX             = 0x2003,
        ControllerY             = 0x2004,
        ControllerBack          = 0x2005,
        ControllerStart         = 0x2006,
        ControllerLeftTrigger   = 0x2007,
        ControllerRightTrigger  = 0x2008,
        ControllerLeftShoulder  = 0x2009,
        ControllerRightShoulder = 0x200A,
        ControllerDPadLeft      = 0x200B,
        ControllerDPadRight     = 0x200C,
        ControllerDPadUp        = 0x200D,
        ControllerDPadDown      = 0x200E,
        ControllerLeftStick     = 0x200F,
        ControllerRightStick    = 0x2010,

        // Switch-specific buttons (Joy-Con extras)
        ControllerMinus         = 0xA001,
        ControllerPlus          = 0xA002,
        ControllerCapture       = 0xA003,
        ControllerHome          = 0xA004,

        // Mouse (docked mode)
        MouseLeft    = 0x3001,
        MouseRight   = 0x3002,
        MouseMiddle  = 0x3003,
        MouseX1      = 0x3004,
        MouseX2      = 0x3005,
    }

    // ── Event argument types ──────────────────────────────────────────────────

    public class ButtonPressedEventArgs : EventArgs {
        public SButton Button { get; }
        public ICursorPosition Cursor { get; }
        internal ButtonPressedEventArgs(SButton button, ICursorPosition cursor) {
            Button = button; Cursor = cursor;
        }
    }

    public class ButtonReleasedEventArgs : EventArgs {
        public SButton Button { get; }
        public ICursorPosition Cursor { get; }
        internal ButtonReleasedEventArgs(SButton button, ICursorPosition cursor) {
            Button = button; Cursor = cursor;
        }
    }

    public class ButtonsChangedEventArgs : EventArgs {
        public ICursorPosition Cursor  { get; }
        public IReadOnlyCollection<SButton> Pressed  { get; }
        public IReadOnlyCollection<SButton> Released { get; }
        public IReadOnlyCollection<SButton> Held     { get; }
        internal ButtonsChangedEventArgs(
            ICursorPosition cursor,
            IEnumerable<SButton> pressed,
            IEnumerable<SButton> released,
            IEnumerable<SButton> held)
        {
            Cursor   = cursor;
            Pressed  = new List<SButton>(pressed);
            Released = new List<SButton>(released);
            Held     = new List<SButton>(held);
        }
    }

    public class CursorMovedEventArgs : EventArgs {
        public ICursorPosition OldPosition { get; }
        public ICursorPosition NewPosition { get; }
        internal CursorMovedEventArgs(ICursorPosition old, ICursorPosition @new) {
            OldPosition = old; NewPosition = @new;
        }
    }

    public class MouseWheelScrolledEventArgs : EventArgs {
        public ICursorPosition Position { get; }
        public int OldValue { get; }
        public int NewValue { get; }
        public int Delta    => NewValue - OldValue;
        internal MouseWheelScrolledEventArgs(ICursorPosition pos, int oldVal, int newVal) {
            Position = pos; OldValue = oldVal; NewValue = newVal;
        }
    }

    /// <summary>Represents the cursor's position in multiple coordinate spaces.</summary>
    public interface ICursorPosition {
        /// <summary>Screen pixel position.</summary>
        Vector2 ScreenPixels { get; }
        /// <summary>Tile position in the current location.</summary>
        Vector2 Tile         { get; }
        /// <summary>World-space pixel position.</summary>
        Vector2 AbsolutePixels { get; }
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>Events related to player input.</summary>
    public interface IInputEvents {
        event EventHandler<ButtonPressedEventArgs>?    ButtonPressed;
        event EventHandler<ButtonReleasedEventArgs>?   ButtonReleased;
        event EventHandler<ButtonsChangedEventArgs>?   ButtonsChanged;
        event EventHandler<CursorMovedEventArgs>?      CursorMoved;
        event EventHandler<MouseWheelScrolledEventArgs>? MouseWheelScrolled;
    }
}
