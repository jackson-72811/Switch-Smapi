using System;
using System.Collections.Generic;

namespace SwitchSMAPI.PlatformEmulation {

    /// <summary>
    /// In-memory stub for Windows registry access.
    /// A small number of PC mods read the registry to locate the game installation;
    /// this stub returns sensible Switch paths for the keys they typically request.
    /// </summary>
    public static class RegistryEmulator {

        private static readonly Dictionary<string, string> s_values
            = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 413150\InstallLocation",
              "sdmc:/atmosphere/contents/0100E65002BB8000/romfs" },
            { @"HKEY_CURRENT_USER\Software\Valve\Steam\SteamPath",
              "sdmc:/SMAPI" },
            { @"HKEY_LOCAL_MACHINE\SOFTWARE\GOG.com\Games\1453375253\path",
              "sdmc:/atmosphere/contents/0100E65002BB8000/romfs" },
        };

        /// <summary>Read a registry value.  Returns null if not found.</summary>
        public static string? GetValue(string keyPath, string? valueName = null) {
            string key = string.IsNullOrEmpty(valueName) ? keyPath : keyPath + @"\" + valueName;
            return s_values.TryGetValue(key, out string? v) ? v : null;
        }

        /// <summary>Check whether a registry key exists (always false on Switch).</summary>
        public static bool KeyExists(string keyPath) => s_values.ContainsKey(keyPath);

        /// <summary>Register a custom override (for mod-provided mappings).</summary>
        public static void SetValue(string keyPath, string value)
            => s_values[keyPath] = value;
    }
}
