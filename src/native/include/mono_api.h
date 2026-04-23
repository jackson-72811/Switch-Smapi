#pragma once

#include "types.h"

// ─── Mono Embedded API ────────────────────────────────────────────────────────
//
// These are the Mono C API types and function pointer signatures we need to
// interact with the managed runtime from native code.
//
// Stardew Valley on Switch ships with Mono 6.x linked statically into the
// game NSO. We resolve these symbols via elf_get_export() at runtime.

// ─── Opaque Mono types ────────────────────────────────────────────────────────

typedef struct _MonoDomain       MonoDomain;
typedef struct _MonoAssembly     MonoAssembly;
typedef struct _MonoImage        MonoImage;
typedef struct _MonoClass        MonoClass;
typedef struct _MonoMethod       MonoMethod;
typedef struct _MonoObject       MonoObject;
typedef struct _MonoString       MonoString;
typedef struct _MonoArray        MonoArray;
typedef struct _MonoException    MonoException;
typedef struct _MonoClassField   MonoClassField;
typedef struct _MonoProperty     MonoProperty;
typedef struct _MonoThread       MonoThread;
typedef struct _MonoMethodDesc   MonoMethodDesc;

// ─── Function pointer typedefs ────────────────────────────────────────────────

// JIT lifecycle
typedef MonoDomain* (*fp_mono_jit_init_version)(const char* file,
                                                 const char* runtime_version);
typedef MonoDomain* (*fp_mono_jit_init)(const char* file);
typedef void        (*fp_mono_jit_cleanup)(MonoDomain* domain);

// Domain management
typedef MonoDomain* (*fp_mono_domain_get)(void);
typedef bool        (*fp_mono_domain_set)(MonoDomain* domain, bool force);

// Assembly loading
typedef MonoAssembly* (*fp_mono_domain_assembly_open)(MonoDomain* domain,
                                                       const char* name);
typedef MonoImage*    (*fp_mono_assembly_get_image)(MonoAssembly* assembly);

// Type / class lookup
typedef MonoClass* (*fp_mono_class_from_name)(MonoImage* image,
                                               const char* name_space,
                                               const char* name);
typedef MonoClass* (*fp_mono_object_get_class)(MonoObject* obj);

// Method lookup
typedef MonoMethod* (*fp_mono_class_get_method_from_name)(MonoClass* klass,
                                                            const char* name,
                                                            int param_count);
typedef MonoMethod* (*fp_mono_method_desc_search_in_class)(MonoMethodDesc* desc,
                                                             MonoClass* klass);
typedef MonoMethodDesc* (*fp_mono_method_desc_new)(const char* name,
                                                    bool include_namespace);
typedef void (*fp_mono_method_desc_free)(MonoMethodDesc* desc);

// Object / method invocation
typedef MonoObject* (*fp_mono_runtime_invoke)(MonoMethod* method,
                                               void*       obj,
                                               void**      params,
                                               MonoObject** exc);
typedef MonoObject* (*fp_mono_object_new)(MonoDomain* domain, MonoClass* klass);
typedef void        (*fp_mono_runtime_object_init)(MonoObject* obj);

// String helpers
typedef MonoString* (*fp_mono_string_new)(MonoDomain* domain, const char* text);
typedef char*       (*fp_mono_string_to_utf8)(MonoString* string_obj);
typedef void        (*fp_mono_free)(void* ptr);

// Array helpers
typedef MonoArray* (*fp_mono_array_new)(MonoDomain* domain,
                                        MonoClass*  eclass,
                                        uintptr_t   n);

// Thread management
typedef MonoThread* (*fp_mono_thread_attach)(MonoDomain* domain);
typedef void        (*fp_mono_thread_detach)(MonoThread* thread);

// Exception helpers
typedef MonoString* (*fp_mono_object_to_string)(MonoObject* obj,
                                                 MonoObject** exc);

// ─── Runtime function table (filled in by mono_hook_resolve) ─────────────────

typedef struct {
    fp_mono_jit_init_version            jit_init_version;
    fp_mono_jit_init                    jit_init;
    fp_mono_jit_cleanup                 jit_cleanup;
    fp_mono_domain_get                  domain_get;
    fp_mono_domain_set                  domain_set;
    fp_mono_domain_assembly_open        domain_assembly_open;
    fp_mono_assembly_get_image          assembly_get_image;
    fp_mono_class_from_name             class_from_name;
    fp_mono_object_get_class            object_get_class;
    fp_mono_class_get_method_from_name  class_get_method_from_name;
    fp_mono_method_desc_search_in_class method_desc_search_in_class;
    fp_mono_method_desc_new             method_desc_new;
    fp_mono_method_desc_free            method_desc_free;
    fp_mono_runtime_invoke              runtime_invoke;
    fp_mono_object_new                  object_new;
    fp_mono_runtime_object_init         runtime_object_init;
    fp_mono_string_new                  string_new;
    fp_mono_string_to_utf8              string_to_utf8;
    fp_mono_free                        mono_free;
    fp_mono_array_new                   array_new;
    fp_mono_thread_attach               thread_attach;
    fp_mono_thread_detach               thread_detach;
    fp_mono_object_to_string            object_to_string;
} MonoFunctions;

// Global table — populated by mono_hook_resolve().
extern MonoFunctions g_mono;

// ─── Public API ───────────────────────────────────────────────────────────────

// Scan loaded modules for Mono exports and fill g_mono.
// Returns the number of resolved symbols (should equal sizeof(MonoFunctions)/sizeof(void*)).
int mono_hook_resolve(void);

// Install hook on mono_jit_init_version so we fire before the Mono domain
// is returned to the game.  Must be called after mono_hook_resolve().
int mono_hook_install(void);

// Called from our hook when Mono initialises a new domain.
// Loads SwitchSMAPI.dll into |domain| and invokes Program.SmapiMain.
void mono_hook_on_domain_created(MonoDomain* domain);

// Load the managed SMAPI assembly from RomFS/SD and invoke its entry point.
bool mono_load_smapi(MonoDomain* domain);
