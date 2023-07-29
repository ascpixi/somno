#pragma once

#include <ntdef.h>
#include <ntifs.h>
#include <ntddk.h>

/// <summary>
/// Reads a region of memory of the given process.
/// </summary>
/// <param name="process">
///		The process to read the memory of.
/// </param>
/// <param name="sourceAddress">
///		The address to write the contents of the memory region to.
/// </param>
/// <param name="targetAddress">
///		The starting address of the target memory region, in the target
///		process's virtual address space.
/// </param>
/// <param name="size">
///		The amount of bytes to read.
/// </param>
/// <returns>
///		STATUS_SUCCESS if the operation was successful, STATUS_ACCESS_DENIED otherwise.
/// </returns>
NTSTATUS spa_readmem(
	PEPROCESS process,
	PVOID sourceAddress,
	PVOID targetAddress,
	SIZE_T size
);

/// <summary>
/// Writes to a region of memory of the given process.
/// </summary>
/// <param name="process">
///		The process to overwrite the memory region of.
/// </param>
/// <param name="sourceAddress">
///		The address to read the bytes to overwrite the existing memory
///		region data with.
/// </param>
/// <param name="targetAddress">
///		The starting address of the target memory region, in the target
///		process's virtual address space.
/// </param>
/// <param name="size">
///		The amount of bytes to write.
/// </param>
/// <returns>
///		STATUS_SUCCESS if the operation was successful, STATUS_ACCESS_DENIED otherwise.
/// </returns>
NTSTATUS spa_writemem(
	PEPROCESS process,
	PVOID sourceAddress,
	PVOID targetAddress,
	SIZE_T size
);