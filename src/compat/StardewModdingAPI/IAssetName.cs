namespace StardewModdingAPI {
    /// <summary>Identifies a game content asset.</summary>
    public interface IAssetName {
        /// <summary>The normalised asset name as it appears in the game's content pipeline.</summary>
        string Name { get; }
        /// <summary>The locale suffix, or null for the default locale.</summary>
        string? LocaleCode { get; }
        /// <summary>Whether the name is equivalent to the given name after normalisation.</summary>
        bool IsEquivalentTo(string assetName, bool useBaseName = false);
        /// <summary>Whether the asset name starts with the given prefix.</summary>
        bool StartsWith(string prefix, bool allowPartialWord = true, bool allowSubfolder = true);
    }

    /// <summary>A typed mutable handle to a content asset being loaded or edited.</summary>
    public interface IAssetData<T> {
        IAssetName Name     { get; }
        T          Data     { get; set; }
        void       ReplaceWith(T value);
    }

    /// <summary>An asset data handle typed as a dictionary.</summary>
    public interface IAssetDataForDictionary<TKey, TValue> : IAssetData<System.Collections.Generic.IDictionary<TKey, TValue>>
        where TKey : notnull
    {
        System.Collections.Generic.IDictionary<TKey, TValue> Data { get; }
    }

    /// <summary>An asset data handle typed as a Texture2D.</summary>
    public interface IAssetDataForImage : IAssetData<Microsoft.Xna.Framework.Graphics.Texture2D> {
        void PatchImage(Microsoft.Xna.Framework.Graphics.Texture2D source,
                        Microsoft.Xna.Framework.Rectangle? sourceArea  = null,
                        Microsoft.Xna.Framework.Rectangle? targetArea  = null,
                        PatchMode                           patchMode   = PatchMode.Replace);
    }

    /// <summary>An asset data handle typed as a map.</summary>
    public interface IAssetDataForMap : IAssetData<xTile.Map> {
        void PatchMap(xTile.Map source,
                      Microsoft.Xna.Framework.Rectangle? sourceArea = null,
                      Microsoft.Xna.Framework.Rectangle? targetArea = null,
                      PatchMapMode                        patchMode  = PatchMapMode.Overlay);
    }

    public enum PatchMode    { Replace, Overlay }
    public enum PatchMapMode { Replace, Overlay, ReplaceByLayer }
}
