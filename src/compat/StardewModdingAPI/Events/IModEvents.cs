namespace StardewModdingAPI.Events {
    public interface IModEvents {
        IGameLoopEvents    GameLoop    { get; }
        IDisplayEvents     Display     { get; }
        IWorldEvents       World       { get; }
        IPlayerEvents      Player      { get; }
        IInputEvents       Input       { get; }
        IMultiplayerEvents Multiplayer { get; }
        IContentEvents     Content     { get; }
        ISpecializedEvents Specialized { get; }
    }
}
