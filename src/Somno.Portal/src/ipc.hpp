#pragma once

#include <Windows.h>
#include <stdint.h>
#include "strcrypt.h"
#include "logging.hpp"
#include <inttypes.h>

#define IPC_MEMORY_SIZE   sizeof(ipc_region_t)
#define IPC_MEMORY_NAME   "Global\\AqW5p2FhqX"
#define IPC_HANDSHAKE_SIG 0x488D0411EBFE90C3ul
#define IPC_HANDSHAKE_RET 0xDEADBEEFDEADBEEFul

// Represents the IPC request ID for a single IPC request.
enum ipc_request_id : uint8_t {
    ipcid_Handshake         = 0xAA,
    ipcid_ReadProcessMemory = 0xB1,
    ipcid_Terminate         = 0xBF
};

__pragma(pack(push, 1))

// Represents the layout of the IPC memory region.
typedef struct ipc_region {
    // Set to 1 if the controller has created a request and is waiting for
    // a response. The portal needs to set this value to 0 in order to notify
    // the controller that the request has been fulfilled.
    uint8_t ctrl_pending_request;

    // The type of action to perform.
    ipc_request_id request_id;

    // Payload data. The meaning of this data depends on the value of request_id.
    uint8_t payload[32];
} ipc_region_t;

__pragma(pack(pop))

uint64_t ipc_payload_read64(ipc_region* map, uint32_t* counter) {
    auto value = *(uint64_t*)(map->payload + *counter);
    *counter += sizeof(uint64_t);
    return value;
}

uint64_t ipc_payload_read64(ipc_region* map) {
    auto value = *(uint64_t*)(map->payload);
    return value;
}

uint32_t ipc_payload_read32(ipc_region* map, uint32_t* counter) {
    auto value = *(uint32_t*)(map->payload + *counter);
    *counter += sizeof(uint32_t);
    return value;
}

uint32_t ipc_payload_read32(ipc_region* map) {
    auto value = *(uint32_t*)(map->payload);
    return value;
}

// Opens a connection to the shared IPC memory region.
ipc_region* open_ipc_memory() {
    HANDLE handle = WwCreateFileMappingA(
        INVALID_HANDLE_VALUE,
        NULL,
        PAGE_READWRITE | SEC_COMMIT | SEC_NOCACHE,
        0, IPC_MEMORY_SIZE,
        str_encrypted(IPC_MEMORY_NAME)
    );

    if (handle == NULL || handle == INVALID_HANDLE_VALUE) {
        LOG_ERROR("Could not open a IPC file mapping handle.");
        return nullptr;
    }

    void* map = WwMapViewOfFile(handle, FILE_MAP_ALL_ACCESS, 0, 0, IPC_MEMORY_SIZE);
    CloseHandle(handle);

    if (map == nullptr) {
        LOG_ERROR("Could not map view of the IPC file map.");
        return nullptr;
    }

    auto ipc = (ipc_region*)map;
    if (!ipc->ctrl_pending_request) {
        LOG_ERROR(
            "Expected a pending request from the controller. (P: %" PRIu8 ", ID: %" PRIu8 ", D: %" PRIx64 ")",
            ipc->ctrl_pending_request,
            (uint8_t)ipc->request_id,
            *(uint64_t*)ipc->payload
        );

        return nullptr;
    }

    if (ipc->request_id != ipcid_Handshake) {
        LOG_ERROR(
            "Expected the pending request to be a Handshake. (P: %" PRIu8 ", ID: %" PRIu8 ", D: %" PRIx64 ")",
            ipc->ctrl_pending_request,
            (uint8_t)ipc->request_id,
            *(uint64_t*)ipc->payload
        );

        return nullptr;
    }

    if (ipc_payload_read64(ipc) != IPC_HANDSHAKE_SIG) {
        LOG_ERROR(
            "Invalid handshake signature. (P: %" PRIu8 ", ID: %" PRIu8 ", D: %" PRIx64 ")",
            ipc->ctrl_pending_request,
            (uint8_t)ipc->request_id,
            *(uint64_t*)ipc->payload
        );

        return nullptr;
    }

    // Handshake succeded!
    LOG_INFO("IPC handshake succeeded.");
    ipc->ctrl_pending_request = 0;
    *(uint64_t*)ipc->payload = IPC_HANDSHAKE_RET;
    return ipc;
}

// Closes a previously opened pointer to shared IPC memory.
void close_ipc_memory(ipc_region* map) {
    WwFlushViewOfFile(map, sizeof(ipc_region));
    WwUnmapViewOfFile(map);
    LOG_INFO("The IPC memory region has been closed.");
}
