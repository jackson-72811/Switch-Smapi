#pragma warning disable CS0618  // IContentHelper is Obsolete — we expose it for back-compat
using System.IO;
using Newtonsoft.Json;
using StardewModdingAPI.Events;
using InternalEventManager   = SwitchSMAPI.Framework.Events.EventManager;
using InternalInputHelper    = SwitchSMAPI.Framework.Input.InputHelper;
using InternalLogManager     = SwitchSMAPI.Framework.Logging.LogManager;
using InternalContentHelper  = SwitchSMAPI.Framework.Content.ContentHelper;
using InternalDataHelper     = SwitchSMAPI.Framework.Data.DataHelper;
using InternalReflector      = SwitchSMAPI.Framework.Reflection.Reflector;
using InternalTranslation    = SwitchSMAPI.Framework.Translation.TranslationHelper;
using InternalMultiplayer    = SwitchSMAPI.Framework.Multiplayer.MultiplayerHelper;

namespace StardewModdingAPI.Framework {

    /// <summary>
    /// Implements <see cref="IModHelper"/> for PC mods running on Switch.
    /// Each mod that loads via the compat shim receives one of these, wired to the
    /// internal SwitchSMAPI engine.
    /// </summary>
    public sealed class SmapiModHelper : IModHelper {

        // ── Config serialiser ─────────────────────────────────────────────────

        private static readonly JsonSerializerSettings ConfigJson = new JsonSerializerSettings {
            Formatting           = Formatting.Indented,
            NullValueHandling    = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
        };

        // ── IModHelper ────────────────────────────────────────────────────────

        public string            DirectoryPath   { get; }
        public IManifest         ModManifest     { get; }
        public IModEvents        Events          { get; }
        public IGameContentHelper GameContent    { get; }
        public IModContentHelper  ModContent     { get; }

        [System.Obsolete("Use GameContent or ModContent instead.")]
        public IContentHelper    Content         { get; }

        public IDataHelper       Data            { get; }
        public IInputHelper      Input           { get; }
        public IReflectionHelper Reflection      { get; }
        public IMultiplayerHelper Multiplayer    { get; }
        public ITranslationHelper Translation    { get; }
        public ICommandHelper    ConsoleCommands { get; }

        // ── Construction ──────────────────────────────────────────────────────

        /// <summary>
        /// Build a fully-wired helper for one PC mod.
        /// All internal helpers are passed in from the <see cref="SwitchSMAPI.Core.ModLoader"/>.
        /// </summary>
        public SmapiModHelper(
            string                           modDirectory,
            SwitchSMAPI.Framework.IManifest  internalManifest,
            InternalEventManager             eventManager,
            InternalInputHelper              inputHelper,
            InternalLogManager               logManager,
            string                           globalDataRoot)
        {
            DirectoryPath = modDirectory;
            ModManifest   = new SmapiManifestAdapter(internalManifest);

            var monitor      = logManager.GetMonitor(internalManifest.Name);
            var contentInner = new InternalContentHelper(modDirectory, internalManifest.UniqueID, monitor);
            var dataInner    = new InternalDataHelper(modDirectory, internalManifest.UniqueID, globalDataRoot, monitor);
            var reflInner    = new InternalReflector();
            var transInner   = new InternalTranslation(modDirectory, monitor);
            var mpInner      = new InternalMultiplayer(internalManifest.UniqueID, monitor);

            Events          = new SmapiModEvents(eventManager);
            GameContent     = new SmapiGameContentHelper(contentInner, monitor);
            ModContent      = new SmapiModContentHelper(contentInner);
            Content         = new SmapiLegacyContentHelper(contentInner);
            Data            = new SmapiDataHelper(dataInner);
            Input           = new SmapiInputHelper(inputHelper);
            Reflection      = new SmapiReflectionHelper(reflInner);
            Multiplayer     = new SmapiMultiplayerHelper(mpInner);
            Translation     = new SmapiTranslationHelper(transInner);
            ConsoleCommands = new SmapiCommandHelper();
        }

        // ── Config ────────────────────────────────────────────────────────────

        public TConfig ReadConfig<TConfig>() where TConfig : class, new() {
            string path = Path.Combine(DirectoryPath, "config.json");
            if (!File.Exists(path)) return new TConfig();
            try {
                return JsonConvert.DeserializeObject<TConfig>(File.ReadAllText(path), ConfigJson)
                    ?? new TConfig();
            } catch {
                return new TConfig();
            }
        }

        public void WriteConfig<TConfig>(TConfig config) where TConfig : class, new() {
            string path = Path.Combine(DirectoryPath, "config.json");
            Directory.CreateDirectory(DirectoryPath);
            File.WriteAllText(path, JsonConvert.SerializeObject(config, ConfigJson));
        }
    }
}
#pragma warning restore CS0618
