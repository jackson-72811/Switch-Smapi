using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SwitchSMAPI.Framework.Logging {

    /// <summary>Writes log messages for one source (a mod or SMAPI itself).</summary>
    public class Monitor : IMonitor {

        // ── State ──────────────────────────────────────────────────────────────

        private readonly string          _source;
        private readonly LogManager      _manager;
        private readonly HashSet<string> _onceSeen = new HashSet<string>(StringComparer.Ordinal);

        // ── Construction ──────────────────────────────────────────────────────

        internal Monitor(string source, LogManager manager, bool verbose = false) {
            _source   = source ?? "(unknown)";
            _manager  = manager;
            IsVerbose = verbose;
        }

        // ── IMonitor ──────────────────────────────────────────────────────────

        public bool IsVerbose { get; }

        public void Log(string message, LogLevel level = LogLevel.Trace) {
            _manager.Write(_source, level, message);
        }

        public void LogOnce(string message, LogLevel level = LogLevel.Trace) {
            if (_onceSeen.Add(message))
                Log(message, level);
        }

        public void VerboseLog(string message) {
            if (IsVerbose) Log(message, LogLevel.Trace);
        }
    }
}
