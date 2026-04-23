using System;
namespace StardewModdingAPI.Events {
    public interface IWorldEvents {
        event EventHandler<LocationListChangedEventArgs>?           LocationListChanged;
        event EventHandler<BuildingListChangedEventArgs>?           BuildingListChanged;
        event EventHandler<DebrisListChangedEventArgs>?             DebrisListChanged;
        event EventHandler<FurnitureListChangedEventArgs>?          FurnitureListChanged;
        event EventHandler<LargeTerrainFeatureListChangedEventArgs>? LargeTerrainFeatureListChanged;
        event EventHandler<NpcListChangedEventArgs>?                NpcListChanged;
        event EventHandler<ObjectListChangedEventArgs>?             ObjectListChanged;
        event EventHandler<TerrainFeatureListChangedEventArgs>?     TerrainFeatureListChanged;
        event EventHandler<ChestInventoryChangedEventArgs>?         ChestInventoryChanged;
    }
}
