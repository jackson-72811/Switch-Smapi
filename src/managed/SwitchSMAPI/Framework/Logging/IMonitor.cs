namespace SwitchSMAPI.Framework.Logging {

    /// <summary>Provides logging for a mod.  Obtained from <c>Mod.Monitor</c>.</summary>
    public interface IMonitor {

        /// <summary>Whether verbose logging is enabled for this monitor.</summary>
        bool IsVerbose { get; }

        /// <summary>Log a message.</summary>
        void Log(string message, LogLevel level = LogLevel.Trace);

        /// <summary>
        /// Log a message at most once per game session.
        /// Subsequent calls with the same <paramref name="message"/> are silently discarded.
        /// </summary>
        void LogOnce(string message, LogLevel level = LogLevel.Trace);

        /// <summary>Log a verbose (trace-level) message only when verbose logging is enabled.</summary>
        void VerboseLog(string message);
    }
}
