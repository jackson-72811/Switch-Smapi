using System;
namespace StardewModdingAPI.Events {
    public interface IContentEvents {
        event EventHandler<AssetsInvalidatedEventArgs>? AssetsInvalidated;
        event EventHandler<AssetReadyEventArgs>?        AssetReady;
        event EventHandler<AssetRequestedEventArgs>?    AssetRequested;
    }
}
