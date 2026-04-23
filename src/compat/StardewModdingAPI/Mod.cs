namespace StardewModdingAPI {
    /// <summary>
    /// Base class for all mods.  Exactly mirrors desktop SMAPI's StardewModdingAPI.Mod so
    /// PC mods load on Switch without recompilation — Mono resolves their reference to this
    /// assembly instead of the real StardewModdingAPI.dll.
    /// </summary>
    public abstract class Mod : IMod {
        // ── Set by ModLoader before Entry() is called ─────────────────────────

        /// <summary>The mod's parsed manifest.</summary>
        public IManifest ModManifest { get; internal set; } = null!;

        /// <summary>Log monitor for this mod.</summary>
        public IMonitor Monitor { get; internal set; } = null!;

        /// <summary>Helper providing all SMAPI APIs.</summary>
        public IModHelper Helper { get; internal set; } = null!;

        // ── Abstract entry point ──────────────────────────────────────────────

        /// <summary>Called by SMAPI after all mod dependencies are loaded.</summary>
        public abstract void Entry(IModHelper helper);

        // ── Optional overrides ────────────────────────────────────────────────

        /// <summary>Return a public API object that other mods can consume via <c>helper.ModRegistry.GetApi</c>.</summary>
        public virtual object? GetApi()                   => null;

        /// <summary>Return a typed public API for a specific consumer.  Override for fine-grained control.</summary>
        public virtual object? GetApi(IModInfo mod)       => GetApi();

        // ── Disposal ──────────────────────────────────────────────────────────

        public virtual void Dispose() { }
    }

    /// <summary>Read-only information about a mod — passed to <c>GetApi(IModInfo)</c>.</summary>
    public interface IModInfo {
        IManifest Manifest { get; }
        bool      HasConsoleCommands { get; }
    }

    /// <summary>Implemented by all mods (used internally).</summary>
    public interface IMod {
        IManifest  ModManifest { get; }
        IMonitor   Monitor     { get; }
        IModHelper Helper      { get; }
        void       Entry(IModHelper helper);
        object?    GetApi();
    }
}
