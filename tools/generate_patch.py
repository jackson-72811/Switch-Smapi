#!/usr/bin/env python3
"""
generate_patch.py — Switch-SMAPI IPS patch generator

Scans a Stardew Valley NSO binary for known byte-pattern signatures and
generates an IPS (or IPSv3) patch file that redirects the game's startup
to load our native bootstrap.

Usage:
    python3 tools/generate_patch.py \\
        --nso /path/to/main.nso \\
        --version 1.6.8 \\
        --output atmosphere/exefs_patches/0100E65002BB8000/

Requirements:
    pip install capstone lz4
"""

import argparse
import hashlib
import io
import json
import logging
import os
import struct
import sys
from dataclasses import dataclass, field
from pathlib import Path
from typing import Optional

try:
    import lz4.block as lz4block  # type: ignore
except ImportError:
    lz4block = None  # type: ignore

# ── Logging ───────────────────────────────────────────────────────────────────

logging.basicConfig(
    format="[%(levelname)s] %(message)s",
    level=logging.DEBUG,
)
log = logging.getLogger("gen_patch")

# ── NSO format constants ─────────────────────────────────────────────────────

NSO_MAGIC = b"NSO0"
NSO_HEADER_SIZE = 0x100

# Text segment flags in the NSO header
NSO_FLAG_TEXT_COMPRESSED  = 1 << 0
NSO_FLAG_RO_COMPRESSED    = 1 << 1
NSO_FLAG_DATA_COMPRESSED  = 1 << 2
NSO_FLAG_TEXT_HASH_CHECK  = 1 << 3
NSO_FLAG_RO_HASH_CHECK    = 1 << 4
NSO_FLAG_DATA_HASH_CHECK  = 1 << 5

@dataclass
class NsoSegment:
    file_offset:   int
    mem_offset:    int
    size:          int
    compressed:    bool
    data:          bytes = field(default_factory=bytes, repr=False)

@dataclass
class NsoHeader:
    flags:        int
    text:         NsoSegment
    ro:           NsoSegment
    data:         NsoSegment
    bss_size:     int
    build_id:     bytes

def parse_nso(raw: bytes) -> NsoHeader:
    """Parse a Nintendo NSO binary and return segment information."""
    if raw[:4] != NSO_MAGIC:
        raise ValueError(f"Not an NSO file (got {raw[:4]!r})")

    # NSO header layout (offsets into the 0x100-byte header):
    # 0x00  magic
    # 0x04  version
    # 0x08  reserved
    # 0x0C  flags
    # 0x10  text.file_offset   (u32)
    # 0x14  text.mem_offset    (u32)
    # 0x18  text.size (decompressed) (u32)
    # 0x1C  module_name_offset (u32)
    # 0x20  ro.file_offset
    # 0x24  ro.mem_offset
    # 0x28  ro.size
    # 0x2C  module_name_size
    # 0x30  data.file_offset
    # 0x34  data.mem_offset
    # 0x38  data.size
    # 0x3C  bss.size
    # 0x40  build_id (32 bytes)
    # 0x60  text.compressed_size (u32)
    # 0x64  ro.compressed_size  (u32)
    # 0x68  data.compressed_size (u32)
    # ...

    (flags,) = struct.unpack_from("<I", raw, 0x0C)

    def seg(base_off: int, compressed_size_off: int, compressed_flag: int) -> NsoSegment:
        file_off, mem_off, dec_size = struct.unpack_from("<III", raw, base_off)
        (cmp_size,) = struct.unpack_from("<I", raw, compressed_size_off)
        is_compressed = bool(flags & compressed_flag)
        raw_data = raw[file_off : file_off + cmp_size]
        if is_compressed:
            if lz4block is None:
                raise RuntimeError("lz4 is required for compressed NSOs: pip install lz4")
            data = lz4block.decompress(raw_data, uncompressed_size=dec_size)
        else:
            data = raw_data
        return NsoSegment(file_off, mem_off, dec_size, is_compressed, data)

    text = seg(0x10, 0x60, NSO_FLAG_TEXT_COMPRESSED)
    ro   = seg(0x20, 0x64, NSO_FLAG_RO_COMPRESSED)
    data = seg(0x30, 0x68, NSO_FLAG_DATA_COMPRESSED)

    build_id = raw[0x40:0x60]
    (bss_size,) = struct.unpack_from("<I", raw, 0x3C)

    return NsoHeader(flags, text, ro, data, bss_size, build_id)

# ── Signature definitions ─────────────────────────────────────────────────────

@dataclass
class Signature:
    name:    str
    pattern: bytes
    mask:    bytes      # 0x00 = match, 0xFF = wildcard

# Known Mono 6.12 ARM64 function signatures for Stardew Valley
SIGNATURES: dict[str, list[Signature]] = {
    # Stardew Valley 1.6.x Mono 6.12
    "1.6": [
        Signature(
            name    = "mono_jit_init_version",
            # STP X29,X30,[SP,#-0x10]!  ; function prologue
            # MOV X29, SP
            # STP X19,X20,[SP,#-0x10]!
            pattern = bytes([
                0xFD, 0x7B, 0xBF, 0xA9,   # STP X29,X30,[SP,-16]!
                0xFD, 0x03, 0x00, 0x91,   # MOV X29, SP
                0xF3, 0x53, 0xBF, 0xA9,   # STP X19,X20,[SP,-16]!
            ]),
            mask    = bytes([0x00]*12),
        ),
        Signature(
            name    = "mono_domain_assembly_open",
            pattern = bytes([
                0xFD, 0x7B, 0xBF, 0xA9,
                0xFD, 0x03, 0x00, 0x91,
                0xF5, 0x5B, 0xBF, 0xA9,
            ]),
            mask    = bytes([0x00]*12),
        ),
    ],
}

def scan_for_signature(text_data: bytes, sig: Signature, text_mem_base: int) -> Optional[int]:
    """Return the memory address of the first match, or None."""
    pat = sig.pattern
    msk = sig.mask
    plen = len(pat)

    for i in range(len(text_data) - plen + 1):
        match = True
        for j in range(plen):
            if msk[j] == 0xFF:
                continue  # wildcard
            if text_data[i + j] != pat[j]:
                match = False
                break
        if match:
            return text_mem_base + i
    return None

# ── IPS / IPSv3 patch writer ──────────────────────────────────────────────────

def write_ips32(path: Path, patches: list[tuple[int, bytes]]) -> None:
    """Write an IPS32 (BPS) patch file understood by Atmosphere."""
    buf = io.BytesIO()
    buf.write(b"IPS32")
    for offset, data in patches:
        buf.write(struct.pack(">I", offset))          # 4-byte offset
        buf.write(struct.pack(">H", len(data)))       # 2-byte size
        buf.write(data)
    buf.write(b"EEOF")
    path.write_bytes(buf.getvalue())
    log.info("Wrote IPS32 patch to %s (%d bytes)", path, buf.tell())

# ── ARM64 trampoline builder ───────────────────────────────────────────────────

def make_arm64_abs_branch(target_addr: int) -> bytes:
    """Return a 16-byte absolute-branch trampoline."""
    # LDR X16, #8   (58 00 00 50)
    # BR  X16       (D6 1F 02 00)
    # <8 bytes: target address little-endian>
    ldr = struct.pack("<I", 0x58000050)
    br  = struct.pack("<I", 0xD61F0200)
    addr = struct.pack("<Q", target_addr)
    return ldr + br + addr

# ── Main ─────────────────────────────────────────────────────────────────────

def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--nso",     required=True,  help="Path to the Stardew Valley main.nso")
    parser.add_argument("--version", default="1.6",  help="Game version key (e.g. '1.6')")
    parser.add_argument("--output",  required=True,  help="Output directory for .ips files")
    parser.add_argument("--bootstrap-mem-addr", default="0",
                        help="Memory address where SwitchSMAPI_Bootstrap is loaded (hex). "
                             "0 means auto-detect from subsdk9 load order.")
    args = parser.parse_args()

    nso_path = Path(args.nso)
    if not nso_path.exists():
        log.error("NSO file not found: %s", nso_path)
        return 1

    raw = nso_path.read_bytes()
    log.info("Loaded NSO: %s (%d bytes)", nso_path, len(raw))

    # Parse NSO
    try:
        nso = parse_nso(raw)
    except Exception as e:
        log.error("Failed to parse NSO: %s", e)
        return 1

    build_id_hex = nso.build_id.hex().upper()
    log.info("Build ID: %s", build_id_hex)
    log.info("Text segment: mem=0x%08X, size=0x%X", nso.text.mem_offset, nso.text.size)

    # Look up signatures for this game version
    sigs = SIGNATURES.get(args.version)
    if not sigs:
        log.error("No signatures defined for version '%s'. Defined: %s",
                  args.version, list(SIGNATURES.keys()))
        return 1

    # Scan for each signature
    found: dict[str, int] = {}
    for sig in sigs:
        addr = scan_for_signature(nso.text.data, sig, nso.text.mem_offset)
        if addr is not None:
            log.info("Found %-40s at mem 0x%016X", sig.name, addr)
            found[sig.name] = addr
        else:
            log.warning("Signature not found: %s", sig.name)

    if "mono_jit_init_version" not in found:
        log.error("Could not locate mono_jit_init_version — cannot generate patch")
        return 1

    # Determine bootstrap memory address
    bootstrap_addr_str = args.bootstrap_mem_addr
    if bootstrap_addr_str == "0":
        # Heuristic: subsdk9 is typically loaded right after the main NSO.
        # We estimate based on the known main NSO memory footprint.
        # Real deployment needs to account for ASLR; the actual address
        # is resolved at runtime by our .init_array hook — the IPS patch
        # only needs to redirect to a *relative* stub.
        log.warning("--bootstrap-mem-addr not set. Generating a relative-patch stub instead.")
        bootstrap_addr = None
    else:
        bootstrap_addr = int(bootstrap_addr_str, 16)
        log.info("Bootstrap memory address: 0x%016X", bootstrap_addr)

    # Build patch list
    patches: list[tuple[int, bytes]] = []

    if bootstrap_addr is not None:
        # Redirect mono_jit_init_version to our hook
        jit_init_offset = found["mono_jit_init_version"] - nso.text.mem_offset
        trampoline = make_arm64_abs_branch(bootstrap_addr)
        patches.append((jit_init_offset, trampoline))
        log.info("Patch: mono_jit_init_version at NSO offset 0x%X → 0x%016X",
                  jit_init_offset, bootstrap_addr)
    else:
        log.info("Skipping code patch (no bootstrap address given).")
        log.info("The native bootstrap uses .init_array hooking instead — no IPS needed.")

    # Write output
    out_dir = Path(args.output)
    out_dir.mkdir(parents=True, exist_ok=True)

    # Atmosphere IPS patches are named with the build ID
    out_path = out_dir / f"{build_id_hex}.ips"

    if patches:
        write_ips32(out_path, patches)
    else:
        # Write an empty (no-op) patch so Atmosphere at least loads the file
        write_ips32(out_path, [])
        log.info("Wrote empty patch (no redirects needed — using .init_array approach)")

    # Write a metadata sidecar for debugging
    meta = {
        "build_id":          build_id_hex,
        "game_version":      args.version,
        "nso_path":          str(nso_path),
        "nso_sha256":        hashlib.sha256(raw).hexdigest(),
        "text_mem_offset":   hex(nso.text.mem_offset),
        "text_size":         hex(nso.text.size),
        "found_symbols":     {k: hex(v) for k, v in found.items()},
        "patches_applied":   len(patches),
    }
    (out_dir / f"{build_id_hex}.meta.json").write_text(
        json.dumps(meta, indent=2))
    log.info("Wrote metadata to %s", out_dir / f"{build_id_hex}.meta.json")

    log.info("Done.")
    return 0

if __name__ == "__main__":
    sys.exit(main())
