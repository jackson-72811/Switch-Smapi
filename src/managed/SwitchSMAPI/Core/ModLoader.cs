using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SwitchSMAPI.Framework;
using SwitchSMAPI.Framework.Events;
using SwitchSMAPI.Framework.Input;
using SwitchSMAPI.Framework.Logging;

namespace SwitchSMAPI.Core {

    /// <summary>
    /// Scans the mods directory, resolves dependency order, loads each mod assembly,
    /// instantiates the <see cref="Mod"/> subclass, and calls <see cref="Mod.Entry"/>.
    /// </summary>
    public class ModLoader {

        private readonly IMonitor      _monitor;
        private readonly LogManager    _logManager;
        private readonly EventManager  _events;
        private readonly InputHelper   _input;
        private readonly ModRegistry   _registry;
        private readonly string        _globalDataRoot;

        public ModLoader(IMonitor monitor, LogManager logManager,
                         EventManager events, InputHelper input,
                         ModRegistry registry, string globalDataRoot) {
            _monitor        = monitor;
            _logManager     = logManager;
            _events         = events;
            _input          = input;
            _registry       = registry;
            _globalDataRoot = globalDataRoot;
        }

        // ── Public entry point ────────────────────────────────────────────────

        /// <summary>Load all mods from <paramref name="modsRoot"/>.</summary>
        public void LoadMods(string modsRoot) {
            if (!Directory.Exists(modsRoot)) {
                _monitor.Log($"Mods folder not found: {modsRoot}", LogLevel.Warn);
                _monitor.Log("Create the folder and add mod subfolders to use mods.", LogLevel.Info);
                return;
            }

            // 1. Discover manifests
            var candidates = DiscoverMods(modsRoot);
            _monitor.Log($"Found {candidates.Count} mod candidate(s) in {modsRoot}", LogLevel.Debug);

            // 2. Resolve dependency order
            var ordered = ResolveDependencyOrder(candidates);

            // 3. Load each mod in order
            foreach (var candidate in ordered) {
                LoadOneMod(candidate);
            }

            // 4. Print summary
            _registry.LogSummary();

            // 5. Raise GameLaunched now that all mods are loaded
            _events.RaiseGameLaunched();
        }

        // ── Mod discovery ─────────────────────────────────────────────────────

        private List<ModCandidate> DiscoverMods(string modsRoot) {
            var result = new List<ModCandidate>();

            foreach (string dir in Directory.GetDirectories(modsRoot)) {
                string manifestPath = Path.Combine(dir, "manifest.json");
                if (!File.Exists(manifestPath)) {
                    _monitor.Log($"Skipping folder with no manifest.json: {dir}", LogLevel.Warn);
                    continue;
                }

                try {
                    var manifest = ModManifest.Load(manifestPath);

                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(manifest.UniqueID)) {
                        _monitor.Log($"Mod at {dir} has an empty UniqueID — skipping", LogLevel.Error);
                        continue;
                    }
                    if (string.IsNullOrWhiteSpace(manifest.EntryDll)) {
                        _monitor.Log($"Mod '{manifest.Name}' has no EntryDll — skipping", LogLevel.Error);
                        continue;
                    }

                    // Check minimum API version
                    if (manifest.MinimumApiVersion != null) {
                        var smapiVersion = SemanticVersion.Parse(typeof(ModLoader).Assembly
                            .GetName().Version!.ToString(3));
                        if (smapiVersion.IsOlderThan(manifest.MinimumApiVersion)) {
                            _monitor.Log(
                                $"Mod '{manifest.Name}' requires Switch-SMAPI {manifest.MinimumApiVersion} " +
                                $"but this is {smapiVersion} — skipping", LogLevel.Error);
                            continue;
                        }
                    }

                    string dllPath = Path.Combine(dir, manifest.EntryDll);
                    if (!File.Exists(dllPath)) {
                        _monitor.Log($"Mod '{manifest.Name}': EntryDll '{manifest.EntryDll}' not found at {dllPath} — skipping", LogLevel.Error);
                        continue;
                    }

                    result.Add(new ModCandidate(dir, dllPath, manifest));
                } catch (Exception ex) {
                    _monitor.Log($"Failed to parse manifest.json at {manifestPath}: {ex.Message}", LogLevel.Error);
                }
            }

            return result;
        }

        // ── Dependency resolution (topological sort) ──────────────────────────

        private List<ModCandidate> ResolveDependencyOrder(List<ModCandidate> candidates) {
            var byId  = candidates.ToDictionary(c => c.Manifest.UniqueID, StringComparer.OrdinalIgnoreCase);
            var order = new List<ModCandidate>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var inStack  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Visit(ModCandidate candidate) {
                string id = candidate.Manifest.UniqueID;
                if (visited.Contains(id)) return;
                if (inStack.Contains(id)) {
                    _monitor.Log($"Circular dependency detected for mod '{candidate.Manifest.Name}' — skipping", LogLevel.Error);
                    candidate.Skip = true;
                    return;
                }

                inStack.Add(id);

                foreach (var dep in candidate.Manifest.Dependencies) {
                    if (!byId.TryGetValue(dep.UniqueID, out var depCandidate)) {
                        if (dep.IsRequired) {
                            _monitor.Log(
                                $"Mod '{candidate.Manifest.Name}' requires '{dep.UniqueID}' " +
                                $"which is not installed — skipping", LogLevel.Error);
                            candidate.Skip = true;
                        } else {
                            _monitor.Log(
                                $"Optional dependency '{dep.UniqueID}' for '{candidate.Manifest.Name}' is not installed", LogLevel.Debug);
                        }
                        continue;
                    }

                    if (dep.MinimumVersion != null &&
                        depCandidate.Manifest.Version.IsOlderThan(dep.MinimumVersion)) {
                        if (dep.IsRequired) {
                            _monitor.Log(
                                $"Mod '{candidate.Manifest.Name}' requires '{dep.UniqueID}' {dep.MinimumVersion} " +
                                $"but found {depCandidate.Manifest.Version} — skipping", LogLevel.Error);
                            candidate.Skip = true;
                        }
                        continue;
                    }

                    Visit(depCandidate);
                }

                inStack.Remove(id);
                visited.Add(id);

                if (!candidate.Skip) order.Add(candidate);
            }

            foreach (var c in candidates) Visit(c);
            return order;
        }

        // ── Load one mod ──────────────────────────────────────────────────────

        private void LoadOneMod(ModCandidate candidate) {
            string name = candidate.Manifest.Name;
            _monitor.Log($"Loading mod: {name} {candidate.Manifest.Version}", LogLevel.Debug);

            Assembly asm;
            try {
                asm = Assembly.LoadFrom(candidate.DllPath);
            } catch (Exception ex) {
                _monitor.Log($"  Failed to load assembly for '{name}': {ex.Message}", LogLevel.Error);
                return;
            }

            // Find the Mod subclass
            Type? modType = null;
            foreach (Type t in asm.GetExportedTypes()) {
                if (!t.IsAbstract && typeof(Mod).IsAssignableFrom(t)) {
                    modType = t;
                    break;
                }
            }

            if (modType == null) {
                _monitor.Log($"  No Mod subclass found in '{name}' — skipping", LogLevel.Error);
                return;
            }

            Mod modInstance;
            try {
                modInstance = (Mod)Activator.CreateInstance(modType)!;
            } catch (Exception ex) {
                _monitor.Log($"  Failed to instantiate {modType.FullName} for '{name}': {ex.Message}", LogLevel.Error);
                return;
            }

            // Build the helper
            var helper = new ModHelper(
                modDirectory: candidate.Directory,
                manifest:     candidate.Manifest,
                events:       _events,
                inputHelper:  _input,
                logManager:   _logManager,
                globalDataRoot: _globalDataRoot);

            // Wire monitor and helper onto the Mod base
            modInstance.ModManifest = candidate.Manifest;
            modInstance.Monitor     = _logManager.GetMonitor(name);
            modInstance.Helper      = helper;

            // Register before calling Entry so mods can look themselves up
            var entry = new ModEntry(candidate.Manifest, modInstance, helper);
            _registry.Register(entry);

            // Call Entry
            try {
                modInstance.Entry(helper);
                _monitor.Log($"  Loaded: {name}", LogLevel.Info);
            } catch (Exception ex) {
                _monitor.Log($"  Mod '{name}' threw an exception in Entry(): {ex}", LogLevel.Error);
                entry.HasErrors = true;
            }
        }

        // ── Inner types ────────────────────────────────────────────────────────

        private sealed class ModCandidate {
            public string       Directory { get; }
            public string       DllPath   { get; }
            public IManifest    Manifest  { get; }
            public bool         Skip      { get; set; }

            public ModCandidate(string directory, string dllPath, IManifest manifest) {
                Directory = directory;
                DllPath   = dllPath;
                Manifest  = manifest;
            }
        }
    }
}
