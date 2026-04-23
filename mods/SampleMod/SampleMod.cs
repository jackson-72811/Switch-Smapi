using System;
using System.Collections.Generic;
using SwitchSMAPI.Framework;
using SwitchSMAPI.Framework.Events;
using SwitchSMAPI.Framework.Logging;

namespace SampleMod {

    // ── Config ──────────────────────────────────────────────────────────────────

    /// <summary>User-editable config for this mod, stored in the mod folder as config.json.</summary>
    internal class ModConfig {
        /// <summary>Print a log message every N ticks (default 600 = every 10 seconds at 60 TPS).</summary>
        public int LogIntervalTicks { get; set; } = 600;

        /// <summary>If true, announce every location the player enters.</summary>
        public bool AnnounceWarps { get; set; } = true;

        /// <summary>Custom greeting shown on the first game tick after a save loads.</summary>
        public string WelcomeMessage { get; set; } = "Welcome back, farmer!";
    }

    // ── Mod entry point ─────────────────────────────────────────────────────────

    /// <summary>The main mod class.  SMAPI finds this automatically because it extends <see cref="Mod"/>.</summary>
    public class ModEntry : Mod {

        private ModConfig _config = new ModConfig();
        private uint      _tickCount;
        private bool      _saveLoaded;

        public override void Entry(IModHelper helper) {
            // Load config (creates config.json with defaults if it doesn't exist)
            _config = helper.ReadConfig<ModConfig>() ?? new ModConfig();
            helper.WriteConfig(_config); // persist defaults so the user can edit them

            Monitor.Log("Sample Mod loaded successfully!", LogLevel.Info);
            Monitor.Log($"Config: LogIntervalTicks={_config.LogIntervalTicks}, " +
                        $"AnnounceWarps={_config.AnnounceWarps}", LogLevel.Debug);

            // ── Subscribe to events ──────────────────────────────────────────

            helper.Events.GameLoop.GameLaunched    += OnGameLaunched;
            helper.Events.GameLoop.SaveLoaded      += OnSaveLoaded;
            helper.Events.GameLoop.DayStarted      += OnDayStarted;
            helper.Events.GameLoop.DayEnding       += OnDayEnding;
            helper.Events.GameLoop.Saving          += OnSaving;
            helper.Events.GameLoop.Saved           += OnSaved;
            helper.Events.GameLoop.UpdateTicked    += OnUpdateTicked;
            helper.Events.GameLoop.TimeChanged     += OnTimeChanged;
            helper.Events.GameLoop.ReturnedToTitle += OnReturnedToTitle;

            helper.Events.Player.Warped            += OnWarped;
            helper.Events.Player.InventoryChanged  += OnInventoryChanged;
            helper.Events.Player.LevelChanged      += OnLevelChanged;

            helper.Events.World.LocationListChanged += OnLocationListChanged;

            helper.Events.Input.ButtonPressed      += OnButtonPressed;

            helper.Events.Display.MenuChanged      += OnMenuChanged;
        }

        // ── Game loop events ─────────────────────────────────────────────────

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e) {
            Monitor.Log("=== GameLaunched: all mods are ready. ===", LogLevel.Info);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e) {
            _saveLoaded = true;
            _tickCount  = 0;
            Monitor.Log(_config.WelcomeMessage, LogLevel.Info);
        }

        private void OnDayStarted(object? sender, DayStartedEventArgs e) {
            Monitor.Log("A new day has started!", LogLevel.Info);
        }

        private void OnDayEnding(object? sender, DayEndingEventArgs e) {
            Monitor.Log("The day is ending — time to sleep.", LogLevel.Debug);
        }

        private void OnSaving(object? sender, SavingEventArgs e) {
            Monitor.Log("Game is saving...", LogLevel.Debug);

            // Example: persist per-save data
            var saveData = new SaveData { TicksPlayed = _tickCount };
            Helper.Data.WriteSaveData("stats", saveData);
        }

        private void OnSaved(object? sender, SavedEventArgs e) {
            Monitor.Log("Game saved.", LogLevel.Debug);
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e) {
            if (!_saveLoaded) return;
            ++_tickCount;

            if (_config.LogIntervalTicks > 0 && e.IsMultipleOf((uint)_config.LogIntervalTicks)) {
                Monitor.Log($"[SampleMod] Tick {_tickCount} (total in session)", LogLevel.Trace);
            }
        }

        private void OnTimeChanged(object? sender, TimeChangedEventArgs e) {
            Monitor.Log($"Time changed: {FormatTime(e.OldTime)} → {FormatTime(e.NewTime)}", LogLevel.Trace);
        }

        private void OnReturnedToTitle(object? sender, ReturnedToTitleEventArgs e) {
            _saveLoaded = false;
            Monitor.Log("Returned to title screen.", LogLevel.Debug);
        }

        // ── Player events ────────────────────────────────────────────────────

        private void OnWarped(object? sender, WarpedEventArgs e) {
            if (!_config.AnnounceWarps) return;
            Monitor.Log($"Player warped to: {e.NewLocation}", LogLevel.Info);
        }

        private void OnInventoryChanged(object? sender, InventoryChangedEventArgs e) {
            foreach (var item in e.Added)
                Monitor.Log($"Picked up: {item}", LogLevel.Debug);
            foreach (var item in e.Removed)
                Monitor.Log($"Lost: {item}", LogLevel.Debug);
        }

        private void OnLevelChanged(object? sender, LevelChangedEventArgs e) {
            Monitor.Log($"Level up! {e.Skill}: {e.OldLevel} → {e.NewLevel}", LogLevel.Info);
        }

        // ── World events ─────────────────────────────────────────────────────

        private void OnLocationListChanged(object? sender, LocationListChangedEventArgs e) {
            foreach (var loc in e.Added)
                Monitor.Log($"Location added to world: {loc}", LogLevel.Debug);
        }

        // ── Input events ─────────────────────────────────────────────────────

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e) {
            // Example: when the player presses Y, print their position
            if (e.Button == SButton.ControllerY) {
                Monitor.Log($"Cursor at tile {e.Cursor.Tile}", LogLevel.Info);
            }
        }

        // ── Display events ────────────────────────────────────────────────────

        private void OnMenuChanged(object? sender, MenuChangedEventArgs e) {
            if (e.NewMenu != null)
                Monitor.Log($"Menu opened: {e.NewMenu.GetType().Name}", LogLevel.Trace);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string FormatTime(int time) {
            int hours   = time / 100;
            int minutes = time % 100;
            string period = hours >= 12 ? "PM" : "AM";
            int displayH  = hours > 12 ? hours - 12 : hours == 0 ? 12 : hours;
            return $"{displayH}:{minutes:D2} {period}";
        }

        // ── Per-save data structure ───────────────────────────────────────────

        private class SaveData {
            public uint TicksPlayed { get; set; }
        }
    }
}
