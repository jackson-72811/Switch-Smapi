using System;
namespace StardewModdingAPI.Events {
    public interface IMultiplayerEvents {
        event EventHandler<PeerContextReceivedEventArgs>? PeerContextReceived;
        event EventHandler<PeerConnectedEventArgs>?       PeerConnected;
        event EventHandler<PeerDisconnectedEventArgs>?    PeerDisconnected;
        event EventHandler<ModMessageReceivedEventArgs>?  ModMessageReceived;
    }
}
