using InternalContentHelper = SwitchSMAPI.Framework.Content.ContentHelper;

namespace StardewModdingAPI.Framework {

    /// <summary>
    /// SMAPI 4 <see cref="IModContentHelper"/> — loads assets from a mod's own folder.
    /// </summary>
    internal sealed class SmapiModContentHelper : IModContentHelper {

        private readonly InternalContentHelper _inner;

        public SmapiModContentHelper(InternalContentHelper inner) => _inner = inner;

        public IAssetName GetInternalAssetName(string relativePath)
            => new SmapiAssetName(relativePath);

        public T Load<T>(string relativePath) where T : notnull
            => _inner.Load<T>(relativePath, SwitchSMAPI.Framework.Content.ContentSource.ModFolder);

        public bool DoesAssetExist<T>(IAssetName assetName) where T : notnull {
            try { Load<T>(assetName.Name); return true; }
            catch { return false; }
        }
    }
}
