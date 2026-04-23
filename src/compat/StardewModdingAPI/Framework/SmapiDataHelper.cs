using InternalDataHelper = SwitchSMAPI.Framework.Data.DataHelper;

namespace StardewModdingAPI.Framework {

    /// <summary>Bridges <see cref="IDataHelper"/> to the internal <see cref="InternalDataHelper"/>.</summary>
    internal sealed class SmapiDataHelper : IDataHelper {

        private readonly InternalDataHelper _inner;

        public SmapiDataHelper(InternalDataHelper inner) => _inner = inner;

        public TModel? ReadJsonFile<TModel>(string path) where TModel : class
            => _inner.ReadJsonFile<TModel>(path);

        public void WriteJsonFile<TModel>(string path, TModel? data) where TModel : class {
            if (data != null) _inner.WriteJsonFile(path, data);
        }

        public TModel? ReadGlobalData<TModel>(string key) where TModel : class
            => _inner.ReadGlobalData<TModel>(key);

        public void WriteGlobalData<TModel>(string key, TModel? data) where TModel : class {
            if (data != null) _inner.WriteGlobalData(key, data);
        }

        public TModel? ReadSaveData<TModel>(string key) where TModel : class
            => _inner.ReadSaveData<TModel>(key);

        public void WriteSaveData<TModel>(string key, TModel? data) where TModel : class {
            if (data != null) _inner.WriteSaveData(key, data);
        }
    }
}
