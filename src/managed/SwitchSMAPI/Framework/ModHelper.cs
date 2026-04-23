using Newtonsoft.Json;
using SwitchSMAPI.Framework.Content;
using SwitchSMAPI.Framework.Data;
using SwitchSMAPI.Framework.Events;
using SwitchSMAPI.Framework.Input;
using SwitchSMAPI.Framework.Logging;
using SwitchSMAPI.Framework.Multiplayer;
using SwitchSMAPI.Framework.Reflection;
using SwitchSMAPI.Framework.Translation;
using System.IO;

namespace SwitchSMAPI.Framework {

    /// <inheritdoc cref="IModHelper"/>
    internal class ModHelper : IModHelper {

        private static readonly JsonSerializerSettings ConfigJson = new JsonSerializerSettings {
            Formatting           = Formatting.Indented,
            NullValueHandling    = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include,
        };

        public string              DirectoryPath  { get; }
        public IManifest           ModManifest    { get; }
        public IModEvents          Events         { get; }
        public IContentHelper      Content        { get; }
        public IDataHelper         Data           { get; }
        public IReflectionHelper   Reflection     { get; }
        public ITranslationHelper  Translation    { get; }
        public IInputHelper        Input          { get; }
        public IMultiplayerHelper  Multiplayer    { get; }

        public ModHelper(
            string           modDirectory,
            IManifest        manifest,
            IModEvents       events,
            InputHelper      inputHelper,
            LogManager       logManager,
            string           globalDataRoot)
        {
            DirectoryPath = modDirectory;
            ModManifest   = manifest;
            Events        = events;

            var monitor      = logManager.GetMonitor(manifest.Name);
            Content          = new ContentHelper(modDirectory, manifest.UniqueID, monitor);
            Data             = new DataHelper(modDirectory, manifest.UniqueID, globalDataRoot, monitor);
            Reflection       = new Reflector();
            Translation      = new TranslationHelper(modDirectory, monitor);
            Input            = inputHelper;
            Multiplayer      = new MultiplayerHelper(manifest.UniqueID, monitor);
        }

        public TModel? ReadConfig<TModel>() where TModel : class, new() {
            string path = Path.Combine(DirectoryPath, "config.json");
            if (!File.Exists(path)) return new TModel();
            try {
                return JsonConvert.DeserializeObject<TModel>(File.ReadAllText(path), ConfigJson)
                    ?? new TModel();
            } catch {
                return new TModel();
            }
        }

        public void WriteConfig<TModel>(TModel config) where TModel : class, new() {
            string path = Path.Combine(DirectoryPath, "config.json");
            File.WriteAllText(path, JsonConvert.SerializeObject(config, ConfigJson));
        }
    }
}
