using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Events;
using InternalHelper = SwitchSMAPI.Framework.Multiplayer.MultiplayerHelper;
using InternalPeer   = SwitchSMAPI.Framework.Events.IMultiplayerPeer;

namespace StardewModdingAPI.Framework {

    /// <summary>Bridges <see cref="IMultiplayerHelper"/> to the internal multiplayer system.</summary>
    internal sealed class SmapiMultiplayerHelper : IMultiplayerHelper {

        private readonly InternalHelper _inner;

        public int MaxMessageSize => _inner.MaxMessageSize;

        public SmapiMultiplayerHelper(InternalHelper inner) => _inner = inner;

        public IEnumerable<IMultiplayerPeer> GetConnectedPlayers()
            => _inner.GetConnectedPlayers().Select(p => (IMultiplayerPeer)new PeerAdapter(p));

        public IMultiplayerPeer? GetConnectedPlayer(long playerID) {
            var p = _inner.GetConnectedPlayer(playerID);
            return p != null ? new PeerAdapter(p) : null;
        }

        public void SendMessage<TMessage>(TMessage message, string messageType,
                                          string[]? modIDs = null, long[]? playerIDs = null)
            => _inner.SendMessage(message, messageType, modIDs, playerIDs);

        // ── IMultiplayerPeer adapter ──────────────────────────────────────────

        private sealed class PeerAdapter : IMultiplayerPeer {
            private readonly InternalPeer _p;
            public PeerAdapter(InternalPeer p) => _p = p;

            public long  PlayerID     => _p.PlayerID;
            public long? ScreenID     => null;
            public bool  IsHost       => _p.IsHost;
            public bool  IsSplitScreen => false;
            public bool  HasSmapi     => _p.HasSmapi;
            public string? Platform   => "Switch";

            public ISemanticVersion? ApiVersion =>
                _p.SmapiVersion != null && SemanticVersion.TryParse(_p.SmapiVersion, out var a) ? a : null;

            public ISemanticVersion? GameVersion =>
                _p.GameVersion != null && SemanticVersion.TryParse(_p.GameVersion, out var g) ? g : null;

            public IEnumerable<IMultiplayerPeerMod> Mods
                => System.Linq.Enumerable.Empty<IMultiplayerPeerMod>();

            public IMultiplayerPeerMod? GetMod(string id) => null;
        }
    }
}
