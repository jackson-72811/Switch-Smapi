using System.Diagnostics;
using HarmonyLib;

namespace SwitchSMAPI.PlatformEmulation {

    /// <summary>
    /// Harmony-patches <c>Process.Start</c> to a no-op so mods that try to open
    /// URLs or executables don't crash on Switch where there is no OS shell.
    /// </summary>
    public static class ProcessEmulator {

        public static void Apply(Harmony harmony) {
            var flags = System.Reflection.BindingFlags.Static
                      | System.Reflection.BindingFlags.NonPublic;

            var noopRet = typeof(ProcessEmulator).GetMethod(nameof(NoopReturn), flags);

            foreach (var types in new[] {
                new[] { typeof(string)                },
                new[] { typeof(string), typeof(string)},
                new[] { typeof(ProcessStartInfo)      },
            }) {
                var m = typeof(Process).GetMethod(nameof(Process.Start), types);
                if (m != null) harmony.Patch(m, prefix: new HarmonyMethod(noopRet));
            }
        }

        private static bool NoopReturn(ref Process? __result) {
            __result = null;
            return false;
        }
    }
}
