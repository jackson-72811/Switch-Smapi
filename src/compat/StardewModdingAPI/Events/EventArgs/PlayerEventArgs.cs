using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Events {

    public class InventoryChangedEventArgs : EventArgs {
        public StardewValley.Farmer                  Player          { get; }
        public IReadOnlyList<StardewValley.Item>     Added           { get; }
        public IReadOnlyList<StardewValley.Item>     Removed         { get; }
        public IReadOnlyList<ItemStackSizeChange>    QuantityChanged { get; }
        public InventoryChangedEventArgs(StardewValley.Farmer player,
                                         IEnumerable<StardewValley.Item> added,
                                         IEnumerable<StardewValley.Item> removed,
                                         IEnumerable<ItemStackSizeChange> qChanged) {
            Player          = player;
            Added           = new List<StardewValley.Item>(added);
            Removed         = new List<StardewValley.Item>(removed);
            QuantityChanged = new List<ItemStackSizeChange>(qChanged);
        }
        public bool IsLocalPlayer => Player == StardewValley.Game1.player;
    }

    public class LevelChangedEventArgs : EventArgs {
        public StardewValley.Farmer Player   { get; }
        public SkillType            Skill    { get; }
        public int                  OldLevel { get; }
        public int                  NewLevel { get; }
        public bool IsLocalPlayer => Player == StardewValley.Game1.player;
        public LevelChangedEventArgs(StardewValley.Farmer player, SkillType skill,
                                      int oldLevel, int newLevel) {
            Player   = player;
            Skill    = skill;
            OldLevel = oldLevel;
            NewLevel = newLevel;
        }
    }

    public class WarpedEventArgs : EventArgs {
        public StardewValley.Farmer       Player      { get; }
        public StardewValley.GameLocation OldLocation { get; }
        public StardewValley.GameLocation NewLocation { get; }
        public bool IsLocalPlayer => Player == StardewValley.Game1.player;
        public WarpedEventArgs(StardewValley.Farmer player,
                                StardewValley.GameLocation oldLoc,
                                StardewValley.GameLocation newLoc) {
            Player      = player;
            OldLocation = oldLoc;
            NewLocation = newLoc;
        }
    }

    public enum SkillType {
        Farming  = 0,
        Fishing  = 1,
        Foraging = 2,
        Mining   = 3,
        Combat   = 4,
        Luck     = 5,
    }
}
