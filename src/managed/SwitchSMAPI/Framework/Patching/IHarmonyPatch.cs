using HarmonyLib;

namespace SwitchSMAPI.Framework.Patching {

    /// <summary>
    /// Implemented by internal SMAPI patches.  Each patch class applies a specific
    /// set of Harmony prefixes / postfixes and raises the corresponding events on the
    /// global <see cref="GamePatcher.Events"/> instance.
    /// </summary>
    internal interface IHarmonyPatch {
        /// <summary>Apply this patch to the given Harmony instance.</summary>
        void Apply(Harmony harmony);
    }
}
