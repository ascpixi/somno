#include "memory.h"
#include <minwindef.h>
#include <ntstatus.h>
#include <ntdef.h>
#include <ntifs.h>
#include <ntddk.h>

// Defined in ntoskrnl.exe.
NTSTATUS NTAPI MmCopyVirtualMemory(
	PEPROCESS SourceProcess,
	PVOID SourceAddress,
	PEPROCESS TargetProcess,
	PVOID TargetAddress,
	SIZE_T BufferSize,
	KPROCESSOR_MODE PreviousMode,
	PSIZE_T ReturnSize
);

NTSTATUS spa_readmem(PEPROCESS process, PVOID sourceAddress, PVOID targetAddress, SIZE_T size)
{
	SIZE_T bytes;
	NTSTATUS status = MmCopyVirtualMemory(
		process,
		sourceAddress,
		PsGetCurrentProcess(),
		targetAddress,
		size,
		KernelMode,
		&bytes
	);

	if (NT_SUCCESS(status))
		return STATUS_SUCCESS;
	else
		return STATUS_ACCESS_DENIED;
}

NTSTATUS spa_writemem(PEPROCESS process, PVOID sourceAddress, PVOID targetAddress, SIZE_T size)
{
	SIZE_T bytes;
	NTSTATUS status = MmCopyVirtualMemory(
		PsGetCurrentProcess(),
		sourceAddress,
		process,
		targetAddress,
		size,
		KernelMode,
		&bytes
	);

	if (NT_SUCCESS(status))
		return STATUS_SUCCESS;
	else
		return STATUS_ACCESS_DENIED;
}