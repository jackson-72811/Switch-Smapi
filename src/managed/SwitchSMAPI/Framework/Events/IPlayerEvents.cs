using System;
using System.Collections.Generic;

namespace SwitchSMAPI.Framework.Events {

    // ── Event argument types ──────────────────────────────────────────────────

    public class InventoryChangedEventArgs : EventArgs {
        public object Player { get; }
        public IReadOnlyCollection<object> Added           { get; }
        public IReadOnlyCollection<object> Removed         { get; }
        public IReadOnlyCollection<object> QuantityChanged { get; }
        internal InventoryChangedEventArgs(
            object player,
            IEnumerable<object> added,
            IEnumerable<object> removed,
            IEnumerable<object> quantityChanged)
        {
            Player          = player;
            Added           = new List<object>(added);
            Removed         = new List<object>(removed);
            QuantityChanged = new List<object>(quantityChanged);
        }
    }

    public class LevelChangedEventArgs : EventArgs {
        public object Player    { get; }
        public SkillType Skill  { get; }
        public int OldLevel     { get; }
        public int NewLevel     { get; }
        internal LevelChangedEventArgs(object player, SkillType skill, int oldLevel, int newLevel) {
            Player   = player;
            Skill    = skill;
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }

    public class WarpedEventArgs : EventArgs {
        public object Player      { get; }
        public object OldLocation { get; }
        public object NewLocation { get; }
        internal WarpedEventArgs(object player, object oldLocation, object newLocation) {
            Player      = player;
            OldLocation = oldLocation;
            NewLocation = newLocation;
        }
    }

    /// <summary>Skills corresponding to Stardew Valley's skill indices.</summary>
    public enum SkillType {
        Farming    = 0,
        Fishing    = 1,
        Foraging   = 2,
        Mining     = 3,
        Combat     = 4,
        Luck       = 5,
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>Events related to the local player.</summary>
    public interface IPlayerEvents {
        /// <summary>Raised after items are added or removed from the player's inventory.</summary>
        event EventHandler<InventoryChangedEventArgs>? InventoryChanged;

        /// <summary>Raised after the player gains a new skill level.</summary>
        event EventHandler<LevelChangedEventArgs>? LevelChanged;

        /// <summary>Raised after the player moves to a different location.</summary>
        event EventHandler<WarpedEventArgs>? Warped;
    }
}
