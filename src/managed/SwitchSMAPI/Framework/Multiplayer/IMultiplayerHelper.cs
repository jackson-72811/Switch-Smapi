using System.Collections.Generic;
using SwitchSMAPI.Framework.Events;

namespace SwitchSMAPI.Framework.Multiplayer {

    /// <summary>Facilitates sending and receiving mod messages in a multiplayer session.</summary>
    public interface IMultiplayerHelper {

        /// <summary>The maximum byte size of a mod message payload.</summary>
        int MaxMessageSize { get; }

        /// <summary>Get metadata about all connected players.</summary>
        IEnumerable<IMultiplayerPeer> GetConnectedPlayers();

        /// <summary>Get metadata about a specific connected player, or null.</summary>
        IMultiplayerPeer? GetConnectedPlayer(long playerID);

        /// <summary>
        /// Broadcast a message to all players in the session (including yourself).
        /// </summary>
        /// <param name="message">The data to send (must be JSON-serialisable).</param>
        /// <param name="messageType">An arbitrary string identifying the message type.</param>
        /// <param name="modIDs">Filter recipients to players with one of these mods installed, or null for all.</param>
        /// <param name="playerIDs">Filter recipients to specific player IDs, or null for all.</param>
        void SendMessage<TMessage>(TMessage message,
                                   string messageType,
                                   string[]? modIDs    = null,
                                   long[]?   playerIDs = null);
    }
}
