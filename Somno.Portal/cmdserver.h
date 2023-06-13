#pragma once
#include <stdint.h>

#define CMDTYPE_GETHANDLEPID    0x5B  // Gets the associated PID of a process handle belonging to the attached process. Parameters: handle.
#define CMDTYPE_WRITEMEMORY     0x5C  // Writes a byte to the memory of a process. Parameters: handle, target, value.
#define CMDTYPE_READMEMORY      0x5D  // Reads a byte from the memory of a process. Parameters: handle, target.
#define CMDTYPE_CLOSE           0x5E  // Closes the DLL thread. Parameters: none.

#define CMDTYPE_MIN CMDTYPE_GETHANDLEPID
#define CMDTYPE_MAX CMDTYPE_CLOSE

typedef struct CommandPacket {
    uint8_t type;

    // For GETHANDLEPID: the handle to receive the PID of.
    // For WRITEMEMORY/READMEMORY: the handle of the process to write to.
    uint64_t handle;

    // For GETHANDLEPID: undefined, as it operates on a handle.
    // For WRITEMEMORY/READMEMORY: the memory address to write to.
    uint64_t target;

    // For GETHANDLEPID/READMEMORY: undefined, as it does not accept a value.
    // For WRITEMEMORY: the value to write.
    uint8_t value;
} _CommandPacket;
