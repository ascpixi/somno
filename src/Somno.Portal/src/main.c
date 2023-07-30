#pragma warning (disable: 28101) // unannotated DriverEntry function

#include <ntifs.h>
#include "ipc.h"
#include "windows.h"
#include "memory.h"
#include "logging.h"
#include "inttypes.h"
#include "syspatch.h"

// https://github.com/hfiref0x/NtCall64/blob/master/Source/NtCall64/tables.h
#define HOOKED_FUNCTION_NAME	"NtDxgkDisplayPortOperation"
#define HOOKED_FUNCTION_DRIVER	"\\SystemRoot\\System32\\drivers\\dxgkrnl.sys"

static memregion_t hook_itm_region;
static function_hook_t main_hook;
static void* driver_alloc_addr;

// one: signature, 0xACE77777DEADDEAD for a control call
// two: caller PID
// three: control struct address, in calling process address space
BOOLEAN mainCallback(ULONG_PTR one, ULONG_PTR two, ULONG_PTR three) {
	if (one == IPCREQ_SIGNATURE) {
		if (two == 0) {
			LOG_ERROR("The caller PID was NULL.");
			return FALSE;
		}

		if (three == 0) {
			LOG_ERROR("The caller control struct address was NULL.");
			return FALSE;
		}

		NTSTATUS status;

		PEPROCESS caller;
		status = PsLookupProcessByProcessId((HANDLE)two, &caller);
		if (!NT_SUCCESS(status)) {
			LOG_ERROR("Process lookup for PID 0x%" PRIx64 " has failed.", two);
			return FALSE;
		}

		uint8_t buffer[32];
		mem_read_foreign(caller, (void*)three, buffer, sizeof(buffer));

		switch (buffer[0]) {
			case IPCREQ_TYPE_READMEMORY: {
				uint32_t pos = 1;

				uint64_t targetPID = *(uint64_t*)(buffer + pos);
				pos += sizeof(uint64_t);

				uint64_t targetAddr = *(uint64_t*)(buffer + pos);
				pos += sizeof(uint64_t);

				uint64_t bufferAddr = *(uint64_t*)(buffer + pos);
				pos += sizeof(uint64_t);

				uint8_t size = *(uint8_t*)(buffer + pos);

				PEPROCESS targetProc;
				status = PsLookupProcessByProcessId((HANDLE)targetPID, &targetProc);
				if (!NT_SUCCESS(status)) {
					LOG_ERROR("Process lookup for PID 0x%" PRIx64 " has failed.", targetPID);
					return FALSE;
				}

				uint8_t intermediary[256];
				mem_read_foreign(targetProc, (void*)targetAddr, intermediary, size);
				mem_write_foreign(caller, (void*)bufferAddr, intermediary, size);
				break;
			}
			case IPCREQ_TYPE_QUERYSTATUS: {
				uint8_t response = TRUE;
				uint64_t outputAddr = *(uint64_t*)(buffer + 1);
				mem_write_foreign(caller, (void*)outputAddr, &response, 1);
				break;
			}
			default: {
				LOG_ERROR("Unknown IPC request type 0x%" PRIx8, buffer[0]);
				break;
			}
		}

		return FALSE;
	}

	// If we didn't receive the signature magic number, continue with
	// the call as usual.
	return TRUE;
}

NTSTATUS SpDriverEntry(_In_ void* allocAddress, _In_ PUNICODE_STRING __p2) {
	UNREFERENCED_PARAMETER(__p2);

	LOG_INFO("The driver has been loaded!");

	if (allocAddress == NULL) {
		LOG_INFO("(warn) The allocation address parameter is NULL.");
	}

	driver_alloc_addr = allocAddress;
	
	void* vectorFunc = syspatch_get_module_export(HOOKED_FUNCTION_DRIVER, HOOKED_FUNCTION_NAME);
	if (vectorFunc == NULL) {
		LOG_ERROR("Failed to obtain the vector function.");
		return 0;
	}

	LOG_INFO("Vector function obtained @ 0x%" PRIx64 ".", (uint64_t)vectorFunc);

	main_hook = syspatch_add_prologue(
		vectorFunc,
		(void*)mainCallback,
		&hook_itm_region
	);

	if (main_hook.function == NULL) {
		LOG_ERROR("Couldn't hook to the function.");
		return 0;
	}

	LOG_INFO("Hooked successfully!");

	return STATUS_SUCCESS;
}