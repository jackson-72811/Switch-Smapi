#include "../include/logger.h"

#include <switch.h>
#include <cstdio>
#include <cstdarg>
#include <cstring>
#include <ctime>

// ─── Internal state ───────────────────────────────────────────────────────────

static FILE*    s_file      = nullptr;
static Mutex    s_mutex;
static bool     s_initialised = false;

static const char* level_label(LogLevel level) {
    switch (level) {
        case LOG_TRACE: return "TRACE";
        case LOG_DEBUG: return "DEBUG";
        case LOG_INFO:  return "INFO ";
        case LOG_WARN:  return "WARN ";
        case LOG_ERROR: return "ERROR";
        case LOG_ALERT: return "ALERT";
        default:        return "?????";
    }
}

// ─── Public implementation ────────────────────────────────────────────────────

int logger_init(const char* log_path) {
    mutexInit(&s_mutex);

    // Ensure the directory exists
    // Extract directory component and create it
    char dir[512];
    strncpy(dir, log_path, sizeof(dir) - 1);
    dir[sizeof(dir) - 1] = '\0';

    char* last_slash = strrchr(dir, '/');
    if (last_slash && last_slash != dir) {
        *last_slash = '\0';
        // Create directories recursively (best-effort)
        for (char* p = dir + 1; *p; ++p) {
            if (*p == '/') {
                *p = '\0';
                mkdir(dir, 0777);
                *p = '/';
            }
        }
        mkdir(dir, 0777);
    }

    s_file = fopen(log_path, "w");
    if (!s_file) {
        return SMAPI_FAIL;
    }

    s_initialised = true;

    // Write header
    fprintf(s_file,
        "============================================================\n"
        "  Switch-SMAPI Bootstrap Log\n"
        "============================================================\n\n");
    fflush(s_file);

    return SMAPI_OK;
}

void logger_log(LogLevel level, const char* tag, const char* fmt, ...) {
    if (!s_initialised || !s_file) return;

    // Format the message
    char msg[2048];
    va_list args;
    va_start(args, fmt);
    vsnprintf(msg, sizeof(msg), fmt, args);
    va_end(args);

    mutexLock(&s_mutex);
    fprintf(s_file, "[%s][%s] %s\n", level_label(level), tag, msg);
    fflush(s_file);
    mutexUnlock(&s_mutex);
}

void logger_close(void) {
    if (!s_initialised) return;

    mutexLock(&s_mutex);
    if (s_file) {
        fprintf(s_file, "\n[INFO ][Logger] Log closed.\n");
        fflush(s_file);
        fclose(s_file);
        s_file = nullptr;
    }
    s_initialised = false;
    mutexUnlock(&s_mutex);
}
