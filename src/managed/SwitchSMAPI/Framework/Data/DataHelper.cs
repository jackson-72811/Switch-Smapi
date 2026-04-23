using System;
using System.IO;
using Newtonsoft.Json;
using SwitchSMAPI.Framework.Logging;

namespace SwitchSMAPI.Framework.Data {

    /// <inheritdoc cref="IDataHelper"/>
    public class DataHelper : IDataHelper {

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings {
            Formatting            = Formatting.Indented,
            NullValueHandling     = NullValueHandling.Ignore,
            DefaultValueHandling  = DefaultValueHandling.Include,
        };

        private readonly string   _modDirectory;
        private readonly string   _modUniqueId;
        private readonly string   _globalDataRoot;
        private readonly IMonitor _monitor;

        public DataHelper(string modDirectory, string modUniqueId,
                          string globalDataRoot, IMonitor monitor) {
            _modDirectory   = modDirectory;
            _modUniqueId    = modUniqueId;
            _globalDataRoot = globalDataRoot;
            _monitor        = monitor;
        }

        // ── JSON files ────────────────────────────────────────────────────────

        public TModel? ReadJsonFile<TModel>(string path) where TModel : class {
            string full = Path.Combine(_modDirectory, Sanitise(path));
            if (!File.Exists(full)) return null;
            try {
                return JsonConvert.DeserializeObject<TModel>(File.ReadAllText(full), JsonSettings);
            } catch (Exception ex) {
                _monitor.Log($"Failed to read JSON file '{full}': {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        public void WriteJsonFile<TModel>(string path, TModel data) where TModel : class {
            string full = Path.Combine(_modDirectory, Sanitise(path));
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllText(full, JsonConvert.SerializeObject(data, JsonSettings));
        }

        // ── Global data ───────────────────────────────────────────────────────

        public TModel? ReadGlobalData<TModel>(string key) where TModel : class {
            string path = GlobalPath(key);
            if (!File.Exists(path)) return null;
            try {
                return JsonConvert.DeserializeObject<TModel>(File.ReadAllText(path), JsonSettings);
            } catch (Exception ex) {
                _monitor.Log($"Failed to read global data '{key}': {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        public void WriteGlobalData<TModel>(string key, TModel data) where TModel : class {
            string path = GlobalPath(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, JsonSettings));
        }

        // ── Per-save data ─────────────────────────────────────────────────────

        public TModel? ReadSaveData<TModel>(string key) where TModel : class {
            string? saveId = GetCurrentSaveId();
            if (saveId == null) return null;
            string path = SavePath(saveId, key);
            if (!File.Exists(path)) return null;
            try {
                return JsonConvert.DeserializeObject<TModel>(File.ReadAllText(path), JsonSettings);
            } catch (Exception ex) {
                _monitor.Log($"Failed to read save data '{key}': {ex.Message}", LogLevel.Error);
                return null;
            }
        }

        public void WriteSaveData<TModel>(string key, TModel data) where TModel : class {
            string? saveId = GetCurrentSaveId();
            if (saveId == null) {
                _monitor.Log("WriteSaveData called but no save file is loaded.", LogLevel.Warn);
                return;
            }
            string path = SavePath(saveId, key);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonConvert.SerializeObject(data, JsonSettings));
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private string GlobalPath(string key) =>
            Path.Combine(_globalDataRoot, "global", _modUniqueId, Sanitise(key) + ".json");

        private string SavePath(string saveId, string key) =>
            Path.Combine(_globalDataRoot, "save", saveId, _modUniqueId, Sanitise(key) + ".json");

        private static string Sanitise(string path) =>
            path.Replace('/', Path.DirectorySeparatorChar)
                .TrimStart(Path.DirectorySeparatorChar);

        private static string? GetCurrentSaveId() {
            // Resolve Game1.uniqueIDForThisGame via reflection to avoid hard dependency
            try {
                var t = Type.GetType("StardewValley.Game1, Stardew Valley");
                if (t == null) return null;
                var f = t.GetField("uniqueIDForThisGame",
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (f == null) return null;
                var val = f.GetValue(null);
                return val?.ToString();
            } catch {
                return null;
            }
        }
    }
}
