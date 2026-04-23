# Switch-SMAPI Architecture

## High-level injection pipeline

```
SD card boot
    │
    ▼
Atmosphere CFW loads
    │
    ├─ Reads atmosphere/contents/0100E65002BB8000/exefs/
    │      subsdk9              ← our native bootstrap NSO
    │      main.npdm            ← extended filesystem permissions
    │
    ▼
Horizon OS process loader
    │  Loads main game NSO (Stardew Valley)
    │  Loads subsdk9 (SwitchSMAPI_Bootstrap.nso) as a sibling module
    │  Runs .init_array of ALL loaded modules before transferring to main()
    │
    ▼
SwitchSMAPI_Bootstrap .init_array fires
    │  smapi_bootstrap_init() runs inside the game process
    │  Initialises SD-card file logger
    │  Scans loaded modules for Mono runtime exports
    │  Installs ARM64 trampoline hooks on:
    │      mono_jit_init_version   ← fires when Mono JIT is created
    │
    ▼
Game main() runs
    │  Calls mono_jit_init_version("stardew_valley", "v4.0.30319")
    │
    ▼
Our mono_jit_init_version hook fires
    │  Calls original to get MonoDomain*
    │  Calls smapi_load_managed(domain)
    │      mono_domain_assembly_open → loads SwitchSMAPI.dll
    │      mono_class_from_name      → finds SwitchSMAPI.Core.Program
    │      mono_class_get_method     → finds SmapiMain
    │      mono_runtime_invoke       → calls SmapiMain(string[])
    │  Returns domain to game
    │
    ▼
SwitchSMAPI.Core.Program.SmapiMain() runs inside Mono
    │  Creates LogManager, writes to sdmc:/SMAPI/logs/
    │  Creates ModLoader
    │      Scans sdmc:/SMAPI/Mods/ for manifest.json files
    │      Resolves dependency order
    │      Loads each mod DLL via Assembly.LoadFrom
    │      Instantiates each Mod subclass
    │  Creates EventManager (wires Harmony patches to game methods)
    │  Calls mod.Entry(helper) for every successfully loaded mod
    │
    ▼
Game continues — mods are active, events fire, patches applied
```

## Memory layout inside the game process

```
0x00000000_XXXXXXXX  game main NSO text
                        mono_jit_init_version + 0  ← patched with trampoline
                        [...]
0x00000000_YYYYYYYY  subsdk9 (SwitchSMAPI_Bootstrap) text
                        smapi_trampoline_mono_jit_init_version
                        smapi_bootstrap_init
                        smapi_load_managed
0x00000000_ZZZZZZZZ  Mono runtime (embedded in game NSO or separate NSO)
                        mono_domain_assembly_open
                        mono_runtime_invoke
                        [...]
```

## Native bootstrap (ARM64 C++)

### Module structure

The bootstrap is a standard Switch NSO compiled with devkitA64.  It exports no symbols to the game — it acts as a parasite that installs itself via the `.init_array` section.

Key source files:

| File | Role |
|---|---|
| `source/main.cpp` | `.init_array` entry, top-level orchestration |
| `source/hook.cpp` | ARM64 trampoline installation / removal |
| `source/mono_hook.cpp` | Mono-specific hooking and managed code invocation |
| `source/elf_utils.cpp` | Module enumeration, export table scanning |
| `source/logger.cpp` | Synchronised file logger writing to SD card |

### ARM64 hook mechanism

We use a 16-byte absolute-branch trampoline:

```asm
; Written at the target function's first 16 bytes:
LDR  X16, #8        ; 0x58000050 — load 8-byte absolute address from +8
BR   X16            ; 0xD61F0200 — branch to register (no link)
.quad <hook_addr>   ; 8 bytes    — absolute address of our handler
```

Original bytes are saved to a trampoline stub so the original function can be called.  Memory is made writable using `svcSetMemoryAttribute` with `MemoryAttribute_IsUncached` cleared, then restored after patching.

### Mono function discovery

Stardew Valley links Mono statically into the main NSO.  Mono exports are resolved by:

1. Walking the game NSO's `.dynsym` / export hash table using the NSO header format
2. If the export table is stripped, falling back to byte-pattern scanning with known ARM64 prologue signatures for each Mono function (version-specific patterns stored in `mono_signatures.h`)

## Managed SMAPI core (C#)

### Assembly loading

`SwitchSMAPI.dll` and its dependencies (`0Harmony.dll`, `Newtonsoft.Json.dll`) are placed in `romfs/SMAPI/` which Atmosphere overlays onto the game's RomFS.  The native bootstrap opens the assembly using the path as visible inside the game process (via `ftp_path` helpers for Switch filesystem abstraction).

### Mod loading pipeline

```
ModLoader.LoadMods(modsRoot)
    │
    ├── for each subdirectory D in modsRoot:
    │       Parse D/manifest.json → IManifest
    │       Validate required fields (UniqueID, Name, Version, EntryDll)
    │       Record as ModCandidate
    │
    ├── TopologicalSort(candidates by Dependencies[])
    │       Detect circular dependencies → log error, skip mod
    │       Detect missing dependencies  → log warning, skip mod
    │
    └── for each candidate (in dependency order):
            Assembly asm = Assembly.LoadFrom(candidate.DllPath)
            Type modType = find type : Mod in asm
            Mod instance = (Mod)Activator.CreateInstance(modType)
            ModHelper helper = new ModHelper(instance, ...)
            Register in ModRegistry
            instance.Entry(helper)
```

### Event system

Events are backed by C# `event` delegates raised by Harmony postfix/prefix patches on game methods.

```
Game method        Harmony patch        EventManager raises
──────────────     ─────────────────    ─────────────────────────────
Game1.Update()     Postfix              IGameLoopEvents.UpdateTicked
Game1.Draw()       Postfix              IDisplayEvents.Rendered
Game1.newDayAfterFade()  Postfix        IWorldEvents.DayStarted
SaveGame.Save()    Prefix               IGameLoopEvents.Saving
Farmer.move()      Postfix              IPlayerEvents.PositionChanged
```

Mods subscribe via:

```csharp
helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
```

### Harmony integration

Harmony 2.x is used for all runtime method patching.  Switch-SMAPI creates a single `Harmony` instance with id `"com.switch-smapi.core"` for its own framework patches, and creates per-mod `Harmony` instances with id `"com.switch-smapi.mod.<UniqueID>"` so mod patches can be cleanly identified and can be toggled at runtime.

The Harmony version shipped with Switch-SMAPI is patched to use `MonoMod.RuntimeDetour` on the Mono runtime rather than the desktop CLR code-patching path.

## Filesystem paths

| Purpose | Path on SD card |
|---|---|
| Mods directory | `sdmc:/SMAPI/Mods/` |
| Log files | `sdmc:/SMAPI/logs/` |
| Per-mod save data | `sdmc:/SMAPI/data/<UniqueID>/` |
| Framework assemblies | via Atmosphere RomFS overlay |
| IPS patches | `sdmc:/atmosphere/exefs_patches/0100E65002BB8000/` |

## Security model

Switch-SMAPI requires Atmosphere CFW.  It does not attempt to run on stock firmware.  The modified `main.npdm` grants:

- `FsPermissionBits::MountSdCardWrite` — read/write access to the SD card
- `FsPermissionBits::MountContentWrite` — needed for save-data helpers

These permissions are normal for homebrew and do not affect system stability beyond the game process.

## Version compatibility

Switch-SMAPI embeds a version compatibility table (`compat_table.json`) mapping Stardew Valley binary versions to Mono function offset signatures.  When the game binary changes (game update), the table is updated in a new Switch-SMAPI release.  The native bootstrap reads this table at startup to select the correct signature set before scanning.
