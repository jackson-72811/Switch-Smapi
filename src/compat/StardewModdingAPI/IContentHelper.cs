using System;
using System.Collections.Generic;

namespace StardewModdingAPI {
    /// <summary>Legacy SMAPI 3 content helper — kept for back-compat.</summary>
    [Obsolete("Use IGameContentHelper or IModContentHelper instead.")]
    public interface IContentHelper {
        string CurrentLocale { get; }
        T      Load<T>(string assetName, ContentSource source = ContentSource.ModFolder)
            where T : notnull;
        string NormaliseAssetName(string assetName);
        IAssetName ParseAssetName(string rawName);
        bool InvalidateCache(string assetName);
        bool InvalidateCache(Func<IAssetInfo, bool> predicate);
        bool InvalidateCache<T>();
        IList<IAssetEditor> AssetEditors { get; }
        IList<IAssetLoader> AssetLoaders { get; }
    }

    public enum ContentSource { ModFolder, GameContent }

    [Obsolete("Use AssetRequested event instead.")]
    public interface IAssetEditor {
        bool CanEdit<T>(IAssetInfo asset);
        void Edit<T>(IAssetData<T> asset);
    }

    [Obsolete("Use AssetRequested event instead.")]
    public interface IAssetLoader {
        bool CanLoad<T>(IAssetInfo asset);
        T    Load<T>(IAssetInfo asset);
    }
}
