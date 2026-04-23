using System;
using SwitchSMAPI.Framework.Events;
using SwitchSMAPI.Framework.Input;
using SwitchSMAPI.Framework.Logging;
using SwitchSMAPI.Framework.Patching;

namespace SwitchSMAPI.Core {

    /// <summary>
    /// Managed entry point for Switch-SMAPI.
    /// Called from native code via mono_runtime_invoke after the Mono domain is created.
    /// </summary>
    public static class Program {

        // Roots are on the SD card, accessible via fsdevMountSdmc in native code.
        private const string SMAPI_ROOT      = "sdmc:/SMAPI";
        private const string MODS_ROOT       = "sdmc:/SMAPI/Mods";
        private const string LOGS_ROOT       = "sdmc:/SMAPI/logs";
        private const string DATA_ROOT       = "sdmc:/SMAPI/data";

        // Kept alive for the lifetime of the process
        private static LogManager? s_logManager;
        private static ModRegistry? s_registry;

        /// <summary>
        /// Entry point called from native bootstrap.
        /// <paramref name="args"/> contains a single element: "switch".
        /// </summary>
        public static void SmapiMain(string[] args) {
            try {
                InternalMain(args);
            } catch (Exception ex) {
                // Last-resort: write to a crash file
                try {
                    System.IO.Directory.CreateDirectory(LOGS_ROOT);
                    System.IO.File.WriteAllText(
                        System.IO.Path.Combine(LOGS_ROOT, "crash.log"),
                        $"SMAPI crashed during startup:\n{ex}");
                } catch { /* truly unrecoverable */ }
            }
        }

        private static void InternalMain(string[] args) {
            // ── Logging ───────────────────────────────────────────────────────
            s_logManager = new LogManager(LOGS_ROOT);
            var monitor  = s_logManager.GetMonitor("SMAPI");

            monitor.Log($"Switch-SMAPI {GetVersion()} started", LogLevel.Info);
            monitor.Log($"Platform: Nintendo Switch / Atmosphere", LogLevel.Info);

            // ── Event system ─────────────────────────────────────────────────
            var eventManager = new EventManager(s_logManager.GetMonitor("Events"));
            var inputHelper  = new InputHelper();

            // ── Game patches ──────────────────────────────────────────────────
            try {
                GamePatcher.Apply(eventManager, inputHelper, s_logManager.GetMonitor("Patcher"));
            } catch (Exception ex) {
                monitor.Log($"Harmony patching failed: {ex.Message}", LogLevel.Error);
                monitor.Log("Events will not fire.  Mods may not work correctly.", LogLevel.Warn);
            }

            // ── PC-mod compatibility: register assembly resolver before loading ──
            //
            // When a PC mod's assembly is loaded, Mono fires AssemblyResolve for
            // "StardewModdingAPI".  We return our compat shim so the mod's Mod
            // subclass can be instantiated without recompilation.
            string compatPath = System.IO.Path.Combine(SMAPI_ROOT, "StardewModdingAPI.dll");
            ModLoader.RegisterAssemblyResolver(compatPath);
            monitor.Log($"Compat resolver registered (shim: {compatPath})", LogLevel.Debug);

            // Apply platform emulation patches (Environment, Process, paths)
            try {
                var harmony = new HarmonyLib.Harmony("switch.smapi.platform");
                SwitchSMAPI.PlatformEmulation.EnvironmentEmulator.Apply(harmony);
                SwitchSMAPI.PlatformEmulation.ProcessEmulator.Apply(harmony);
                monitor.Log("Platform emulation patches applied", LogLevel.Debug);
            } catch (Exception ex) {
                monitor.Log($"Platform emulation patching failed (non-fatal): {ex.Message}", LogLevel.Warn);
            }

            // ── Mod loading ───────────────────────────────────────────────────
            s_registry = new ModRegistry(s_logManager.GetMonitor("Registry"));

            var loader = new ModLoader(
                monitor:        monitor,
                logManager:     s_logManager,
                events:         eventManager,
                input:          inputHelper,
                registry:       s_registry,
                globalDataRoot: DATA_ROOT);

            loader.LoadMods(MODS_ROOT);

            // Control returns to the game here.  All mods are loaded and active.
            // The event system fires asynchronously as Harmony patches intercept game methods.
        }

        private static string GetVersion() {
            var v = typeof(Program).Assembly.GetName().Version;
            return v != null ? $"{v.Major}.{v.Minor}.{v.Build}" : "unknown";
        }
    }
}
