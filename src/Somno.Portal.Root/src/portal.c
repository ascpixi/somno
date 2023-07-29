#include <ntddk.h>
#include <inttypes.h>

#include "memory.h"
#include "logging.h"

static HANDLE mainThreadHandle;

// The main portal agent thread. Expected to be started from PsCreateSystemThread.
void main_thread(_In_ PVOID startContext) {
	UNREFERENCED_PARAMETER(startContext); // This is similar to lpParameter of the user-mode CreateThread.
}

// Somno Portal Agent driver entry-point
NTSTATUS SPADriverEntry(_In_ PDRIVER_OBJECT __p1, _In_ PUNICODE_STRING __p2) {
	// Under kdmapper, the parameters will always be NULL.
	// https://github.com/TheCruZ/kdmapper/blob/master/kdmapper/main.cpp#L105
	UNREFERENCED_PARAMETER(__p1);
	UNREFERENCED_PARAMETER(__p2);

	NTSTATUS status;
	status = PsCreateSystemThread(
		&mainThreadHandle,
		THREAD_ALL_ACCESS,	
		NULL,
		NULL,				// This value should be NULL for a driver-created thread
		NULL,				// (see above comment)
		main_thread,
		NULL
	);

	if (!NT_SUCCESS(status)) {
		LOG_ERROR("Main thread creation failed with status 0x%" PRIx32 ".", status);
	}

	return 0;
}