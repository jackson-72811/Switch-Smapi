using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace SwitchSMAPI.Framework.Logging {

    /// <summary>
    /// Owns the log file(s) and hands out <see cref="Monitor"/> instances.
    /// All writes go through here and are serialised with a lock.
    /// </summary>
    public class LogManager : IDisposable {

        // ── Constants ─────────────────────────────────────────────────────────

        private const string LATEST_FILE   = "SMAPI-latest.log";
        private const string PREVIOUS_FILE = "SMAPI-previous.log";
        private const int    MAX_FILE_BYTES = 10 * 1024 * 1024; // 10 MB

        // ── State ─────────────────────────────────────────────────────────────

        private readonly string     _logDir;
        private          StreamWriter? _writer;
        private readonly object     _lock    = new object();
        private          bool       _disposed;
        private          long       _bytesWritten;

        // Level labels aligned to 5 chars for tidy columns
        private static readonly string[] LevelLabels = {
            "TRACE", "DEBUG", "INFO ", "WARN ", "ERROR", "ALERT"
        };

        // ── Construction ──────────────────────────────────────────────────────

        public LogManager(string logDirectory) {
            _logDir = logDirectory;
            Directory.CreateDirectory(logDirectory);

            // Rotate previous log
            string latestPath   = Path.Combine(logDirectory, LATEST_FILE);
            string previousPath = Path.Combine(logDirectory, PREVIOUS_FILE);
            if (File.Exists(latestPath)) {
                File.Copy(latestPath, previousPath, overwrite: true);
            }

            _writer = new StreamWriter(
                new FileStream(latestPath, FileMode.Create, FileAccess.Write, FileShare.Read),
                Encoding.UTF8,
                bufferSize: 4096);

            WriteHeader();
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>Get a <see cref="Monitor"/> tagged with <paramref name="source"/>.</summary>
        public Monitor GetMonitor(string source, bool verbose = false) {
            return new Monitor(source, this, verbose);
        }

        /// <summary>Write a log entry.  Called by <see cref="Monitor"/>.</summary>
        internal void Write(string source, LogLevel level, string message) {
            if (_disposed) return;

            int   levelIndex = Math.Max(0, Math.Min((int)level, LevelLabels.Length - 1));
            string label     = LevelLabels[levelIndex];
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string line      = $"[{timestamp}][{label}][{source,-20}] {message}";

            lock (_lock) {
                if (_writer == null) return;
                _writer.WriteLine(line);
                _writer.Flush();
                _bytesWritten += line.Length + Environment.NewLine.Length;
            }

            // Also echo to syslog / debug output for Atmosphere's crash log
            // On Switch, Console.Error maps to svc::OutputDebugString via libnx
            if (level >= LogLevel.Warn) {
                Console.Error.WriteLine(line);
            }
        }

        public void Dispose() {
            if (_disposed) return;
            _disposed = true;

            lock (_lock) {
                try {
                    _writer?.WriteLine($"[{DateTime.Now:HH:mm:ss}][INFO ][LogManager          ] Log closed.");
                    _writer?.Flush();
                    _writer?.Dispose();
                } catch { /* best-effort */ }
                _writer = null;
            }
        }

        // ── Private ───────────────────────────────────────────────────────────

        private void WriteHeader() {
            string sep = new string('═', 60);
            Write("SMAPI", LogLevel.Info, sep);
            Write("SMAPI", LogLevel.Info, "  Switch-SMAPI — Stardew Valley Mod Loader");
            Write("SMAPI", LogLevel.Info, $"  Session started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Write("SMAPI", LogLevel.Info, sep);
        }
    }
}
