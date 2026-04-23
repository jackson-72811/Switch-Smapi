using System;
using Microsoft.Xna.Framework;

namespace SwitchSMAPI.Framework.Events {

    // ── Event argument types ───────────────────────────────────────────────────

    public class MenuChangedEventArgs : EventArgs {
        /// <summary>The menu that was previously open, or null.</summary>
        public object? OldMenu { get; }
        /// <summary>The menu that is now open, or null.</summary>
        public object? NewMenu { get; }
        internal MenuChangedEventArgs(object? oldMenu, object? newMenu) {
            OldMenu = oldMenu; NewMenu = newMenu;
        }
    }

    public class RenderingEventArgs       : EventArgs { }
    public class RenderedEventArgs        : EventArgs { }
    public class RenderingHudEventArgs    : EventArgs { }
    public class RenderedHudEventArgs     : EventArgs { }
    public class RenderingWorldEventArgs  : EventArgs { }
    public class RenderedWorldEventArgs   : EventArgs { }
    public class RenderingActiveMenuEventArgs  : EventArgs { }
    public class RenderedActiveMenuEventArgs   : EventArgs { }

    public class WindowResizedEventArgs : EventArgs {
        public Point OldSize { get; }
        public Point NewSize { get; }
        internal WindowResizedEventArgs(Point oldSize, Point newSize) {
            OldSize = oldSize; NewSize = newSize;
        }
    }

    // ── Interface ──────────────────────────────────────────────────────────────

    /// <summary>Events related to rendering.</summary>
    public interface IDisplayEvents {
        /// <summary>Raised after the active menu changes.</summary>
        event EventHandler<MenuChangedEventArgs>? MenuChanged;

        /// <summary>Raised before the game draws anything to the screen this tick.</summary>
        event EventHandler<RenderingEventArgs>? Rendering;

        /// <summary>Raised after the game draws everything to the screen this tick.</summary>
        event EventHandler<RenderedEventArgs>? Rendered;

        /// <summary>Raised before the HUD is drawn to the screen.</summary>
        event EventHandler<RenderingHudEventArgs>? RenderingHud;

        /// <summary>Raised after the HUD is drawn to the screen.</summary>
        event EventHandler<RenderedHudEventArgs>? RenderedHud;

        /// <summary>Raised before the game world is drawn.</summary>
        event EventHandler<RenderingWorldEventArgs>? RenderingWorld;

        /// <summary>Raised after the game world is drawn.</summary>
        event EventHandler<RenderedWorldEventArgs>? RenderedWorld;

        /// <summary>Raised before the active menu is drawn.</summary>
        event EventHandler<RenderingActiveMenuEventArgs>? RenderingActiveMenu;

        /// <summary>Raised after the active menu is drawn.</summary>
        event EventHandler<RenderedActiveMenuEventArgs>? RenderedActiveMenu;

        /// <summary>Raised after the game window is resized.</summary>
        event EventHandler<WindowResizedEventArgs>? WindowResized;
    }
}
