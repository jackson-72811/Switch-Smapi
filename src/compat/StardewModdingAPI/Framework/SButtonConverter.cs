using System.Collections.Generic;
using InternalSButton = SwitchSMAPI.Framework.Events.SButton;

namespace StardewModdingAPI.Framework {

    /// <summary>
    /// Converts between the internal <see cref="InternalSButton"/> and the
    /// compat <see cref="SButton"/> enums, which use different numeric layouts.
    /// </summary>
    internal static class SButtonConverter {

        // ── Internal → Compat ─────────────────────────────────────────────────
        //
        // Keyboard range (< 0x2000) maps directly — both use XNA Keys values.
        // Gamepad and mouse need explicit translation tables.

        private static readonly Dictionary<InternalSButton, SButton> s_toCompat
            = new Dictionary<InternalSButton, SButton> {
            // Gamepad
            { InternalSButton.ControllerA,             SButton.ControllerA             },
            { InternalSButton.ControllerB,             SButton.ControllerB             },
            { InternalSButton.ControllerX,             SButton.ControllerX             },
            { InternalSButton.ControllerY,             SButton.ControllerY             },
            { InternalSButton.ControllerBack,          SButton.ControllerBack          },
            { InternalSButton.ControllerStart,         SButton.ControllerStart         },
            { InternalSButton.ControllerLeftTrigger,   SButton.ControllerLeftTrigger   },
            { InternalSButton.ControllerRightTrigger,  SButton.ControllerRightTrigger  },
            { InternalSButton.ControllerLeftShoulder,  SButton.ControllerLeftShoulder  },
            { InternalSButton.ControllerRightShoulder, SButton.ControllerRightShoulder },
            { InternalSButton.ControllerDPadLeft,      SButton.ControllerDPadLeft      },
            { InternalSButton.ControllerDPadRight,     SButton.ControllerDPadRight     },
            { InternalSButton.ControllerDPadUp,        SButton.ControllerDPadUp        },
            { InternalSButton.ControllerDPadDown,      SButton.ControllerDPadDown      },
            { InternalSButton.ControllerLeftStick,     SButton.ControllerLeftStick     },
            { InternalSButton.ControllerRightStick,    SButton.ControllerRightStick    },
            // Mouse
            { InternalSButton.MouseLeft,   SButton.MouseLeft   },
            { InternalSButton.MouseRight,  SButton.MouseRight  },
            { InternalSButton.MouseMiddle, SButton.MouseMiddle },
            { InternalSButton.MouseX1,     SButton.MouseX1     },
            { InternalSButton.MouseX2,     SButton.MouseX2     },
            // Switch extras
            { InternalSButton.ControllerMinus,   SButton.ControllerMinus   },
            { InternalSButton.ControllerPlus,    SButton.ControllerPlus    },
            { InternalSButton.ControllerCapture, SButton.ControllerCapture },
            { InternalSButton.ControllerHome,    SButton.ControllerHome    },
        };

        private static readonly Dictionary<SButton, InternalSButton> s_toInternal
            = new Dictionary<SButton, InternalSButton>();

        static SButtonConverter() {
            foreach (var (k, v) in s_toCompat)
                s_toInternal[v] = k;
        }

        /// <summary>Convert an internal SButton to its compat equivalent.</summary>
        public static SButton ToCompat(InternalSButton b) {
            if (s_toCompat.TryGetValue(b, out var r)) return r;
            // Keyboard values — same numeric value in both enums
            int v = (int)b;
            if (v > 0 && v < 0x1000) return (SButton)v;
            return SButton.None;
        }

        /// <summary>Convert a compat SButton to its internal equivalent.</summary>
        public static InternalSButton ToInternal(SButton b) {
            if (s_toInternal.TryGetValue(b, out var r)) return r;
            int v = (int)b;
            if (v > 0 && v < 1000) return (InternalSButton)v;
            return InternalSButton.None;
        }
    }
}
