using System;
using Microsoft.Xna.Framework;
namespace StardewModdingAPI.Events {
    public class LoadStageChangedEventArgs : EventArgs {
        public LoadStage OldStage { get; }
        public LoadStage NewStage { get; }
        public LoadStageChangedEventArgs(LoadStage old, LoadStage @new) {
            OldStage = old; NewStage = @new;
        }
    }
    public class UnvalidatedUpdateTickingEventArgs : EventArgs {
        public uint Ticks { get; }
        public bool IsOneSecond => Ticks % 60 == 0;
        public bool IsMultipleOf(uint n) => n != 0 && Ticks % n == 0;
        public UnvalidatedUpdateTickingEventArgs(uint ticks) { Ticks = ticks; }
    }
    public class UnvalidatedUpdateTickedEventArgs : EventArgs {
        public uint Ticks { get; }
        public bool IsOneSecond => Ticks % 60 == 0;
        public bool IsMultipleOf(uint n) => n != 0 && Ticks % n == 0;
        public UnvalidatedUpdateTickedEventArgs(uint ticks) { Ticks = ticks; }
    }
    public enum LoadStage {
        None, CreatedBasicInfo, SaveParsed, SaveLoadedBasicInfo,
        Loaded, Ready, ReturningToTitle,
    }
    public interface ISpecializedEvents {
        event EventHandler<LoadStageChangedEventArgs>?         LoadStageChanged;
        event EventHandler<UnvalidatedUpdateTickingEventArgs>? UnvalidatedUpdateTicking;
        event EventHandler<UnvalidatedUpdateTickedEventArgs>?  UnvalidatedUpdateTicked;
    }
}
