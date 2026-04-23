# Switch-SMAPI

A complete port of the Stardew Valley Modding API (SMAPI) to Nintendo Switch, distributed as an Atmosphere custom firmware mod.

## Overview

Switch-SMAPI brings the full SMAPI modding framework to the Nintendo Switch version of Stardew Valley. It works by injecting a native ARM64 bootstrap module alongside the game process at launch. The bootstrap hooks into the Mono JIT runtime before the game code runs, loads the managed SMAPI core assembly, which then scans the SD card for mods and initialises them — all before the first game frame is drawn.

Mods written for Switch-SMAPI use the same API surface as desktop SMAPI. PC mods **do not need to be recompiled** — Switch-SMAPI ships a binary-compatible `StardewModdingAPI.dll` shim that Mono resolves instead of the real desktop DLL. Drop any compiled PC mod into `sdmc:/SMAPI/Mods/` and it loads automatically.

## Requirements

| Requirement | Version |
|---|---|
| Nintendo Switch (any model) | — |
| Atmosphere CFW | 1.4.0 or newer |
| Stardew Valley (any region) | 1.5.4 – 1.6.x |
| SD card free space | 200 MB minimum |

**Supported title IDs**

| Region | Title ID |
|---|---|
| Global (most common) | `0100E65002BB8000` |
| Japan | `01009DF004CBC000` |

## Installation

1. Download the latest release ZIP from the Releases page.
2. Power off the Switch and remove the SD card.
3. Unzip the archive. Merge the `atmosphere/` folder into the root of the SD card. If prompted about conflicts, always choose **Merge** — never replace the entire `atmosphere/` folder.
4. Reinsert the SD card and boot into Atmosphere.
5. Launch Stardew Valley. A log file will appear at `sdmc:/SMAPI/logs/SMAPI-latest.log` to confirm the framework loaded.

### Installing mods

Place each mod's folder (containing `manifest.json` and the mod DLL) inside:

```
sdmc:/SMAPI/Mods/<ModName>/
```

Example layout:

```
sdmc:/SMAPI/
├── logs/
│   └── SMAPI-latest.log
└── Mods/
    ├── ContentPatcher/
    │   ├── manifest.json
    │   └── ContentPatcher.dll
    └── SAAT/
        ├── manifest.json
        └── SAAT.Mod.dll
```

## Building from source

See [BUILDING.md](BUILDING.md) for the complete build guide.

## Mod development

See [docs/MOD_DEVELOPMENT.md](docs/MOD_DEVELOPMENT.md) for the mod authoring guide and API reference.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for a technical deep-dive into how the injection pipeline works.

## Project layout

```
Switch-SMAPI/
├── atmosphere/                      Atmosphere mod overlay
│   └── contents/
│       └── 0100E65002BB8000/
│           ├── exefs/               Native NSO override files
│           └── romfs/               RomFS overlays (unused by SMAPI itself)
├── src/
│   ├── native/                      ARM64 C++ bootstrap module
│   │   ├── include/
│   │   └── source/
│   ├── managed/                     C# SMAPI framework (SwitchSMAPI.dll)
│   │   └── SwitchSMAPI/
│   │       ├── Core/                Mod loader, registry, patcher
│   │       ├── Framework/           Public API for native mod authors
│   │       └── PlatformEmulation/   Harmony patches for Environment/Process/Registry
│   └── compat/                      PC-mod binary-compat shim (StardewModdingAPI.dll)
│       └── StardewModdingAPI/
│           ├── Framework/           Adapters bridging internal ↔ SMAPI 4 types
│           └── Events/              SMAPI 4 event interfaces and arg types
├── mods/
│   └── SampleMod/                   Reference mod project
└── tools/                           Build and packaging utilities
```

## PC Mod Compatibility

Switch-SMAPI ships a drop-in `StardewModdingAPI.dll` shim (in `src/compat/`) that makes PC mods run without recompilation:

- **Assembly resolver** — The managed engine registers an `AppDomain.AssemblyResolve` hook before loading any mods. When a PC mod's assembly references `StardewModdingAPI`, Mono calls this hook and receives our compat DLL instead of failing.
- **Full API surface** — The shim implements `IModHelper`, `IMonitor`, all `IModEvents` groups, `IGameContentHelper`, `IModContentHelper`, `IDataHelper`, `IReflectionHelper`, `ITranslationHelper`, `IInputHelper`, `IMultiplayerHelper`, and `ICommandHelper` — the complete SMAPI 4 public API.
- **Platform emulation** — Harmony patches intercept `Environment.GetFolderPath`, `Process.Start`, and registry reads to return Switch-appropriate values. PC mods that try to read the Steam install path receive `sdmc:/atmosphere/contents/0100E65002BB8000/romfs` instead.
- **SButton mapping** — Controller and mouse button values are translated between the internal Switch representation and the PC enum values SMAPI mods expect.

Mods that depend on desktop-only platform features (P/Invoke to Win32 DLLs, DirectX-specific APIs) will still fail at runtime, but the vast majority of game-logic mods load and run correctly.

## Compatibility

Switch-SMAPI targets `netstandard2.0` so it is compatible with the Mono runtime shipped with Stardew Valley on Switch (Mono 6.x).

## Credits

- The SMAPI team (Pathoschild et al.) for the original Stardew Valley Modding API
- The Atmosphere-NX team for the custom firmware
- The devkitPro project for the ARM64 toolchain
- The Harmony project (pardeike) for the runtime method patching library

## License

MIT License — see [LICENSE](LICENSE).
