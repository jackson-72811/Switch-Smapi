namespace SwitchSMAPI.Framework.Events {

    /// <summary>Root access point for all Switch-SMAPI event categories.</summary>
    public interface IModEvents {
        /// <summary>Events related to the game loop (ticks, save, day cycle).</summary>
        IGameLoopEvents GameLoop { get; }

        /// <summary>Events related to screen rendering.</summary>
        IDisplayEvents Display { get; }

        /// <summary>Events related to world state (locations, NPCs, objects).</summary>
        IWorldEvents World { get; }

        /// <summary>Events related to the local player.</summary>
        IPlayerEvents Player { get; }

        /// <summary>Events related to controller / keyboard / mouse input.</summary>
        IInputEvents Input { get; }

        /// <summary>Events related to multiplayer sessions.</summary>
        IMultiplayerEvents Multiplayer { get; }
    }
}
