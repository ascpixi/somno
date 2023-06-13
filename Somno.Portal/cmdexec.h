#pragma once
#define WIN32_NO_STATUS

#include <stdint.h>
#include <windows.h>
#include "cmdserver.h"

extern "C" NTSTATUS ZwRVM(HANDLE hProcess, void* lpBaseAddress, void* lpBuffer, SIZE_T nSize, SIZE_T * lpNumberOfBytesRead = NULL);
extern "C" NTSTATUS ZwWVM(HANDLE hProcess, void* lpBaseAddress, void* lpBuffer, SIZE_T nSize, SIZE_T * lpNumberOfBytesRead = NULL);

uint64_t ExecuteCommandPacket(CommandPacket pkt) {
	switch (pkt.type) {
		case CMDTYPE_GETHANDLEPID: {
			return GetProcessId((HANDLE)pkt.handle);
		}
		case CMDTYPE_WRITEMEMORY: {
			uint8_t value = pkt.value;

			return ZwWVM(
				(HANDLE)pkt.handle,
				(LPVOID)pkt.target,
				&value,
				1,
				NULL
			);
		}
		case CMDTYPE_READMEMORY: {
			uint8_t value = 0;

			bool succeeded = ZwRVM(
				(HANDLE)pkt.handle,
				(LPVOID)pkt.target,
				&value,
				1,
				NULL
			);

			if (succeeded) {
				return value;
			}

			return 0xDEADBEEF;
		}
	}

	return 0x0BADC0DE;
}