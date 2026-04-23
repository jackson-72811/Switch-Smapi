#pragma once
#include "types.h"   // u8, u16, u32, u64, s32, s64 — already defined by libnx/switch.h

// Minimal ELF64 type and constant definitions, defined with SMAPI_ prefix
// to guarantee zero name conflicts with any system or toolchain <elf.h>.
// The aarch64-none-elf cross-compiler sysroot may expose its own ELF types;
// using distinct names avoids every form of redefinition error.

typedef struct {
    s64 d_tag;
    u64 d_ptr;      // Union collapsed — d_val and d_ptr are both u64
} SmapiDynEntry;

typedef struct {
    u32           st_name;
    unsigned char st_info;
    unsigned char st_other;
    u16           st_shndx;
    u64           st_value;
    u64           st_size;
} SmapiElfSym;

typedef u32 SmapiElfWord;

// Dynamic section tags
#define SMAPI_DT_NULL      0
#define SMAPI_DT_HASH      4
#define SMAPI_DT_STRTAB    5
#define SMAPI_DT_SYMTAB    6
#define SMAPI_DT_GNU_HASH  0x6ffffef5

// Undefined symbol table index
#define SMAPI_STN_UNDEF    0
