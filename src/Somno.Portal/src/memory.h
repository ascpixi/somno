#pragma once

#include <ntddk.h>
#include "inttypes.h"

typedef struct memregion {
	PMDL mdl;
	void* base;
	void* alloc_base;
} memregion_t;

/// <summary>
/// Determines whether a given value is a valid user-mode address.
/// This function does not perform any paging checks.
/// </summary>
/// <param name="ptr">The raw pointer value to check.</param>
/// <returns>TRUE if the address is in the address range of user-mode processes and isn't NULL; FALSE otherwise.</returns>
BOOLEAN mem_usermode_addr_valid(uint64_t ptr);

/// <summary>
/// Allocates a read-write-execute region of memory.
/// </summary>
/// <param name="size">- The amount of bytes to allocate.</param>
/// <param name="tag">- The allocation tag to use.</param>
/// <returns>A pointer to the allocated region, or NULL if the operation had failed.</returns>
memregion_t mem_alloc_rwx(ULONG size, ULONG tag);

/// <summary>
/// Frees a previously allocated memory region.
/// </summary>
/// <param name="region">- The region of memory to free.</param>
/// <param name="tag">- The tag the region was allocated with.</param>
void mem_free_region(memregion_t region, ULONG tag);

/// <summary>
/// Writes to a read-only or execute-only region of memory.
/// </summary>
/// <param name="address">- The target address to write to.</param>
/// <param name="buffer">- The bytes to read the data to write from.</param>
/// <param name="size">- The amount of bytes to overwrite.</param>
/// <returns>TRUE if the operation had succeeded - FALSE otherwise.</returns>
BOOLEAN mem_write_ro(void* address, void* buffer, ULONG size);

/// <summary>
/// Reads from a region of memory of the given process.
/// </summary>
/// <param name="process">- The target process.</param>
/// <param name="address">- The address to read from, in the address space of the target process.</param>
/// <param name="buffer">- The buffer to copy the data to, in the address space of the current process.</param>
/// <param name="size">- The amount of bytes to read.</param>
/// <returns>The result of the operation.</returns>
NTSTATUS mem_read_foreign(PEPROCESS process, void* address, void* buffer, SIZE_T size);

/// <summary>
/// Writes to a region of memory of the given process.
/// </summary>
/// <param name="process">- The target process.</param>
/// <param name="address">- The address to write to, in the address space of the target process.</param>
/// <param name="buffer">- The buffer to copy the data from, in the address space of the current process.</param>
/// <param name="size">- The amount of bytes to write.</param>
/// <returns>The result of the operation.</returns>
NTSTATUS mem_write_foreign(PEPROCESS process, void* address, void* buffer, SIZE_T size);
