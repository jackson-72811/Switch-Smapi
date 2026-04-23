#pragma warning disable CS0618  // suppress Obsolete warnings for the members we are implementing
using System;
using System.Collections.Generic;
using InternalContentHelper = SwitchSMAPI.Framework.Content.ContentHelper;
using InternalSource        = SwitchSMAPI.Framework.Content.ContentSource;
using InternalAssetEditor   = SwitchSMAPI.Framework.Content.IAssetEditor;
using InternalAssetLoader   = SwitchSMAPI.Framework.Content.IAssetLoader;
using InternalAssetInfo     = SwitchSMAPI.Framework.Content.IAssetInfo;
using InternalAssetData     = SwitchSMAPI.Framework.Content.IAssetData;

namespace StardewModdingAPI.Framework {

    /// <summary>
    /// Legacy SMAPI 3 <see cref="IContentHelper"/> — wraps the internal content helper and
    /// bridges the old <see cref="IAssetEditor"/>/<see cref="IAssetLoader"/> APIs.
    /// </summary>
    internal sealed class SmapiLegacyContentHelper : IContentHelper {

        private readonly InternalContentHelper _inner;
        private readonly string                _currentLocale;

        public string CurrentLocale => _currentLocale;

        public IList<IAssetEditor> AssetEditors { get; } = new List<IAssetEditor>();
        public IList<IAssetLoader> AssetLoaders { get; } = new List<IAssetLoader>();

        public SmapiLegacyContentHelper(InternalContentHelper inner, string locale = "en") {
            _inner         = inner;
            _currentLocale = locale;
        }

        public T Load<T>(string assetName, ContentSource source = ContentSource.ModFolder) where T : notnull
            => _inner.Load<T>(assetName,
                source == ContentSource.ModFolder ? InternalSource.ModFolder : InternalSource.GameContent);

        public string NormaliseAssetName(string assetName)
            => _inner.NormaliseAssetName(assetName);

        public IAssetName ParseAssetName(string rawName)
            => new SmapiAssetName(rawName);

        public bool InvalidateCache(string assetName) {
            _inner.InvalidateCache(assetName);
            return true;
        }

        public bool InvalidateCache(Func<IAssetInfo, bool> predicate) {
            _inner.InvalidateCache(info => predicate(new AssetInfoAdapter(info)));
            return true;
        }

        public bool InvalidateCache<T>() {
            _inner.InvalidateCache(info => info.DataType == typeof(T));
            return true;
        }

        // ── Adapter: internal IAssetInfo → compat IAssetInfo ─────────────────

        private sealed class AssetInfoAdapter : IAssetInfo {
            private readonly InternalAssetInfo _i;
            public AssetInfoAdapter(InternalAssetInfo i) => _i = i;
            public IAssetName Name     => new SmapiAssetName(_i.AssetName);
            public Type       DataType => _i.DataType;
            public bool AssetNameEquals(string path) => _i.AssetNameEquals(path);
        }
    }
}
#pragma warning restore CS0618
