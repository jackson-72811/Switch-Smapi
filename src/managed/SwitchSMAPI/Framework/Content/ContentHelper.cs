using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using SwitchSMAPI.Framework.Logging;
using Microsoft.Xna.Framework.Graphics;

namespace SwitchSMAPI.Framework.Content {

    /// <inheritdoc cref="IContentHelper"/>
    public class ContentHelper : IContentHelper {

        private readonly string   _modDirectory;
        private readonly IMonitor _monitor;

        // Global asset editor/loader lists shared across all mods
        private static readonly List<(IAssetEditor editor, string modId)>  s_editors
            = new List<(IAssetEditor, string)>();
        private static readonly List<(IAssetLoader loader, string modId)>  s_loaders
            = new List<(IAssetLoader, string)>();

        private readonly string   _modId;

        // Per-instance views filtering to this mod's registrations
        private readonly ProxyList<IAssetEditor> _editorProxy;
        private readonly ProxyList<IAssetLoader> _loaderProxy;

        public IList<IAssetEditor> AssetEditors => _editorProxy;
        public IList<IAssetLoader> AssetLoaders => _loaderProxy;

        public ContentHelper(string modDirectory, string modId, IMonitor monitor) {
            _modDirectory = modDirectory;
            _modId        = modId;
            _monitor      = monitor;
            _editorProxy  = new ProxyList<IAssetEditor>(s_editors, modId);
            _loaderProxy  = new ProxyList<IAssetLoader>(s_loaders, modId);
        }

        public T Load<T>(string assetName, ContentSource source = ContentSource.ModFolder) {
            if (source == ContentSource.ModFolder) {
                return LoadFromModFolder<T>(assetName);
            }

            // For GameContent we delegate to the game's own content manager
            // via reflection so we don't need a hard compile-time dependency.
            return LoadFromGame<T>(assetName);
        }

        private T LoadFromModFolder<T>(string relativePath) {
            string fullPath = Path.Combine(_modDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));

            if (typeof(T) == typeof(Texture2D)) {
                throw new NotSupportedException(
                    "Loading Texture2D from mod folder is not yet supported on Switch — " +
                    "place textures in the game's content tree and use ContentSource.GameContent.");
            }

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Asset file not found: {fullPath}");

            // JSON / plain text fallback
            string text = File.ReadAllText(fullPath);
            var result  = JsonConvert.DeserializeObject<T>(text);
            if (result == null)
                throw new InvalidOperationException($"Failed to deserialise asset: {fullPath}");
            return result;
        }

        private T LoadFromGame<T>(string assetName) {
            // Attempt to call Game1.content.Load<T>(assetName) via reflection.
            // This avoids a hard compile-time reference to Stardew Valley.
            try {
                var game1Type = Type.GetType("StardewValley.Game1, Stardew Valley");
                if (game1Type == null)
                    throw new InvalidOperationException("Could not resolve StardewValley.Game1");

                var contentField = game1Type.GetField("content",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (contentField == null)
                    throw new InvalidOperationException("Could not find Game1.content");

                object? contentManager = contentField.GetValue(null);
                if (contentManager == null)
                    throw new InvalidOperationException("Game1.content is null");

                var loadMethod = contentManager.GetType().GetMethod("Load")
                    ?? throw new InvalidOperationException("ContentManager.Load not found");

                var generic = loadMethod.MakeGenericMethod(typeof(T));
                return (T)generic.Invoke(contentManager, new object[] { assetName })!;
            } catch (Exception ex) {
                throw new InvalidOperationException($"Failed to load game asset '{assetName}': {ex.Message}", ex);
            }
        }

        public string NormaliseAssetName(string assetName) {
            return assetName.Replace('\\', '/').TrimStart('/');
        }

        public void InvalidateCache(string assetName) {
            _monitor.Log($"InvalidateCache: {assetName}", LogLevel.Debug);
            // Actual invalidation calls the game's content manager — NYI
        }

        public void InvalidateCache(Func<IAssetInfo, bool> predicate) {
            _monitor.Log("InvalidateCache(predicate) called", LogLevel.Debug);
        }

        // ── Global interceptors called by a Harmony patch on ContentManager.Load ──

        public static bool TryEditAsset(IAssetData data) {
            bool edited = false;
            foreach (var (editor, _) in s_editors) {
                try {
                    var canEdit = typeof(IAssetEditor)
                        .GetMethod(nameof(IAssetEditor.CanEdit))!
                        .MakeGenericMethod(data.DataType)
                        .Invoke(editor, new object[] { data });
                    if (canEdit is true) {
                        typeof(IAssetEditor)
                            .GetMethod(nameof(IAssetEditor.Edit))!
                            .MakeGenericMethod(data.DataType)
                            .Invoke(editor, new object[] { data });
                        edited = true;
                    }
                } catch { /* individual editor errors must not break loading */ }
            }
            return edited;
        }

        // ── Proxy list that adds to/removes from the shared list with mod tag ──

        private sealed class ProxyList<T> : IList<T> {
            private readonly List<(T item, string modId)> _backing;
            private readonly string _modId;
            public ProxyList(List<(T, string)> backing, string modId) {
                _backing = backing; _modId = modId;
            }
            public void Add(T item)            => _backing.Add((item, _modId));
            public bool Remove(T item) {
                int idx = _backing.FindIndex(x => Equals(x.item, item) && x.modId == _modId);
                if (idx < 0) return false;
                _backing.RemoveAt(idx);
                return true;
            }
            public void Clear()                => _backing.RemoveAll(x => x.modId == _modId);
            public bool Contains(T item)       => _backing.Exists(x => Equals(x.item, item) && x.modId == _modId);
            public int  Count                  => _backing.FindAll(x => x.modId == _modId).Count;
            public bool IsReadOnly             => false;
            public T    this[int i]            { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public int  IndexOf(T item)        => throw new NotSupportedException();
            public void Insert(int i, T item)  => throw new NotSupportedException();
            public void RemoveAt(int i)        => throw new NotSupportedException();
            public void CopyTo(T[] arr, int i) => throw new NotSupportedException();
            public IEnumerator<T> GetEnumerator() {
                foreach (var (item, id) in _backing)
                    if (id == _modId) yield return item;
            }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                => GetEnumerator();
        }
    }
}
