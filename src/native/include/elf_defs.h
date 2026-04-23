#pragma once
#include <stdint.h>

// Minimal ELF64 type and constant definitions.
//
// We define these ourselves instead of including <elf.h> because:
//  - <elf.h> is a Linux system header absent from devkitPro's ARM64 sysroot
//  - The cross-compiler (aarch64-none-elf-g++) has no host system includes
//
// Only the subset used by elf_utils.cpp is defined here.

typedef uint32_t  Elf64_Word;
typedef uint64_t  Elf64_Xword;
typedef int64_t   Elf64_Sxword;
typedef uint64_t  Elf64_Addr;

typedef struct {
    Elf64_Sxword d_tag;
    union {
        Elf64_Xword d_val;
        Elf64_Addr  d_ptr;
    } d_un;
} Elf64_Dyn;

typedef struct {
    Elf64_Word    st_name;
    unsigned char st_info;
    unsigned char st_other;
    uint16_t      st_shndx;
    Elf64_Addr    st_value;
    Elf64_Xword   st_size;
} Elf64_Sym;

// Dynamic section tags
#define DT_NULL      0
#define DT_HASH      4
#define DT_STRTAB    5
#define DT_SYMTAB    6
#define DT_GNU_HASH  0x6ffffef5

// Undefined symbol index
#define STN_UNDEF    0
