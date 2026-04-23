using System.Collections.Generic;

namespace SwitchSMAPI.Framework.Content {

    /// <summary>Where to load a content asset from.</summary>
    public enum ContentSource {
        /// <summary>Load from this mod's folder (relative path).</summary>
        ModFolder,
        /// <summary>Load from the game's content pipeline.</summary>
        GameContent,
    }

    /// <summary>Metadata about a content asset being loaded.</summary>
    public interface IAssetInfo {
        /// <summary>The locale-normalised asset name (e.g. "Maps/Farm").</summary>
        string AssetName { get; }
        /// <summary>The type of the asset (e.g. <c>Texture2D</c>).</summary>
        System.Type DataType { get; }
        /// <summary>True if the asset name matches the given path.</summary>
        bool AssetNameEquals(string path);
    }

    /// <summary>A mutable handle to a content asset being loaded.</summary>
    public interface IAssetData : IAssetInfo {
        /// <summary>Get the asset as a dictionary.</summary>
        IAssetDataForDictionary<TKey, TValue> AsDictionary<TKey, TValue>() where TKey : notnull;
        /// <summary>Get the asset as an image.</summary>
        IAssetDataForImage AsImage();
        /// <summary>Replace the entire asset data with a new value.</summary>
        void ReplaceWith<T>(T value);
    }

    public interface IAssetDataForDictionary<TKey, TValue> where TKey : notnull {
        IDictionary<TKey, TValue> Data { get; }
    }

    public interface IAssetDataForImage {
        Microsoft.Xna.Framework.Graphics.Texture2D Data { get; }
        void PatchImage(Microsoft.Xna.Framework.Graphics.Texture2D source,
                        Microsoft.Xna.Framework.Rectangle? sourceArea = null,
                        Microsoft.Xna.Framework.Rectangle? targetArea = null,
                        PatchMode patchMode = PatchMode.Replace);
    }

    public enum PatchMode { Replace, Overlay }

    /// <summary>Intercepts a content asset before it is returned to the game.</summary>
    public interface IAssetEditor {
        bool CanEdit<T>(IAssetInfo asset);
        void Edit<T>(IAssetData asset);
    }

    /// <summary>Provides a new content asset to inject into the game.</summary>
    public interface IAssetLoader {
        bool CanLoad<T>(IAssetInfo asset);
        T    Load<T>(IAssetInfo asset);
    }

    /// <summary>Loads and edits game content assets.</summary>
    public interface IContentHelper {
        /// <summary>Load a content asset.</summary>
        T Load<T>(string assetName, ContentSource source = ContentSource.ModFolder);

        /// <summary>Normalise an asset name to the canonical form used internally.</summary>
        string NormaliseAssetName(string assetName);

        /// <summary>Remove an asset from the content cache, forcing a reload next time it is requested.</summary>
        void InvalidateCache(string assetName);

        /// <summary>Remove all cached assets matching a predicate.</summary>
        void InvalidateCache(System.Func<IAssetInfo, bool> predicate);

        /// <summary>Register an asset editor for this mod.</summary>
        IList<IAssetEditor> AssetEditors { get; }

        /// <summary>Register an asset loader for this mod.</summary>
        IList<IAssetLoader> AssetLoaders { get; }
    }
}
