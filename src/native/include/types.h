#pragma once

#include <stdint.h>
#include <stddef.h>
#include <stdbool.h>

// ─── Convenience integer aliases ─────────────────────────────────────────────

typedef uint8_t   u8;
typedef uint16_t  u16;
typedef uint32_t  u32;
typedef uint64_t  u64;
typedef int8_t    s8;
typedef int16_t   s16;
typedef int32_t   s32;
typedef int64_t   s64;
typedef uintptr_t uptr;

// ─── Compiler helpers ─────────────────────────────────────────────────────────

#define SMAPI_EXPORT    __attribute__((visibility("default")))
#define SMAPI_UNUSED    __attribute__((unused))
#define SMAPI_NOINLINE  __attribute__((noinline))
#define SMAPI_PACKED    __attribute__((packed))
#define SMAPI_ALIGNED(n) __attribute__((aligned(n)))

#define SMAPI_LIKELY(x)   __builtin_expect(!!(x), 1)
#define SMAPI_UNLIKELY(x) __builtin_expect(!!(x), 0)

// ─── ARM64 page / cache helpers ───────────────────────────────────────────────

#define PAGE_SIZE       0x1000u
#define PAGE_MASK       (~(PAGE_SIZE - 1))
#define PAGE_ALIGN(a)   (((uptr)(a) + PAGE_SIZE - 1) & PAGE_MASK)
#define PAGE_FLOOR(a)   ((uptr)(a) & PAGE_MASK)

// ARM64 instruction width is always 4 bytes
#define INSN_SIZE       4u

// ─── Result helpers ───────────────────────────────────────────────────────────

#define SMAPI_OK        0
#define SMAPI_FAIL     -1

#define SMAPI_SUCCEEDED(r) ((r) == SMAPI_OK)
#define SMAPI_FAILED(r)    ((r) != SMAPI_OK)
