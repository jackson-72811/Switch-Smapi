using System;
namespace StardewModdingAPI.Events {
    public interface IGameLoopEvents {
        event EventHandler<GameLaunchedEventArgs>?          GameLaunched;
        event EventHandler<UpdateTickingEventArgs>?         UpdateTicking;
        event EventHandler<UpdateTickedEventArgs>?          UpdateTicked;
        event EventHandler<OneSecondUpdateTickingEventArgs>? OneSecondUpdateTicking;
        event EventHandler<OneSecondUpdateTickedEventArgs>?  OneSecondUpdateTicked;
        event EventHandler<SaveCreatingEventArgs>?           SaveCreating;
        event EventHandler<SaveCreatedEventArgs>?            SaveCreated;
        event EventHandler<SavingEventArgs>?                 Saving;
        event EventHandler<SavedEventArgs>?                  Saved;
        event EventHandler<SaveLoadedEventArgs>?             SaveLoaded;
        event EventHandler<DayStartedEventArgs>?             DayStarted;
        event EventHandler<DayEndingEventArgs>?              DayEnding;
        event EventHandler<TimeChangedEventArgs>?            TimeChanged;
        event EventHandler<ReturnedToTitleEventArgs>?        ReturnedToTitle;
    }
}
