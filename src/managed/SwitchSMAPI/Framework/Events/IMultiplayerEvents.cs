using System;

namespace SwitchSMAPI.Framework.Events {

    // ── Event argument types ──────────────────────────────────────────────────

    public class PeerConnectedEventArgs : EventArgs {
        public IMultiplayerPeer Peer { get; }
        internal PeerConnectedEventArgs(IMultiplayerPeer peer) { Peer = peer; }
    }

    public class PeerDisconnectedEventArgs : EventArgs {
        public IMultiplayerPeer Peer { get; }
        internal PeerDisconnectedEventArgs(IMultiplayerPeer peer) { Peer = peer; }
    }

    public class ModMessageReceivedEventArgs : EventArgs {
        public long   FromPlayerID  { get; }
        public string FromModID     { get; }
        public string Type          { get; }
        private readonly string _jsonData;

        internal ModMessageReceivedEventArgs(long fromPlayerID, string fromModID,
                                             string type, string jsonData) {
            FromPlayerID = fromPlayerID;
            FromModID    = fromModID;
            Type         = type;
            _jsonData    = jsonData;
        }

        /// <summary>Deserialise the message data to the given type.</summary>
        public TModel ReadAs<TModel>() {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<TModel>(_jsonData)!;
        }
    }

    /// <summary>Metadata about a connected multiplayer peer.</summary>
    public interface IMultiplayerPeer {
        long   PlayerID { get; }
        bool   IsHost   { get; }
        bool   HasSmapi { get; }
        string? SmapiVersion { get; }
        string? GameVersion  { get; }
    }

    // ── Interface ─────────────────────────────────────────────────────────────

    /// <summary>Events related to multiplayer sessions.</summary>
    public interface IMultiplayerEvents {
        event EventHandler<PeerConnectedEventArgs>?        PeerConnected;
        event EventHandler<PeerDisconnectedEventArgs>?     PeerDisconnected;
        event EventHandler<ModMessageReceivedEventArgs>?   ModMessageReceived;
    }
}
