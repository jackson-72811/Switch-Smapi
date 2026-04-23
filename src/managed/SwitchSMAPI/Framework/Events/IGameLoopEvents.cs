using System;

namespace SwitchSMAPI.Framework.Events {

    // ── Event argument types ───────────────────────────────────────────────────

    public class GameLaunchedEventArgs   : EventArgs { }
    public class ReturnedToTitleEventArgs : EventArgs { }

    public class UpdateTickingEventArgs : EventArgs {
        public uint Ticks { get; }
        public bool IsOneSecond  => Ticks % 60 == 0;
        public bool IsMultipleOf(uint interval) => Ticks % interval == 0;
        internal UpdateTickingEventArgs(uint ticks) { Ticks = ticks; }
    }

    public class UpdateTickedEventArgs : EventArgs {
        public uint Ticks { get; }
        public bool IsOneSecond  => Ticks % 60 == 0;
        public bool IsMultipleOf(uint interval) => Ticks % interval == 0;
        internal UpdateTickedEventArgs(uint ticks) { Ticks = ticks; }
    }

    public class OneSecondUpdateTickingEventArgs : EventArgs {
        public uint Ticks { get; }
        internal OneSecondUpdateTickingEventArgs(uint ticks) { Ticks = ticks; }
    }

    public class OneSecondUpdateTickedEventArgs : EventArgs {
        public uint Ticks { get; }
        internal OneSecondUpdateTickedEventArgs(uint ticks) { Ticks = ticks; }
    }

    public class SaveCreatingEventArgs  : EventArgs { }
    public class SaveCreatedEventArgs   : EventArgs { }
    public class SavingEventArgs        : EventArgs { }
    public class SavedEventArgs         : EventArgs { }
    public class SaveLoadedEventArgs    : EventArgs { }

    public class DayStartedEventArgs    : EventArgs { }

    public class DayEndingEventArgs     : EventArgs { }

    public class TimeChangedEventArgs : EventArgs {
        public int OldTime { get; }
        public int NewTime { get; }
        internal TimeChangedEventArgs(int oldTime, int newTime) {
            OldTime = oldTime; NewTime = newTime;
        }
    }

    // ── Interface ──────────────────────────────────────────────────────────────

    /// <summary>Events related to the game loop.</summary>
    public interface IGameLoopEvents {
        /// <summary>Raised after all mods are loaded and the title screen is shown for the first time.</summary>
        event EventHandler<GameLaunchedEventArgs>? GameLaunched;

        /// <summary>Raised before each game update tick.</summary>
        event EventHandler<UpdateTickingEventArgs>? UpdateTicking;

        /// <summary>Raised after each game update tick.</summary>
        event EventHandler<UpdateTickedEventArgs>? UpdateTicked;

        /// <summary>Raised before update ticks that are a multiple of 60 (approx. once per second).</summary>
        event EventHandler<OneSecondUpdateTickingEventArgs>? OneSecondUpdateTicking;

        /// <summary>Raised after update ticks that are a multiple of 60.</summary>
        event EventHandler<OneSecondUpdateTickedEventArgs>? OneSecondUpdateTicked;

        /// <summary>Raised before a new save file is created.</summary>
        event EventHandler<SaveCreatingEventArgs>? SaveCreating;

        /// <summary>Raised after a new save file is created.</summary>
        event EventHandler<SaveCreatedEventArgs>? SaveCreated;

        /// <summary>Raised before the game writes save data to disk.</summary>
        event EventHandler<SavingEventArgs>? Saving;

        /// <summary>Raised after the game writes save data to disk.</summary>
        event EventHandler<SavedEventArgs>? Saved;

        /// <summary>Raised after a save file is loaded.</summary>
        event EventHandler<SaveLoadedEventArgs>? SaveLoaded;

        /// <summary>Raised after the day begins (after waking up / after the previous day's save).</summary>
        event EventHandler<DayStartedEventArgs>? DayStarted;

        /// <summary>Raised before the day ends (before the player sleeps / before saving).</summary>
        event EventHandler<DayEndingEventArgs>? DayEnding;

        /// <summary>Raised when in-game time changes (every 10 in-game minutes).</summary>
        event EventHandler<TimeChangedEventArgs>? TimeChanged;

        /// <summary>Raised when the player returns to the title screen.</summary>
        event EventHandler<ReturnedToTitleEventArgs>? ReturnedToTitle;
    }
}
