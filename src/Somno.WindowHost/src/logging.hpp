#pragma once

#include <cstdio>
#include <Windows.h>
#include "strcrypt.h"

#define REPORT_INFO	  true
#define REPORT_ERRORS true

#if REPORT_ERRORS
	#define LOG_ERROR(...) { \
		char cad[512]; \
		sprintf_s(cad, "[SWH::err!] " __VA_ARGS__); \
		OutputDebugStringA(cad); \
	}
#else
	#define LOG_ERROR(...) ;
#endif

#if REPORT_INFO
	#define LOG_INFO(...) { \
		char cad[512]; \
		sprintf_s(cad, "[SWH::info] " __VA_ARGS__); \
		OutputDebugStringA(cad); \
	}
#else
	#define LOG_INFO(...) ;
#endif