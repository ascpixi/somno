#include <ntddk.h>
#include "windows.h"
#include "safety.h"
#include "logging.h"
#include "inttypes.h"
#include "memory.h"

#pragma warning (disable: 4996) // deprecation warnings

memregion_t mem_alloc_rwx(ULONG size, ULONG tag) {
	memregion_t region = { NULL, NULL };

	if (size == 0) {
		LOG_ERROR("Cannot allocate a R/W/X region of 0 bytes.");
		return region;
	}

	PVOID mem = ExAllocatePoolWithTag(NonPagedPool, size, tag);
	if (mem == NULL) {
		LOG_ERROR("Couldn't allocate %" PRIu32 " of R/W/X bytes.", size);
		return region;
	}

	PMDL mdl = IoAllocateMdl(mem, size, FALSE, FALSE, NULL);

	if (mdl == NULL) {
		ExFreePoolWithTag(mem, tag);
		LOG_ERROR("Couldn't allocate R/W/X region - MDL allocation failed.");
		return region;
	}

	MmProbeAndLockPages(mdl, KernelMode, IoReadAccess);
	PVOID mapping = MmMapLockedPagesSpecifyCache(
		mdl,
		KernelMode,
		MmNonCached,
		NULL,
		FALSE,
		NormalPagePriority
	);

	if (mapping == NULL) {
		LOG_ERROR("Couldn't allocate R/W/X region - page mapping failed.");
		ExFreePoolWithTag(mem, tag);
		IoFreeMdl(mdl);
		return region;
	}

	NTSTATUS status = MmProtectMdlSystemAddress(mdl, PAGE_EXECUTE_READWRITE);
	if (!NT_SUCCESS(status)) {
		LOG_ERROR("Couldn't allocate R/W/X region - cannot change protection flags.");
		ExFreePoolWithTag(mem, tag);
		IoFreeMdl(mdl);
		return region;
	}

	region.alloc_base = mem;
	region.base = mapping;
	region.mdl = mdl;
	return region;
}

void mem_free_region(memregion_t region, ULONG tag) {
	NULL_CHECK_RETVOID(region.alloc_base);
	NULL_CHECK_RETVOID(region.base);
	NULL_CHECK_RETVOID(region.mdl);

	ExFreePoolWithTag(region.alloc_base, tag);
	MmUnmapLockedPages(region.base, region.mdl);
	MmUnlockPages(region.mdl);
	IoFreeMdl(region.mdl);
}

BOOLEAN mem_write_ro(void* address, void* buffer, ULONG size) {
	NULL_CHECK_RETZERO(address);
	NULL_CHECK_RETZERO(buffer);

	PMDL mdl = IoAllocateMdl(address, size, FALSE, FALSE, NULL);

	if (mdl == NULL) {
		LOG_ERROR("Could not write to R/O memory - MDL allocation failed.");
		return FALSE;
	}

	MmProbeAndLockPages(mdl, KernelMode, IoReadAccess);
	PVOID mapping = MmMapLockedPagesSpecifyCache(
		mdl,
		KernelMode,
		MmNonCached,
		NULL,
		FALSE,
		NormalPagePriority
	);

	if (mapping == NULL) {
		LOG_ERROR("Could not write to R/O memory - page mapping failed.");
		IoFreeMdl(mdl);
		return FALSE;
	}

	BOOLEAN success = TRUE;

	NTSTATUS status = MmProtectMdlSystemAddress(mdl, PAGE_READWRITE);
	if (!NT_SUCCESS(status)) {
		LOG_ERROR("Could not write to R/O memory - cannot change protection flags.");
		success = FALSE;
	}

	if (success) {
		if (!RtlCopyMemory(mapping, buffer, size)) {
			LOG_ERROR("Could not write to R/O memory - copy failed.");
			success = FALSE;
		}
	}

	MmUnmapLockedPages(mapping, mdl);
	MmUnlockPages(mdl);
	IoFreeMdl(mdl);

	return success;
}

NTSTATUS mem_read_foreign(PEPROCESS process, void* address, void* buffer, SIZE_T size) {
	NULL_CHECK_RET(process, STATUS_INVALID_PARAMETER_1);
	NULL_CHECK_RET(address, STATUS_INVALID_PARAMETER_2);
	NULL_CHECK_RET(buffer, STATUS_INVALID_PARAMETER_3);

	if (size == 0) {
		LOG_INFO("(warn) A read of zero bytes from 0x%p to 0x%p was requested.", address, buffer);
		return STATUS_SUCCESS;
	}
	
	SIZE_T bytes = 0;
	return MmCopyVirtualMemory(
		process,				// SourceProcess
		address,				// SourceAddress
		PsGetCurrentProcess(),	// TargetProcess
		buffer,					// TargetAddress
		size,					// BufferSize
		KernelMode,				// PreviousMode
		&bytes					// ReturnSize
	);
}

NTSTATUS mem_write_foreign(PEPROCESS process, void* address, void* buffer, SIZE_T size) {
	NULL_CHECK_RET(process, STATUS_INVALID_PARAMETER_1);
	NULL_CHECK_RET(address, STATUS_INVALID_PARAMETER_2);
	NULL_CHECK_RET(buffer, STATUS_INVALID_PARAMETER_3);

	if (size == 0) {
		LOG_INFO("(warn) A write of zero bytes to 0x%p from 0x%p was requested.", address, buffer);
		return STATUS_SUCCESS;
	}
	
	SIZE_T bytes = 0;
	return MmCopyVirtualMemory(
		PsGetCurrentProcess(),	// SourceProcess
		buffer,					// SourceAddress
		process,				// TargetProcess
		address,				// TargetAddress
		size,					// BufferSize
		KernelMode,				// PreviousMode
		&bytes					// ReturnSize
	);
}