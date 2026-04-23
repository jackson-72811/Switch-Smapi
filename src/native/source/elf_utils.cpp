#include "../include/elf_utils.h"
#include "../include/logger.h"

#include <switch.h>
#include <cstdio>
#include <cstring>
#include <cstdlib>
#include "../include/elf_defs.h"

#define TAG "ELF"

// ─── MOD0 header (Switch NSO internal structure) ──────────────────────────────
//
// Every NSO loaded by Horizon has a MOD0 header embedded at a specific offset
// pointed to by a 4-byte value at NSO image offset +4.
// The MOD0 magic is "MOD0" (0x304F444D).

#define MOD0_MAGIC 0x304F444Du

typedef struct SMAPI_PACKED {
    u32 magic;              // "MOD0"
    s32 dynamic_offset;     // offset from &this to .dynamic section
    s32 bss_start_offset;   // offset from &this to start of .bss
    s32 bss_end_offset;     // offset from &this to end of .bss
    s32 eh_frame_hdr_start; // offset from &this to .eh_frame_hdr start
    s32 eh_frame_hdr_end;   // offset from &this to .eh_frame_hdr end
    s32 runtime_ptr;        // offset from &this to runtime-generated module object (or 0)
} Mod0Header;

// ─── Dynamic entry ────────────────────────────────────────────────────────────

typedef Elf64_Dyn  DynEntry;
typedef Elf64_Sym  ElfSymbol;
typedef Elf64_Word ElfWord;

// ─── Module enumeration ───────────────────────────────────────────────────────

// We walk memory starting from the base of our own module and probe
// each page-aligned address for the NSO identity bytes.
// Each NSO starts with: 4 bytes padding, then a 4-byte offset to MOD0.
// The initial 4 bytes are the "NSO magic" but the actual text segment
// starts at the module base. We detect a valid NSO text page by checking
// that offset+4 into the page points to a valid MOD0 header.

static bool probe_module_base(uptr addr, ModuleInfo* out) {
    // All pages in the process are in [0x08000000, 0xFFFFFFFFFF].
    // We use svcQueryMemory to walk allocated regions.
    MemoryInfo minfo;
    u32        page_info;
    Result rc = svcQueryMemory(&minfo, &page_info, addr);
    if (R_FAILED(rc)) return false;

    // Only care about executable mapped regions
    if (!(minfo.perm & Perm_X)) return false;
    if (minfo.type != MemType_CodeStatic &&
        minfo.type != MemType_CodeMutable) return false;

    uptr base = minfo.addr;

    // The MOD0 pointer is a s32 at offset +4 from the NSO text start.
    // mod0_ptr = base + *(s32*)(base+4)  (relative)
    if (base + 8 > base + minfo.size) return false;

    s32 mod0_rel = *(s32*)(base + 4);
    uptr mod0_addr = base + mod0_rel;

    // Bounds check
    if (mod0_addr < base || mod0_addr + sizeof(Mod0Header) > base + minfo.size)
        return false;

    Mod0Header* mod0 = (Mod0Header*)mod0_addr;
    if (mod0->magic != MOD0_MAGIC) return false;

    out->base       = base;
    out->text_size  = minfo.size;
    out->rodata_size = 0;
    out->data_size   = 0;
    snprintf(out->name, sizeof(out->name), "module@%016lX", base);
    return true;
}

int elf_enumerate_modules(ModuleInfo out[MODULE_MAX]) {
    int count = 0;
    uptr addr = 0;

    while (count < (int)MODULE_MAX) {
        MemoryInfo minfo;
        u32        page_info;
        Result rc = svcQueryMemory(&minfo, &page_info, addr);
        if (R_FAILED(rc)) break;
        if (addr + minfo.size < addr) break; // overflow

        if (minfo.perm & Perm_X) {
            ModuleInfo mi = {};
            if (probe_module_base(minfo.addr, &mi)) {
                out[count++] = mi;
                LOG_D(TAG, "Found module: base=0x%016lX size=0x%zX",
                    mi.base, mi.text_size);
            }
        }

        addr = minfo.addr + minfo.size;
        if (addr == 0) break;
    }

    LOG_I(TAG, "Enumerated %d modules", count);
    return count;
}

uptr elf_find_module(const char* substr) {
    ModuleInfo modules[MODULE_MAX];
    int count = elf_enumerate_modules(modules);

    for (int i = 0; i < count; ++i) {
        if (strstr(modules[i].name, substr)) {
            return modules[i].base;
        }
    }
    return 0;
}

// ─── Export lookup ────────────────────────────────────────────────────────────

bool elf_read_mod0(uptr base,
                   uptr* out_dynamic_offset,
                   uptr* out_bss_start,
                   uptr* out_bss_end)
{
    s32 mod0_rel = *(s32*)(base + 4);
    uptr mod0_addr = base + mod0_rel;
    Mod0Header* mod0 = (Mod0Header*)mod0_addr;

    if (mod0->magic != MOD0_MAGIC) return false;

    if (out_dynamic_offset)
        *out_dynamic_offset = mod0_addr + mod0->dynamic_offset;
    if (out_bss_start)
        *out_bss_start = mod0_addr + mod0->bss_start_offset;
    if (out_bss_end)
        *out_bss_end = mod0_addr + mod0->bss_end_offset;

    return true;
}

// Walk the PT_DYNAMIC segment to find DT_SYMTAB, DT_STRTAB, DT_HASH / DT_GNU_HASH.
uptr elf_get_export(uptr base, const char* name) {
    uptr dyn_addr = 0;
    if (!elf_read_mod0(base, &dyn_addr, nullptr, nullptr)) {
        LOG_W(TAG, "elf_get_export: no MOD0 at base 0x%016lX", base);
        return 0;
    }

    // Parse .dynamic entries
    const DynEntry* dyn = (const DynEntry*)dyn_addr;

    uptr symtab  = 0;
    uptr strtab  = 0;
    uptr hash    = 0;
    uptr gnu_hash = 0;

    for (int i = 0; dyn[i].d_tag != DT_NULL && i < 256; ++i) {
        uptr val = (uptr)dyn[i].d_un.d_ptr;
        switch (dyn[i].d_tag) {
            case DT_SYMTAB:    symtab   = base + val; break;
            case DT_STRTAB:    strtab   = base + val; break;
            case DT_HASH:      hash     = base + val; break;
            case DT_GNU_HASH:  gnu_hash = base + val; break;
            default: break;
        }
    }

    if (!symtab || !strtab) {
        LOG_W(TAG, "elf_get_export: missing symtab/strtab at 0x%016lX", base);
        return 0;
    }

    // Try SYSV hash table first
    if (hash) {
        const ElfWord* htab   = (const ElfWord*)hash;
        ElfWord nbucket       = htab[0];
        ElfWord nchain        = htab[1];
        const ElfWord* bucket = htab + 2;
        const ElfWord* chain  = bucket + nbucket;

        // SYSV hash function
        unsigned long h = 0, g;
        for (const char* c = name; *c; ++c) {
            h = (h << 4) + (unsigned char)*c;
            if ((g = h & 0xF0000000u)) h ^= g >> 24;
            h &= ~g;
        }

        ElfWord idx = bucket[h % nbucket];
        while (idx != STN_UNDEF && idx < nchain) {
            const ElfSymbol* sym = (const ElfSymbol*)symtab + idx;
            const char* sym_name = (const char*)strtab + sym->st_name;
            if (strcmp(sym_name, name) == 0 && sym->st_value != 0) {
                return base + sym->st_value;
            }
            idx = chain[idx];
        }
    }

    // Fall back to GNU hash table
    if (gnu_hash) {
        const u32* gh    = (const u32*)gnu_hash;
        u32 nbuckets     = gh[0];
        u32 symoffset    = gh[1];
        u32 bloom_size   = gh[2];
        u32 bloom_shift  = gh[3];
        const u64* bloom = (const u64*)(gh + 4);
        const u32* bkt   = (const u32*)(bloom + bloom_size);
        const u32* chn   = bkt + nbuckets;

        u32 h1 = 5381;
        for (const char* c = name; *c; ++c)
            h1 = h1 * 33 + (unsigned char)*c;
        u32 h2 = h1 >> bloom_shift;

        u64 bword = bloom[(h1 / 64) % bloom_size];
        if (!((bword >> (h1 % 64)) & 1)) goto done;
        if (!((bword >> (h2 % 64)) & 1)) goto done;

        {
            u32 bidx = h1 % nbuckets;
            u32 sidx = bkt[bidx];
            if (sidx == 0) goto done;

            const u32*     hchain = chn + (sidx - symoffset);
            const ElfSymbol* sym  = (const ElfSymbol*)symtab + sidx;

            for (;;) {
                const char* sym_name = (const char*)strtab + sym->st_name;
                if (((*hchain) | 1) == (h1 | 1) && strcmp(sym_name, name) == 0) {
                    if (sym->st_value != 0)
                        return base + sym->st_value;
                }
                if (*hchain & 1) break;
                ++hchain;
                ++sym;
            }
        }
    }

done:
    LOG_D(TAG, "Export '%s' not found in module at 0x%016lX", name, base);
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
            if (mask && mask[j] == 0xFF) continue; // wildcard
            if (data[i + j] != pattern[j]) { match = false; break; }
        }
        if (match) return start + i;
    }
    return 0;
}
