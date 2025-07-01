#pragma warning (disable: 28101) // unannotated DriverEntry function

#include <ntifs.h>
#include "ipc.h"
#include "windows.h"
#include "memory.h"
#include "logging.h"
#include "inttypes.h"
#include "syspatch.h"
#include "error_handling.h"

// https://github.com/hfiref0x/NtCall64/blob/master/Source/NtCall64/tables.h
#define HOOKED_FUNCTION_NAME	"NtDxgkDisplayPortOperation"
#define HOOKED_FUNCTION_DRIVER	"\\SystemRoot\\System32\\drivers\\dxgkrnl.sys"

static memregion_t main_hook_itm_region;
static function_hook_t main_hook;

// one: signature, 0xACE77777DEADDEAD for a control call
// two: caller PID
// three: control struct address, in calling process address space
BOOLEAN mainCallback(ULONG_PTR one, ULONG_PTR two, ULONG_PTR three) {
	try {
		if (one == IPCREQ_SIGNATURE) {
			if (two == 0) {
				LOG_ERROR("The caller PID is NULL.");
				return FALSE;
			}

			if (!mem_usermode_addr_valid(three)) {
				LOG_ERROR("The caller control struct address 0x%" PRIx64 " is incorrect.", three);
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
			status = mem_read_foreign(caller, (void*)three, buffer, sizeof(buffer));
			if (!NT_SUCCESS(status)) {
				LOG_ERROR(
					"Couldn't read the control struct from 0x%" PRIx64 ", caller PID 0x%" PRIx64,
					three, two
				);

				ObDereferenceObject(caller);
				return FALSE;
			}

			switch (buffer[0]) {
				case IPCREQ_TYPE_READMEMORY: {
					uint32_t pos = 1;

					uint64_t targetPID = *(uint64_t*)(buffer + pos);
					pos += sizeof(uint64_t);
					if (targetPID == 0) {
						LOG_ERROR("IPCREQ_READMEMORY: The target PID was NULL.");
						ObDereferenceObject(caller);
						return FALSE;
					}

					uint64_t targetAddr = *(uint64_t*)(buffer + pos);
					pos += sizeof(uint64_t);
					if (targetAddr == 0) {
						LOG_ERROR("IPCREQ_READMEMORY: The target address was NULL.");
						ObDereferenceObject(caller);
						return FALSE;
					}

					uint64_t bufferAddr = *(uint64_t*)(buffer + pos);
					pos += sizeof(uint64_t);
					if (bufferAddr == 0) {
						LOG_ERROR("IPCREQ_READMEMORY: The buffer address was NULL.");
						ObDereferenceObject(caller);
						return FALSE;
					}

					uint8_t size = *(uint8_t*)(buffer + pos);
					if (size == 0) {
						LOG_INFO("(warn) IPCREQ_READMEMORY: A read of 0 bytes was performed; ignoring.");
						ObDereferenceObject(caller);
						return FALSE;
					}

					PEPROCESS targetProc;
					status = PsLookupProcessByProcessId((HANDLE)targetPID, &targetProc);
					if (!NT_SUCCESS(status)) {
						LOG_ERROR("IPCREQ_READMEMORY: Process lookup for PID 0x%" PRIx64 " has failed.", targetPID);
						ObDereferenceObject(caller);
						return FALSE;
					}

					uint8_t intermediary[256];
					status = mem_read_foreign(targetProc, (void*)targetAddr, intermediary, size);
					ObDereferenceObject(targetProc);

					if (!NT_SUCCESS(status)) {
						LOG_ERROR(
							"IPCREQ_READMEMORY: Couldn't read from 0x%" PRIx64 ", target PID 0x% " PRIx64,
							targetAddr, targetPID
						);
						
						ObDereferenceObject(caller);
						return FALSE;
					}
					
					status = mem_write_foreign(caller, (void*)bufferAddr, intermediary, size);
					ObDereferenceObject(caller);
					if (!NT_SUCCESS(status)) {
						LOG_ERROR(
							"IPCREQ_READMEMORY: Couldn't write to 0x%" PRIx64 ", caller PID 0x%" PRIx64,
							bufferAddr, targetPID
						);

						return FALSE;
					}

					break;
				}
				case IPCREQ_TYPE_QUERYSTATUS: {
					uint8_t response = TRUE;
					uint64_t outputAddr = *(uint64_t*)(buffer + 1);
					
					status = mem_write_foreign(caller, (void*)outputAddr, &response, 1);
					ObDereferenceObject(caller);
					if (!NT_SUCCESS(status)) {
						LOG_ERROR(
							"IPCREQ_QUERYSTATUS: Couldn't write to 0x%" PRIx64 ", caller PID 0x % " PRIx64,
							outputAddr, two
						);

						return FALSE;
					}

					break;
				}
				case IPCREQ_TYPE_CHECK_PID: {
					uint64_t targetPID = *(uint64_t*)(buffer + 1);
					if (targetPID == 0) {
						LOG_ERROR("IPCREQ_CHECK_PID: The target PID was NULL.");
						ObDereferenceObject(caller);
						return FALSE;
					}

					uint64_t outputAddr = *(uint64_t*)(buffer + 1 + sizeof(uint64_t));
					if (outputAddr == 0) {
						LOG_ERROR("IPCREQ_CHECK_PID: The output address was NULL.");
						ObDereferenceObject(caller);
						return FALSE;
					}

					uint8_t response = FALSE;

					PEPROCESS process;
					status = PsLookupProcessByProcessId((HANDLE)targetPID, &process);
					if (NT_SUCCESS(status)) {
						LARGE_INTEGER zeroTime = { 0 };
						if (KeWaitForSingleObject(process, Executive, KernelMode, FALSE, &zeroTime) != STATUS_WAIT_0) {
							response = TRUE;
						}

						ObDereferenceObject(process);
					}

					status = mem_write_foreign(caller, (void*)outputAddr, &response, 1);
					ObDereferenceObject(caller);
					if (!NT_SUCCESS(status)) {
						LOG_ERROR(
							"IPCREQ_CHECK_PID: Couldn't write to 0x%" PRIx64 ", caller PID 0x % " PRIx64,
							outputAddr, two
						);

						return FALSE;
					}

					break;
				}
				default: {
					LOG_ERROR("Unknown IPC request type 0x%" PRIx8 " - ignoring.", buffer[0]);
					ObDereferenceObject(caller);
					break;
				}
			}

			return FALSE;
		}

		// If we didn't receive the signature magic number, continue with
		// the call as usual.
		return TRUE;
	} except(eh_report_exception(GetExceptionInformation(), one, two, three)) {
		LOG_ERROR("(end of exception)");
		return FALSE;
	}
}

NTSTATUS SpDriverEntry(_In_ void* allocAddress, _In_ PUNICODE_STRING __p2) {
	UNREFERENCED_PARAMETER(__p2);

	LOG_INFO("The driver has been loaded!");

	if (allocAddress == NULL) {
		LOG_INFO("(warn) The allocation address parameter is NULL.");
	}

	void* vectorFunc = syspatch_get_module_export(HOOKED_FUNCTION_DRIVER, HOOKED_FUNCTION_NAME);
	if (vectorFunc == NULL) {
		LOG_ERROR("Failed to obtain the vector function.");
		return 0;
	}

	LOG_INFO("Vector function obtained @ 0x%" PRIx64 ".", (uint64_t)vectorFunc);

	main_hook = syspatch_add_prologue(
		vectorFunc,
		(void*)mainCallback,
		&main_hook_itm_region
	);

	if (main_hook.function == NULL) {
		LOG_ERROR("Couldn't hook to the function.");
		return 0;
	}

	LOG_INFO("Hooked successfully!");

	return STATUS_SUCCESS;
}