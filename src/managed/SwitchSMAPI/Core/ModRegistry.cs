using System;
using System.Collections.Generic;
using System.Linq;
using SwitchSMAPI.Framework;
using SwitchSMAPI.Framework.Logging;

namespace SwitchSMAPI.Core {

    /// <summary>A loaded and initialised native SwitchSMAPI mod.</summary>
    public class ModEntry {
        public IManifest  Manifest   { get; }
        public Mod        Instance   { get; }
        public IModHelper Helper     { get; }
        public bool       HasErrors  { get; internal set; }

        internal ModEntry(IManifest manifest, Mod instance, IModHelper helper) {
            Manifest = manifest;
            Instance = instance;
            Helper   = helper;
        }
    }

    /// <summary>A loaded and initialised PC mod (subclasses StardewModdingAPI.Mod via compat shim).</summary>
    public class SmapiModEntry {
        public IManifest  Manifest   { get; }
        public object     Instance   { get; }
        public string     Name       { get; }
        public bool       HasErrors  { get; internal set; }

        internal SmapiModEntry(IManifest manifest, object instance, string name) {
            Manifest = manifest;
            Instance = instance;
            Name     = name;
        }
    }

    /// <summary>Tracks all mods that have been loaded into the session.</summary>
    public class ModRegistry {

        private readonly Dictionary<string, ModEntry>      _byId
            = new Dictionary<string, ModEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, SmapiModEntry> _smapiById
            = new Dictionary<string, SmapiModEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly IMonitor _monitor;

        public ModRegistry(IMonitor monitor) {
            _monitor = monitor;
        }

        // ── Registration ──────────────────────────────────────────────────────

        internal void Register(ModEntry entry) {
            if (_byId.ContainsKey(entry.Manifest.UniqueID)) {
                _monitor.Log($"Duplicate mod UniqueID '{entry.Manifest.UniqueID}' — skipping second registration", LogLevel.Warn);
                return;
            }
            _byId[entry.Manifest.UniqueID] = entry;
        }

        internal void RegisterSmapiMod(SmapiModEntry entry) {
            if (_smapiById.ContainsKey(entry.Manifest.UniqueID)) {
                _monitor.Log($"Duplicate mod UniqueID '{entry.Manifest.UniqueID}' — skipping second registration", LogLevel.Warn);
                return;
            }
            _smapiById[entry.Manifest.UniqueID] = entry;
        }

        // ── Lookups ───────────────────────────────────────────────────────────

        public ModEntry? Get(string uniqueId) =>
            _byId.TryGetValue(uniqueId, out var e) ? e : null;

        public bool IsLoaded(string uniqueId) =>
            _byId.ContainsKey(uniqueId) || _smapiById.ContainsKey(uniqueId);

        public IReadOnlyCollection<ModEntry> GetAll() => _byId.Values.ToList();

        /// <summary>Get the mod that owns the given type.</summary>
        public ModEntry? GetFrom(Type type) =>
            _byId.Values.FirstOrDefault(e =>
                e.Instance.GetType().Assembly == type.Assembly);

        // ── Summary ───────────────────────────────────────────────────────────

        public void LogSummary() {
            int nativeCount = _byId.Count;
            int smapiCount  = _smapiById.Count;
            int total  = nativeCount + smapiCount;
            int errors = _byId.Values.Count(m => m.HasErrors)
                       + _smapiById.Values.Count(m => m.HasErrors);

            _monitor.Log("", LogLevel.Info);
            _monitor.Log($"Loaded {total} mod(s){(errors > 0 ? $", {errors} with errors" : string.Empty)}:", LogLevel.Info);

            foreach (var m in _byId.Values.OrderBy(x => x.Manifest.Name)) {
                string status = m.HasErrors ? " [ERROR]" : string.Empty;
                _monitor.Log($"   {m.Manifest.Name} {m.Manifest.Version} by {m.Manifest.Author}{status}", LogLevel.Info);
            }
            foreach (var m in _smapiById.Values.OrderBy(x => x.Manifest.Name)) {
                string status = m.HasErrors ? " [ERROR]" : string.Empty;
                _monitor.Log($"   {m.Manifest.Name} {m.Manifest.Version} by {m.Manifest.Author} (PC mod){status}", LogLevel.Info);
            }
            _monitor.Log("", LogLevel.Info);
        }
    }
}
