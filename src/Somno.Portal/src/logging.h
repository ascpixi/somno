#pragma once

#include <ntddk.h>

#define LOG_PREFIX	  "SPA"
#define REPORT_INFO	  false
#define REPORT_ERRORS true

#if REPORT_ERRORS == true

// Logs an error to the kernel debugger.
#define LOG_ERROR(content, ...)	DbgPrintEx(0, 0, "[" LOG_PREFIX "::err!] " content "\n", __VA_ARGS__)

#else

// Error logging is disabled - as such, this macro is a no-op.
#define LOG_ERROR(...)

#endif

#if REPORT_INFO == true

// Logs an information message to the kernel debugger.
#define LOG_INFO(content, ...) DbgPrintEx(0, 0, "[" LOG_PREFIX "::info] " content "\n", __VA_ARGS__)

#else

// Information logging is disabled - as such, this macro is a no-op.
#define LOG_INFO(...)

#endif