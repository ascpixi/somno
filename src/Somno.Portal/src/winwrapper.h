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

typedef BOOL(WINAPI* FlushViewOfFileProc)(
    LPCVOID lpBaseAddress,
    SIZE_T dwNumberOfBytesToFlush
);

typedef NTSTATUS(NTAPI* NtCreateThreadExProc) (
    OUT PHANDLE hThread,
    IN ACCESS_MASK DesiredAccess,
    IN PVOID ObjectAttributes,
    IN HANDLE ProcessHandle,
    IN PVOID lpStartAddress,
    IN PVOID lpParameter,
    IN ULONG Flags,
    IN SIZE_T StackZeroBits,
    IN SIZE_T SizeOfStackCommit,
    IN SIZE_T SizeOfStackReserve,
    OUT PVOID lpBytesBuffer
);

typedef DWORD(WINAPI* NtDelayExecutionProc)(
    BOOL Alertable,
    PLARGE_INTEGER DelayInterval
);

// We are forced to have the definition and implementation of inline methods
// in one translation unit, so we declare the variables we declared in winwrapper.cpp
// as extern here.

extern HMODULE ww_kernel32;
extern CreateFileMappingAProc ww_fptrCreateFileMappingA;
extern MapViewOfFileProc      ww_fptrMapViewOfFile;
extern UnmapViewOfFileProc    ww_fptrUnmapViewOfFile;
extern FlushViewOfFileProc    ww_fptrFlushViewOfFile;
extern NtCreateThreadExProc   ww_fptrNtCreateThreadEx;
extern NtDelayExecutionProc   ww_fptrNtDelayExecution;

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

// Writes to the disk a byte range within a mapped view of a file.
// This is a wrapped function, which does uses directly imported functions.
__forceinline BOOL WwFlushViewOfFile(
    LPCVOID lpBaseAddress,
    SIZE_T  dwNumberOfBytesToFlush
) {
    return ww_fptrFlushViewOfFile(
        lpBaseAddress,
        dwNumberOfBytesToFlush
    );
}

// Unmaps a mapped view of a file from the calling process's address space.
// This is a wrapped function, which does uses directly imported functions.
__forceinline BOOL WwUnmapViewOfFile(LPCVOID lpBaseAddress) {
    return ww_fptrUnmapViewOfFile(lpBaseAddress);
}

// This is a wrapped function, which does uses directly imported functions.
__forceinline NTSTATUS WwNtCreateThreadEx(
    OUT PHANDLE hThread,
    IN ACCESS_MASK DesiredAccess,
    IN PVOID ObjectAttributes,
    IN HANDLE ProcessHandle,
    IN PVOID lpStartAddress,
    IN PVOID lpParameter,
    IN ULONG Flags,
    IN SIZE_T StackZeroBits,
    IN SIZE_T SizeOfStackCommit,
    IN SIZE_T SizeOfStackReserve,
    OUT PVOID lpBytesBuffer
) {
    return ww_fptrNtCreateThreadEx(
        hThread,
        DesiredAccess,
        ObjectAttributes,
        ProcessHandle,
        lpStartAddress,
        lpParameter,
        Flags,
        StackZeroBits,
        SizeOfStackCommit,
        SizeOfStackReserve,
        lpBytesBuffer
    );
}