#include "../include/elf_utils.h"
#include "../include/logger.h"
#include "../include/elf_defs.h"

#include <switch.h>
#include <cstdio>
#include <cstring>
#include <cstdlib>

#define TAG "ELF"

// ─── MOD0 header (Switch NSO internal structure) ──────────────────────────────
//
// Every NSO loaded by Horizon has a MOD0 header at the offset stored in a
// little-endian s32 at image base + 4.  Magic is "MOD0" (0x304F444D).

#define MOD0_MAGIC 0x304F444Du

typedef struct SMAPI_PACKED {
    u32 magic;
    s32 dynamic_offset;
    s32 bss_start_offset;
    s32 bss_end_offset;
    s32 eh_frame_hdr_start;
    s32 eh_frame_hdr_end;
    s32 runtime_ptr;
} Mod0Header;

// ─── Module enumeration ───────────────────────────────────────────────────────

static bool probe_module_base(uptr addr, ModuleInfo* out) {
    MemoryInfo minfo;
    u32        page_info;
    Result rc = svcQueryMemory(&minfo, &page_info, addr);
    if (R_FAILED(rc)) return false;

    if (!(minfo.perm & Perm_X)) return false;
    if (minfo.type != MemType_CodeStatic &&
        minfo.type != MemType_CodeMutable) return false;

    uptr base = (uptr)minfo.addr;
    if (minfo.size < 8u) return false;

    s32 mod0_rel;
    memcpy(&mod0_rel, (const void*)(base + 4), sizeof(mod0_rel));
    uptr mod0_addr = base + (uptr)(s64)mod0_rel;

    if (mod0_addr < base || mod0_addr + sizeof(Mod0Header) > base + minfo.size)
        return false;

    Mod0Header mod0;
    memcpy(&mod0, (const void*)mod0_addr, sizeof(mod0));
    if (mod0.magic != MOD0_MAGIC) return false;

    out->base        = base;
    out->text_size   = (size_t)minfo.size;
    out->rodata_size = 0;
    out->data_size   = 0;
    snprintf(out->name, sizeof(out->name), "module@%016llX", (unsigned long long)base);
    return true;
}

int elf_enumerate_modules(ModuleInfo out[MODULE_MAX]) {
    int  count = 0;
    uptr addr  = 0;

    while (count < (int)MODULE_MAX) {
        MemoryInfo minfo;
        u32        page_info;
        Result rc = svcQueryMemory(&minfo, &page_info, addr);
        if (R_FAILED(rc)) break;
        if (minfo.size == 0) break;

        if (minfo.perm & Perm_X) {
            ModuleInfo mi = {};
            if (probe_module_base((uptr)minfo.addr, &mi)) {
                out[count++] = mi;
                LOG_D(TAG, "Module: base=0x%016llX size=0x%llX",
                    (unsigned long long)mi.base, (unsigned long long)mi.text_size);
            }
        }

        uptr next = (uptr)minfo.addr + (uptr)minfo.size;
        if (next <= addr) break;  // overflow or zero-size
        addr = next;
    }

    LOG_I(TAG, "Enumerated %d module(s)", count);
    return count;
}

uptr elf_find_module(const char* substr) {
    ModuleInfo modules[MODULE_MAX];
    int count = elf_enumerate_modules(modules);
    for (int i = 0; i < count; ++i) {
        if (strstr(modules[i].name, substr))
            return modules[i].base;
    }
    return 0;
}

// ─── MOD0 reader ─────────────────────────────────────────────────────────────

bool elf_read_mod0(uptr base,
                   uptr* out_dynamic_offset,
                   uptr* out_bss_start,
                   uptr* out_bss_end)
{
    s32 mod0_rel;
    memcpy(&mod0_rel, (const void*)(base + 4), sizeof(mod0_rel));
    uptr mod0_addr = base + (uptr)(s64)mod0_rel;

    Mod0Header mod0;
    memcpy(&mod0, (const void*)mod0_addr, sizeof(mod0));
    if (mod0.magic != MOD0_MAGIC) return false;

    if (out_dynamic_offset)
        *out_dynamic_offset = mod0_addr + (uptr)(s64)mod0.dynamic_offset;
    if (out_bss_start)
        *out_bss_start = mod0_addr + (uptr)(s64)mod0.bss_start_offset;
    if (out_bss_end)
        *out_bss_end = mod0_addr + (uptr)(s64)mod0.bss_end_offset;

    return true;
}

// ─── Export lookup ────────────────────────────────────────────────────────────

uptr elf_get_export(uptr base, const char* name) {
    uptr dyn_addr = 0;
    if (!elf_read_mod0(base, &dyn_addr, nullptr, nullptr)) {
        LOG_W(TAG, "elf_get_export: no MOD0 at 0x%016llX", (unsigned long long)base);
        return 0;
    }

    uptr symtab   = 0;
    uptr strtab   = 0;
    uptr hash     = 0;
    uptr gnu_hash = 0;

    const SmapiDynEntry* dyn = (const SmapiDynEntry*)dyn_addr;
    for (int i = 0; dyn[i].d_tag != SMAPI_DT_NULL && i < 256; ++i) {
        uptr val = (uptr)dyn[i].d_ptr;
        switch ((int)dyn[i].d_tag) {
            case SMAPI_DT_SYMTAB:   symtab   = base + val; break;
            case SMAPI_DT_STRTAB:   strtab   = base + val; break;
            case SMAPI_DT_HASH:     hash     = base + val; break;
            case SMAPI_DT_GNU_HASH: gnu_hash = base + val; break;
            default: break;
        }
    }

    if (!symtab || !strtab) {
        LOG_W(TAG, "elf_get_export: missing symtab/strtab in 0x%016llX",
              (unsigned long long)base);
        return 0;
    }

    // ── SYSV hash table ────────────────────────────────────────────────────
    if (hash) {
        const SmapiElfWord* htab   = (const SmapiElfWord*)hash;
        SmapiElfWord nbucket       = htab[0];
        SmapiElfWord nchain        = htab[1];
        const SmapiElfWord* bucket = htab + 2;
        const SmapiElfWord* chain  = bucket + nbucket;

        unsigned long h = 0, g;
        for (const char* c = name; *c; ++c) {
            h = (h << 4) + (unsigned char)*c;
            if ((g = h & 0xF0000000uL)) h ^= g >> 24;
            h &= ~g;
        }

        SmapiElfWord idx = bucket[h % nbucket];
        while (idx != SMAPI_STN_UNDEF && idx < nchain) {
            const SmapiElfSym* sym = (const SmapiElfSym*)symtab + idx;
            const char* sym_name   = (const char*)strtab + sym->st_name;
            if (strcmp(sym_name, name) == 0 && sym->st_value != 0)
                return base + (uptr)sym->st_value;
            idx = chain[idx];
        }
    }

    // ── GNU hash table ─────────────────────────────────────────────────────
    if (gnu_hash) {
        const u32* gh      = (const u32*)gnu_hash;
        u32 nbuckets       = gh[0];
        u32 symoffset      = gh[1];
        u32 bloom_size     = gh[2];
        u32 bloom_shift    = gh[3];
        const u64* bloom   = (const u64*)(gh + 4);
        const u32* bkt     = (const u32*)(bloom + bloom_size);
        const u32* chn     = bkt + nbuckets;

        u32 h1 = 5381;
        for (const char* c = name; *c; ++c)
            h1 = h1 * 33 + (unsigned char)*c;
        u32 h2 = h1 >> bloom_shift;

        u64 bword = bloom[(h1 / 64) % bloom_size];
        if (!((bword >> (h1 % 64)) & 1u)) goto done;
        if (!((bword >> (h2 % 64)) & 1u)) goto done;

        {
            u32 bidx = h1 % nbuckets;
            u32 sidx = bkt[bidx];
            if (sidx == 0) goto done;

            const u32*       hchain = chn + (sidx - symoffset);
            const SmapiElfSym* sym  = (const SmapiElfSym*)symtab + sidx;

            for (;;) {
                const char* sym_name = (const char*)strtab + sym->st_name;
                if (((*hchain) | 1u) == (h1 | 1u) &&
                    strcmp(sym_name, name) == 0 &&
                    sym->st_value != 0)
                {
                    return base + (uptr)sym->st_value;
                }
                if (*hchain & 1u) break;
                ++hchain;
                ++sym;
            }
        }
    }

done:
    LOG_D(TAG, "Export '%s' not found in 0x%016llX", name, (unsigned long long)base);
    return 0;
}

// ─── Pattern scanning ─────────────────────────────────────────────────────────

uptr elf_pattern_scan(uptr start, size_t size,
                      const u8* pattern, const u8* mask, size_t pat_len)
{
    if (!start || !size || !pattern || pat_len == 0) return 0;

    const u8* data = (const u8*)start;
    for (size_t i = 0; i + pat_len <= size; ++i) {
        bool match = true;
        for (size_t j = 0; j < pat_len; ++j) {
            if (mask && mask[j] == 0xFF) continue;
            if (data[i + j] != pattern[j]) { match = false; break; }
        }
        if (match) return start + i;
    }
    return 0;
}
