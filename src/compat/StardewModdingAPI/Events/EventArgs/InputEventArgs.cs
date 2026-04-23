using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StardewModdingAPI.Events {

    public class ButtonPressedEventArgs : EventArgs {
        public SButton         Button { get; }
        public ICursorPosition Cursor { get; }
        public ButtonPressedEventArgs(SButton button, ICursorPosition cursor) {
            Button = button; Cursor = cursor;
        }
    }

    public class ButtonReleasedEventArgs : EventArgs {
        public SButton         Button { get; }
        public ICursorPosition Cursor { get; }
        public ButtonReleasedEventArgs(SButton button, ICursorPosition cursor) {
            Button = button; Cursor = cursor;
        }
    }

    public class ButtonsChangedEventArgs : EventArgs {
        public ICursorPosition             Cursor   { get; }
        public IReadOnlySet<SButton>       Pressed  { get; }
        public IReadOnlySet<SButton>       Released { get; }
        public IReadOnlySet<SButton>       Held     { get; }
        public ButtonsChangedEventArgs(ICursorPosition cursor,
                                        IEnumerable<SButton> pressed,
                                        IEnumerable<SButton> released,
                                        IEnumerable<SButton> held) {
            Cursor   = cursor;
            Pressed  = new HashSet<SButton>(pressed);
            Released = new HashSet<SButton>(released);
            Held     = new HashSet<SButton>(held);
        }
    }

    public class CursorMovedEventArgs : EventArgs {
        public ICursorPosition OldPosition { get; }
        public ICursorPosition NewPosition { get; }
        public CursorMovedEventArgs(ICursorPosition old, ICursorPosition @new) {
            OldPosition = old; NewPosition = @new;
        }
    }

    public class MouseWheelScrolledEventArgs : EventArgs {
        public ICursorPosition Position { get; }
        public int OldValue { get; }
        public int NewValue { get; }
        public int Delta    => NewValue - OldValue;
        public MouseWheelScrolledEventArgs(ICursorPosition pos, int oldVal, int newVal) {
            Position = pos; OldValue = oldVal; NewValue = newVal;
        }
    }

    public interface ICursorPosition {
        Vector2 ScreenPixels   { get; }
        Vector2 Tile           { get; }
        Vector2 AbsolutePixels { get; }
        ICursorPosition Clone();
    }

    // IReadOnlySet polyfill for netstandard2.0
    public interface IReadOnlySet<T> : IReadOnlyCollection<T> {
        bool Contains(T item);
    }
}
