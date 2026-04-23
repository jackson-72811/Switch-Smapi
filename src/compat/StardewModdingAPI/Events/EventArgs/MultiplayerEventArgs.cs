using System;

namespace StardewModdingAPI.Events {

    public class PeerConnectedEventArgs : EventArgs {
        public IMultiplayerPeer Peer { get; }
        public PeerConnectedEventArgs(IMultiplayerPeer peer) { Peer = peer; }
    }

    public class PeerDisconnectedEventArgs : EventArgs {
        public IMultiplayerPeer Peer { get; }
        public PeerDisconnectedEventArgs(IMultiplayerPeer peer) { Peer = peer; }
    }

    public class ModMessageReceivedEventArgs : EventArgs {
        public long   FromPlayerID { get; }
        public string FromModID    { get; }
        public string Type         { get; }
        private readonly string _json;
        public ModMessageReceivedEventArgs(long fromID, string fromMod, string type, string json) {
            FromPlayerID = fromID; FromModID = fromMod; Type = type; _json = json;
        }
        public TModel ReadAs<TModel>() =>
            Newtonsoft.Json.JsonConvert.DeserializeObject<TModel>(_json)!;
    }

    public class PeerContextReceivedEventArgs : EventArgs {
        public IMultiplayerPeer Peer { get; }
        public PeerContextReceivedEventArgs(IMultiplayerPeer peer) { Peer = peer; }
    }

    public interface IMultiplayerPeer {
        long    PlayerID      { get; }
        long?   ScreenID      { get; }
        bool    IsHost        { get; }
        bool    IsSplitScreen { get; }
        bool    HasSmapi      { get; }
        ISemanticVersion? ApiVersion  { get; }
        ISemanticVersion? GameVersion { get; }
        string?           Platform    { get; }
        IMultiplayerPeerMod? GetMod(string id);
        System.Collections.Generic.IEnumerable<IMultiplayerPeerMod> Mods { get; }
    }

    public interface IMultiplayerPeerMod {
        string           ID      { get; }
        ISemanticVersion Version { get; }
    }
}
