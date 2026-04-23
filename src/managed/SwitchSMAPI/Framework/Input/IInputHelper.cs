using SwitchSMAPI.Framework.Events;

namespace SwitchSMAPI.Framework.Input {

    /// <summary>Provides the current state of player input.</summary>
    public interface IInputHelper {

        /// <summary>The current cursor position in multiple coordinate spaces.</summary>
        ICursorPosition GetCursorPosition();

        /// <summary>True if the given button is currently held down.</summary>
        bool IsDown(SButton button);

        /// <summary>True if the given button was just pressed this tick.</summary>
        bool IsPressedThisTick(SButton button);

        /// <summary>True if the given button is suppressed (won't be passed to the game).</summary>
        bool IsSuppressed(SButton button);

        /// <summary>Suppress a button so the game doesn't see it this tick.</summary>
        void Suppress(SButton button);

        /// <summary>Get all buttons currently held down.</summary>
        System.Collections.Generic.IEnumerable<SButton> GetPressedButtons();
    }
}
