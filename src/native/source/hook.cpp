#include "../include/hook.h"
#include "../include/logger.h"

#include <switch.h>
#include <cstring>
#include <cstdlib>

#define TAG "Hook"

// ─── ARM64 trampoline encoding ────────────────────────────────────────────────
//
// We write a 16-byte trampoline at the target function:
//
//   +0: LDR X16, #8    (0x58000050) — PC-relative load, 8 bytes forward
//   +4: BR  X16        (0xD61F0200) — unconditional branch to register
//   +8: <8 bytes>      absolute address of the hook function
//
// For the "original" stub we allocate a small RWX region:
//   [saved first 16 bytes of target]
//   LDR X16, #8
//   BR  X16
//   [absolute address of target + 16]
// This lets the hook handler call the real function seamlessly.

static const u32 INSN_LDR_X16_8  = 0x58000050u;  // LDR X16, #8
static const u32 INSN_BR_X16     = 0xD61F0200u;  // BR X16

// ─── Trampoline pool ─────────────────────────────────────────────────────────

// Each stub is 16 (saved) + 16 (trampoline) = 32 bytes, aligned to 64.
#define STUB_SIZE   64u
#define POOL_SIZE   (STUB_SIZE * HOOK_MAX_SLOTS)

static u8    s_pool[POOL_SIZE] SMAPI_ALIGNED(PAGE_SIZE);
static bool  s_pool_used[HOOK_MAX_SLOTS];

// Hook slot table
static HookSlot s_slots[HOOK_MAX_SLOTS];
static u32      s_slot_count = 0;
static Mutex    s_mutex;

// ─── Pool allocation ──────────────────────────────────────────────────────────

static u8* alloc_stub(u32* out_index) {
    for (u32 i = 0; i < HOOK_MAX_SLOTS; ++i) {
        if (!s_pool_used[i]) {
            s_pool_used[i] = true;
            if (out_index) *out_index = i;
            return s_pool + i * STUB_SIZE;
        }
    }
    return nullptr;
}

static void free_stub(u8* stub) {
    ptrdiff_t offset = stub - s_pool;
    if (offset >= 0 && (size_t)offset < POOL_SIZE) {
        s_pool_used[offset / STUB_SIZE] = false;
    }
}

// ─── Memory attribute helpers ────────────────────────────────────────────────

int hook_make_rw(void* addr, size_t size) {
    uptr page_start = PAGE_FLOOR(addr);
    size_t aligned  = PAGE_ALIGN((uptr)addr + size) - page_start;

    Result rc = svcSetMemoryAttribute(
        (void*)page_start, aligned,
        MemoryAttribute_IsUncached, 0);

    if (R_FAILED(rc)) {
        LOG_E(TAG, "svcSetMemoryAttribute RW failed: 0x%08X", rc);
        return SMAPI_FAIL;
    }
    return SMAPI_OK;
}

int hook_make_rx(void* addr, size_t size) {
    uptr page_start = PAGE_FLOOR(addr);
    size_t aligned  = PAGE_ALIGN((uptr)addr + size) - page_start;

    Result rc = svcSetMemoryAttribute(
        (void*)page_start, aligned,
        0, MemoryAttribute_IsUncached);

    if (R_FAILED(rc)) {
        LOG_E(TAG, "svcSetMemoryAttribute RX failed: 0x%08X", rc);
        return SMAPI_FAIL;
    }
    return SMAPI_OK;
}

void hook_flush_icache(void* addr, size_t size) {
    __builtin___clear_cache((char*)addr, (char*)addr + size);
}

// ─── Write the 16-byte absolute-branch trampoline ────────────────────────────

static void write_trampoline(void* dst, u64 target_addr) {
    u32* insns = (u32*)dst;
    insns[0] = INSN_LDR_X16_8;
    insns[1] = INSN_BR_X16;
    memcpy((u8*)dst + 8, &target_addr, 8);
}

// ─── Public API ───────────────────────────────────────────────────────────────

int hook_init(void) {
    mutexInit(&s_mutex);
    memset(s_slots,     0, sizeof(s_slots));
    memset(s_pool_used, 0, sizeof(s_pool_used));

    // Make the entire stub pool RWX once at startup.
    // We use a mapped region at a known page-aligned address.
    // (s_pool is page-aligned via SMAPI_ALIGNED(PAGE_SIZE))
    Result rc = svcSetMemoryAttribute(
        s_pool, POOL_SIZE,
        MemoryAttribute_IsUncached, 0);

    if (R_FAILED(rc)) {
        LOG_W(TAG, "Could not mark pool RWX (0x%08X) — hooks may fault", rc);
    }

    LOG_I(TAG, "Hook system initialised (pool at %p, %u slots)", s_pool, HOOK_MAX_SLOTS);
    return SMAPI_OK;
}

int hook_install(void* target, void* hook_fn, void** original) {
    if (!target || !hook_fn) return SMAPI_FAIL;

    mutexLock(&s_mutex);

    // Check for duplicate
    for (u32 i = 0; i < s_slot_count; ++i) {
        if (s_slots[i].active && s_slots[i].target == target) {
            LOG_W(TAG, "Hook already installed at %p", target);
            mutexUnlock(&s_mutex);
            return SMAPI_FAIL;
        }
    }

    if (s_slot_count >= HOOK_MAX_SLOTS) {
        LOG_E(TAG, "Hook slot table full");
        mutexUnlock(&s_mutex);
        return SMAPI_FAIL;
    }

    // Allocate stub for calling original
    u32   stub_idx;
    u8*   stub = alloc_stub(&stub_idx);
    if (!stub) {
        LOG_E(TAG, "Out of stub memory");
        mutexUnlock(&s_mutex);
        return SMAPI_FAIL;
    }

    // Save original bytes
    memcpy(stub, target, HOOK_TRAMPOLINE_SIZE);

    // Write: [original 16 bytes] + [trampoline to target+16]
    u64 continue_addr = (u64)target + HOOK_TRAMPOLINE_SIZE;
    write_trampoline(stub + HOOK_TRAMPOLINE_SIZE, continue_addr);

    // Flush stub cache
    hook_flush_icache(stub, STUB_SIZE);

    // Patch target function
    if (hook_make_rw(target, HOOK_TRAMPOLINE_SIZE) != SMAPI_OK) {
        free_stub(stub);
        mutexUnlock(&s_mutex);
        return SMAPI_FAIL;
    }

    write_trampoline(target, (u64)hook_fn);
    hook_flush_icache(target, HOOK_TRAMPOLINE_SIZE);
    hook_make_rx(target, HOOK_TRAMPOLINE_SIZE);

    // Record the slot
    HookSlot* slot = &s_slots[s_slot_count++];
    slot->target     = target;
    slot->hook       = hook_fn;
    slot->trampoline = stub;
    slot->active     = true;
    memcpy(slot->original_bytes, stub, HOOK_TRAMPOLINE_SIZE);

    if (original) *original = stub;

    LOG_I(TAG, "Installed hook: %p → %p (stub at %p)", target, hook_fn, stub);
    mutexUnlock(&s_mutex);
    return SMAPI_OK;
}

int hook_remove(void* target) {
    mutexLock(&s_mutex);

    for (u32 i = 0; i < s_slot_count; ++i) {
        HookSlot* slot = &s_slots[i];
        if (!slot->active || slot->target != target) continue;

        // Restore original bytes
        if (hook_make_rw(target, HOOK_TRAMPOLINE_SIZE) == SMAPI_OK) {
            memcpy(target, slot->original_bytes, HOOK_TRAMPOLINE_SIZE);
            hook_flush_icache(target, HOOK_TRAMPOLINE_SIZE);
            hook_make_rx(target, HOOK_TRAMPOLINE_SIZE);
        }

        free_stub((u8*)slot->trampoline);
        slot->active = false;

        LOG_I(TAG, "Removed hook at %p", target);
        mutexUnlock(&s_mutex);
        return SMAPI_OK;
    }

    LOG_W(TAG, "hook_remove: no hook found at %p", target);
    mutexUnlock(&s_mutex);
    return SMAPI_FAIL;
}

void hook_remove_all(void) {
    mutexLock(&s_mutex);
    for (u32 i = 0; i < s_slot_count; ++i) {
        HookSlot* slot = &s_slots[i];
        if (!slot->active) continue;

        if (hook_make_rw(slot->target, HOOK_TRAMPOLINE_SIZE) == SMAPI_OK) {
            memcpy(slot->target, slot->original_bytes, HOOK_TRAMPOLINE_SIZE);
            hook_flush_icache(slot->target, HOOK_TRAMPOLINE_SIZE);
            hook_make_rx(slot->target, HOOK_TRAMPOLINE_SIZE);
        }

        free_stub((u8*)slot->trampoline);
        slot->active = false;
    }
    s_slot_count = 0;
    mutexUnlock(&s_mutex);
    LOG_I(TAG, "All hooks removed");
}
