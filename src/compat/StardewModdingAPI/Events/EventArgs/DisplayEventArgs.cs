using System;
using Microsoft.Xna.Framework;

namespace StardewModdingAPI.Events {

    public class MenuChangedEventArgs : EventArgs {
        public object? OldMenu { get; }
        public object? NewMenu { get; }
        public MenuChangedEventArgs(object? oldMenu, object? newMenu) {
            OldMenu = oldMenu; NewMenu = newMenu;
        }
    }

    public class RenderingEventArgs         : EventArgs { }
    public class RenderedEventArgs          : EventArgs { }
    public class RenderingHudEventArgs      : EventArgs { }
    public class RenderedHudEventArgs       : EventArgs { }
    public class RenderingWorldEventArgs    : EventArgs { }
    public class RenderedWorldEventArgs     : EventArgs { }
    public class RenderingActiveMenuEventArgs : EventArgs { }
    public class RenderedActiveMenuEventArgs  : EventArgs { }
    public class RenderingStepEventArgs     : EventArgs {
        public RenderingStep Step { get; }
        public RenderingStepEventArgs(RenderingStep step) { Step = step; }
    }

    public class WindowResizedEventArgs : EventArgs {
        public Point OldSize { get; }
        public Point NewSize { get; }
        public WindowResizedEventArgs(Point oldSize, Point newSize) {
            OldSize = oldSize; NewSize = newSize;
        }
    }

    /// <summary>Rendering steps used by ISpecializedEvents.Rendering step events.</summary>
    public enum RenderingStep {
        Sorted,
        World,
        WorldFrontLayers,
        WorldMiniMap,
        Lights,
        Weather,
        WorldAboveWeather,
        Tools,
        Player,
        FrontLayer,
        Cutscene,
        HudUnderToolbar,
        Toolbar,
        Hud,
        Letterbox,
        Transition,
        TextDisplay,
        Screen,
    }
}
