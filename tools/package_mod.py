#!/usr/bin/env python3
"""
package_mod.py — Switch-SMAPI release packager

Assembles the SD card payload ZIP from the built native NSO and managed assemblies.

Usage:
    python3 tools/package_mod.py --version 1.0.0 [--output dist/]

Expects:
    src/native/output/SwitchSMAPI_Bootstrap.nso   (built by make)
    src/managed/SwitchSMAPI/bin/Release/netstandard2.0/SwitchSMAPI.dll
    src/managed/SwitchSMAPI/bin/Release/netstandard2.0/0Harmony.dll
    src/managed/SwitchSMAPI/bin/Release/netstandard2.0/Newtonsoft.Json.dll
"""

import argparse
import datetime
import json
import logging
import os
import shutil
import sys
import zipfile
from pathlib import Path

logging.basicConfig(format="[%(levelname)s] %(message)s", level=logging.INFO)
log = logging.getLogger("packager")

REPO_ROOT    = Path(__file__).resolve().parent.parent
NATIVE_NSO   = REPO_ROOT / "src" / "native" / "output" / "SwitchSMAPI_Bootstrap.nso"
MANAGED_BIN  = REPO_ROOT / "src" / "managed" / "SwitchSMAPI" / "bin" / "Release" / "netstandard2.0"

# Title IDs to package for
TITLE_IDS = [
    "0100E65002BB8000",   # Global
    "01009DF004CBC000",   # Japan
]

# Managed assemblies to include
MANAGED_ASSEMBLIES = [
    "SwitchSMAPI.dll",
    "SwitchSMAPI.xml",    # documentation (optional)
    "0Harmony.dll",
    "Newtonsoft.Json.dll",
    "Microsoft.CSharp.dll",
]

def check_prerequisites() -> bool:
    ok = True
    if not NATIVE_NSO.exists():
        log.error("Native NSO not found: %s", NATIVE_NSO)
        log.error("Run 'cd src/native && make' first.")
        ok = False
    if not MANAGED_BIN.exists():
        log.error("Managed build output not found: %s", MANAGED_BIN)
        log.error("Run 'cd src/managed && dotnet build --configuration Release' first.")
        ok = False
    else:
        for asm in MANAGED_ASSEMBLIES:
            path = MANAGED_BIN / asm
            if not path.exists() and not asm.endswith(".xml"):
                log.error("Missing managed assembly: %s", path)
                ok = False
    return ok

def make_layout(staging: Path, version: str) -> None:
    """Build the SD card directory layout in the staging directory."""

    for title_id in TITLE_IDS:
        exefs_dir  = staging / "atmosphere" / "contents" / title_id / "exefs"
        romfs_dir  = staging / "atmosphere" / "contents" / title_id / "romfs" / "SMAPI"
        exefs_dir.mkdir(parents=True, exist_ok=True)
        romfs_dir.mkdir(parents=True, exist_ok=True)

        # Native bootstrap → subsdk9 (loaded as an additional NSO module)
        log.info("Copying NSO → exefs/subsdk9 for %s", title_id)
        shutil.copy2(NATIVE_NSO, exefs_dir / "subsdk9")

        # Managed assemblies → romfs/SMAPI/
        for asm in MANAGED_ASSEMBLIES:
            src = MANAGED_BIN / asm
            if src.exists():
                log.info("Copying %s → romfs/SMAPI/", asm)
                shutil.copy2(src, romfs_dir / asm)

    # Version metadata (read by the managed Program.cs)
    for title_id in TITLE_IDS:
        romfs_dir = staging / "atmosphere" / "contents" / title_id / "romfs" / "SMAPI"
        version_info = {
            "version":      version,
            "build_date":   datetime.datetime.utcnow().isoformat() + "Z",
            "title_ids":    TITLE_IDS,
        }
        (romfs_dir / "version.json").write_text(json.dumps(version_info, indent=2))

    # Mods placeholder directory
    mods_readme = staging / "SMAPI" / "Mods"
    mods_readme.mkdir(parents=True, exist_ok=True)
    (mods_readme / "README.txt").write_text(
        "Place mod folders here.  Each folder must contain a manifest.json.\n"
        "See https://github.com/jackson-72811/switch-smapi for details.\n")

    # SDK folder (for mod authors)
    sdk_dir = staging / "sdk"
    sdk_dir.mkdir(parents=True, exist_ok=True)
    for asm in MANAGED_ASSEMBLIES:
        src = MANAGED_BIN / asm
        if src.exists():
            shutil.copy2(src, sdk_dir / asm)

def create_zip(staging: Path, out_path: Path) -> None:
    log.info("Creating archive: %s", out_path)
    with zipfile.ZipFile(out_path, "w", zipfile.ZIP_DEFLATED, compresslevel=9) as zf:
        for file in sorted(staging.rglob("*")):
            if file.is_file():
                arcname = file.relative_to(staging)
                zf.write(file, arcname)
                log.debug("  + %s", arcname)
    log.info("Archive size: %.1f MB", out_path.stat().st_size / 1_048_576)

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True, help="Release version string (e.g. 1.0.0)")
    parser.add_argument("--output",  default="dist", help="Output directory")
    parser.add_argument("--skip-checks", action="store_true",
                        help="Skip prerequisite checks (for CI with partial builds)")
    args = parser.parse_args()

    if not args.skip_checks and not check_prerequisites():
        return 1

    out_dir = REPO_ROOT / args.output
    out_dir.mkdir(parents=True, exist_ok=True)
    out_zip = out_dir / f"SwitchSMAPI-{args.version}.zip"

    staging = out_dir / "staging"
    if staging.exists():
        shutil.rmtree(staging)
    staging.mkdir(parents=True)

    try:
        make_layout(staging, args.version)
        create_zip(staging, out_zip)
    finally:
        shutil.rmtree(staging, ignore_errors=True)

    log.info("Package ready: %s", out_zip)
    return 0

if __name__ == "__main__":
    sys.exit(main())
