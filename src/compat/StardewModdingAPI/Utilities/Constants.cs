using System;
using System.IO;

namespace StardewModdingAPI.Utilities {

    /// <summary>Constant values used by SMAPI.</summary>
    public static class Constants {

        // ── API version ───────────────────────────────────────────────────────

        public static ISemanticVersion ApiVersion { get; } = new SemanticVersion(4, 0, 0);

        // ── Game version ──────────────────────────────────────────────────────

        public static ISemanticVersion MinimumGameVersion { get; } = new SemanticVersion(1, 5, 4);
        public static ISemanticVersion MaximumGameVersion { get; } = new SemanticVersion(1, 6, 99);

        // ── Platform ──────────────────────────────────────────────────────────

        // Reported as Android so mods targeting non-Windows still load.
        // Most cross-platform mods check for Linux|Mac|Android to skip Windows-only code.
        public static GamePlatform TargetPlatform { get; } = GamePlatform.Android;

        // ── Paths — all redirected to the SD card ─────────────────────────────

        public static string ExecutionPath       { get; } = "sdmc:/";
        public static string ContentPath         { get; } = "sdmc:/atmosphere/contents/0100E65002BB8000/romfs/Content";
        public static string LogDir              { get; } = "sdmc:/SMAPI/logs";
        public static string SavesPath           { get; } = "sdmc:/atmosphere/contents/0100E65002BB8000/romfs/Saves";
        public static string DataPath            { get; } = "sdmc:/SMAPI/data";
        public static string ModsPath            { get; } = "sdmc:/SMAPI/Mods";

        // ── Build ─────────────────────────────────────────────────────────────

        public static string GameFramework       { get; } = "MonoGame";
        public static bool   IsDebugBuild        { get; } =
#if DEBUG
            true;
#else
            false;
#endif
    }

    public enum GamePlatform {
        Android = 0,
        Linux   = 1,
        Mac     = 2,
        Windows = 3,
        iOS     = 4,
        Xbox    = 5,
        PS4     = 6,
        Switch  = 7,    // added — never returned for compatibility
    }
}
