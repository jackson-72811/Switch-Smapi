namespace StardewModdingAPI {
    /// <summary>Desktop SMAPI-compatible monitor interface.</summary>
    public interface IMonitor {
        bool IsVerbose { get; }
        void Log(string message, LogLevel level = LogLevel.Trace);
        void LogOnce(string message, LogLevel level = LogLevel.Trace);
        void VerboseLog(string message);
    }
}
