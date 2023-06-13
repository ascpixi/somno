#pragma once

#undef WIN32_NO_STATUS
#include <ntstatus.h>
#define WIN32_NO_STATUS

#define WIN32_NO_STATUS
#include <windows.h>
#include <stdint.h>
#include "cmdserver.h"
#include <stdio.h>

bool HandleGetHandlePIDPacket(CommandPacket* pkt, HANDLE pipe, uint8_t buffer[]) {
    NTSTATUS status;
    ULONG read;
    status = ReadFile(pipe, buffer, 8, &read, nullptr);

    pkt->type = CMDTYPE_GETHANDLEPID;
    pkt->target = *(uint64_t*)buffer;
    pkt->value = 0;
    return true;
}

bool HandleWriteMemoryPacket(CommandPacket* pkt, HANDLE pipe, uint8_t buffer[]) {
    pkt->type = CMDTYPE_WRITEMEMORY;

    NTSTATUS status;
    ULONG read;

    // Read handle
    status = ReadFile(pipe, buffer, 8, &read, nullptr);
    pkt->handle = *(uint64_t*)buffer;

    // Read target
    status = ReadFile(pipe, buffer, 8, &read, nullptr);

    pkt->target = *(uint64_t*)buffer;

    // Read value
    status = ReadFile(pipe, buffer, 1, &read, nullptr);

    pkt->value = buffer[0];
    return true;
}

bool HandleReadMemoryPacket(CommandPacket* pkt, HANDLE pipe, uint8_t buffer[]) {
    pkt->type = CMDTYPE_READMEMORY;

    NTSTATUS status;
    ULONG read;

    // Read handle
    status = ReadFile(pipe, buffer, 8, &read, nullptr);
    pkt->handle = *(uint64_t*)buffer;

    // Read target
    status = ReadFile(pipe, buffer, 8, &read, nullptr);

    pkt->target = *(uint64_t*)buffer;
    return true;
}

CommandPacket ReadCommandPacket(HANDLE pipe) {
    CommandPacket pkt = { 0 };
    uint8_t buffer[8]{};
    ULONG read;

    NTSTATUS status;
    while (true) {
        status = ReadFile(
            pipe,
            buffer,
            1,
            NULL,
            nullptr
        );

        // ID read, now try to decode the rest of the packet
        switch (buffer[0]) {
            case CMDTYPE_GETHANDLEPID:
                if (HandleGetHandlePIDPacket(&pkt, pipe, buffer)) {
                    return pkt;
                }
                break;
            case CMDTYPE_WRITEMEMORY:
                if (HandleWriteMemoryPacket(&pkt, pipe, buffer)) {
                    return pkt;
                }
                break;
            case CMDTYPE_READMEMORY:
                if (HandleReadMemoryPacket(&pkt, pipe, buffer)) {
                    return pkt;
                }
                break;
            case CMDTYPE_CLOSE:
                CommandPacket pkt = {};
                pkt.type = CMDTYPE_CLOSE;
                return pkt;
        }

        // if we reach this point, the loop will continue
    }
}