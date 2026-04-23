#pragma once

#include "types.h"

// Log severity levels — matches C# LogLevel enum values in managed code
typedef enum {
    LOG_TRACE   = 0,
    LOG_DEBUG   = 1,
    LOG_INFO    = 2,
    LOG_WARN    = 3,
    LOG_ERROR   = 4,
    LOG_ALERT   = 5,
} LogLevel;

// ─── Public API ───────────────────────────────────────────────────────────────

// Initialise the logger.  Must be called before any log calls.
// log_path: absolute path on SD card, e.g. "sdmc:/SMAPI/logs/bootstrap.log"
int  logger_init(const char* log_path);

// Write a formatted log entry.  Thread-safe via a lightweight mutex.
void logger_log(LogLevel level, const char* tag, const char* fmt, ...)
    __attribute__((format(printf, 3, 4)));

// Flush pending writes and close the log file.
void logger_close(void);

// ─── Convenience macros ───────────────────────────────────────────────────────

#define LOG_T(tag, ...) logger_log(LOG_TRACE, tag, __VA_ARGS__)
#define LOG_D(tag, ...) logger_log(LOG_DEBUG, tag, __VA_ARGS__)
#define LOG_I(tag, ...) logger_log(LOG_INFO,  tag, __VA_ARGS__)
#define LOG_W(tag, ...) logger_log(LOG_WARN,  tag, __VA_ARGS__)
#define LOG_E(tag, ...) logger_log(LOG_ERROR, tag, __VA_ARGS__)
