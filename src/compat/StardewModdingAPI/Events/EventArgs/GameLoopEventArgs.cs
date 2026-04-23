using System;

namespace StardewModdingAPI.Events {

    public class GameLaunchedEventArgs    : EventArgs { }
    public class ReturnedToTitleEventArgs : EventArgs { }
    public class SaveCreatingEventArgs    : EventArgs { }
    public class SaveCreatedEventArgs     : EventArgs { }
    public class SavingEventArgs          : EventArgs { }
    public class SavedEventArgs           : EventArgs { }
    public class SaveLoadedEventArgs      : EventArgs { }
    public class DayStartedEventArgs      : EventArgs { }
    public class DayEndingEventArgs       : EventArgs { }

    public class UpdateTickingEventArgs : EventArgs {
        public uint Ticks { get; }
        public bool IsOneSecond => Ticks % 60 == 0;
        public bool IsMultipleOf(uint interval) => interval != 0 && Ticks % interval == 0;
        public UpdateTickingEventArgs(uint ticks) { Ticks = ticks; }
    }

    public class UpdateTickedEventArgs : EventArgs {
        public uint Ticks { get; }
        public bool IsOneSecond => Ticks % 60 == 0;
        public bool IsMultipleOf(uint interval) => interval != 0 && Ticks % interval == 0;
        public UpdateTickedEventArgs(uint ticks) { Ticks = ticks; }
    }

    public class OneSecondUpdateTickingEventArgs : EventArgs {
        public uint Ticks { get; }
        public OneSecondUpdateTickingEventArgs(uint ticks) { Ticks = ticks; }
    }

    public class OneSecondUpdateTickedEventArgs : EventArgs {
        public uint Ticks { get; }
        public OneSecondUpdateTickedEventArgs(uint ticks) { Ticks = ticks; }
    }

    public class TimeChangedEventArgs : EventArgs {
        public int OldTime { get; }
        public int NewTime { get; }
        public TimeChangedEventArgs(int oldTime, int newTime) {
            OldTime = oldTime; NewTime = newTime;
        }
    }
}
