using StardewModdingAPI.Events;

namespace StardewModdingAPI {
    /// <summary>
    /// Full desktop-SMAPI-compatible IModHelper.
    /// Mods compiled against the real SMAPI can use every property here without recompilation.
    /// </summary>
    public interface IModHelper {
        // ── Path ───────────────────────────────────────────────────────────────
        string    DirectoryPath { get; }
        IManifest ModManifest   { get; }

        // ── Events ─────────────────────────────────────────────────────────────
        IModEvents Events { get; }

        // ── Content (SMAPI 4 API) ──────────────────────────────────────────────
        IGameContentHelper GameContent { get; }
        IModContentHelper  ModContent  { get; }

        // ── Content (legacy SMAPI 3 API — kept for back-compat) ───────────────
        [System.Obsolete("Use GameContent or ModContent instead.")]
        IContentHelper Content { get; }

        // ── Other helpers ──────────────────────────────────────────────────────
        IDataHelper         Data           { get; }
        IInputHelper        Input          { get; }
        IReflectionHelper   Reflection     { get; }
        IMultiplayerHelper  Multiplayer    { get; }
        ITranslationHelper  Translation    { get; }
        ICommandHelper      ConsoleCommands { get; }

        // ── Config ─────────────────────────────────────────────────────────────
        TConfig ReadConfig<TConfig>()  where TConfig : class, new();
        void    WriteConfig<TConfig>(TConfig config) where TConfig : class, new();
    }
}
