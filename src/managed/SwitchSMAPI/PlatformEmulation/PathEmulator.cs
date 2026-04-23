using System;

namespace SwitchSMAPI.PlatformEmulation {

    /// <summary>
    /// Translates Windows-style absolute paths that PC mods may construct
    /// into SD-card equivalents so file operations don't fail on Switch.
    /// </summary>
    public static class PathEmulator {

        private const string SdRoot = "sdmc:/SMAPI";

        /// <summary>
        /// If <paramref name="path"/> looks like a Windows absolute path, remap it.
        /// Otherwise returns the path unchanged.
        /// </summary>
        public static string Remap(string path) {
            if (string.IsNullOrEmpty(path)) return path;

            if (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':') {
                string rest = path[2..].Replace('\\', '/').TrimStart('/');
                return SdRoot + "/Windows/" + rest;
            }

            if (path.StartsWith(@"\\", StringComparison.Ordinal)) {
                string rest = path[2..].Replace('\\', '/');
                return SdRoot + "/UNC/" + rest;
            }

            return path.Replace('\\', '/');
        }

        /// <summary>Normalise path separators to forward-slashes (fatFS on SD card).</summary>
        public static string Normalise(string path)
            => path?.Replace('\\', '/') ?? string.Empty;
    }
}
