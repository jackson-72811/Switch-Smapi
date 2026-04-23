#!/usr/bin/env python3
"""
install.py — Switch-SMAPI SD card installer

Merges the Switch-SMAPI payload onto a mounted SD card.

Usage:
    # macOS
    python3 tools/install.py --sd /Volumes/NO_NAME

    # Linux
    python3 tools/install.py --sd /media/$USER/NO_NAME

    # From a pre-built ZIP
    python3 tools/install.py --sd /Volumes/NO_NAME --zip dist/SwitchSMAPI-1.0.0.zip
"""

import argparse
import logging
import os
import shutil
import sys
import tempfile
import zipfile
from pathlib import Path

logging.basicConfig(format="[%(levelname)s] %(message)s", level=logging.INFO)
log = logging.getLogger("installer")

REPO_ROOT = Path(__file__).resolve().parent.parent

REQUIRED_ATMOSPHERE_DIRS = [
    "atmosphere/contents",
]

TITLE_ID_GLOBAL = "0100E65002BB8000"


def verify_sd(sd: Path) -> bool:
    """Check that the path looks like a valid SD card root."""
    if not sd.exists():
        log.error("SD card path does not exist: %s", sd)
        return False
    if not (sd / "atmosphere").exists():
        log.warning("No 'atmosphere' folder found on SD card — is Atmosphere installed?")
        # Not fatal: first-time install
    return True


def merge_directory(src: Path, dst: Path) -> int:
    """Recursively copy src into dst, overwriting existing files.  Returns file count."""
    count = 0
    for item in src.rglob("*"):
        if item.is_dir():
            continue
        rel    = item.relative_to(src)
        target = dst / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(item, target)
        log.debug("  Installed: %s", rel)
        count += 1
    return count


def install_from_staging(staging: Path, sd: Path) -> None:
    """Merge the staging layout onto the SD card."""
    log.info("Installing Switch-SMAPI to %s", sd)

    count = merge_directory(staging, sd)
    log.info("Installed %d file(s)", count)

    # Verify the critical file landed
    bootstrap = sd / "atmosphere" / "contents" / TITLE_ID_GLOBAL / "exefs" / "subsdk9"
    if bootstrap.exists():
        log.info("Bootstrap NSO: OK (%d bytes)", bootstrap.stat().st_size)
    else:
        log.error("Bootstrap NSO not found at %s — install may have failed!", bootstrap)


def install_from_zip(zip_path: Path, sd: Path) -> None:
    with tempfile.TemporaryDirectory(prefix="smapi_install_") as tmp:
        staging = Path(tmp)
        log.info("Extracting %s ...", zip_path)
        with zipfile.ZipFile(zip_path) as zf:
            zf.extractall(staging)
        install_from_staging(staging, sd)


def install_from_repo(sd: Path) -> None:
    """Install directly from a local build (no ZIP needed)."""
    staging = REPO_ROOT / "dist" / "staging"
    if not staging.exists():
        log.error("No staging directory found.  Run 'python3 tools/package_mod.py' first.")
        sys.exit(1)
    install_from_staging(staging, sd)


def create_mods_folder(sd: Path) -> None:
    """Ensure the Mods directory exists on the SD card."""
    mods = sd / "SMAPI" / "Mods"
    mods.mkdir(parents=True, exist_ok=True)
    readme = mods / "README.txt"
    if not readme.exists():
        readme.write_text(
            "Place mod folders here.\n"
            "Each folder must contain a manifest.json and the mod DLL.\n")
    log.info("Mods folder: %s", mods)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sd",  required=True,
                        help="Path to the SD card root (e.g. /Volumes/NO_NAME)")
    parser.add_argument("--zip", default=None,
                        help="Path to a pre-built SwitchSMAPI-*.zip to install from")
    parser.add_argument("--dry-run", action="store_true",
                        help="Print what would be installed without copying files")
    args = parser.parse_args()

    sd = Path(args.sd)

    if not verify_sd(sd):
        return 1

    if args.dry_run:
        log.info("Dry-run mode — no files will be copied.")
        return 0

    if args.zip:
        zip_path = Path(args.zip)
        if not zip_path.exists():
            log.error("ZIP not found: %s", zip_path)
            return 1
        install_from_zip(zip_path, sd)
    else:
        install_from_repo(sd)

    create_mods_folder(sd)

    log.info("")
    log.info("Installation complete!")
    log.info("Safely eject the SD card, reinsert it into the Switch, and launch Stardew Valley.")
    log.info("Check sdmc:/SMAPI/logs/SMAPI-latest.log to verify the framework loaded.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
