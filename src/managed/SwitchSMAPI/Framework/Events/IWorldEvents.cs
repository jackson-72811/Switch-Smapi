using System;
using System.Collections.Generic;

namespace SwitchSMAPI.Framework.Events {

    // ── Helper types ──────────────────────────────────────────────────────────

    /// <summary>A pair of collections describing what was added and removed.</summary>
    public class ChangeSet<T> {
        public IReadOnlyCollection<T> Added   { get; }
        public IReadOnlyCollection<T> Removed { get; }
        internal ChangeSet(IEnumerable<T> added, IEnumerable<T> removed) {
            Added   = new List<T>(added);
            Removed = new List<T>(removed);
        }
    }

    // ── Event argument types ──────────────────────────────────────────────────

    public class LocationListChangedEventArgs : EventArgs {
        public IReadOnlyCollection<object> Added   { get; }
        public IReadOnlyCollection<object> Removed { get; }
        internal LocationListChangedEventArgs(IEnumerable<object> added, IEnumerable<object> removed) {
            Added   = new List<object>(added);
            Removed = new List<object>(removed);
        }
    }

    public class NpcListChangedEventArgs : EventArgs {
        public object Location { get; }
        public IReadOnlyCollection<object> Added   { get; }
        public IReadOnlyCollection<object> Removed { get; }
        internal NpcListChangedEventArgs(object location, IEnumerable<object> added, IEnumerable<object> removed) {
            Location = location;
            Added    = new List<object>(added);
            Removed  = new List<object>(removed);
        }
    }

    public class ObjectListChangedEventArgs : EventArgs {
        public object Location { get; }
        public IReadOnlyCollection<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>> Added   { get; }
        public IReadOnlyCollection<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>> Removed { get; }
        internal ObjectListChangedEventArgs(
            object location,
            IEnumerable<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>> added,
            IEnumerable<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>> removed)
        {
            Location = location;
            Added    = new List<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>>(added);
            Removed  = new List<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>>(removed);
        }
    }

    public class ChestInventoryChangedEventArgs : EventArgs {
        public object Chest    { get; }
        public object Location { get; }
        public IReadOnlyCollection<object> Added   { get; }
        public IReadOnlyCollection<object> Removed { get; }
        public IReadOnlyCollection<object> QuantityChanged { get; }
        internal ChestInventoryChangedEventArgs(
            object chest, object location,
            IEnumerable<object> added,
            IEnumerable<object> removed,
            IEnumerable<object> quantityChanged)
        {
            Chest           = chest;
            Location        = location;
            Added           = new List<object>(added);
            Removed         = new List<object>(removed);
            QuantityChanged = new List<object>(quantityChanged);
        }
    }

    public class TerrainFeatureListChangedEventArgs : EventArgs {
        public object Location { get; }
        public IReadOnlyCollection<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>> Added   { get; }
        public IReadOnlyCollection<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>> Removed { get; }
        internal TerrainFeatureListChangedEventArgs(
            object location,
            IEnumerable<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>> added,
            IEnumerable<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>> removed)
        {
            Location = location;
            Added    = new List<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>>(added);
            Removed  = new List<KeyValuePair<Microsoft.Xna.Framework.Vector2, object>>(removed);
        }
    }

    public class FurnitureListChangedEventArgs : EventArgs {
        public object Location { get; }
        public IReadOnlyCollection<object> Added   { get; }
        public IReadOnlyCollection<object> Removed { get; }
        internal FurnitureListChangedEventArgs(object location, IEnumerable<object> added, IEnumerable<object> removed) {
            Location = location;
            Added    = new List<object>(added);
            Removed  = new List<object>(removed);
        }
    }

    public class DebrisListChangedEventArgs : EventArgs {
        public object Location { get; }
        public IReadOnlyCollection<object> Added   { get; }
        public IReadOnlyCollection<object> Removed { get; }
        internal DebrisListChangedEventArgs(object location, IEnumerable<object> added, IEnumerable<object> removed) {
            Location = location;
            Added    = new List<object>(added);
            Removed  = new List<object>(removed);
        }
    }

    public class LargeTerrainFeatureListChangedEventArgs : EventArgs {
        public object Location { get; }
        public IReadOnlyCollection<object> Added   { get; }
        public IReadOnlyCollection<object> Removed { get; }
        internal LargeTerrainFeatureListChangedEventArgs(object location, IEnumerable<object> added, IEnumerable<object> removed) {
            Location = location;
            Added    = new List<object>(added);
            Removed  = new List<object>(removed);
        }
    }

    public class BuildingListChangedEventArgs : EventArgs {
        public object Location { get; }
        public IReadOnlyCollection<object> Added   { get; }
        public IReadOnlyCollection<object> Removed { get; }
        internal BuildingListChangedEventArgs(object location, IEnumerable<object> added, IEnumerable<object> removed) {
            Location = location;
            Added    = new List<object>(added);
            Removed  = new List<object>(removed);
        }
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>Events related to the game world.</summary>
    public interface IWorldEvents {
        event EventHandler<LocationListChangedEventArgs>?         LocationListChanged;
        event EventHandler<NpcListChangedEventArgs>?              NpcListChanged;
        event EventHandler<ObjectListChangedEventArgs>?           ObjectListChanged;
        event EventHandler<ChestInventoryChangedEventArgs>?       ChestInventoryChanged;
        event EventHandler<TerrainFeatureListChangedEventArgs>?   TerrainFeatureListChanged;
        event EventHandler<FurnitureListChangedEventArgs>?        FurnitureListChanged;
        event EventHandler<DebrisListChangedEventArgs>?           DebrisListChanged;
        event EventHandler<LargeTerrainFeatureListChangedEventArgs>? LargeTerrainFeatureListChanged;
        event EventHandler<BuildingListChangedEventArgs>?         BuildingListChanged;
    }
}
