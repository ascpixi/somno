#include <Windows.h>
#include <winternl.h>
#include <atlbase.h>
#include "strcrypt.h"
#include "winwrapper.h"
#include "logging.hpp"

HMODULE ww_kernel32;
CreateFileMappingAProc ww_fptrCreateFileMappingA;
MapViewOfFileProc      ww_fptrMapViewOfFile;
UnmapViewOfFileProc    ww_fptrUnmapViewOfFile;
ReadProcessMemoryProc  ww_fptrReadProcessMemory;

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

bool initialize_winwrapper() {
    if (ww_kernel32 == NULL) {
        ww_kernel32 = WwLoadLibraryW(str_encrypted_w(L"kernel32.dll"));

        if (ww_kernel32 == NULL) {
            LOG_ERROR("Could not load kernel32.dll when loading dynamically linked functions.");
            return false;
        }
    }

    ww_fptrCreateFileMappingA = (CreateFileMappingAProc)WwGetProcAddress(
        ww_kernel32, str_encrypted("CreateFileMappingA")
    );

    if (ww_fptrCreateFileMappingA == NULL) {
        LOG_ERROR("Could not load CreateFileMappingA from kernel32.dll.");
        return false;
    }

    ww_fptrMapViewOfFile = (MapViewOfFileProc)WwGetProcAddress(
        ww_kernel32, str_encrypted("MapViewOfFile")
    );

    if (ww_fptrMapViewOfFile == NULL) {
        LOG_ERROR("Could not load MapViewOfFile from kernel32.dll.");
        return false;
    }

    ww_fptrUnmapViewOfFile = (UnmapViewOfFileProc)WwGetProcAddress(
        ww_kernel32, str_encrypted("UnmapViewOfFile")
    );

    if (ww_fptrUnmapViewOfFile == NULL) {
        LOG_ERROR("Could not load UnmapViewOfFile from kernel32.dll.");
        return false;
    }

    ww_fptrReadProcessMemory = (ReadProcessMemoryProc)WwGetProcAddress(
        ww_kernel32, str_encrypted("ReadProcessMemory")
    );

    if (ww_fptrReadProcessMemory == NULL) {
        LOG_ERROR("Could not load ReadProcessMemory from kernel32.dll.");
        return false;
    }

    LOG_INFO("All dynamically loaded functions were successfully initialized.");
    return true;
}

