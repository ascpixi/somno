#pragma once

#include <cstdio>
#include <Windows.h>
#include "strcrypt.h"

#define REPORT_INFO	  false
#define REPORT_ERRORS false

#if REPORT_ERRORS
	#define LOG_ERROR(...) { \
		char cad[512]; \
		sprintf_s(cad, "[SPA::err!] " __VA_ARGS__); \
		OutputDebugStringA(cad); \
	}
#else
	#define LOG_ERROR(...) ;
#endif

#if REPORT_INFO
	#define LOG_INFO(...) { \
		char cad[512]; \
		sprintf_s(cad, "[SPA::info] " __VA_ARGS__); \
		OutputDebugStringA(cad); \
	}
#else
	#define LOG_INFO(...) ;
#endif