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

        // Path to the compat StardewModdingAPI.dll that lives next to SwitchSMAPI.dll
        private static string? s_compatAssemblyPath;
        private static Assembly? s_compatAssembly;

        /// <summary>
        /// Register the AssemblyResolve hook once so PC mods that reference
        /// "StardewModdingAPI" receive our compat shim instead of failing to load.
        /// Call this before <see cref="LoadMods"/>.
        /// </summary>
        public static void RegisterAssemblyResolver(string compatAssemblyPath) {
            s_compatAssemblyPath = compatAssemblyPath;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args) {
            var name = new AssemblyName(args.Name);
            if (!string.Equals(name.Name, "StardewModdingAPI", StringComparison.OrdinalIgnoreCase))
                return null;

            if (s_compatAssembly != null) return s_compatAssembly;

            if (s_compatAssemblyPath != null && File.Exists(s_compatAssemblyPath)) {
                s_compatAssembly = Assembly.LoadFrom(s_compatAssemblyPath);
                return s_compatAssembly;
            }

            // Try same directory as SwitchSMAPI.dll
            string dir      = Path.GetDirectoryName(typeof(ModLoader).Assembly.Location) ?? string.Empty;
            string fallback = Path.Combine(dir, "StardewModdingAPI.dll");
            if (File.Exists(fallback)) {
                s_compatAssembly = Assembly.LoadFrom(fallback);
                return s_compatAssembly;
            }

            return null;
        }

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

            // ── Check for PC mods that subclass StardewModdingAPI.Mod (compat path) ──
            //
            // We detect via string comparison on the base-type name so that SwitchSMAPI.dll
            // does NOT need a compile-time reference to StardewModdingAPI.dll — which would
            // create a circular dependency.  At runtime the compat assembly is already loaded
            // by the AssemblyResolve hook before we get here.
            //
            bool isSmapiMod = false;
            Type? smapiModType = null;
            foreach (Type t in asm.GetExportedTypes()) {
                if (t.IsAbstract) continue;
                Type? baseType = t.BaseType;
                while (baseType != null) {
                    if (baseType.FullName == "StardewModdingAPI.Mod") {
                        isSmapiMod  = true;
                        smapiModType = t;
                        break;
                    }
                    baseType = baseType.BaseType;
                }
                if (isSmapiMod) break;
            }

            if (isSmapiMod && smapiModType != null) {
                LoadSmapiMod(candidate, smapiModType);
                return;
            }

            // ── Native SwitchSMAPI mod path ────────────────────────────────────

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

        // ── PC mod loader (via StardewModdingAPI compat shim) ─────────────────
        //
        // The mod subclasses StardewModdingAPI.Mod.  We create a SmapiModHelper
        // (from the compat assembly) and wire it using the same reflection trick.

        private void LoadSmapiMod(ModCandidate candidate, Type modType) {
            string name = candidate.Manifest.Name;
            _monitor.Log($"  Detected as PC mod (StardewModdingAPI.Mod) — using compat shim", LogLevel.Debug);

            // Instantiate the PC mod
            object modInstance;
            try {
                modInstance = Activator.CreateInstance(modType)!;
            } catch (Exception ex) {
                _monitor.Log($"  Failed to instantiate {modType.FullName} for '{name}': {ex.Message}", LogLevel.Error);
                return;
            }

            // Build the compat IModHelper via StardewModdingAPI.Framework.SmapiModHelper
            // We do everything via reflection so SwitchSMAPI.dll doesn't reference the compat dll.
            Assembly? compatAsm = s_compatAssembly;
            if (compatAsm == null) {
                _monitor.Log($"  Compat assembly (StardewModdingAPI.dll) not loaded — cannot load PC mod '{name}'", LogLevel.Error);
                return;
            }

            Type? helperType = compatAsm.GetType("StardewModdingAPI.Framework.SmapiModHelper");
            if (helperType == null) {
                _monitor.Log($"  SmapiModHelper type not found in compat assembly — skipping '{name}'", LogLevel.Error);
                return;
            }

            // Build the manifest adapter: SmapiModHelper expects StardewModdingAPI.IManifest.
            // We pass the same IManifest instance from our internal manifest load;
            // SmapiModHelper only uses it for Name/UniqueID/Version which are interface-compatible.
            object? smapiHelper;
            try {
                smapiHelper = Activator.CreateInstance(helperType,
                    candidate.Directory,    // string modDirectory
                    candidate.Manifest,     // IManifest  (duck-typed — same shape)
                    _events,               // InternalEventManager
                    _input,                // InternalInputHelper
                    _logManager,           // InternalLogManager
                    _globalDataRoot        // string globalDataRoot
                );
            } catch (Exception ex) {
                _monitor.Log($"  Failed to create SmapiModHelper for '{name}': {ex.Message}", LogLevel.Error);
                return;
            }

            // Wire properties on the mod instance using reflection
            try {
                // Walk up to find the StardewModdingAPI.Mod base class (where the property setters live)
                Type? baseType = modType;
                while (baseType != null && baseType.FullName != "StardewModdingAPI.Mod")
                    baseType = baseType.BaseType;

                if (baseType == null) {
                    _monitor.Log($"  Could not find StardewModdingAPI.Mod in hierarchy for '{name}'", LogLevel.Error);
                    return;
                }

                // Create a compat-typed manifest adapter so the Mod.ModManifest property gets the right type
                object? adaptedManifest = candidate.Manifest;
                Type? manifestAdapterType = compatAsm.GetType("StardewModdingAPI.Framework.SmapiManifestAdapter");
                if (manifestAdapterType != null)
                    adaptedManifest = Activator.CreateInstance(manifestAdapterType, candidate.Manifest);

                // Create a compat-typed monitor wrapper
                object? smapiMonitor = null;
                Type? monitorType = compatAsm.GetType("StardewModdingAPI.Framework.SmapiMonitor");
                if (monitorType != null) {
                    var innerMonitor = _logManager.GetMonitor(name);
                    smapiMonitor = Activator.CreateInstance(monitorType, innerMonitor);
                }

                var propFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                baseType.GetProperty("ModManifest", propFlags)?.SetValue(modInstance, adaptedManifest);
                if (smapiMonitor != null)
                    baseType.GetProperty("Monitor", propFlags)?.SetValue(modInstance, smapiMonitor);
                baseType.GetProperty("Helper", propFlags)?.SetValue(modInstance, smapiHelper);

            } catch (Exception ex) {
                _monitor.Log($"  Failed to wire mod properties for '{name}': {ex.Message}", LogLevel.Error);
                return;
            }

            // Register before calling Entry so mods can look themselves up
            var entry = new SmapiModEntry(candidate.Manifest, modInstance, name);
            _registry.RegisterSmapiMod(entry);

            // Call Entry(helper) — mod declares Entry(IModHelper) on the actual type
            try {
                var entryMethod = modType.GetMethod("Entry", BindingFlags.Instance | BindingFlags.Public);
                entryMethod?.Invoke(modInstance, new[] { smapiHelper });
                _monitor.Log($"  Loaded (PC mod): {name}", LogLevel.Info);
            } catch (Exception ex) {
                _monitor.Log($"  PC mod '{name}' threw an exception in Entry(): {ex}", LogLevel.Error);
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
