namespace StardewModdingAPI.Utilities {

    /// <summary>Provides context about the current game state.</summary>
    public static class Context {
        // These mirror Game1's own state flags via reflection at runtime.

        public static bool IsGameLaunched    => _isGameLaunched;
        public static bool IsWorldReady      => GetBool("isWorldReady");
        public static bool IsMultiplayer     => GetBool("IsMultiplayer");
        public static bool IsMainPlayer      => true; // Switch only supports one local player
        public static bool IsSplitScreen     => false;
        public static bool IsOnHomeScreen    => !IsWorldReady;
        public static bool IsPlayerFree      => GetBool("IsPlayerFree");
        public static bool CanPlayerMove     => GetBool("CanPlayerMove");
        public static int  ScreenId          => 0;

        internal static bool _isGameLaunched = false;

        private static bool GetBool(string name) {
            try {
                var t = System.Type.GetType("StardewValley.Context, Stardew Valley")
                     ?? System.Type.GetType("StardewValley.Game1, Stardew Valley");
                if (t == null) return false;
                var p = t.GetProperty(name,
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                if (p != null) return (bool)(p.GetValue(null) ?? false);
                var f = t.GetField(name,
                    System.Reflection.BindingFlags.Static |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic);
                return f != null && (bool)(f.GetValue(null) ?? false);
            } catch { return false; }
        }
    }
}
