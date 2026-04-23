using System;
using System.Collections.Generic;

namespace StardewModdingAPI {
    /// <summary>Loads and modifies assets from the game's content pipeline (SMAPI 4+ API).</summary>
    public interface IGameContentHelper {
        /// <summary>Parse a raw asset name into a normalised <see cref="IAssetName"/>.</summary>
        IAssetName ParseAssetName(string rawName);

        /// <summary>Load a game asset.</summary>
        T Load<T>(string assetName) where T : notnull;

        /// <summary>Remove one or more cached assets so they are reloaded next use.</summary>
        bool InvalidateCache(string assetName);
        bool InvalidateCache(Func<IAssetInfo, bool> predicate);
        bool InvalidateCache<T>();

        /// <summary>Check whether the given asset exists in the game's content pipeline.</summary>
        bool DoesAssetExist<T>(IAssetName assetName) where T : notnull;

        IAssetName CurrentLocaleConstant { get; }
    }

    /// <summary>Information about a content asset being requested.</summary>
    public interface IAssetInfo {
        IAssetName Name     { get; }
        Type       DataType { get; }
        bool AssetNameEquals(string path);
    }
}
