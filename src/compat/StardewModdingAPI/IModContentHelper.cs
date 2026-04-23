namespace StardewModdingAPI {
    /// <summary>Loads assets from this mod's folder (SMAPI 4+ API).</summary>
    public interface IModContentHelper {
        /// <summary>Parse a relative path inside the mod folder into an <see cref="IAssetName"/>.</summary>
        IAssetName GetInternalAssetName(string relativePath);

        /// <summary>Load an asset from the mod folder.</summary>
        T Load<T>(string relativePath) where T : notnull;

        /// <summary>Check whether a given relative path exists in this mod's folder.</summary>
        bool DoesAssetExist<T>(IAssetName assetName) where T : notnull;
    }
}
