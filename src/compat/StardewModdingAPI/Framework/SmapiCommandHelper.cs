using System;
using System.Collections.Generic;

namespace StardewModdingAPI.Framework {

    /// <summary>Standalone command registry for mods — no internal equivalent.</summary>
    internal sealed class SmapiCommandHelper : ICommandHelper {

        private readonly Dictionary<string, (string doc, Action<string, string[]> cb)> _commands
            = new Dictionary<string, (string, Action<string, string[]>)>(StringComparer.OrdinalIgnoreCase);

        public ICommandHelper Add(string name, string documentation, Action<string, string[]> callback) {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Command name must not be blank.", nameof(name));
            _commands[name.Trim()] = (documentation ?? string.Empty, callback
                ?? throw new ArgumentNullException(nameof(callback)));
            return this;
        }

        public bool Trigger(string name, string[] arguments) {
            if (_commands.TryGetValue(name?.Trim() ?? string.Empty, out var cmd)) {
                try { cmd.cb(name!, arguments ?? Array.Empty<string>()); }
                catch { /* mod errors must not crash SMAPI */ }
                return true;
            }
            return false;
        }
    }
}
