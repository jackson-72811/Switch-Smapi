namespace SwitchSMAPI.Framework.Data {

    /// <summary>Reads and writes per-mod data on the SD card.</summary>
    public interface IDataHelper {

        // ── JSON files in the mod folder ──────────────────────────────────────

        /// <summary>Read a JSON file relative to the mod folder, or null if it doesn't exist.</summary>
        TModel? ReadJsonFile<TModel>(string path) where TModel : class;

        /// <summary>Write a JSON file relative to the mod folder, creating directories as needed.</summary>
        void WriteJsonFile<TModel>(string path, TModel data) where TModel : class;

        // ── Global save data (not tied to a save file) ────────────────────────

        /// <summary>Read a value stored in <c>sdmc:/SMAPI/data/global/&lt;UniqueID&gt;/&lt;key&gt;.json</c>.</summary>
        TModel? ReadGlobalData<TModel>(string key) where TModel : class;

        /// <summary>Write a value to the global data store.</summary>
        void WriteGlobalData<TModel>(string key, TModel data) where TModel : class;

        // ── Per-save data (tied to the current save file) ─────────────────────

        /// <summary>Read a value tied to the current save file.  Returns null if no save is loaded.</summary>
        TModel? ReadSaveData<TModel>(string key) where TModel : class;

        /// <summary>Write a value to the per-save data store.  No-op if no save is loaded.</summary>
        void WriteSaveData<TModel>(string key, TModel data) where TModel : class;
    }
}
