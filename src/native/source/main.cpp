#include "../include/types.h"
#include "../include/logger.h"
#include "../include/hook.h"
#include "../include/elf_utils.h"
#include "../include/mono_api.h"

#include <switch.h>
#include <cstdio>
#include <cstring>

#define TAG "Bootstrap"

// ─── SD card mount ────────────────────────────────────────────────────────────

static bool s_sdmc_mounted = false;

static void mount_sdmc(void) {
    if (s_sdmc_mounted) return;
    Result rc = fsdevMountSdmc();
    s_sdmc_mounted = R_SUCCEEDED(rc);
    if (!s_sdmc_mounted) {
        // Cannot log yet — use svcOutputDebugString as last resort
        const char* msg = "[Switch-SMAPI] FATAL: Could not mount sdmc!\n";
        svcOutputDebugString(msg, strlen(msg));
    }
}

// ─── Bootstrap entry point ────────────────────────────────────────────────────
//
// This function is placed in .init_array so the linker/loader calls it
// automatically before transferring control to the game's main().
// At this point, most of the Switch SDK is already initialised by the
// C runtime startup (crt0.s in libnx).

static void smapi_bootstrap_init(void) {
    // 1. Mount the SD card so we can write logs and read the managed assembly
    mount_sdmc();

    // 2. Initialise the file logger
    if (logger_init("sdmc:/SMAPI/logs/bootstrap.log") != SMAPI_OK) {
        const char* msg = "[Switch-SMAPI] WARNING: Could not open bootstrap.log\n";
        svcOutputDebugString(msg, strlen(msg));
        return;
    }

    LOG_I(TAG, "╔══════════════════════════════════════════════════════════╗");
    LOG_I(TAG, "║              Switch-SMAPI Bootstrap v1.0                ║");
    LOG_I(TAG, "╚══════════════════════════════════════════════════════════╝");

    // 3. Initialise the hook engine
    if (hook_init() != SMAPI_OK) {
        LOG_E(TAG, "Hook engine initialisation failed — aborting");
        return;
    }

    // 4. Resolve Mono runtime symbols from the game process
    int resolved = mono_hook_resolve();
    if (resolved == 0) {
        LOG_E(TAG, "No Mono symbols resolved — is this really Stardew Valley?");
        return;
    }

    if (!g_mono.jit_init_version) {
        LOG_E(TAG, "mono_jit_init_version not found — cannot hook Mono startup");
        return;
    }

    // 5. Install hook on Mono JIT initialisation
    if (mono_hook_install() != SMAPI_OK) {
        LOG_E(TAG, "Failed to install Mono hook — mods will not load");
        return;
    }

    LOG_I(TAG, "Bootstrap complete — waiting for Mono to initialise...");
}

// ─── Place smapi_bootstrap_init in .init_array ────────────────────────────────
//
// The __attribute__((constructor)) or explicit .init_array section placement
// tells the startup code to call our function before main().
// Priority 101 puts us after libnx's own constructors (priority 100).

__attribute__((constructor(101)))
static void smapi_ctor(void) {
    smapi_bootstrap_init();
}

// ─── Cleanup ──────────────────────────────────────────────────────────────────
//
// Called when the game exits (either normally or via an applet transition).

__attribute__((destructor))
static void smapi_dtor(void) {
    LOG_I(TAG, "Game exiting — cleaning up Switch-SMAPI");
    hook_remove_all();
    logger_close();
    if (s_sdmc_mounted) fsdevUnmountAll();
}

// ─── nninitStartup override (optional second injection point) ─────────────────
//
// Some Switch games call nninitStartup before Mono initialises.
// We hook this as a backup entry point if the .init_array approach
// is blocked by NRO ordering constraints.
//
// Commented out by default — enable if the constructor approach doesn't fire.
//
// extern "C" void nninitStartup(void);
// extern "C" void __real_nninitStartup(void);
// extern "C" void __wrap_nninitStartup(void) {
//     smapi_bootstrap_init();
//     __real_nninitStartup();
// }

// ─── Required by libnx crt0 ───────────────────────────────────────────────────
// This NSO is loaded as a subsdk module — main() is never called, but the
// linker still requires the symbol because crt0.s references it unconditionally.
extern "C" int main(int /*argc*/, char* /*argv*/[]) {
    return 0;
}
