# Switch-SMAPI

A complete port of the Stardew Valley Modding API (SMAPI) to Nintendo Switch, distributed as an Atmosphere custom firmware mod.

## Overview

Switch-SMAPI brings the full SMAPI modding framework to the Nintendo Switch version of Stardew Valley. It works by injecting a native ARM64 bootstrap module alongside the game process at launch. The bootstrap hooks into the Mono JIT runtime before the game code runs, loads the managed SMAPI core assembly, which then scans the SD card for mods and initialises them — all before the first game frame is drawn.

Mods written for Switch-SMAPI use the same API surface as desktop SMAPI, so most PC mods require only a recompile against the Switch-SMAPI SDK targeting `netstandard2.0`.

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
│   └── managed/                     C# SMAPI framework
│       └── SwitchSMAPI/
│           ├── Core/                Mod loader, registry, patcher
│           └── Framework/           Public API for mod authors
├── mods/
│   └── SampleMod/                   Reference mod project
└── tools/                           Build and packaging utilities
```

## Compatibility

Switch-SMAPI targets `netstandard2.0` so it is compatible with the Mono runtime shipped with Stardew Valley on Switch (Mono 6.x). Mods that depend on desktop-only features (Windows registry, P/Invoke to Win32 APIs, XNA sound APIs) will not work and should be ported to the FNA equivalents used by the Switch version.

## Credits

- The SMAPI team (Pathoschild et al.) for the original Stardew Valley Modding API
- The Atmosphere-NX team for the custom firmware
- The devkitPro project for the ARM64 toolchain
- The Harmony project (pardeike) for the runtime method patching library

## License

MIT License — see [LICENSE](LICENSE).
