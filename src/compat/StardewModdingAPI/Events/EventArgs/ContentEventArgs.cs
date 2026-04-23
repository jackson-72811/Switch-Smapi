using System;

namespace StardewModdingAPI.Events {

    public class AssetsInvalidatedEventArgs : EventArgs {
        public System.Collections.Generic.IReadOnlySet<IAssetName> Names { get; }
        public AssetsInvalidatedEventArgs(System.Collections.Generic.IEnumerable<IAssetName> names) {
            Names = new HashSet<IAssetName>(names);
        }
        private sealed class HashSet<T> : System.Collections.Generic.HashSet<T>, IReadOnlySet<T> { }
    }

    public class AssetReadyEventArgs : EventArgs {
        public IAssetName Name { get; }
        public AssetReadyEventArgs(IAssetName name) { Name = name; }
    }

    public class AssetRequestedEventArgs : EventArgs {
        public IAssetName Name      { get; }
        public Type       DataType  { get; }
        public AssetRequestedEventArgs(IAssetName name, Type dataType) {
            Name = name; DataType = dataType;
        }
        public void LoadFrom<T>(Func<T> load, AssetLoadPriority priority) { }
        public void LoadFromModFile<T>(string relativePath, AssetLoadPriority priority) { }
        public void Edit<T>(Action<IAssetData<T>> apply, AssetEditPriority priority = AssetEditPriority.Default) { }
    }

    public enum AssetLoadPriority { Low = -1000, Medium = 0, High = 1000, Exclusive = int.MaxValue }
    public enum AssetEditPriority { Early = -1000, Default = 0, Late = 1000 }
}
