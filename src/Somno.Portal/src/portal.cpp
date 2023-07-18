// dllmain.cpp : Defines the entry point for the DLL application.
#include <iostream>
#include <inttypes.h>
#include <stdint.h>

#include <intrin.h>
#include <Windows.h>

#include "winwrapper.h"
#include "directcall.h"
#include "threading.hpp"
#include "logging.hpp"
#include "strcrypt.h"
#include "ipc.hpp"

enum portal_state : uint8_t {
    NotInitialized,
    Running,
    Terminated
};

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

    ipc_region_t* ipc = open_ipc_memory();

    if (ipc == nullptr) {
        LOG_ERROR("Couldn't open IPC.");
        run_state = portal_state::Terminated;
        return 0;
    }

    bool success = somno_create_thread(main_thread, ipc);
    if (!success) {
        LOG_ERROR("Could not create main thread.");
    }

    return 0;
}

DWORD WINAPI main_thread(void* lpParam) {
    LOG_INFO("Main thread procedure started.");

    if (lpParam == NULL) {
        LOG_ERROR("The thread received a null pointer as the IPC memory region.");
        return 0;
    }

    ipc_region_t* ipc = (ipc_region_t*)lpParam;

    while (run_state == portal_state::Running) {
        while (!ipc->ctrl_pending_request && run_state == portal_state::Running) {
            // Signal to the CPU that this we're spinning (although this is non-busy spinning)
            _mm_pause();

            // "If you specify 0 milliseconds, the thread will relinquish the remainder of its time slice but remain ready"
            SleepEx(0, false);
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
                ipc->request_id = ipcid_Null;
                ipc->ctrl_pending_request = FALSE;
                break;
            }

            case ipcid_Terminate: {
                LOG_INFO("Received a termination IPC message.");
                run_state = portal_state::Terminated;
                goto portal_end;
            }
        }
    }

    portal_end:

    ipc->ctrl_pending_request = FALSE;
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
                bool success = somno_create_thread(init_thread, NULL);
                if (!success) {
                    LOG_ERROR("Could not create the initialization thread.");
                    return FALSE;
                }

                LOG_INFO("Thread created - exiting from DllMain.");
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

