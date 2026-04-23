#include "../include/mono_api.h"
#include "../include/hook.h"
#include "../include/elf_utils.h"
#include "../include/logger.h"

#include <switch.h>
#include <cstring>
#include <cstdlib>

#define TAG "MonoHook"

// ─── Global Mono function table ───────────────────────────────────────────────

MonoFunctions g_mono = {};

// ─── Original function pointer (saved by hook_install) ────────────────────────

static fp_mono_jit_init_version s_orig_jit_init_version = nullptr;
static bool                      s_smapi_loaded           = false;

// ─── SMAPI assembly paths ─────────────────────────────────────────────────────
//
// Atmosphere's RomFS overlay makes these paths accessible inside the game
// process via the standard filesystem API after fsdevMountSdmc().
//
// Primary:  SD card at sdmc:/SMAPI/
// Fallback: Atmosphere RomFS overlay at romfs:/SMAPI/

static const char* SMAPI_PATHS[] = {
    "sdmc:/SMAPI/SwitchSMAPI.dll",
    "romfs:/SMAPI/SwitchSMAPI.dll",
    nullptr
};

// ─── Mono symbol names to resolve ────────────────────────────────────────────

struct SymbolEntry {
    const char* name;
    void**      target;
};

#define SYM(fn_name, field) { fn_name, (void**)&g_mono.field }

static const SymbolEntry k_symbols[] = {
    SYM("mono_jit_init_version",             jit_init_version),
    SYM("mono_jit_init",                     jit_init),
    SYM("mono_jit_cleanup",                  jit_cleanup),
    SYM("mono_domain_get",                   domain_get),
    SYM("mono_domain_set",                   domain_set),
    SYM("mono_domain_assembly_open",         domain_assembly_open),
    SYM("mono_assembly_get_image",           assembly_get_image),
    SYM("mono_class_from_name",              class_from_name),
    SYM("mono_object_get_class",             object_get_class),
    SYM("mono_class_get_method_from_name",   class_get_method_from_name),
    SYM("mono_method_desc_search_in_class",  method_desc_search_in_class),
    SYM("mono_method_desc_new",              method_desc_new),
    SYM("mono_method_desc_free",             method_desc_free),
    SYM("mono_runtime_invoke",               runtime_invoke),
    SYM("mono_object_new",                   object_new),
    SYM("mono_runtime_object_init",          runtime_object_init),
    SYM("mono_string_new",                   string_new),
    SYM("mono_string_to_utf8",               string_to_utf8),
    SYM("mono_free",                         mono_free),
    SYM("mono_array_new",                    array_new),
    SYM("mono_thread_attach",                thread_attach),
    SYM("mono_thread_detach",                thread_detach),
    SYM("mono_object_to_string",             object_to_string),
    { nullptr, nullptr }
};

// ─── ARM64 prologue signatures for Mono functions ─────────────────────────────
//
// If Mono is statically linked and the export table is stripped, we use
// byte-pattern scanning. These patterns are for Mono 6.12 on ARM64.
// Each entry: { function_field_ptr, pattern_bytes, wildcard_mask, length }

struct PatternEntry {
    void**       target;
    const u8*    pattern;
    const u8*    mask;
    size_t       pat_len;
};

// mono_jit_init_version: STP X29,X30,[SP,#-0x10]!; MOV X29,SP
static const u8 k_pat_jit_init_version[]  = { 0xFD, 0x7B, 0xBF, 0xA9, 0xFD, 0x03, 0x00, 0x91 };
static const u8 k_mask_jit_init_version[] = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

// ─── Symbol resolution ────────────────────────────────────────────────────────

int mono_hook_resolve(void) {
    ModuleInfo modules[MODULE_MAX];
    int mod_count = elf_enumerate_modules(modules);
    int resolved  = 0;
    int total     = 0;

    // Count total symbols
    for (int i = 0; k_symbols[i].name; ++i) ++total;

    // Try each module as a potential Mono host
    for (int m = 0; m < mod_count; ++m) {
        uptr base = modules[m].base;

        for (int i = 0; k_symbols[i].name; ++i) {
            if (*k_symbols[i].target) continue; // already found

            uptr addr = elf_get_export(base, k_symbols[i].name);
            if (addr) {
                *k_symbols[i].target = (void*)addr;
                LOG_I(TAG, "Resolved %-45s @ 0x%016llX", k_symbols[i].name, (unsigned long long)addr);
                ++resolved;
            }
        }

        // Stop scanning other modules if we found the critical function
        if (g_mono.jit_init_version) break;
    }

    // Fall back to pattern scanning for any unresolved critical symbols
    if (!g_mono.jit_init_version) {
        LOG_W(TAG, "Export scan failed for mono_jit_init_version — trying pattern scan");
        for (int m = 0; m < mod_count; ++m) {
            uptr base = modules[m].base;
            uptr addr = elf_pattern_scan(base, modules[m].text_size,
                                         k_pat_jit_init_version,
                                         k_mask_jit_init_version,
                                         sizeof(k_pat_jit_init_version));
            if (addr) {
                g_mono.jit_init_version = (fp_mono_jit_init_version)addr;
                LOG_I(TAG, "Pattern-found mono_jit_init_version @ 0x%016llX", (unsigned long long)addr);
                ++resolved;
                break;
            }
        }
    }

    LOG_I(TAG, "Resolved %d / %d Mono symbols", resolved, total);
    return resolved;
}

// ─── Our mono_jit_init_version hook ──────────────────────────────────────────

static MonoDomain* hook_mono_jit_init_version(const char* file,
                                               const char* runtime_version)
{
    LOG_I(TAG, "mono_jit_init_version intercepted (file=%s, runtime=%s)",
          file ? file : "(null)",
          runtime_version ? runtime_version : "(null)");

    // Call the original to create the domain
    MonoDomain* domain = s_orig_jit_init_version(file, runtime_version);

    if (domain && !s_smapi_loaded) {
        LOG_I(TAG, "MonoDomain created at 0x%016llX — loading Switch-SMAPI", (unsigned long long)(uptr)domain);
        s_smapi_loaded = mono_load_smapi(domain);

        if (!s_smapi_loaded) {
            LOG_E(TAG, "Failed to load Switch-SMAPI managed assembly");
        }
    }

    return domain;
}

int mono_hook_install(void) {
    if (!g_mono.jit_init_version) {
        LOG_E(TAG, "Cannot install hook: mono_jit_init_version not resolved");
        return SMAPI_FAIL;
    }

    int rc = hook_install(
        (void*)g_mono.jit_init_version,
        (void*)hook_mono_jit_init_version,
        (void**)&s_orig_jit_init_version);

    if (SMAPI_SUCCEEDED(rc)) {
        LOG_I(TAG, "Hook installed on mono_jit_init_version");
    }
    return rc;
}

// ─── Load the managed SMAPI assembly and call its entry point ────────────────

bool mono_load_smapi(MonoDomain* domain) {
    if (!g_mono.domain_assembly_open  ||
        !g_mono.assembly_get_image    ||
        !g_mono.class_from_name       ||
        !g_mono.class_get_method_from_name ||
        !g_mono.runtime_invoke)
    {
        LOG_E(TAG, "Critical Mono APIs not resolved — cannot load managed SMAPI");
        return false;
    }

    // Find the assembly on disk
    const char* smapi_path = nullptr;
    for (int i = 0; SMAPI_PATHS[i]; ++i) {
        FILE* f = fopen(SMAPI_PATHS[i], "rb");
        if (f) {
            fclose(f);
            smapi_path = SMAPI_PATHS[i];
            LOG_I(TAG, "Found SwitchSMAPI.dll at: %s", smapi_path);
            break;
        }
    }

    if (!smapi_path) {
        LOG_E(TAG, "SwitchSMAPI.dll not found in any search path");
        return false;
    }

    // Load the assembly into the domain
    MonoAssembly* asm_smapi = g_mono.domain_assembly_open(domain, smapi_path);
    if (!asm_smapi) {
        LOG_E(TAG, "mono_domain_assembly_open failed for: %s", smapi_path);
        return false;
    }
    LOG_I(TAG, "SwitchSMAPI assembly loaded");

    MonoImage* image = g_mono.assembly_get_image(asm_smapi);
    if (!image) {
        LOG_E(TAG, "mono_assembly_get_image returned null");
        return false;
    }

    // Find SwitchSMAPI.Core.Program
    MonoClass* klass = g_mono.class_from_name(image, "SwitchSMAPI.Core", "Program");
    if (!klass) {
        LOG_E(TAG, "Could not find class SwitchSMAPI.Core.Program");
        return false;
    }

    // Find SmapiMain(string[])
    MonoMethod* method = g_mono.class_get_method_from_name(klass, "SmapiMain", 1);
    if (!method) {
        LOG_E(TAG, "Could not find method Program.SmapiMain(string[])");
        return false;
    }

    // Attach a Mono thread so we can call into managed code
    MonoThread* thread = nullptr;
    if (g_mono.thread_attach) {
        thread = g_mono.thread_attach(domain);
    }

    // Build args array: string[] { "switch" }
    MonoClass* string_class = g_mono.class_from_name(image, "System", "String");
    void* args_array_ptr = nullptr;
    if (string_class && g_mono.array_new && g_mono.string_new) {
        MonoArray* arr = g_mono.array_new(domain, string_class, 1);
        MonoString* arg0 = g_mono.string_new(domain, "switch");
        // mono_array_set is a macro in mono headers; use direct memory write here
        // The array data starts at offset sizeof(MonoArray) from the object
        // For simplicity, pass null args array (SmapiMain handles null)
        args_array_ptr = arr;
        (void)arg0;
    }

    // Invoke Program.SmapiMain(args)
    void* invoke_args[1] = { args_array_ptr };
    MonoObject* exc = nullptr;
    g_mono.runtime_invoke(method, nullptr, invoke_args, &exc);

    if (exc) {
        if (g_mono.object_to_string && g_mono.string_to_utf8) {
            MonoObject* exc2 = nullptr;
            MonoString* exc_str = (MonoString*)g_mono.object_to_string(exc, &exc2);
            if (exc_str && !exc2) {
                char* msg = g_mono.string_to_utf8(exc_str);
                LOG_E(TAG, "SmapiMain threw exception: %s", msg ? msg : "(null)");
                if (msg && g_mono.mono_free) g_mono.mono_free(msg);
            }
        } else {
            LOG_E(TAG, "SmapiMain threw an unhandled exception");
        }
        if (thread && g_mono.thread_detach) g_mono.thread_detach(thread);
        return false;
    }

    LOG_I(TAG, "SmapiMain returned successfully — mods are loaded");
    if (thread && g_mono.thread_detach) g_mono.thread_detach(thread);
    return true;
}

void mono_hook_on_domain_created(MonoDomain* domain) {
    (void)domain;
}
