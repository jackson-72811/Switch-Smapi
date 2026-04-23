using InternalLogLevel = SwitchSMAPI.Framework.Logging.LogLevel;
using InternalMonitor  = SwitchSMAPI.Framework.Logging.Monitor;

namespace StardewModdingAPI.Framework {

    /// <summary>Bridges <see cref="IMonitor"/> calls to the internal <see cref="InternalMonitor"/>.</summary>
    internal sealed class SmapiMonitor : IMonitor {

        private readonly InternalMonitor _inner;

        public SmapiMonitor(InternalMonitor inner) => _inner = inner;

        public bool IsVerbose => _inner.IsVerbose;

        public void Log(string message, LogLevel level = LogLevel.Trace)
            => _inner.Log(message, (InternalLogLevel)(int)level);

        public void LogOnce(string message, LogLevel level = LogLevel.Trace)
            => _inner.LogOnce(message, (InternalLogLevel)(int)level);

        public void VerboseLog(string message) => _inner.VerboseLog(message);
    }
}
