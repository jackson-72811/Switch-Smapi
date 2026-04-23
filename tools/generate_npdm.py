#!/usr/bin/env python3
"""
generate_npdm.py — Switch-SMAPI NPDM permission patcher

Reads an original main.npdm from a legal game dump, adds the filesystem
permissions needed by Switch-SMAPI (SD card read/write, content write),
and writes a new binary NPDM.

The output is placed into the Atmosphere content override so Horizon OS
uses it instead of the game's built-in NPDM.

Usage:
    python3 tools/generate_npdm.py \\
        --input  /path/to/original/main.npdm \\
        --title-id 0100E65002BB8000 \\
        --output atmosphere/contents/0100E65002BB8000/exefs/main.npdm

References:
    https://switchbrew.org/wiki/NPDM
"""

import argparse
import logging
import struct
import sys
from pathlib import Path

logging.basicConfig(format="[%(levelname)s] %(message)s", level=logging.INFO)
log = logging.getLogger("npdm")

# ── NPDM constants ────────────────────────────────────────────────────────────

NPDM_MAGIC       = b"META"
ACID_MAGIC       = b"ACID"
ACI0_MAGIC       = b"ACI0"

# FsPermissionBits relevant to SMAPI
FS_PERM_MOUNT_SD_CARD_WRITE   = 1 << 21
FS_PERM_MOUNT_CONTENT_WRITE   = 1 << 19

# ── NPDM layout ───────────────────────────────────────────────────────────────
# The NPDM is a multi-section binary.  Layout:
#   +0x000  META header (0x80 bytes)
#   + META.aci0_offset  ACI0 section
#   + ACI0.fs_offset    FS access control
#   + META.acid_offset  ACID section (optional, may be absent in development)
#
# We only patch the ACI0 FS access control bits.

class NpdmError(Exception):
    pass

def patch_npdm(data: bytearray, title_id: str) -> bytearray:
    if data[:4] != NPDM_MAGIC:
        raise NpdmError(f"Not an NPDM file (magic={data[:4]!r})")

    # META header fields (offsets are little-endian u32)
    # 0x00  magic
    # 0x04  reserved
    # 0x08  mmu_flags
    # 0x09  reserved
    # 0x0A  main_thread_prio
    # 0x0B  default_cpu_id
    # 0x0C  system_resource_size
    # 0x10  process_category
    # 0x14  main_thread_stack_size
    # 0x18  title_name (0x50 bytes)
    # 0x68  product_code (0x10 bytes)
    # 0x78  reserved (8 bytes)
    # 0x80  aci0_offset  (u32)
    # 0x84  aci0_size    (u32)
    # 0x88  acid_offset  (u32)
    # 0x8C  acid_size    (u32)

    aci0_offset, aci0_size = struct.unpack_from("<II", data, 0x80)
    log.info("ACI0 at offset 0x%X, size 0x%X", aci0_offset, aci0_size)

    aci0 = data[aci0_offset : aci0_offset + aci0_size]
    if aci0[:4] != ACI0_MAGIC:
        raise NpdmError(f"ACI0 magic mismatch: {aci0[:4]!r}")

    # ACI0 layout:
    # 0x00  magic
    # 0x04  reserved (0x0C bytes)
    # 0x10  title_id             (u64)
    # 0x18  reserved
    # 0x20  fs_access_offset     (u32)
    # 0x24  fs_access_size       (u32)
    # 0x28  service_access_offset
    # 0x2C  service_access_size
    # 0x30  kernel_access_offset
    # 0x34  kernel_access_size

    fs_off, fs_size = struct.unpack_from("<II", aci0, 0x20)
    log.info("FS access control at ACI0+0x%X, size 0x%X", fs_off, fs_size)

    # The FS access descriptor starts with a u64 permission bitmask
    abs_fs_off = aci0_offset + fs_off
    (existing_perms,) = struct.unpack_from("<Q", data, abs_fs_off)
    log.info("Existing FS permissions: 0x%016X", existing_perms)

    new_perms = existing_perms | FS_PERM_MOUNT_SD_CARD_WRITE | FS_PERM_MOUNT_CONTENT_WRITE
    struct.pack_into("<Q", data, abs_fs_off, new_perms)
    log.info("Updated FS permissions:  0x%016X", new_perms)

    if new_perms == existing_perms:
        log.info("No permission changes needed.")
    else:
        added = new_perms ^ existing_perms
        log.info("Added bits: 0x%016X", added)

    return data


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--input",    required=True,
                        help="Path to the original main.npdm from the game dump")
    parser.add_argument("--title-id", default="0100E65002BB8000",
                        help="Title ID (for log output only)")
    parser.add_argument("--output",   required=True,
                        help="Output path for the patched main.npdm")
    args = parser.parse_args()

    src = Path(args.input)
    if not src.exists():
        log.error("Input NPDM not found: %s", src)
        return 1

    data = bytearray(src.read_bytes())
    log.info("Loaded NPDM: %s (%d bytes)", src, len(data))

    try:
        patched = patch_npdm(data, args.title_id)
    except NpdmError as e:
        log.error("NPDM patch failed: %s", e)
        return 1

    dst = Path(args.output)
    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_bytes(patched)
    log.info("Wrote patched NPDM to %s", dst)
    return 0


if __name__ == "__main__":
    sys.exit(main())
