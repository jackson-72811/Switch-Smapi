using System;
using System.Reflection;
using InternalContentHelper = SwitchSMAPI.Framework.Content.ContentHelper;
using InternalMonitor       = SwitchSMAPI.Framework.Logging.Monitor;

namespace StardewModdingAPI.Framework {

    /// <summary>
    /// SMAPI 4 <see cref="IGameContentHelper"/> — loads and invalidates assets from the game's
    /// content pipeline, bridged through the internal content helper and game reflection.
    /// </summary>
    internal sealed class SmapiGameContentHelper : IGameContentHelper {

        private readonly InternalContentHelper _inner;
        private readonly InternalMonitor       _monitor;

        public IAssetName CurrentLocaleConstant {
            get {
                string locale = GetGameLocale();
                return new SmapiAssetName("Locale/" + locale, locale);
            }
        }

        public SmapiGameContentHelper(InternalContentHelper inner, InternalMonitor monitor) {
            _inner   = inner;
            _monitor = monitor;
        }

        public IAssetName ParseAssetName(string rawName)
            => new SmapiAssetName(rawName);

        public T Load<T>(string assetName) where T : notnull
            => _inner.Load<T>(assetName, SwitchSMAPI.Framework.Content.ContentSource.GameContent);

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

        public bool DoesAssetExist<T>(IAssetName assetName) where T : notnull {
            try { Load<T>(assetName.Name); return true; }
            catch { return false; }
        }

        // ── Adapter for internal IAssetInfo → compat IAssetInfo ───────────────

        private sealed class AssetInfoAdapter : IAssetInfo {
            private readonly SwitchSMAPI.Framework.Content.IAssetInfo _i;
            public AssetInfoAdapter(SwitchSMAPI.Framework.Content.IAssetInfo i) => _i = i;
            public IAssetName Name     => new SmapiAssetName(_i.AssetName);
            public Type       DataType => _i.DataType;
            public bool AssetNameEquals(string path) => _i.AssetNameEquals(path);
        }

        private static string GetGameLocale() {
            try {
                var t = Type.GetType("StardewValley.LocalizedContentManager, Stardew Valley");
                var p = t?.GetProperty("CurrentLanguageCode",
                    BindingFlags.Static | BindingFlags.Public);
                return p?.GetValue(null)?.ToString() ?? "en";
            } catch { return "en"; }
        }
    }
}
