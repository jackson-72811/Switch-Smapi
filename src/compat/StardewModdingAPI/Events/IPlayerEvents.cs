using System;
namespace StardewModdingAPI.Events {
    public interface IPlayerEvents {
        event EventHandler<InventoryChangedEventArgs>? InventoryChanged;
        event EventHandler<LevelChangedEventArgs>?     LevelChanged;
        event EventHandler<WarpedEventArgs>?           Warped;
    }
}
