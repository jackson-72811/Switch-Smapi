using System.Collections.Generic;
using StardewModdingAPI.Events;

namespace StardewModdingAPI {
    public interface IInputHelper {
        ICursorPosition   GetCursorPosition();
        bool              IsDown(SButton button);
        bool              IsSuppressed(SButton button);
        void              Suppress(SButton button);
        SButtonState      GetState(SButton button);
        IEnumerable<SButton> GetPressedButtons();
    }

    public enum SButtonState {
        None     = 0,
        Pressed  = 1,
        Held     = 2,
        Released = 3,
    }
}
