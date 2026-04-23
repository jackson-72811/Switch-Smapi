using SwitchSMAPI.Framework.Logging;

namespace SwitchSMAPI.Framework {

    /// <summary>
    /// Base class for every Switch-SMAPI mod.
    /// Every mod assembly must contain exactly one non-abstract subclass of this type.
    /// </summary>
    public abstract class Mod {

        // ── Infrastructure (set by ModLoader before calling Entry) ────────────

        /// <summary>Manifest data from this mod's manifest.json.</summary>
        public IManifest ModManifest { get; internal set; } = null!;

        /// <summary>Log monitor tagged with this mod's name.</summary>
        public IMonitor Monitor { get; internal set; } = null!;

        /// <summary>Helper providing access to all SMAPI APIs.</summary>
        public IModHelper Helper { get; internal set; } = null!;

        // ── Abstract entry point ──────────────────────────────────────────────

        /// <summary>
        /// Called by SMAPI once after all mods in the dependency chain are loaded.
        /// Register event handlers and perform one-time initialisation here.
        /// Never call game code from the constructor — wait for this method.
        /// </summary>
        public abstract void Entry(IModHelper helper);
    }
}
