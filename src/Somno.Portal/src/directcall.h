#pragma once

#include <Windows.h>

extern "C" NTSTATUS SomnoReadVirtualMemory(
	HANDLE hProcess,
	void* lpBaseAddress,
	void* lpBuffer,
	SIZE_T nSize,
	SIZE_T* lpNumberOfBytesRead = NULL
);
