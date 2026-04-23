using System;
namespace StardewModdingAPI.Events {
    public interface IInputEvents {
        event EventHandler<ButtonPressedEventArgs>?      ButtonPressed;
        event EventHandler<ButtonReleasedEventArgs>?     ButtonReleased;
        event EventHandler<ButtonsChangedEventArgs>?     ButtonsChanged;
        event EventHandler<CursorMovedEventArgs>?        CursorMoved;
        event EventHandler<MouseWheelScrolledEventArgs>? MouseWheelScrolled;
    }
}
