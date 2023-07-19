// dllmain.cpp : Defines the entry point for the DLL application.
#include <iostream>
#include <inttypes.h>
#include <stdint.h>

#include <intrin.h>
#include <Windows.h>

#include "winwrapper.h"
#include "logging.hpp"
#include "threading.hpp"
#include "strcrypt.h"
#include "stealthsleep.h"
#include "memio.h"
#include "ipc.hpp"

enum portal_state : uint8_t {
    NotInitialized,
    Running,
    Terminated
};

static HMODULE dll_base;
static portal_state run_state = portal_state::NotInitialized;

DWORD WINAPI init_thread(void* lpParam);
DWORD WINAPI main_thread(void* lpParam);

DWORD WINAPI init_thread(void* lpParam) {
    LOG_INFO("Initialization thread procedure started.");

    // Initialize Ww... functions (dynamically loaded)
    if (!initialize_winwrapper()) {
        LOG_ERROR("Couldn't initialize dynamically loaded functions.")
        return 0;
    }

    if (!ss_sleep_init(dll_base)) {
        LOG_ERROR("Couldn't initialize deferred execution functionality.");
        run_state = portal_state::Terminated;
        return 0;
    }

    dll_base = nullptr;

    ipc_region_t* ipc = open_ipc_memory();
    if (ipc == nullptr) {
        LOG_ERROR("Couldn't open IPC.");
        run_state = portal_state::Terminated;
        return 0;
    }

    bool success = somno_create_thread(main_thread, ipc);
    if (!success) {
        LOG_ERROR("Could not create the main thread.");
        run_state = portal_state::Terminated;
    }

    return 0;
}

DWORD WINAPI main_thread(void* lpParam) {
    LOG_INFO("Main thread procedure started.");
    ipc_region_t* ipc = (ipc_region_t*)lpParam;

    while (run_state == portal_state::Running) {
        while (!ipc->ctrl_pending_request && run_state == portal_state::Running) {
            _mm_pause();
            ss_sleep(2, true);
        }

        if (run_state != portal_state::Running) {
            goto portal_end;
        }

        // Pending request detected, handle it
        uint32_t ipc_read_counter = 0;

        switch (ipc->request_id)
        {
            case ipcid_ReadProcessMemory: {
                // Payload parameter 1: handle to use; 64-bit
                // Payload parameter 2: target address; 64-bit
                // Payload parameter 3: no. of bytes to read (max. 32); 32-bit
                auto handle = (HANDLE)ipc_payload_read64(ipc, &ipc_read_counter);
                auto target = (void*)ipc_payload_read64(ipc, &ipc_read_counter);
                auto amount = ipc_payload_read32(ipc, &ipc_read_counter);

                SomnoReadVirtualMemory(handle, target, ipc->payload, amount, NULL);
                ipc->ctrl_pending_request = 0;
                break;
            }

            case ipcid_Terminate: {
                LOG_INFO("Received a termination IPC message.");
                goto portal_end;
            }

            default: {
                LOG_ERROR(
                    "Unknown IPC request. (P: %" PRIu8 ", ID: 0x%" PRIx8 ", d64: %" PRIx64 ")",
                    ipc->ctrl_pending_request,
                    ipc->request_id,
                    *(uint64_t*)ipc->payload
                );
                break;
            }
        }
    }

    portal_end:
        run_state = portal_state::Terminated;
        ipc->ctrl_pending_request = 0;
        close_ipc_memory(ipc);

        LOG_INFO("Closing main thread!");
        return 0;
}

BOOL APIENTRY DllMain(
    HMODULE  hModule,
    uint32_t reasonForCall,
    void*    lpReserved
) {
    LOG_INFO("Received a DllMain call with reason %" PRIu32 ".", reasonForCall);

    if (run_state == portal_state::Terminated) {
        LOG_INFO("Ignoring the call - the portal is already closed.");
        return TRUE;
    }

    switch (reasonForCall)
    {
        case DLL_PROCESS_ATTACH: {
            if (run_state == portal_state::NotInitialized) {
                run_state = portal_state::Running;
                dll_base = hModule;

                auto handle = CreateThread(NULL, 0, init_thread, NULL, 0, NULL);

                if (handle == NULL) {
                    LOG_ERROR("Could not create the initialization thread.");
                    return FALSE;
                }

                LOG_INFO("Thread created - exiting from DllMain.");
                CloseHandle(handle); // does NOT terminate the thread
            }

            break;
        }
        case DLL_PROCESS_DETACH: {
            LOG_INFO("DLL is being detached, terminating portal connection.");
            run_state = portal_state::Terminated;
            break;
        }
    }

    return TRUE;
}

