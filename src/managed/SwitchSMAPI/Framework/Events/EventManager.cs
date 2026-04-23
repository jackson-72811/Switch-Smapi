using System;
using SwitchSMAPI.Framework.Logging;

namespace SwitchSMAPI.Framework.Events {

    /// <summary>
    /// Owns all event objects and provides the <see cref="IModEvents"/> root.
    /// Called from Harmony patches to raise events to subscribed mods.
    /// </summary>
    public class EventManager : IModEvents {

        // ── Event group implementations ────────────────────────────────────────

        private readonly GameLoopEvents    _gameLoop;
        private readonly DisplayEvents     _display;
        private readonly WorldEvents       _world;
        private readonly PlayerEvents      _player;
        private readonly InputEvents       _input;
        private readonly MultiplayerEvents _multiplayer;
        private readonly IMonitor          _monitor;

        // ── IModEvents ────────────────────────────────────────────────────────

        public IGameLoopEvents    GameLoop    => _gameLoop;
        public IDisplayEvents     Display     => _display;
        public IWorldEvents       World       => _world;
        public IPlayerEvents      Player      => _player;
        public IInputEvents       Input       => _input;
        public IMultiplayerEvents Multiplayer => _multiplayer;

        // ── Construction ──────────────────────────────────────────────────────

        public EventManager(IMonitor monitor) {
            _monitor     = monitor;
            _gameLoop    = new GameLoopEvents(monitor);
            _display     = new DisplayEvents(monitor);
            _world       = new WorldEvents(monitor);
            _player      = new PlayerEvents(monitor);
            _input       = new InputEvents(monitor);
            _multiplayer = new MultiplayerEvents(monitor);
        }

        // ── Raising methods (called by Harmony patches) ───────────────────────

        public void RaiseGameLaunched()   => _gameLoop.RaiseGameLaunched();
        public void RaiseReturnedToTitle() => _gameLoop.RaiseReturnedToTitle();

        public void RaiseUpdateTicking(uint ticks) => _gameLoop.RaiseUpdateTicking(ticks);
        public void RaiseUpdateTicked(uint ticks)  => _gameLoop.RaiseUpdateTicked(ticks);

        public void RaiseSaveCreating()  => _gameLoop.RaiseSaveCreating();
        public void RaiseSaveCreated()   => _gameLoop.RaiseSaveCreated();
        public void RaiseSaving()        => _gameLoop.RaiseSaving();
        public void RaiseSaved()         => _gameLoop.RaiseSaved();
        public void RaiseSaveLoaded()    => _gameLoop.RaiseSaveLoaded();
        public void RaiseDayStarted()    => _gameLoop.RaiseDayStarted();
        public void RaiseDayEnding()     => _gameLoop.RaiseDayEnding();

        public void RaiseTimeChanged(int oldTime, int newTime)
            => _gameLoop.RaiseTimeChanged(oldTime, newTime);

        public void RaiseMenuChanged(object? oldMenu, object? newMenu)
            => _display.RaiseMenuChanged(oldMenu, newMenu);

        public void RaiseRendering()             => _display.RaiseRendering();
        public void RaiseRendered()              => _display.RaiseRendered();
        public void RaiseRenderingHud()          => _display.RaiseRenderingHud();
        public void RaiseRenderedHud()           => _display.RaiseRenderedHud();
        public void RaiseRenderingWorld()        => _display.RaiseRenderingWorld();
        public void RaiseRenderedWorld()         => _display.RaiseRenderedWorld();
        public void RaiseRenderingActiveMenu()   => _display.RaiseRenderingActiveMenu();
        public void RaiseRenderedActiveMenu()    => _display.RaiseRenderedActiveMenu();

        public void RaiseWindowResized(Microsoft.Xna.Framework.Point oldSize,
                                       Microsoft.Xna.Framework.Point newSize)
            => _display.RaiseWindowResized(oldSize, newSize);

        public void RaiseLocationListChanged(System.Collections.Generic.IEnumerable<object> added,
                                             System.Collections.Generic.IEnumerable<object> removed)
            => _world.RaiseLocationListChanged(added, removed);

        public void RaisePlayerWarped(object player, object from, object to)
            => _player.RaiseWarped(player, from, to);

        public void RaiseButtonPressed(SButton button, ICursorPosition cursor)
            => _input.RaiseButtonPressed(button, cursor);

        public void RaiseButtonReleased(SButton button, ICursorPosition cursor)
            => _input.RaiseButtonReleased(button, cursor);

        public void RaisePeerConnected(IMultiplayerPeer peer)
            => _multiplayer.RaisePeerConnected(peer);

        public void RaisePeerDisconnected(IMultiplayerPeer peer)
            => _multiplayer.RaisePeerDisconnected(peer);

        public void RaiseModMessageReceived(long fromID, string fromMod,
                                            string type, string json)
            => _multiplayer.RaiseModMessageReceived(fromID, fromMod, type, json);
    }

    // ── Concrete event group classes ──────────────────────────────────────────

    internal class GameLoopEvents : IGameLoopEvents {
        private readonly IMonitor _monitor;
        public GameLoopEvents(IMonitor monitor) { _monitor = monitor; }

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

        private void Raise<TArgs>(EventHandler<TArgs>? handler, TArgs args, string name)
            where TArgs : EventArgs
        {
            if (handler == null) return;
            foreach (EventHandler<TArgs> sub in handler.GetInvocationList()) {
                try { sub.Invoke(null, args); }
                catch (Exception ex) {
                    _monitor.Log($"Mod threw exception in {name}: {ex}", LogLevel.Error);
                }
            }
        }

        public void RaiseGameLaunched()    => Raise(GameLaunched,    new GameLaunchedEventArgs(),    nameof(GameLaunched));
        public void RaiseReturnedToTitle() => Raise(ReturnedToTitle, new ReturnedToTitleEventArgs(), nameof(ReturnedToTitle));
        public void RaiseSaveCreating()    => Raise(SaveCreating,    new SaveCreatingEventArgs(),    nameof(SaveCreating));
        public void RaiseSaveCreated()     => Raise(SaveCreated,     new SaveCreatedEventArgs(),     nameof(SaveCreated));
        public void RaiseSaving()          => Raise(Saving,          new SavingEventArgs(),          nameof(Saving));
        public void RaiseSaved()           => Raise(Saved,           new SavedEventArgs(),           nameof(Saved));
        public void RaiseSaveLoaded()      => Raise(SaveLoaded,      new SaveLoadedEventArgs(),      nameof(SaveLoaded));
        public void RaiseDayStarted()      => Raise(DayStarted,      new DayStartedEventArgs(),      nameof(DayStarted));
        public void RaiseDayEnding()       => Raise(DayEnding,       new DayEndingEventArgs(),       nameof(DayEnding));

        public void RaiseUpdateTicking(uint ticks) {
            Raise(UpdateTicking, new UpdateTickingEventArgs(ticks), nameof(UpdateTicking));
            if (ticks % 60 == 0)
                Raise(OneSecondUpdateTicking, new OneSecondUpdateTickingEventArgs(ticks), nameof(OneSecondUpdateTicking));
        }

        public void RaiseUpdateTicked(uint ticks) {
            Raise(UpdateTicked, new UpdateTickedEventArgs(ticks), nameof(UpdateTicked));
            if (ticks % 60 == 0)
                Raise(OneSecondUpdateTicked, new OneSecondUpdateTickedEventArgs(ticks), nameof(OneSecondUpdateTicked));
        }

        public void RaiseTimeChanged(int old, int @new) =>
            Raise(TimeChanged, new TimeChangedEventArgs(old, @new), nameof(TimeChanged));
    }

    internal class DisplayEvents : IDisplayEvents {
        private readonly IMonitor _monitor;
        public DisplayEvents(IMonitor monitor) { _monitor = monitor; }

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

        private void Raise<T>(EventHandler<T>? h, T a, string n) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) {
                try { s(null, a); } catch (Exception ex) {
                    _monitor.Log($"Mod threw in {n}: {ex}", LogLevel.Error);
                }
            }
        }

        public void RaiseMenuChanged(object? old, object? @new) =>
            Raise(MenuChanged, new MenuChangedEventArgs(old, @new), nameof(MenuChanged));
        public void RaiseRendering()           => Raise(Rendering,          new RenderingEventArgs(),          nameof(Rendering));
        public void RaiseRendered()            => Raise(Rendered,           new RenderedEventArgs(),           nameof(Rendered));
        public void RaiseRenderingHud()        => Raise(RenderingHud,       new RenderingHudEventArgs(),       nameof(RenderingHud));
        public void RaiseRenderedHud()         => Raise(RenderedHud,        new RenderedHudEventArgs(),        nameof(RenderedHud));
        public void RaiseRenderingWorld()      => Raise(RenderingWorld,     new RenderingWorldEventArgs(),     nameof(RenderingWorld));
        public void RaiseRenderedWorld()       => Raise(RenderedWorld,      new RenderedWorldEventArgs(),      nameof(RenderedWorld));
        public void RaiseRenderingActiveMenu() => Raise(RenderingActiveMenu,new RenderingActiveMenuEventArgs(),nameof(RenderingActiveMenu));
        public void RaiseRenderedActiveMenu()  => Raise(RenderedActiveMenu, new RenderedActiveMenuEventArgs(), nameof(RenderedActiveMenu));
        public void RaiseWindowResized(Microsoft.Xna.Framework.Point o, Microsoft.Xna.Framework.Point n) =>
            Raise(WindowResized, new WindowResizedEventArgs(o, n), nameof(WindowResized));
    }

    internal class WorldEvents : IWorldEvents {
        private readonly IMonitor _monitor;
        public WorldEvents(IMonitor monitor) { _monitor = monitor; }

        public event EventHandler<LocationListChangedEventArgs>?          LocationListChanged;
        public event EventHandler<NpcListChangedEventArgs>?               NpcListChanged;
        public event EventHandler<ObjectListChangedEventArgs>?            ObjectListChanged;
        public event EventHandler<ChestInventoryChangedEventArgs>?        ChestInventoryChanged;
        public event EventHandler<TerrainFeatureListChangedEventArgs>?    TerrainFeatureListChanged;
        public event EventHandler<FurnitureListChangedEventArgs>?         FurnitureListChanged;
        public event EventHandler<DebrisListChangedEventArgs>?            DebrisListChanged;
        public event EventHandler<LargeTerrainFeatureListChangedEventArgs>? LargeTerrainFeatureListChanged;
        public event EventHandler<BuildingListChangedEventArgs>?          BuildingListChanged;

        private void Raise<T>(EventHandler<T>? h, T a, string n) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) {
                try { s(null, a); } catch (Exception ex) {
                    _monitor.Log($"Mod threw in {n}: {ex}", LogLevel.Error);
                }
            }
        }

        public void RaiseLocationListChanged(System.Collections.Generic.IEnumerable<object> added,
                                             System.Collections.Generic.IEnumerable<object> removed)
            => Raise(LocationListChanged,
                     new LocationListChangedEventArgs(added, removed),
                     nameof(LocationListChanged));
    }

    internal class PlayerEvents : IPlayerEvents {
        private readonly IMonitor _monitor;
        public PlayerEvents(IMonitor monitor) { _monitor = monitor; }

        public event EventHandler<InventoryChangedEventArgs>? InventoryChanged;
        public event EventHandler<LevelChangedEventArgs>?     LevelChanged;
        public event EventHandler<WarpedEventArgs>?           Warped;

        private void Raise<T>(EventHandler<T>? h, T a, string n) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) {
                try { s(null, a); } catch (Exception ex) {
                    _monitor.Log($"Mod threw in {n}: {ex}", LogLevel.Error);
                }
            }
        }

        public void RaiseWarped(object player, object from, object to) =>
            Raise(Warped, new WarpedEventArgs(player, from, to), nameof(Warped));
    }

    internal class InputEvents : IInputEvents {
        private readonly IMonitor _monitor;
        public InputEvents(IMonitor monitor) { _monitor = monitor; }

        public event EventHandler<ButtonPressedEventArgs>?     ButtonPressed;
        public event EventHandler<ButtonReleasedEventArgs>?    ButtonReleased;
        public event EventHandler<ButtonsChangedEventArgs>?    ButtonsChanged;
        public event EventHandler<CursorMovedEventArgs>?       CursorMoved;
        public event EventHandler<MouseWheelScrolledEventArgs>? MouseWheelScrolled;

        private void Raise<T>(EventHandler<T>? h, T a, string n) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) {
                try { s(null, a); } catch (Exception ex) {
                    _monitor.Log($"Mod threw in {n}: {ex}", LogLevel.Error);
                }
            }
        }

        public void RaiseButtonPressed(SButton b, ICursorPosition c) =>
            Raise(ButtonPressed, new ButtonPressedEventArgs(b, c), nameof(ButtonPressed));
        public void RaiseButtonReleased(SButton b, ICursorPosition c) =>
            Raise(ButtonReleased, new ButtonReleasedEventArgs(b, c), nameof(ButtonReleased));
    }

    internal class MultiplayerEvents : IMultiplayerEvents {
        private readonly IMonitor _monitor;
        public MultiplayerEvents(IMonitor monitor) { _monitor = monitor; }

        public event EventHandler<PeerConnectedEventArgs>?      PeerConnected;
        public event EventHandler<PeerDisconnectedEventArgs>?   PeerDisconnected;
        public event EventHandler<ModMessageReceivedEventArgs>? ModMessageReceived;

        private void Raise<T>(EventHandler<T>? h, T a, string n) where T : EventArgs {
            if (h == null) return;
            foreach (EventHandler<T> s in h.GetInvocationList()) {
                try { s(null, a); } catch (Exception ex) {
                    _monitor.Log($"Mod threw in {n}: {ex}", LogLevel.Error);
                }
            }
        }

        public void RaisePeerConnected(IMultiplayerPeer p)    => Raise(PeerConnected,    new PeerConnectedEventArgs(p),    nameof(PeerConnected));
        public void RaisePeerDisconnected(IMultiplayerPeer p) => Raise(PeerDisconnected, new PeerDisconnectedEventArgs(p), nameof(PeerDisconnected));
        public void RaiseModMessageReceived(long id, string mod, string type, string json) =>
            Raise(ModMessageReceived, new ModMessageReceivedEventArgs(id, mod, type, json), nameof(ModMessageReceived));
    }
}
