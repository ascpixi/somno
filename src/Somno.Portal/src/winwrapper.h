#pragma once

#include <Windows.h>
#include "strcrypt.h"

typedef HANDLE(WINAPI* CreateFileMappingAProc)(
    HANDLE hFile,
    LPSECURITY_ATTRIBUTES lpFileMappingAttributes,
    DWORD flProtect,
    DWORD dwMaximumSizeHigh,
    DWORD dwMaximumSizeLow,
    LPCSTR lpName
);

typedef LPVOID(WINAPI* MapViewOfFileProc)(
    HANDLE hFileMappingObject,
    DWORD dwDesiredAccess,
    DWORD dwFileOffsetHigh,
    DWORD dwFileOffsetLow,
    SIZE_T dwNumberOfBytesToMap
);

typedef BOOL(WINAPI* UnmapViewOfFileProc)(LPCVOID lpBaseAddress);

typedef BOOL(WINAPI* ReadProcessMemoryProc)(
    HANDLE  hProcess,
    LPCVOID lpBaseAddress,
    LPVOID  lpBuffer,
    SIZE_T  nSize,
    SIZE_T* lpNumberOfBytesRead
);

// We are forced to have the definition and implementation of inline methods
// in one translation unit, so we declare the variables we declared in winwrapper.cpp
// as extern here.

extern HMODULE ww_kernel32;
extern CreateFileMappingAProc ww_fptrCreateFileMappingA;
extern MapViewOfFileProc      ww_fptrMapViewOfFile;
extern UnmapViewOfFileProc    ww_fptrUnmapViewOfFile;
extern ReadProcessMemoryProc  ww_fptrReadProcessMemory;

// Initializes the direct-load wrapper functions.
// Returns a value that determines whether the operation has failed.
bool initialize_winwrapper();

// Creates or opens a named or unnamed file mapping object for a specified file.
// This is a wrapped function, which does uses directly imported functions.
__forceinline HANDLE WwCreateFileMappingA(
    HANDLE                hFile,
    LPSECURITY_ATTRIBUTES lpFileMappingAttributes,
    DWORD                 flProtect,
    DWORD                 dwMaximumSizeHigh,
    DWORD                 dwMaximumSizeLow,
    LPCSTR                lpName
) {
    return ww_fptrCreateFileMappingA(
        hFile,
        lpFileMappingAttributes,
        flProtect,
        dwMaximumSizeHigh,
        dwMaximumSizeLow,
        lpName
    );
}

// Maps a view of a file mapping into the address space of a calling process.
// This is a wrapped function, which does uses directly imported functions.
__forceinline LPVOID WwMapViewOfFile(
    HANDLE hFileMappingObject,
    DWORD  dwDesiredAccess,
    DWORD  dwFileOffsetHigh,
    DWORD  dwFileOffsetLow,
    SIZE_T dwNumberOfBytesToMap
) {
    return ww_fptrMapViewOfFile(
        hFileMappingObject,
        dwDesiredAccess,
        dwFileOffsetHigh,
        dwFileOffsetLow,
        dwNumberOfBytesToMap
    );
}

// Unmaps a mapped view of a file from the calling process's address space.
// This is a wrapped function, which does uses directly imported functions.
__forceinline BOOL WwUnmapViewOfFile(LPCVOID lpBaseAddress) {
    return ww_fptrUnmapViewOfFile(lpBaseAddress);
}

// ReadProcessMemory copies the data in the specified address range from the address space
// of the specified process into the specified buffer of the current process. Any process
// that has a handle with PROCESS_VM_READ access can call the function.
// 
// This is a wrapped function, which does uses directly imported functions.
__forceinline BOOL WwReadProcessMemory(
    HANDLE  hProcess,
    LPCVOID lpBaseAddress,
    LPVOID  lpBuffer,
    SIZE_T  nSize,
    SIZE_T* lpNumberOfBytesRead
) {
    return ww_fptrReadProcessMemory(
        hProcess,
        lpBaseAddress,
        lpBuffer,
        nSize,
        lpNumberOfBytesRead
    );
}
