using System;
using System.Reflection;
using HarmonyLib;
using SwitchSMAPI.Framework.Events;
using SwitchSMAPI.Framework.Input;
using SwitchSMAPI.Framework.Logging;
using Microsoft.Xna.Framework;

namespace SwitchSMAPI.Framework.Patching {

    /// <summary>
    /// Applies all internal Switch-SMAPI Harmony patches and wires them to the
    /// <see cref="EventManager"/> so mods receive events.
    /// </summary>
    internal static class GamePatcher {

        private static Harmony?       s_harmony;
        private static EventManager?  s_events;
        private static InputHelper?   s_input;
        private static IMonitor?      s_monitor;
        private static uint           s_ticks;

        // Reference to the live Game1 type resolved at apply-time
        private static Type? s_game1;

        public static EventManager? Events => s_events;
        public static InputHelper?  Input  => s_input;

        public static void Apply(EventManager events, InputHelper input, IMonitor monitor) {
            s_events  = events;
            s_input   = input;
            s_monitor = monitor;
            s_harmony = new Harmony("com.switch-smapi.core");

            s_game1 = Type.GetType("StardewValley.Game1, Stardew Valley");
            if (s_game1 == null) {
                monitor.Log("Could not resolve StardewValley.Game1 — event patches skipped", LogLevel.Warn);
                return;
            }

            PatchUpdate();
            PatchDraw();
            PatchNewDay();
            PatchSave();
            PatchLoadSave();
            PatchExitToTitle();
            PatchActiveMenuChanged();
            PatchTimePassed();

            monitor.Log("Applied all SMAPI Harmony patches", LogLevel.Debug);
        }

        // ── Game1.Update() ────────────────────────────────────────────────────

        private static void PatchUpdate() {
            var original = s_game1!.GetMethod("Update",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(Microsoft.Xna.Framework.GameTime) }, null);

            if (original == null) {
                s_monitor!.Log("Could not find Game1.Update — UpdateTicked event unavailable", LogLevel.Warn);
                return;
            }

            s_harmony!.Patch(original,
                prefix:  new HarmonyMethod(typeof(GamePatcher), nameof(Game1_Update_Prefix)),
                postfix: new HarmonyMethod(typeof(GamePatcher), nameof(Game1_Update_Postfix)));
        }

        private static void Game1_Update_Prefix() {
            try {
                s_input?.BeginTick();
                s_events?.RaiseUpdateTicking(s_ticks);
            } catch (Exception ex) {
                s_monitor?.Log($"Error in UpdateTicking: {ex}", LogLevel.Error);
            }
        }

        private static void Game1_Update_Postfix() {
            try {
                s_events?.RaiseUpdateTicked(s_ticks);
                ++s_ticks;
            } catch (Exception ex) {
                s_monitor?.Log($"Error in UpdateTicked: {ex}", LogLevel.Error);
            }
        }

        // ── Game1.Draw() ──────────────────────────────────────────────────────

        private static void PatchDraw() {
            var original = s_game1!.GetMethod("Draw",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(Microsoft.Xna.Framework.GameTime) }, null);

            if (original == null) return;

            s_harmony!.Patch(original,
                prefix:  new HarmonyMethod(typeof(GamePatcher), nameof(Game1_Draw_Prefix)),
                postfix: new HarmonyMethod(typeof(GamePatcher), nameof(Game1_Draw_Postfix)));
        }

        private static void Game1_Draw_Prefix()  => s_events?.RaiseRendering();
        private static void Game1_Draw_Postfix() => s_events?.RaiseRendered();

        // ── Game1.newDayAfterFade() ───────────────────────────────────────────

        private static void PatchNewDay() {
            var original = s_game1!.GetMethod("newDayAfterFade",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (original == null) return;

            s_harmony!.Patch(original,
                postfix: new HarmonyMethod(typeof(GamePatcher), nameof(Game1_NewDay_Postfix)));
        }

        private static void Game1_NewDay_Postfix() {
            try {
                s_events?.RaiseDayStarted();
            } catch (Exception ex) {
                s_monitor?.Log($"Error in DayStarted: {ex}", LogLevel.Error);
            }
        }

        // ── SaveGame.Save() ───────────────────────────────────────────────────

        private static void PatchSave() {
            var saveType = Type.GetType("StardewValley.SaveGame, Stardew Valley");
            if (saveType == null) return;

            var saveMethod = saveType.GetMethod("Save",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (saveMethod == null) return;

            s_harmony!.Patch(saveMethod,
                prefix:  new HarmonyMethod(typeof(GamePatcher), nameof(SaveGame_Save_Prefix)),
                postfix: new HarmonyMethod(typeof(GamePatcher), nameof(SaveGame_Save_Postfix)));
        }

        private static void SaveGame_Save_Prefix()  => s_events?.RaiseSaving();
        private static void SaveGame_Save_Postfix() => s_events?.RaiseSaved();

        // ── Game1.loadForNewGame() / loadDataToLocations() ────────────────────

        private static void PatchLoadSave() {
            // loadForNewGame fires when a new save is started; afterLoad fires on continue
            TryPatch(s_game1!, "loadForNewGame",
                postfix: nameof(Game1_SaveLoaded_Postfix));
            TryPatch(s_game1!, "afterLoadSave",
                postfix: nameof(Game1_SaveLoaded_Postfix));
        }

        private static void Game1_SaveLoaded_Postfix() {
            try { s_events?.RaiseSaveLoaded(); }
            catch (Exception ex) { s_monitor?.Log($"Error in SaveLoaded: {ex}", LogLevel.Error); }
        }

        // ── Game1.exitActiveMenu() / activeClickableMenu setter ───────────────

        private static void PatchActiveMenuChanged() {
            // Hook the setter for Game1.activeClickableMenu via Harmony Transpiler
            // is complex — use the property setter wrapper approach instead.
            // This is a simplified version; full implementation would use a Transpiler.
            TryPatch(s_game1!, "set_activeClickableMenu",
                prefix: nameof(Game1_MenuChanged_Prefix));
        }

        private static object? s_lastMenu;
        private static void Game1_MenuChanged_Prefix(object? __0) {
            try {
                object? oldMenu = s_lastMenu;
                s_lastMenu = __0;
                s_events?.RaiseMenuChanged(oldMenu, __0);
            } catch { /* suppress */ }
        }

        // ── Game1.exitToTitle() ───────────────────────────────────────────────

        private static void PatchExitToTitle() {
            TryPatch(s_game1!, "exitToTitle",
                postfix: nameof(Game1_ExitToTitle_Postfix));
        }

        private static void Game1_ExitToTitle_Postfix() {
            try { s_events?.RaiseReturnedToTitle(); }
            catch { /* suppress */ }
        }

        // ── Game1.performTenMinuteClockUpdate() ───────────────────────────────

        private static void PatchTimePassed() {
            TryPatch(s_game1!, "performTenMinuteClockUpdate",
                prefix: nameof(Game1_TimePassed_Prefix));
        }

        private static int s_lastTime;
        private static void Game1_TimePassed_Prefix() {
            try {
                int newTime = GetGameTime();
                if (newTime != s_lastTime) {
                    s_events?.RaiseTimeChanged(s_lastTime, newTime);
                    s_lastTime = newTime;
                }
            } catch { /* suppress */ }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void TryPatch(Type type, string methodName,
                                     string? prefix = null, string? postfix = null) {
            var method = type.GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.Public   | BindingFlags.NonPublic);

            if (method == null) {
                s_monitor?.Log($"Patch target '{type.Name}.{methodName}' not found — skipped", LogLevel.Debug);
                return;
            }

            HarmonyMethod? pre  = prefix  != null ? new HarmonyMethod(typeof(GamePatcher), prefix)  : null;
            HarmonyMethod? post = postfix != null ? new HarmonyMethod(typeof(GamePatcher), postfix) : null;
            s_harmony!.Patch(method, prefix: pre, postfix: post);
        }

        private static int GetGameTime() {
            try {
                var t = Type.GetType("StardewValley.Game1, Stardew Valley");
                var f = t?.GetField("timeOfDay",
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return (int)(f?.GetValue(null) ?? 0);
            } catch { return 0; }
        }
    }
}
