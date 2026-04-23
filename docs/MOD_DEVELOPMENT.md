# Mod Development Guide

This guide explains how to write a mod for Switch-SMAPI.  The API is designed to be source-compatible with desktop SMAPI so existing PC mods usually require only a project file change and a recompile.

## 1. Project setup

### Using the template

The fastest way to start is the sample mod in `mods/SampleMod/`.  Copy that folder, rename it, and update `manifest.json`.

### Manual setup

Create a `netstandard2.0` class library:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <Nullable>enable</Nullable>
    <Optimize>true</Optimize>
  </PropertyGroup>

  <ItemGroup>
    <!-- Reference Switch-SMAPI, not the desktop SMAPI package -->
    <Reference Include="SwitchSMAPI">
      <HintPath>$(SwitchSMAPISDK)\SwitchSMAPI.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <!-- Stardew Valley assemblies -->
    <Reference Include="Stardew Valley">
      <HintPath>$(StardewSwitchDump)\Stardew Valley.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

Set environment variables:
- `SwitchSMAPISDK` — path to the `sdk/` folder from the Switch-SMAPI release
- `StardewSwitchDump` — path to a legal dump of the game assemblies

## 2. Manifest

Every mod folder must contain `manifest.json`:

```json
{
  "Name": "My Awesome Mod",
  "Author": "YourName",
  "Version": "1.0.0",
  "Description": "Does something awesome.",
  "UniqueID": "YourName.MyAwesomeMod",
  "EntryDll": "MyAwesomeMod.dll",
  "MinimumApiVersion": "1.0.0",
  "Dependencies": [
    {
      "UniqueID": "Pathoschild.ContentPatcher",
      "MinimumVersion": "1.30.0",
      "IsRequired": false
    }
  ],
  "UpdateKeys": []
}
```

### Manifest fields

| Field | Required | Description |
|---|---|---|
| `Name` | Yes | Human-readable mod name |
| `Author` | Yes | Author name |
| `Version` | Yes | Semantic version (`major.minor.patch`) |
| `Description` | Yes | Short description |
| `UniqueID` | Yes | Globally unique identifier (`Author.ModName`) |
| `EntryDll` | Yes | Filename of the mod DLL inside this folder |
| `MinimumApiVersion` | No | Minimum Switch-SMAPI version required |
| `Dependencies` | No | Other mods this mod depends on |
| `UpdateKeys` | No | Keys for the update checker (not used on Switch) |

## 3. Entry point

Every mod must have exactly one class that extends `SwitchSMAPI.Framework.Mod`:

```csharp
using SwitchSMAPI.Framework;
using SwitchSMAPI.Framework.Events;
using SwitchSMAPI.Framework.Logging;

namespace MyAwesomeMod {
    public class ModEntry : Mod {
        public override void Entry(IModHelper helper) {
            Monitor.Log("Hello from My Awesome Mod!", LogLevel.Info);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.World.LocationListChanged += OnLocationListChanged;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e) {
            Monitor.Log("Game launched — all mods are ready.", LogLevel.Debug);
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e) {
            if (!e.IsMultipleOf(60)) return;   // once per second
            Monitor.Log($"Tick {e.Ticks}", LogLevel.Trace);
        }

        private void OnLocationListChanged(object sender, LocationListChangedEventArgs e) {
            foreach (var loc in e.Added)
                Monitor.Log($"Location added: {loc.Name}", LogLevel.Debug);
        }
    }
}
```

`Entry(IModHelper helper)` is called by SMAPI after all mods in the dependency chain have loaded.  Never call game code inside the constructor — always do it in `Entry` or in event handlers.

## 4. Events

Events are accessed via `helper.Events.*`.  The event system is split into logical groups:

### GameLoop

| Event | When |
|---|---|
| `GameLaunched` | After all mods are loaded, before the title screen |
| `UpdateTicking` | Before each update tick |
| `UpdateTicked` | After each update tick |
| `OneSecondUpdateTicking` | Before update ticks that are multiples of 60 |
| `OneSecondUpdateTicked` | After update ticks that are multiples of 60 |
| `SaveCreating` | Before a new save file is created |
| `SaveCreated` | After a new save file is created |
| `Saving` | Before the game saves |
| `Saved` | After the game saves |
| `SaveLoaded` | After a save file is loaded |
| `DayStarted` | After the day begins (after waking up) |
| `DayEnding` | Before the day ends (before sleeping) |
| `TimeChanged` | When in-game time changes |
| `ReturnedToTitle` | When the player returns to the title screen |

### Display

| Event | When |
|---|---|
| `MenuChanged` | When the active menu changes |
| `Rendering` | Before the game draws the current frame |
| `Rendered` | After the game draws the current frame |
| `RenderingHud` | Before the HUD is drawn |
| `RenderedHud` | After the HUD is drawn |
| `RenderingWorld` | Before the world is drawn |
| `RenderedWorld` | After the world is drawn |
| `WindowResized` | When the game window is resized |

### World

| Event | When |
|---|---|
| `LocationListChanged` | When locations are added/removed from the world |
| `NpcListChanged` | When NPCs are added/removed from a location |
| `ObjectListChanged` | When objects are added/removed from a location |
| `ChestInventoryChanged` | When items in a chest change |
| `TerrainFeatureListChanged` | When terrain features change |
| `FurnitureListChanged` | When furniture in a location changes |
| `DebrisListChanged` | When debris changes |
| `LargeTerrainFeatureListChanged` | When large terrain features change |
| `BuildingListChanged` | When buildings change |

### Player

| Event | When |
|---|---|
| `InventoryChanged` | When the player's inventory changes |
| `LevelChanged` | When a player skill level increases |
| `Warped` | When the player moves to a different location |

### Input

| Event | When |
|---|---|
| `ButtonPressed` | When a button is pressed |
| `ButtonReleased` | When a button is released |
| `ButtonsChanged` | When the set of pressed buttons changes |
| `CursorMoved` | When the cursor/pointer moves |
| `MouseWheelScrolled` | When the mouse wheel scrolls (docked mode) |

### Multiplayer

| Event | When |
|---|---|
| `PeerConnected` | When a player joins the session |
| `PeerDisconnected` | When a player leaves |
| `ModMessageReceived` | When a mod message is received from another player |

## 5. Content helpers

### Loading assets

```csharp
// Load a Texture2D from your mod folder
var texture = helper.Content.Load<Texture2D>("assets/mysprite.png");

// Load a dictionary from a JSON file in your mod folder  
var data = helper.Content.Load<Dictionary<string, string>>("assets/data.json");

// Load a game asset
var farmTexture = helper.Content.Load<Texture2D>("Maps/Farm", ContentSource.GameContent);
```

### Editing game content

```csharp
// In Entry():
helper.Content.AssetEditors.Add(new MyAssetEditor());

// Asset editor class:
public class MyAssetEditor : IAssetEditor {
    public bool CanEdit<T>(IAssetInfo asset) {
        return asset.AssetNameEquals("Data/ObjectInformation");
    }

    public void Edit<T>(IAssetData asset) {
        var data = asset.AsDictionary<int, string>().Data;
        data[1000] = "My Custom Item/100/1/Basic/My Custom Item/My custom item description.";
    }
}
```

## 6. Data helpers

Store and retrieve per-mod data on the SD card:

```csharp
// Save data (stored in sdmc:/SMAPI/data/<UniqueID>/)
helper.Data.WriteJsonFile("config.json", new MyConfig { Speed = 5 });
var config = helper.Data.ReadJsonFile<MyConfig>("config.json") ?? new MyConfig();

// Global save data (shared across save files)
helper.Data.WriteGlobalData("global-key", myValue);
var value = helper.Data.ReadGlobalData<MyType>("global-key");

// Per-save data (tied to the current save file)
helper.Data.WriteSaveData("save-key", myValue);
var saveValue = helper.Data.ReadSaveData<MyType>("save-key");
```

## 7. Translation / i18n

Place locale files in your mod's `i18n/` subfolder:

```
MyMod/
├── manifest.json
├── MyMod.dll
└── i18n/
    ├── default.json    ← English (always required)
    ├── de.json
    └── zh.json
```

`default.json`:
```json
{
  "greeting": "Hello, {{name}}!",
  "item-count": "You have {{count}} items."
}
```

Usage:
```csharp
string greeting = helper.Translation.Get("greeting", new { name = "Player" });
string count    = helper.Translation.Get("item-count", new { count = 5 });
```

## 8. Reflection helper

Access private game members safely:

```csharp
// Read a private field
int health = helper.Reflection.GetField<int>(Game1.player, "health").GetValue();

// Write a private field
helper.Reflection.GetField<int>(Game1.player, "health").SetValue(100);

// Call a private method
helper.Reflection.GetMethod(Game1.player, "somePrivateMethod").Invoke();

// Get a private property
string name = helper.Reflection.GetProperty<string>(Game1.player, "Name").GetValue();
```

## 9. Harmony patching

Use Harmony to patch game methods.  Always use the Harmony ID injected for your mod:

```csharp
using HarmonyLib;

// In Entry():
var harmony = new Harmony(ModManifest.UniqueID);
harmony.PatchAll();   // discovers all [HarmonyPatch] attributes in the assembly

// Or patch manually:
harmony.Patch(
    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.gainExperience)),
    postfix:  new HarmonyMethod(typeof(MyPatches), nameof(MyPatches.AfterGainExperience))
);
```

## 10. Multiplayer messaging

Send and receive messages between mod instances in a multiplayer session:

```csharp
// Send to all players
helper.Multiplayer.SendMessage(
    message:    new MyMessage { Text = "hello" },
    messageType: "greeting",
    modIDs:      new[] { ModManifest.UniqueID }
);

// Receive
helper.Events.Multiplayer.ModMessageReceived += (s, e) => {
    if (e.FromModID == ModManifest.UniqueID && e.Type == "greeting") {
        var msg = e.ReadAs<MyMessage>();
        Monitor.Log($"Received: {msg.Text}", LogLevel.Info);
    }
};
```

## 11. Logging

Use `Monitor` (available in any `Mod` subclass) or `IMonitor` injected via the helper:

```csharp
Monitor.Log("Information message", LogLevel.Info);
Monitor.Log("Debug detail",        LogLevel.Debug);
Monitor.Log("Trace-level noise",   LogLevel.Trace);
Monitor.Log("Warning!",            LogLevel.Warn);
Monitor.Log("Non-fatal error",     LogLevel.Error);
Monitor.LogOnce("Only logged once per session", LogLevel.Info);
```

Log output is written to `sdmc:/SMAPI/logs/SMAPI-latest.log` and rotated to `SMAPI-previous.log`.

## 12. Switch-specific notes

- **No XNA** — The Switch version uses FNA.  Do not import `Microsoft.Xna.Framework` — use `Microsoft.Xna.Framework` from the FNA assemblies in the game dump.
- **No mouse cursor** — In handheld mode there is no cursor.  Use `SButton` values from the Switch controller (`SButton.ControllerA`, `SButton.ControllerB`, etc.).
- **Filesystem paths** — Never hardcode Windows-style paths.  Use `helper.DirectoryPath` for your mod folder, or `Path.Combine` with forward slashes.
- **Performance** — The Switch CPU is significantly weaker than a desktop.  Avoid per-frame allocations and expensive reflection in hot paths.
- **Mods folder** — Mods live on the SD card at `sdmc:/SMAPI/Mods/`, not in the game's install directory.
