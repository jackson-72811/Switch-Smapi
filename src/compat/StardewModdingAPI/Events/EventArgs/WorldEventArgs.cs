using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace StardewModdingAPI.Events {

    public class LocationListChangedEventArgs : EventArgs {
        public IReadOnlyList<StardewValley.GameLocation> Added   { get; }
        public IReadOnlyList<StardewValley.GameLocation> Removed { get; }
        public LocationListChangedEventArgs(IEnumerable<StardewValley.GameLocation> added,
                                            IEnumerable<StardewValley.GameLocation> removed) {
            Added   = new List<StardewValley.GameLocation>(added);
            Removed = new List<StardewValley.GameLocation>(removed);
        }
    }

    public class NpcListChangedEventArgs : EventArgs {
        public StardewValley.GameLocation      Location { get; }
        public IReadOnlyList<StardewValley.NPC> Added   { get; }
        public IReadOnlyList<StardewValley.NPC> Removed { get; }
        public NpcListChangedEventArgs(StardewValley.GameLocation loc,
                                       IEnumerable<StardewValley.NPC> added,
                                       IEnumerable<StardewValley.NPC> removed) {
            Location = loc;
            Added    = new List<StardewValley.NPC>(added);
            Removed  = new List<StardewValley.NPC>(removed);
        }
    }

    public class ObjectListChangedEventArgs : EventArgs {
        public StardewValley.GameLocation Location { get; }
        public IReadOnlyList<KeyValuePair<Vector2, StardewValley.Object>> Added   { get; }
        public IReadOnlyList<KeyValuePair<Vector2, StardewValley.Object>> Removed { get; }
        public ObjectListChangedEventArgs(StardewValley.GameLocation loc,
                                          IEnumerable<KeyValuePair<Vector2, StardewValley.Object>> added,
                                          IEnumerable<KeyValuePair<Vector2, StardewValley.Object>> removed) {
            Location = loc;
            Added    = new List<KeyValuePair<Vector2, StardewValley.Object>>(added);
            Removed  = new List<KeyValuePair<Vector2, StardewValley.Object>>(removed);
        }
    }

    public class ChestInventoryChangedEventArgs : EventArgs {
        public StardewValley.Objects.Chest           Chest    { get; }
        public StardewValley.GameLocation            Location { get; }
        public IReadOnlyList<StardewValley.Item>     Added           { get; }
        public IReadOnlyList<StardewValley.Item>     Removed         { get; }
        public IReadOnlyList<ItemStackSizeChange>    QuantityChanged { get; }
        public ChestInventoryChangedEventArgs(StardewValley.Objects.Chest chest,
                                              StardewValley.GameLocation loc,
                                              IEnumerable<StardewValley.Item> added,
                                              IEnumerable<StardewValley.Item> removed,
                                              IEnumerable<ItemStackSizeChange> qChanged) {
            Chest           = chest;
            Location        = loc;
            Added           = new List<StardewValley.Item>(added);
            Removed         = new List<StardewValley.Item>(removed);
            QuantityChanged = new List<ItemStackSizeChange>(qChanged);
        }
    }

    public class ItemStackSizeChange {
        public StardewValley.Item Item     { get; }
        public int                OldSize  { get; }
        public int                NewSize  { get; }
        public ItemStackSizeChange(StardewValley.Item item, int oldSize, int newSize) {
            Item = item; OldSize = oldSize; NewSize = newSize;
        }
    }

    public class TerrainFeatureListChangedEventArgs : EventArgs {
        public StardewValley.GameLocation Location { get; }
        public IReadOnlyList<KeyValuePair<Vector2, StardewValley.TerrainFeature>> Added   { get; }
        public IReadOnlyList<KeyValuePair<Vector2, StardewValley.TerrainFeature>> Removed { get; }
        public TerrainFeatureListChangedEventArgs(
            StardewValley.GameLocation loc,
            IEnumerable<KeyValuePair<Vector2, StardewValley.TerrainFeature>> added,
            IEnumerable<KeyValuePair<Vector2, StardewValley.TerrainFeature>> removed) {
            Location = loc;
            Added    = new List<KeyValuePair<Vector2, StardewValley.TerrainFeature>>(added);
            Removed  = new List<KeyValuePair<Vector2, StardewValley.TerrainFeature>>(removed);
        }
    }

    public class FurnitureListChangedEventArgs : EventArgs {
        public StardewValley.GameLocation              Location { get; }
        public IReadOnlyList<StardewValley.Objects.Furniture> Added   { get; }
        public IReadOnlyList<StardewValley.Objects.Furniture> Removed { get; }
        public FurnitureListChangedEventArgs(StardewValley.GameLocation loc,
                                             IEnumerable<StardewValley.Objects.Furniture> added,
                                             IEnumerable<StardewValley.Objects.Furniture> removed) {
            Location = loc;
            Added    = new List<StardewValley.Objects.Furniture>(added);
            Removed  = new List<StardewValley.Objects.Furniture>(removed);
        }
    }

    public class DebrisListChangedEventArgs : EventArgs {
        public StardewValley.GameLocation            Location { get; }
        public IReadOnlyList<StardewValley.Debris>   Added   { get; }
        public IReadOnlyList<StardewValley.Debris>   Removed { get; }
        public DebrisListChangedEventArgs(StardewValley.GameLocation loc,
                                          IEnumerable<StardewValley.Debris> added,
                                          IEnumerable<StardewValley.Debris> removed) {
            Location = loc;
            Added    = new List<StardewValley.Debris>(added);
            Removed  = new List<StardewValley.Debris>(removed);
        }
    }

    public class LargeTerrainFeatureListChangedEventArgs : EventArgs {
        public StardewValley.GameLocation Location { get; }
        public IReadOnlyList<StardewValley.TerrainFeature> Added   { get; }
        public IReadOnlyList<StardewValley.TerrainFeature> Removed { get; }
        public LargeTerrainFeatureListChangedEventArgs(
            StardewValley.GameLocation loc,
            IEnumerable<StardewValley.TerrainFeature> added,
            IEnumerable<StardewValley.TerrainFeature> removed) {
            Location = loc;
            Added    = new List<StardewValley.TerrainFeature>(added);
            Removed  = new List<StardewValley.TerrainFeature>(removed);
        }
    }

    public class BuildingListChangedEventArgs : EventArgs {
        public StardewValley.GameLocation          Location { get; }
        public IReadOnlyList<StardewValley.Buildings.Building> Added   { get; }
        public IReadOnlyList<StardewValley.Buildings.Building> Removed { get; }
        public BuildingListChangedEventArgs(StardewValley.GameLocation loc,
                                            IEnumerable<StardewValley.Buildings.Building> added,
                                            IEnumerable<StardewValley.Buildings.Building> removed) {
            Location = loc;
            Added    = new List<StardewValley.Buildings.Building>(added);
            Removed  = new List<StardewValley.Buildings.Building>(removed);
        }
    }
}
