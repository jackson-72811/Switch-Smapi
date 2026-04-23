#pragma once

#include "types.h"

// ─── ARM64 trampoline hook system ────────────────────────────────────────────
//
// We use a 16-byte absolute-branch trampoline:
//
//   Offset  Bytes        ARM64 assembly
//   +0      50 00 00 58  LDR  X16, #8        ; load target address from +8
//   +4      00 02 1F D6  BR   X16            ; branch to it (no link)
//   +8      <8 bytes>    .quad <hook_fn_ptr>  ; hook function address
//
// The original 16 bytes at the target are preserved in a stub so the
// hook handler can call the original function.

#define HOOK_TRAMPOLINE_SIZE  16u

// Maximum simultaneously installed hooks
#define HOOK_MAX_SLOTS  64u

typedef struct {
    void*  target;                          // Patched function address
    void*  hook;                            // Our replacement function
    void*  trampoline;                      // Allocated stub to call original
    uint8_t original_bytes[HOOK_TRAMPOLINE_SIZE]; // Saved original bytes
    bool   active;
} HookSlot;

// ─── Public API ───────────────────────────────────────────────────────────────

// Initialise the hook subsystem (allocates trampoline pool).
int hook_init(void);

// Install a hook at |target|.
//   target   : address of the function to hook
//   hook_fn  : address of our replacement handler
//   original : receives a pointer to a stub that calls the original function
// Returns SMAPI_OK on success, SMAPI_FAIL on error.
int hook_install(void* target, void* hook_fn, void** original);

// Remove a previously installed hook by its target address.
int hook_remove(void* target);

// Remove all active hooks (call before unloading the module).
void hook_remove_all(void);

// Make |size| bytes at |addr| writable/executable.
int hook_make_rw(void* addr, size_t size);

// Restore the default memory attributes after patching.
int hook_make_rx(void* addr, size_t size);

// Flush the instruction cache for the given range so the CPU sees our writes.
void hook_flush_icache(void* addr, size_t size);
