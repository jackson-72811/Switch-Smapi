using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI.Events;
using InternalEvents  = SwitchSMAPI.Framework.Events.EventManager;
using InternalSButton = SwitchSMAPI.Framework.Events.SButton;
using InternalCursor  = SwitchSMAPI.Framework.Events.ICursorPosition;

namespace StardewModdingAPI.Framework {

    /// <summary>
    /// Implements <see cref="IModEvents"/> for PC mods running on Switch.
    /// Subscribes to the internal <see cref="InternalEvents"/> and re-raises
    /// events with SMAPI-typed argument objects.
    /// </summary>
    internal sealed class SmapiModEvents : IModEvents {

        // ── Event groups ─────────────────────────────────────────────────────

        public IGameLoopEvents    GameLoop    { get; }
        public IDisplayEvents     Display     { get; }
        public IWorldEvents       World       { get; }
        public IPlayerEvents      Player      { get; }
        public IInputEvents       Input       { get; }
        public IMultiplayerEvents Multiplayer { get; }
        public IContentEvents     Content     { get; }
        public ISpecializedEvents Specialized { get; }

        // ── Construction ──────────────────────────────────────────────────────

        public SmapiModEvents(InternalEvents source) {
            var gl    = new SmapiGameLoopEvents();
            var disp  = new SmapiDisplayEvents();
            var world = new SmapiWorldEvents();
            var play  = new SmapiPlayerEvents();
            var inp   = new SmapiInputEvents();
            var mp    = new SmapiMultiplayerEvents();

            GameLoop    = gl;
            Display     = disp;
            World       = world;
            Player      = play;
            Input       = inp;
            Multiplayer = mp;
            Content     = new StubContentEvents();
            Specialized = new SmapiSpecializedEvents(source);

            WireInternalToCompat(source, gl, disp, world, play, inp, mp);
        }

        // ── Internal-event wiring ─────────────────────────────────────────────

        private static void WireInternalToCompat(
            InternalEvents src,
            SmapiGameLoopEvents    gl,
            SmapiDisplayEvents     disp,
            SmapiWorldEvents       world,
            SmapiPlayerEvents      play,
            SmapiInputEvents       inp,
            SmapiMultiplayerEvents mp)
        {
            // ── Game loop ──────────────────────────────────────────────────────
            src.GameLoop.GameLaunched      += (_, _) => gl.RaiseGameLaunched();
            src.GameLoop.ReturnedToTitle   += (_, _) => gl.RaiseReturnedToTitle();
            src.GameLoop.UpdateTicking     += (_, e) => gl.RaiseUpdateTicking(e.Ticks);
            src.GameLoop.UpdateTicked      += (_, e) => gl.RaiseUpdateTicked(e.Ticks);
            src.GameLoop.OneSecondUpdateTicking += (_, e) => gl.RaiseOneSecondUpdateTicking(e.Ticks);
            src.GameLoop.OneSecondUpdateTicked  += (_, e) => gl.RaiseOneSecondUpdateTicked(e.Ticks);
            src.GameLoop.SaveCreating      += (_, _) => gl.RaiseSaveCreating();
            src.GameLoop.SaveCreated       += (_, _) => gl.RaiseSaveCreated();
            src.GameLoop.Saving            += (_, _) => gl.RaiseSaving();
            src.GameLoop.Saved             += (_, _) => gl.RaiseSaved();
            src.GameLoop.SaveLoaded        += (_, _) => gl.RaiseSaveLoaded();
            src.GameLoop.DayStarted        += (_, _) => gl.RaiseDayStarted();
            src.GameLoop.DayEnding         += (_, _) => gl.RaiseDayEnding();
            src.GameLoop.TimeChanged       += (_, e) => gl.RaiseTimeChanged(e.OldTime, e.NewTime);

            // ── Display ────────────────────────────────────────────────────────
            src.Display.MenuChanged        += (_, e) => disp.RaiseMenuChanged(e.OldMenu, e.NewMenu);
            src.Display.Rendering          += (_, _) => disp.RaiseRendering();
            src.Display.Rendered           += (_, _) => disp.RaiseRendered();
            src.Display.RenderingHud       += (_, _) => disp.RaiseRenderingHud();
            src.Display.RenderedHud        += (_, _) => disp.RaiseRenderedHud();
            src.Display.RenderingWorld     += (_, _) => disp.RaiseRenderingWorld();
            src.Display.RenderedWorld      += (_, _) => disp.RaiseRenderedWorld();
            src.Display.RenderingActiveMenu += (_, _) => disp.RaiseRenderingActiveMenu();
            src.Display.RenderedActiveMenu  += (_, _) => disp.RaiseRenderedActiveMenu();
            src.Display.WindowResized      += (_, e) => disp.RaiseWindowResized(e.OldSize, e.NewSize);

            // ── World ──────────────────────────────────────────────────────────
            src.World.LocationListChanged  += (_, e) => world.RaiseLocationListChanged(e.Added, e.Removed);

            // ── Player ─────────────────────────────────────────────────────────
            src.Player.Warped              += (_, e) => play.RaiseWarped(e.Player, e.OldLocation, e.NewLocation);

            // ── Input ──────────────────────────────────────────────────────────
            src.Input.ButtonPressed  += (_, e) => inp.RaiseButtonPressed(
                SButtonConverter.ToCompat(e.Button), new CursorAdapter(e.Cursor));
            src.Input.ButtonReleased += (_, e) => inp.RaiseButtonReleased(
                SButtonConverter.ToCompat(e.Button), new CursorAdapter(e.Cursor));

            // ── Multiplayer ────────────────────────────────────────────────────
            src.Multiplayer.PeerConnected    += (_, e) => mp.RaisePeerConnected(new PeerAdapter(e.Peer));
            src.Multiplayer.PeerDisconnected += (_, e) => mp.RaisePeerDisconnected(new PeerAdapter(e.Peer));
            src.Multiplayer.ModMessageReceived += (_, e) =>
                mp.RaiseModMessageReceived(e.FromPlayerID, e.FromModID, e.Type);
        }

        // ── CursorPosition adapter ────────────────────────────────────────────

        private sealed class CursorAdapter : ICursorPosition {
            private readonly InternalCursor _c;
            public CursorAdapter(InternalCursor c) => _c = c;
            public Vector2 ScreenPixels   => _c.ScreenPixels;
            public Vector2 Tile           => _c.Tile;
            public Vector2 AbsolutePixels => _c.AbsolutePixels;
            public ICursorPosition Clone() => new CursorAdapter(_c);
        }

        // ── PeerAdapter ───────────────────────────────────────────────────────

        private sealed class PeerAdapter : IMultiplayerPeer {
            private readonly SwitchSMAPI.Framework.Events.IMultiplayerPeer _p;
            public PeerAdapter(SwitchSMAPI.Framework.Events.IMultiplayerPeer p) => _p = p;
            public long    PlayerID     => _p.PlayerID;
            public long?   ScreenID     => null;
            public bool    IsHost       => _p.IsHost;
            public bool    IsSplitScreen => false;
            public bool    HasSmapi     => _p.HasSmapi;
            public string? Platform     => "Switch";
            public ISemanticVersion? ApiVersion  =>
                _p.SmapiVersion != null && SemanticVersion.TryParse(_p.SmapiVersion, out var a) ? a : null;
            public ISemanticVersion? GameVersion =>
                _p.GameVersion != null && SemanticVersion.TryParse(_p.GameVersion, out var g) ? g : null;
            public IEnumerable<IMultiplayerPeerMod> Mods => Array.Empty<IMultiplayerPeerMod>();
            public IMultiplayerPeerMod? GetMod(string id) => null;
        }
    }

    // ── Concrete game-loop event group ────────────────────────────────────────

    internal sealed class SmapiGameLoopEvents : IGameLoopEvents {
        public event EventHandler<GameLaunchedEventArgs>?          GameLaunched;
        public event EventHandler<UpdateTickingEventArgs>?          UpdateTicking;
        public event EventHandler<UpdateTickedEventArgs>?           UpdateTicked;
        public event EventHandler<OneSecondUpdateTickingEventArgs>?  OneSecondUpdateTicking;
        public event EventHandler<OneSecondUpdateTickedEventArgs>?   OneSecondUpdateTicked;
        public event EventHandler<SaveCreatingEventArgs>?           SaveCreating;
        public event EventHandler<SaveCreatedEventArgs>?            SaveCreated;
        public event EventHandler<SavingEventArgs>?                 Saving;
        public event EventHandler<SavedEventArgs>?                  Saved;
        public event EventHandler<SaveLoadedEventArgs>?             SaveLoaded;
        public event EventHandler<DayStartedEventArgs>?             DayStarted;
        public event EventHandler<DayEndingEventArgs>?              DayEnding;
        public event EventHandler<TimeChangedEventArgs>?            TimeChanged;
        public event EventHandler<ReturnedToTitleEventArgs>?        ReturnedToTitle;

        private static void Raise<T>(EventHandler<T>? h, T a) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList())
                try { s(null, a); } catch { }
        }

        public void RaiseGameLaunched()    => Raise(GameLaunched,    new GameLaunchedEventArgs());
        public void RaiseReturnedToTitle() => Raise(ReturnedToTitle, new ReturnedToTitleEventArgs());
        public void RaiseSaveCreating()    => Raise(SaveCreating,    new SaveCreatingEventArgs());
        public void RaiseSaveCreated()     => Raise(SaveCreated,     new SaveCreatedEventArgs());
        public void RaiseSaving()          => Raise(Saving,          new SavingEventArgs());
        public void RaiseSaved()           => Raise(Saved,           new SavedEventArgs());
        public void RaiseSaveLoaded()      => Raise(SaveLoaded,      new SaveLoadedEventArgs());
        public void RaiseDayStarted()      => Raise(DayStarted,      new DayStartedEventArgs());
        public void RaiseDayEnding()       => Raise(DayEnding,       new DayEndingEventArgs());
        public void RaiseUpdateTicking(uint t) => Raise(UpdateTicking, new UpdateTickingEventArgs(t));
        public void RaiseUpdateTicked(uint t)  => Raise(UpdateTicked,  new UpdateTickedEventArgs(t));
        public void RaiseOneSecondUpdateTicking(uint t) => Raise(OneSecondUpdateTicking, new OneSecondUpdateTickingEventArgs(t));
        public void RaiseOneSecondUpdateTicked(uint t)  => Raise(OneSecondUpdateTicked,  new OneSecondUpdateTickedEventArgs(t));
        public void RaiseTimeChanged(int old, int @new) => Raise(TimeChanged, new TimeChangedEventArgs(old, @new));
    }

    // ── Concrete display event group ──────────────────────────────────────────

    internal sealed class SmapiDisplayEvents : IDisplayEvents {
        public event EventHandler<MenuChangedEventArgs>?          MenuChanged;
        public event EventHandler<RenderingEventArgs>?            Rendering;
        public event EventHandler<RenderedEventArgs>?             Rendered;
        public event EventHandler<RenderingHudEventArgs>?         RenderingHud;
        public event EventHandler<RenderedHudEventArgs>?          RenderedHud;
        public event EventHandler<RenderingWorldEventArgs>?       RenderingWorld;
        public event EventHandler<RenderedWorldEventArgs>?        RenderedWorld;
        public event EventHandler<RenderingActiveMenuEventArgs>?  RenderingActiveMenu;
        public event EventHandler<RenderedActiveMenuEventArgs>?   RenderedActiveMenu;
        public event EventHandler<WindowResizedEventArgs>?        WindowResized;

        private static void Raise<T>(EventHandler<T>? h, T a) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) try { s(null, a); } catch { }
        }

        public void RaiseMenuChanged(object? old, object? @new) =>
            Raise(MenuChanged, new MenuChangedEventArgs(old, @new));
        public void RaiseRendering()            => Raise(Rendering,           new RenderingEventArgs());
        public void RaiseRendered()             => Raise(Rendered,            new RenderedEventArgs());
        public void RaiseRenderingHud()         => Raise(RenderingHud,        new RenderingHudEventArgs());
        public void RaiseRenderedHud()          => Raise(RenderedHud,         new RenderedHudEventArgs());
        public void RaiseRenderingWorld()       => Raise(RenderingWorld,      new RenderingWorldEventArgs());
        public void RaiseRenderedWorld()        => Raise(RenderedWorld,       new RenderedWorldEventArgs());
        public void RaiseRenderingActiveMenu()  => Raise(RenderingActiveMenu, new RenderingActiveMenuEventArgs());
        public void RaiseRenderedActiveMenu()   => Raise(RenderedActiveMenu,  new RenderedActiveMenuEventArgs());
        public void RaiseWindowResized(Point o, Point n) =>
            Raise(WindowResized, new WindowResizedEventArgs(o, n));
    }

    // ── Concrete world event group ────────────────────────────────────────────

    internal sealed class SmapiWorldEvents : IWorldEvents {
        public event EventHandler<LocationListChangedEventArgs>?           LocationListChanged;
        public event EventHandler<NpcListChangedEventArgs>?                NpcListChanged;
        public event EventHandler<ObjectListChangedEventArgs>?             ObjectListChanged;
        public event EventHandler<ChestInventoryChangedEventArgs>?         ChestInventoryChanged;
        public event EventHandler<TerrainFeatureListChangedEventArgs>?     TerrainFeatureListChanged;
        public event EventHandler<FurnitureListChangedEventArgs>?          FurnitureListChanged;
        public event EventHandler<DebrisListChangedEventArgs>?             DebrisListChanged;
        public event EventHandler<LargeTerrainFeatureListChangedEventArgs>? LargeTerrainFeatureListChanged;
        public event EventHandler<BuildingListChangedEventArgs>?           BuildingListChanged;

        private static void Raise<T>(EventHandler<T>? h, T a) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) try { s(null, a); } catch { }
        }

        public void RaiseLocationListChanged(
            System.Collections.Generic.IEnumerable<object> added,
            System.Collections.Generic.IEnumerable<object> removed)
        {
            var addedLocations   = new System.Collections.Generic.List<StardewValley.GameLocation>();
            var removedLocations = new System.Collections.Generic.List<StardewValley.GameLocation>();
            foreach (var a in added)   if (a is StardewValley.GameLocation gl) addedLocations.Add(gl);
            foreach (var r in removed) if (r is StardewValley.GameLocation gl) removedLocations.Add(gl);
            Raise(LocationListChanged, new LocationListChangedEventArgs(addedLocations, removedLocations));
        }
    }

    // ── Concrete player event group ───────────────────────────────────────────

    internal sealed class SmapiPlayerEvents : IPlayerEvents {
        public event EventHandler<InventoryChangedEventArgs>? InventoryChanged;
        public event EventHandler<LevelChangedEventArgs>?     LevelChanged;
        public event EventHandler<WarpedEventArgs>?           Warped;

        private static void Raise<T>(EventHandler<T>? h, T a) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) try { s(null, a); } catch { }
        }

        public void RaiseWarped(object player, object from, object to) {
            if (player is StardewValley.Farmer f
             && from   is StardewValley.GameLocation fl
             && to     is StardewValley.GameLocation tl)
            {
                Raise(Warped, new WarpedEventArgs(f, fl, tl));
            }
        }
    }

    // ── Concrete input event group ────────────────────────────────────────────

    internal sealed class SmapiInputEvents : IInputEvents {
        public event EventHandler<ButtonPressedEventArgs>?      ButtonPressed;
        public event EventHandler<ButtonReleasedEventArgs>?     ButtonReleased;
        public event EventHandler<ButtonsChangedEventArgs>?     ButtonsChanged;
        public event EventHandler<CursorMovedEventArgs>?        CursorMoved;
        public event EventHandler<MouseWheelScrolledEventArgs>? MouseWheelScrolled;

        private static void Raise<T>(EventHandler<T>? h, T a) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) try { s(null, a); } catch { }
        }

        public void RaiseButtonPressed(SButton b, ICursorPosition c) =>
            Raise(ButtonPressed,  new ButtonPressedEventArgs(b, c));
        public void RaiseButtonReleased(SButton b, ICursorPosition c) =>
            Raise(ButtonReleased, new ButtonReleasedEventArgs(b, c));
    }

    // ── Concrete multiplayer event group ──────────────────────────────────────

    internal sealed class SmapiMultiplayerEvents : IMultiplayerEvents {
        public event EventHandler<PeerConnectedEventArgs>?      PeerConnected;
        public event EventHandler<PeerDisconnectedEventArgs>?   PeerDisconnected;
        public event EventHandler<ModMessageReceivedEventArgs>? ModMessageReceived;
        public event EventHandler<PeerContextReceivedEventArgs>? PeerContextReceived;

        private static void Raise<T>(EventHandler<T>? h, T a) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) try { s(null, a); } catch { }
        }

        public void RaisePeerConnected(IMultiplayerPeer p) =>
            Raise(PeerConnected, new PeerConnectedEventArgs(p));
        public void RaisePeerDisconnected(IMultiplayerPeer p) =>
            Raise(PeerDisconnected, new PeerDisconnectedEventArgs(p));
        public void RaiseModMessageReceived(long fromId, string fromMod, string type) =>
            Raise(ModMessageReceived, new ModMessageReceivedEventArgs(fromId, fromMod, type, "{}"));
    }

    // ── Stub content events (fired by asset pipeline, not needed for basic compat) ──

    internal sealed class StubContentEvents : IContentEvents {
        public event EventHandler<AssetsInvalidatedEventArgs>? AssetsInvalidated;
        public event EventHandler<AssetReadyEventArgs>?        AssetReady;
        public event EventHandler<AssetRequestedEventArgs>?    AssetRequested;
    }

    // ── Specialized events (unvalidated ticking, load stage) ─────────────────

    internal sealed class SmapiSpecializedEvents : ISpecializedEvents {
        public event EventHandler<LoadStageChangedEventArgs>?         LoadStageChanged;
        public event EventHandler<UnvalidatedUpdateTickingEventArgs>? UnvalidatedUpdateTicking;
        public event EventHandler<UnvalidatedUpdateTickedEventArgs>?  UnvalidatedUpdateTicked;

        private static void Raise<T>(EventHandler<T>? h, T a) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) try { s(null, a); } catch { }
        }

        public SmapiSpecializedEvents(InternalEvents src) {
            // Unvalidated ticking fires every tick regardless of game state
            src.GameLoop.UpdateTicking += (_, e) =>
                Raise(UnvalidatedUpdateTicking, new UnvalidatedUpdateTickingEventArgs(e.Ticks));
            src.GameLoop.UpdateTicked  += (_, e) =>
                Raise(UnvalidatedUpdateTicked, new UnvalidatedUpdateTickedEventArgs(e.Ticks));
        }
    }
}
