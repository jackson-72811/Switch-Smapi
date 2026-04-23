#pragma once

#include "types.h"

// ─── Switch NSO / module utilities ───────────────────────────────────────────
//
// On Horizon OS, the kernel maintains a list of loaded NSO modules.
// We can walk it via the _DYNAMIC linker structure exposed by libnx:
//   extern "C" void __nx_module_runtime[];
// or via svcQueryMemory / MOD0 header scanning.

// Maximum modules expected in the game process (game + sdk + subsdk0..9 etc.)
#define MODULE_MAX  32u

typedef struct {
    uptr        base;           // Load base address (after ASLR)
    size_t      text_size;      // .text segment size
    size_t      rodata_size;    // .rodata segment size
    size_t      data_size;      // .data segment size
    char        name[256];      // Guessed name from MOD0 or path
} ModuleInfo;

// ─── Public API ───────────────────────────────────────────────────────────────

// Fill |out| with info about all modules loaded in this process.
// Returns the number of modules found (up to MODULE_MAX).
int elf_enumerate_modules(ModuleInfo out[MODULE_MAX]);

// Find the load base of a module whose name contains |substr|.
// Returns 0 if not found.
uptr elf_find_module(const char* substr);

// Look up a named export in the module at |base|.
// Searches the GNU hash table / SYSV hash table in the NSO's dynamic segment.
// Returns 0 if not found.
uptr elf_get_export(uptr base, const char* name);

// Scan the byte range [start, start+size) for |pattern| of |pat_len| bytes.
// Wildcard byte: 0xFF in |mask| means "don't care".
// Returns the address of the first match, or 0 if not found.
uptr elf_pattern_scan(uptr start, size_t size,
                      const u8* pattern, const u8* mask, size_t pat_len);

// Read the "MOD0" header at |base| to get section offsets.
// Returns true on success.
bool elf_read_mod0(uptr base,
                   uptr* out_dynamic_offset,
                   uptr* out_bss_start,
                   uptr* out_bss_end);
