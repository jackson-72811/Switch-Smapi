using System.Collections.Generic;
using StardewModdingAPI.Events;

namespace StardewModdingAPI {
    public interface IMultiplayerHelper {
        int MaxMessageSize { get; }
        IEnumerable<IMultiplayerPeer> GetConnectedPlayers();
        IMultiplayerPeer?             GetConnectedPlayer(long playerID);
        void SendMessage<TMessage>(TMessage message, string messageType,
                                   string[]? modIDs = null, long[]? playerIDs = null);
    }
}
