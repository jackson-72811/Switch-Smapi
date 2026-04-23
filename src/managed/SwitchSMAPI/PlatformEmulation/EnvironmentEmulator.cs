using System;
using System.Collections.Generic;
using HarmonyLib;

namespace SwitchSMAPI.PlatformEmulation {

    /// <summary>
    /// Harmony-patches <c>Environment.GetFolderPath</c> so mods that look up
    /// special folders receive Switch-compatible SD-card paths.
    /// </summary>
    public static class EnvironmentEmulator {

        private static readonly Dictionary<Environment.SpecialFolder, string> s_overrides
            = new Dictionary<Environment.SpecialFolder, string> {
            { Environment.SpecialFolder.ApplicationData,       "sdmc:/SMAPI/AppData"        },
            { Environment.SpecialFolder.LocalApplicationData,  "sdmc:/SMAPI/AppData/Local"  },
            { Environment.SpecialFolder.CommonApplicationData, "sdmc:/SMAPI/AppData/Common" },
            { Environment.SpecialFolder.UserProfile,           "sdmc:/SMAPI/User"           },
            { Environment.SpecialFolder.MyDocuments,           "sdmc:/SMAPI/User/Documents" },
            { Environment.SpecialFolder.Desktop,               "sdmc:/SMAPI/User/Desktop"   },
        };

        public static void Apply(Harmony harmony) {
            var original = typeof(Environment).GetMethod(
                nameof(Environment.GetFolderPath),
                new[] { typeof(Environment.SpecialFolder) });
            if (original == null) return;

            var prefix = typeof(EnvironmentEmulator).GetMethod(
                nameof(PrefixGetFolderPath),
                System.Reflection.BindingFlags.Static |
                System.Reflection.BindingFlags.NonPublic);

            harmony.Patch(original, prefix: new HarmonyMethod(prefix));
        }

        private static bool PrefixGetFolderPath(Environment.SpecialFolder folder, ref string __result) {
            if (s_overrides.TryGetValue(folder, out string? path)) {
                __result = path;
                return false;
            }
            return true;
        }
    }
}
