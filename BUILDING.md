# Building Switch-SMAPI

This document covers building both the native ARM64 bootstrap module and the managed C# SMAPI framework from source.

## Prerequisites

### Native bootstrap (C++)

Install [devkitPro](https://devkitpro.org/wiki/Getting_Started) with the Switch development package:

```bash
# macOS / Linux — use the devkitPro pacman installer
sudo dkp-pacman -S switch-dev
```

Required packages installed by `switch-dev`:

- `devkitA64` — ARM64 cross-compiler toolchain (`aarch64-none-elf-g++`)
- `libnx` — Nintendo Switch homebrew library
- `switch-tools` — `elf2nso`, `npdm-tool`, `nacptool`, etc.

Verify the installation:

```bash
source /etc/profile.d/devkit-env.sh
echo $DEVKITPRO    # should print /opt/devkitpro
aarch64-none-elf-g++ --version
```

### Managed framework (C#)

Install the [.NET SDK](https://dotnet.microsoft.com/download) 6.0 or newer:

```bash
dotnet --version   # 6.0.x or newer
```

The project targets `netstandard2.0` so it is compatible with the Mono runtime on Switch.

## Building the native bootstrap

```bash
cd src/native
make
```

This produces `SwitchSMAPI_Bootstrap.nso` in `src/native/output/`.  The Makefile invokes `elf2nso` automatically after linking.

### Make targets

| Target | Description |
|---|---|
| `make` / `make all` | Full release build |
| `make debug` | Debug build with `-g3`, assertions enabled |
| `make clean` | Remove build artefacts |

### Output

```
src/native/output/
├── SwitchSMAPI_Bootstrap.elf   Intermediate ELF (kept for debugging)
└── SwitchSMAPI_Bootstrap.nso   Final NSO module to deploy as subsdk9
```

## Building the managed framework

The solution (`src/managed/SwitchSMAPI.sln`) contains two projects:

| Project | Output DLL | Purpose |
|---|---|---|
| `SwitchSMAPI` | `SwitchSMAPI.dll` | Core engine — mod loader, event system, helpers |
| `StardewModdingAPI` | `StardewModdingAPI.dll` | Binary-compat shim for unmodified PC mods |

### Quick build (engine only, no game DLLs required)

```bash
cd src/managed
dotnet build SwitchSMAPI.sln --configuration Release
```

This builds `SwitchSMAPI.dll` fully. The compat shim builds but **world/player events** that directly reference `StardewValley.GameLocation` and `StardewValley.Farmer` will compile without game-type resolution — they need the game DLLs for a functional build.

### Full build (with game DLLs)

Provide the path to a legal game assembly dump:

```bash
cd src/managed
dotnet build SwitchSMAPI.sln --configuration Release \
    /p:StardewSwitchDump=/path/to/switch-game-dump/managed
```

`StardewSwitchDump` must point to the folder containing `Stardew Valley.dll`, `MonoGame.Framework.dll`, and `xTile.dll` extracted from a legally-owned Switch cartridge or eShop dump.

### Output assemblies

```
src/managed/SwitchSMAPI/bin/Release/netstandard2.0/
├── SwitchSMAPI.dll          Core engine

src/compat/StardewModdingAPI/bin/Release/netstandard2.0/
└── StardewModdingAPI.dll    PC-mod compat shim
```

Both DLLs must be deployed to `atmosphere/contents/0100E65002BB8000/romfs/SMAPI/` on the SD card. The packaging script handles this automatically.

### NuGet dependencies

Required packages (restored automatically by `dotnet build`):

| Package | Version | Purpose |
|---|---|---|
| `Lib.Harmony` | 2.3.x | Runtime method patching |
| `Newtonsoft.Json` | 13.x | Manifest and data JSON |
| `Microsoft.CSharp` | 4.7.x | Dynamic keyword support |

## Assembling the SD card payload

After both components are built, run the packaging script:

```bash
python3 tools/package_mod.py --version 1.0.0
```

This creates `dist/SwitchSMAPI-1.0.0.zip` ready to merge onto an SD card.  The script:

1. Copies `SwitchSMAPI_Bootstrap.nso` → `atmosphere/contents/0100E65002BB8000/exefs/subsdk9`
2. Copies managed assemblies → `atmosphere/contents/0100E65002BB8000/romfs/SMAPI/`
3. Copies the modified `main.npdm` granting SD card and filesystem permissions
4. Generates version metadata at `atmosphere/contents/0100E65002BB8000/romfs/SMAPI/version.json`

## Generating IPS patches (optional)

IPS patches let you hook into specific game binary versions without replacing the full ExeFS.  The patch generator performs signature scanning against a provided game dump:

```bash
python3 tools/generate_patch.py \
    --nso /path/to/stardew_main.nso \
    --version 1.6.8 \
    --output atmosphere/exefs_patches/0100E65002BB8000/
```

The script outputs a `.ips` file compatible with Atmosphere's IPS patcher.

> **Note:** You must legally own a copy of Stardew Valley and dump the NSO yourself. Game binaries are not distributed with Switch-SMAPI.

## CI / automated builds

A GitHub Actions workflow is provided in `.github/workflows/build.yml`. It:

1. Sets up the devkitPro Docker image for the native build
2. Runs `dotnet build` for the managed build
3. Packages both outputs and uploads as a workflow artefact

## Flashing to hardware

After building and packaging:

```bash
# Mount your SD card, then:
python3 tools/install.py --sd /Volumes/NO_NAME   # macOS example
# or
python3 tools/install.py --sd /media/user/NO_NAME  # Linux example
```

The installer merges the `atmosphere/` tree onto the SD card and verifies the file layout.
