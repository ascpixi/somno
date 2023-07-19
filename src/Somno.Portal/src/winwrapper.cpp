#include <Windows.h>
#include <winternl.h>
#include <atlbase.h>
#include "strcrypt.h"
#include "winwrapper.h"
#include "logging.hpp"

HMODULE ww_ntdll;
HMODULE ww_kernel32;
CreateFileMappingAProc ww_fptrCreateFileMappingA;
MapViewOfFileProc      ww_fptrMapViewOfFile;
UnmapViewOfFileProc    ww_fptrUnmapViewOfFile;
FlushViewOfFileProc    ww_fptrFlushViewOfFile;
VirtualProtectProc     ww_fptrVirtualProtect;
VirtualQueryExProc     ww_fptrVirtualQueryEx;
GetThreadContextProc   ww_fptrGetThreadContext;
SetThreadContextProc   ww_fptrSetThreadContext;
ResumeThreadProc       ww_fptrResumeThread;
GetSystemInfoProc      ww_fptrGetSystemInfo;
NtCreateThreadExProc   ww_fptrNtCreateThreadEx;

typedef VOID(NTAPI* pRtlInitUnicodeString)(PUNICODE_STRING DestinationString, PCWSTR SourceString);
typedef NTSTATUS(NTAPI* pLdrLoadDll) (
    PWCHAR PathToFile,
    ULONG Flags,
    PUNICODE_STRING ModuleFileName,
    PHANDLE ModuleHandle
);

// A manual implementation of GetProcAddress.
FARPROC WwGetProcAddress(HMODULE hModule, LPCSTR lpProcName) {
    PIMAGE_DOS_HEADER dosHeader = (PIMAGE_DOS_HEADER)hModule;
    PIMAGE_NT_HEADERS ntHeaders = (PIMAGE_NT_HEADERS)((BYTE*)hModule + dosHeader->e_lfanew);
    PIMAGE_EXPORT_DIRECTORY exportDirectory = (PIMAGE_EXPORT_DIRECTORY)((BYTE*)hModule +
        ntHeaders->OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress);

    DWORD* addressOfFunctions = (DWORD*)((BYTE*)hModule + exportDirectory->AddressOfFunctions);
    WORD* addressOfNameOrdinals = (WORD*)((BYTE*)hModule + exportDirectory->AddressOfNameOrdinals);
    DWORD* addressOfNames = (DWORD*)((BYTE*)hModule + exportDirectory->AddressOfNames);

    for (DWORD i = 0; i < exportDirectory->NumberOfNames; ++i) {
        if (strcmp(lpProcName, (const char*)hModule + addressOfNames[i]) == 0) {
            return (FARPROC)((BYTE*)hModule + addressOfFunctions[addressOfNameOrdinals[i]]);
        }
    }

    return NULL;
}

static int cmpUnicodeStr(WCHAR substr[], WCHAR mystr[]) {
    _wcslwr_s(substr, MAX_PATH);
    _wcslwr_s(mystr, MAX_PATH);

    int result = 0;
    if (StrStrW(mystr, substr) != NULL) {
        result = 1;
    }

    return result;
}

HMODULE WwGetModuleHandleW(LPCWSTR lModuleName) {
    // obtaining the offset of PPEB from the beginning of TEB
    PEB* pPeb = (PEB*)__readgsqword(0x60);

    // for x86
    // PEB* pPeb = (PEB*)__readgsqword(0x30);

    // obtaining the address of the head node in a linked list 
    // which represents all the models that are loaded into the process.
    PEB_LDR_DATA* Ldr = pPeb->Ldr;
    LIST_ENTRY* ModuleList = &Ldr->InMemoryOrderModuleList;

    // iterating to the next node. this will be our starting point.
    LIST_ENTRY* pStartListEntry = ModuleList->Flink;

    // iterating through the linked list.
    WCHAR mystr[MAX_PATH] = { 0 };
    WCHAR substr[MAX_PATH] = { 0 };
    for (LIST_ENTRY* pListEntry = pStartListEntry; pListEntry != ModuleList; pListEntry = pListEntry->Flink) {

        // getting the address of current LDR_DATA_TABLE_ENTRY (which represents the DLL).
        LDR_DATA_TABLE_ENTRY* pEntry = (LDR_DATA_TABLE_ENTRY*)((BYTE*)pListEntry - sizeof(LIST_ENTRY));

        // checking if this is the DLL we are looking for
        memset(mystr, 0, MAX_PATH * sizeof(WCHAR));
        memset(substr, 0, MAX_PATH * sizeof(WCHAR));
        wcscpy_s(mystr, MAX_PATH, pEntry->FullDllName.Buffer);
        wcscpy_s(substr, MAX_PATH, lModuleName);
        if (cmpUnicodeStr(substr, mystr)) {
            // returning the DLL base address.
            return (HMODULE)pEntry->DllBase;
        }
    }

    // the needed DLL wasn't found
    LOG_ERROR("WwGetModuleHandleW: failed to get a handle to %ls.", lModuleName);
    return NULL;
}

#pragma optimize ("t", off)

// A manual implementation of LoadLibraryW.
HMODULE WwLoadLibraryW(LPCWSTR lpFileName) {
    UNICODE_STRING ustrModule;
    HANDLE hModule = NULL;

    HMODULE hNtdll = WwGetModuleHandleW(str_encrypted_w(L"ntdll.dll"));
    if (hNtdll == NULL) {
        return NULL;
    }

    pRtlInitUnicodeString RtlInitUnicodeString = (pRtlInitUnicodeString)WwGetProcAddress(
        hNtdll,
        str_encrypted("RtlInitUnicodeString")
    );

    if (RtlInitUnicodeString == NULL) {
        LOG_ERROR("Could not get the address of RtlInitUnicodeString.");
        return NULL;
    }

    RtlInitUnicodeString(&ustrModule, lpFileName);

    pLdrLoadDll myLdrLoadDll = (pLdrLoadDll)WwGetProcAddress(
        hNtdll,
        str_encrypted("LdrLoadDll")
    );

    if (!myLdrLoadDll) {
        LOG_ERROR("Could not get the address of LdrLoadDll.");
        return NULL;
    }

    NTSTATUS status = myLdrLoadDll(NULL, 0, &ustrModule, &hModule);
    return (HMODULE)hModule;
}

#define __ww_initproc(fptr, fptrType, libptr, name, libname)        \
    fptr = (fptrType)WwGetProcAddress(libptr, str_encrypted(name)); \
    if(fptr == NULL) {                                              \
        LOG_ERROR("Could not load " name " from " libname ".");     \
        return false;                                               \
    }

bool initialize_winwrapper() {
    if (ww_kernel32 == NULL) {
        ww_kernel32 = WwLoadLibraryW(str_encrypted_w(L"kernel32.dll"));

        if (ww_kernel32 == NULL) {
            LOG_ERROR("Could not load kernel32.dll when loading dynamically linked functions.");
            return false;
        }
    }

    if (ww_ntdll == NULL) {
        ww_ntdll = WwLoadLibraryW(str_encrypted_w(L"ntdll.dll"));

        if (ww_ntdll == NULL) {
            LOG_ERROR("Could not load ntdll.dll when loading dynamically linked functions.");
            return false;
        }
    }

    // kernel32.dll functions
    __ww_initproc(
        ww_fptrCreateFileMappingA, CreateFileMappingAProc,
        ww_kernel32, "CreateFileMappingA", "kernel32.dll"
    );

    __ww_initproc(
        ww_fptrMapViewOfFile, MapViewOfFileProc,
        ww_kernel32, "MapViewOfFile", "kernel32.dll"
    );

    __ww_initproc(
        ww_fptrFlushViewOfFile, FlushViewOfFileProc,
        ww_kernel32, "FlushViewOfFile", "kernel32.dll"
    );

    __ww_initproc(
        ww_fptrUnmapViewOfFile, UnmapViewOfFileProc,
        ww_kernel32, "UnmapViewOfFile", "kernel32.dll"
    );

    __ww_initproc(
        ww_fptrVirtualProtect, VirtualProtectProc,
        ww_kernel32, "VirtualProtect", "kernel32.dll"
    );

    __ww_initproc(
        ww_fptrVirtualQueryEx, VirtualQueryExProc,
        ww_kernel32, "VirtualQueryEx", "kernel32.dll"
    );

    __ww_initproc(
        ww_fptrGetThreadContext, GetThreadContextProc,
        ww_kernel32, "GetThreadContext", "kernel32.dll"
    );

    __ww_initproc(
        ww_fptrSetThreadContext, SetThreadContextProc,
        ww_kernel32, "SetThreadContext", "kernel32.dll"
    );

    __ww_initproc(
        ww_fptrResumeThread, ResumeThreadProc,
        ww_kernel32, "ResumeThread", "kernel32.dll"
    );

    __ww_initproc(
        ww_fptrGetSystemInfo, GetSystemInfoProc,
        ww_kernel32, "GetSystemInfo", "kernel32.dll"
    );

    // ntdll.dll functions
    __ww_initproc(
        ww_fptrNtCreateThreadEx, NtCreateThreadExProc,
        ww_ntdll, "NtCreateThreadEx", "ntdll.dll"
    );

    LOG_INFO("All dynamically loaded functions were successfully initialized.");
    return true;
}

#pragma optimize ("t", on)