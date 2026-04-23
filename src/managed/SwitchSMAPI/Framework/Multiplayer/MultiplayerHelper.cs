using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using SwitchSMAPI.Framework.Events;
using SwitchSMAPI.Framework.Logging;

namespace SwitchSMAPI.Framework.Multiplayer {

    /// <inheritdoc cref="IMultiplayerHelper"/>
    public class MultiplayerHelper : IMultiplayerHelper {

        private readonly string   _modId;
        private readonly IMonitor _monitor;
        private readonly Dictionary<long, MultiplayerPeer> _peers
            = new Dictionary<long, MultiplayerPeer>();

        public int MaxMessageSize => 32768;

        public MultiplayerHelper(string modId, IMonitor monitor) {
            _modId   = modId;
            _monitor = monitor;
        }

        public IEnumerable<IMultiplayerPeer> GetConnectedPlayers() =>
            _peers.Values.Cast<IMultiplayerPeer>();

        public IMultiplayerPeer? GetConnectedPlayer(long playerID) =>
            _peers.TryGetValue(playerID, out var p) ? p : null;

        public void SendMessage<TMessage>(TMessage message,
                                          string messageType,
                                          string[]? modIDs    = null,
                                          long[]?   playerIDs = null) {
            try {
                string payload = JsonConvert.SerializeObject(message);
                if (payload.Length > MaxMessageSize) {
                    _monitor.Log($"SendMessage: payload ({payload.Length} bytes) exceeds MaxMessageSize ({MaxMessageSize})", LogLevel.Warn);
                    return;
                }

                // Delegate to Game1's multiplayer field via reflection
                var envelope = new ModMessageEnvelope {
                    FromModID    = _modId,
                    Type         = messageType,
                    Payload      = payload,
                    TargetModIDs = modIDs,
                    TargetPlayers = playerIDs,
                };

                SendViaGame(envelope);
            } catch (Exception ex) {
                _monitor.Log($"SendMessage failed: {ex.Message}", LogLevel.Error);
            }
        }

        // ── Peer registration (called by EventManager when peer events fire) ──

        internal void OnPeerConnected(long playerID, string? smapiVersion, string? gameVersion, bool isHost) {
            _peers[playerID] = new MultiplayerPeer(playerID, isHost, smapiVersion, gameVersion);
        }

        internal void OnPeerDisconnected(long playerID) {
            _peers.Remove(playerID);
        }

        // ── Send via game reflection ───────────────────────────────────────────

        private void SendViaGame(ModMessageEnvelope envelope) {
            try {
                var multiplayer = GetGameMultiplayer();
                if (multiplayer == null) return;

                string json = JsonConvert.SerializeObject(envelope);
                var sendMethod = multiplayer.GetType().GetMethod("sendMessage",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, new[] { typeof(long), typeof(byte), typeof(Farmer), typeof(object[]) }, null);

                // The actual method signature varies by game version — this is a best-effort stub.
                // Real implementation would match the specific Multiplayer.broadcastModData method.
                _monitor.Log($"[MP] Would send message type '{envelope.Type}' from '{envelope.FromModID}'", LogLevel.Debug);
            } catch (Exception ex) {
                _monitor.Log($"Failed to send multiplayer message: {ex.Message}", LogLevel.Debug);
            }
        }

        private static object? GetGameMultiplayer() {
            try {
                var t = Type.GetType("StardewValley.Game1, Stardew Valley");
                var f = t?.GetField("multiplayer",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return f?.GetValue(null);
            } catch { return null; }
        }

        // ── Inner types ────────────────────────────────────────────────────────

        private sealed class MultiplayerPeer : IMultiplayerPeer {
            public long    PlayerID     { get; }
            public bool    IsHost       { get; }
            public bool    HasSmapi     => SmapiVersion != null;
            public string? SmapiVersion { get; }
            public string? GameVersion  { get; }
            public MultiplayerPeer(long id, bool isHost, string? smapi, string? game) {
                PlayerID    = id;
                IsHost      = isHost;
                SmapiVersion = smapi;
                GameVersion  = game;
            }
        }

        private sealed class ModMessageEnvelope {
            public string   FromModID     { get; set; } = string.Empty;
            public string   Type          { get; set; } = string.Empty;
            public string   Payload       { get; set; } = string.Empty;
            public string[]? TargetModIDs  { get; set; }
            public long[]?   TargetPlayers { get; set; }
        }

        // Placeholder type reference to suppress "unused import" on Farmer
        private sealed class Farmer { }
    }
}
