#define WIN32_NO_STATUS

#include <windows.h>
#include <stdint.h>
#include "cmdread.h"
#include "cmdexec.h"
#include "cmdserver.h"
#include "strenc.h"

#define COMMAND_PIPE_NAME ENC_LPWSTR("\\\\.\\pipe\\somno_game_replay_capture")

DWORD WINAPI ClientThread(LPVOID pipe) {
    while (true) {
        CommandPacket pkt = ReadCommandPacket(pipe);

        if (pkt.type == CMDTYPE_CLOSE) {
            // Special case for DLL thread termination
            return 0;
        }

        uint64_t response = ExecuteCommandPacket(pkt);

        WriteFile(
            pipe,
            &response,
            8,
            NULL,
            NULL
        );
    }

    return 0;
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    if (ul_reason_for_call != DLL_PROCESS_ATTACH) {
        return TRUE;
    }

    HANDLE pipe = CreateFileW(
        COMMAND_PIPE_NAME,
        GENERIC_READ | GENERIC_WRITE,
        FILE_SHARE_WRITE,
        NULL, OPEN_EXISTING,
        0, NULL
    );

    if (pipe == INVALID_HANDLE_VALUE) {
        char msgBuffer[64];

        snprintf(msgBuffer, 64,
            ENC_STRING("Could not connect to pipe; error %d."), GetLastError()
        );

        MessageBoxA(
            NULL,
            msgBuffer,
            ENC_STRING("Somno DLL Injection Error"),
            MB_ICONERROR | MB_OK
        );

        return TRUE;
    }

    HANDLE thread = CreateThread(nullptr, 0, ClientThread, pipe, 0, nullptr);

    if (thread == INVALID_HANDLE_VALUE) {
        MessageBoxW(
            NULL,
            ENC_LPWSTR("Could not create a thread from the DLL!"),
            ENC_LPWSTR("Somno DLL Injection Error"),
            MB_ICONERROR | MB_OK
        );

        return TRUE;
    }

    return TRUE;
}

