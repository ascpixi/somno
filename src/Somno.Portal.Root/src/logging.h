#pragma once

#define REPORT_INFO	  true
#define REPORT_ERRORS true

#if REPORT_ERRORS == true
#define LOG_ERROR(content, ...) \
	DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_ERROR_LEVEL, "[SPA::err!] " content, __VA_ARGS__)
#else
#define LOG_ERROR(...)
#endif

#if REPORT_INFO == true
#define LOG_INFO(content, ...) \
	DbgPrintEx(DPFLTR_IHVDRIVER_ID, DPFLTR_INFO_LEVEL, "[SPA::info] " content, __VA_ARGS__)
#else
#define LOG_INFO(...)
#endif