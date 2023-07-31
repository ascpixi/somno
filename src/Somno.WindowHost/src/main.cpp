#include <inttypes.h>
#include <stdint.h>
#include <Windows.h>

#include "logging.hpp"
#include "strcrypt.h"

#define PIPE_NAME "\\\\.\\pipe\\RpRZOLoxHp"

static HWND targetHwnd;
static HANDLE hPipe;
static LONG_PTR previousWndProc;

BOOL CALLBACK EnumWindowsCallback(HWND hWnd, LPARAM lParam) {
    DWORD ownerPID;
    GetWindowThreadProcessId(hWnd, &ownerPID);

    if (ownerPID == GetCurrentProcessId()) {
        DWORD dwStyle = (DWORD)GetWindowLong(hWnd, GWL_STYLE);
        DWORD dwExStyle = (DWORD)GetWindowLong(hWnd, GWL_EXSTYLE);

        if (!(dwStyle & WS_CAPTION) || !(dwStyle && WS_VISIBLE)) {
            return TRUE;
        }

        *(HWND*)lParam = hWnd;
        return FALSE; // Do not continue enumerating
    }

    return TRUE; // Continue with the enumeration
}

LRESULT CALLBACK forward_wndproc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam) {
    DWORD bytesWritten = 0;
    WriteFile(hPipe, &hWnd, sizeof(hWnd), &bytesWritten, NULL);
    WriteFile(hPipe, &message, sizeof(message), &bytesWritten, NULL);
    WriteFile(hPipe, &wParam, sizeof(wParam), &bytesWritten, NULL);
    WriteFile(hPipe, &lParam, sizeof(lParam), &bytesWritten, NULL);

    // Wait for the LRESULT from the controller
    LRESULT lResult = 0;
    DWORD totalBytesRead = 0;

    do {
        BOOL success = ReadFile(hPipe, &lResult, sizeof(LRESULT), &bytesWritten, nullptr);
        
        if (!success) {
            return DefWindowProc(hWnd, message, wParam, lParam);
        }
        
        totalBytesRead += bytesWritten;
    } while (totalBytesRead < sizeof(LRESULT));

    LOG_INFO("Full WndProc call complete");

    return lResult;
}

#pragma optimize ("gst", off)
void initialize_pipe() {
    hPipe = CreateFileA(
        str_encrypted(PIPE_NAME),
        GENERIC_READ | GENERIC_WRITE,
        0, nullptr,
        OPEN_EXISTING, 0, nullptr
    );
}
#pragma optimize ("gst", on)

DWORD initialize_thread(LPVOID lpParam) {
    initialize_pipe();

    if (hPipe == NULL || hPipe == INVALID_HANDLE_VALUE) {
        LOG_ERROR("Failed to connect to the named pipe; error 0x%" PRIx32, GetLastError());
        return 0;
    }

    targetHwnd = 0;
    EnumWindows(EnumWindowsCallback, (LPARAM)&targetHwnd);

    if (targetHwnd == 0) {
        LOG_ERROR("Could not find a suitable window handle.");
        return 0;
    }

    LOG_INFO("Found HWND 0x%" PRIx64 ".", (uint64_t)targetHwnd);

    DWORD bytesWritten;
    WriteFile(hPipe, &targetHwnd, sizeof(targetHwnd), &bytesWritten, NULL);

    previousWndProc = GetWindowLongPtr(targetHwnd, GWLP_WNDPROC);
    SetWindowLongPtr(targetHwnd, GWLP_WNDPROC, (LONG_PTR)&forward_wndproc);

    LOG_INFO("Initialization done.");

    return 0;
}

BOOL APIENTRY DllMain(
    HMODULE  hModule,
    uint32_t reasonForCall,
    void* lpReserved
) {
    LOG_INFO("Received a DllMain call with reason %" PRIu32 ".", reasonForCall);

    switch (reasonForCall)
    {
        case DLL_PROCESS_ATTACH: {
            CreateThread(NULL, NULL, initialize_thread, NULL, NULL, NULL);
            break;
        }

        case DLL_PROCESS_DETACH: {
            SetWindowLongPtr(targetHwnd, GWLP_WNDPROC, previousWndProc);
            CloseHandle(hPipe);
            break;
        }
    }

    return TRUE;
}

