using System;

namespace StardewModdingAPI {
    /// <summary>Registers console commands for a mod.</summary>
    public interface ICommandHelper {
        /// <summary>Register a console command.</summary>
        ICommandHelper Add(string name, string documentation, Action<string, string[]> callback);
        /// <summary>Trigger a registered command manually.</summary>
        bool Trigger(string name, string[] arguments);
    }
}
