namespace StardewModdingAPI {
    public interface IDataHelper {
        TModel? ReadJsonFile<TModel>(string path) where TModel : class;
        void    WriteJsonFile<TModel>(string path, TModel? data) where TModel : class;
        TModel? ReadGlobalData<TModel>(string key) where TModel : class;
        void    WriteGlobalData<TModel>(string key, TModel? data) where TModel : class;
        TModel? ReadSaveData<TModel>(string key) where TModel : class;
        void    WriteSaveData<TModel>(string key, TModel? data) where TModel : class;
    }
}
