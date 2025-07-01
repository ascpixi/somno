#include <ntddk.h>
#include "asm.h"
#include "safety.h"
#include "memory.h"
#include "logging.h"
#include "windows.h"
#include "inttypes.h"
#include "syspatch.h"

#pragma warning (disable: 4996) // deprecation warnings

// Represents an x86-64 64-bit immediate value.
// Valid in array initializations.
#define ASM_ARRDEF_IMM64 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00

void* syspatch_get_module_base(const char* searchFor) {
	NULL_CHECK_RETZERO(searchFor);

	NTSTATUS status;
	ULONG asize = 0;

	// Get the amount of bytes we need to allocate.
	status = ZwQuerySystemInformation(SystemModuleInformation, NULL, 0, &asize);
	if (asize == 0) {
		LOG_ERROR("The allocation size requested by ZwQuerySystemInformation was 0. (status: %" PRIx32 ")", status);
		return NULL;
	}

	// Allocate said amount of bytes.
	PRTL_PROCESS_MODULES modules = (PRTL_PROCESS_MODULES)ExAllocatePoolWithTag(
		NonPagedPool, asize, ALLOC_TAG('spMd')
	);

	if (modules == NULL) {
		LOG_ERROR("Could not allocate the bytes needed to query system modules.");
		return NULL;
	}

	status = ZwQuerySystemInformation(SystemModuleInformation, modules, asize, &asize);
	if (!NT_SUCCESS(status)) {
		LOG_ERROR("Could not query system module information. (status: %" PRIx32 ", asize: %" PRIu32 ")", status, asize);
		return NULL;
	}

	PVOID moduleBase = NULL;
	for (uint32_t i = 0; i < modules->NumberOfModules; i++)
	{
		RTL_PROCESS_MODULE_INFORMATION* module = &modules->Modules[i];
		if (strcmp((char*)module->FullPathName, searchFor) == 0) {
			moduleBase = module->ImageBase;
			break;
		}
	}

	ExFreePoolWithTag(modules, ALLOC_TAG('spMd'));

	if (moduleBase <= NULL) {
		LOG_ERROR("Could not get the module base - 0x%" PRIx64 " <= NULL", (uint64_t)moduleBase);
		return NULL;
	}

	return moduleBase;
}

void* syspatch_get_module_export(const char* moduleName, const char* routineName) {
	NULL_CHECK_RETZERO(moduleName);
	NULL_CHECK_RETZERO(routineName);
	
	void* lpModule = syspatch_get_module_base(moduleName);
	if (lpModule == NULL) {
		LOG_ERROR("Could not get export %s from module %s.", moduleName, routineName);
		return NULL;
	}

	LOG_INFO("Module address '%s' is 0x%" PRIx64 ".", moduleName, (uint64_t)lpModule);
	return RtlFindExportedRoutineByName(lpModule, routineName);
}

function_hook_t syspatch_add_prologue(void* target, void* callback, memregion_t* itmRegion) {
	function_hook_t info = { NULL, NULL, 0, 0 };
	
	NULL_CHECK_RET(target, info);
	NULL_CHECK_RET(callback, info);

	// Initialize the target-to-intermediary entry shellcode
	uint8_t hookShellcodeBuffer[64] = {
		// mov rax, <<intermediary>>
		0x48, 0xB8,     0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
		// jmp rax
		0xFF, 0xE0,
	};

	uint32_t hookScSize = 12;

	// Find a suitable offset that is on a instruction boundary.
	// We don't want to jump to a instruction that has been half-overwritten.
	uint32_t boundary = 0;
	do {
		uint32_t delta = asm_get_instruction_size((uint8_t*)target + boundary);
		if (delta == 0) {
			LOG_INFO("Cannot determine the instruction boundary. (current: %u)", boundary);
			return info;
		}

		boundary += delta;
	} while (boundary < hookScSize);

	if (boundary > sizeof(hookShellcodeBuffer)) {
		// The instruction boundary lies at such a high offset, that the buffer
		// cannot contain the padding needed to safely insert the hook.
		LOG_ERROR("The instruction boundary offset is too large. (%u)", boundary);
		return info;
	}

	for (size_t i = hookScSize; i < boundary; i++) {
		hookShellcodeBuffer[i] = 0x90; // pad with NOPs
	}

	// Update the shellcode size, with the added padding
	hookScSize = boundary;

	// Initialize the intermediary shellcode. The intermediary shellcode
	// will call the callback, execute the instructions that we overwrote,
	// and jump back to the hooked function.
	uint8_t itmShellcodeInit[] = {
		// ; Push all argument registers.
		0x51,					// push rcx
		0x52,					// push rdx
		0x41, 0x50,				// push r8
		0x41, 0x51,				// push r9
		// ; Reserve space on the stack.
		0x48, 0x83, 0xEC, 0x20	// sub rsp, 0x20
	};

	// (...hook memory region starts)
	uint8_t itmShellcodePre[] = {
		0x48, 0xB8, ASM_ARRDEF_IMM64,	// mov rax, <callback>
		0xFF, 0xD0,						// call rax
		// (RAX will now have the return value of <callback>)
		0x48, 0x83, 0xC4, 0x20,			// add rsp, 0x20
		0x41, 0x59,						// pop r9
		0x41, 0x58,						// pop r8
		0x5A,							// pop rdx
		0x59,							// pop rcx
		0x48, 0x85, 0xC0,				// test rax, rax
		0x75, 0x01,						// jne [rip + 1]
		0xC3,							// ret ; (return if callback returned FALSE)
	};

	// (...overwritten code)
	uint8_t overwrittenCodeBuffer[512];

	uint8_t itmShellcodePost[] = {
		0x48, 0xB8, ASM_ARRDEF_IMM64,	// mov rax, <<target + (overwritten code size)>>
		0xFF, 0xE0						// jmp rax
	};

	// Copy the original instructions from the target that we're about to overwrite.
	memcpy(overwrittenCodeBuffer, target, hookScSize);

	// ...and assign the callback call and original target jmp targets.
	*(void**)(itmShellcodePre + 2) = callback;
	*(void**)(itmShellcodePost + 2) = (uint8_t*)target + hookScSize;

	// Allocate the intermediary shellcode into the free store.
	ULONG intermediarySize = sizeof(itmShellcodeInit) + sizeof(itmShellcodePre) + hookScSize + sizeof(itmShellcodePost);

	*itmRegion = mem_alloc_rwx(intermediarySize, ALLOC_TAG('Hook'));
	uint8_t* intermediary = itmRegion->base;

	if (intermediary == NULL) {
		LOG_INFO("Could not allocate memory for the intermediary buffer.");
		return info;
	}

	uint32_t pos = 0;
	memcpy(intermediary + pos, itmShellcodeInit, sizeof(itmShellcodeInit));
	pos += sizeof(itmShellcodeInit);

	memcpy(intermediary + pos, itmShellcodePre, sizeof(itmShellcodePre));
	pos += sizeof(itmShellcodePre);

	memcpy(intermediary + pos, overwrittenCodeBuffer, hookScSize);
	pos += hookScSize;

	memcpy(intermediary + pos, itmShellcodePost, sizeof(itmShellcodePost));

	// Make the hook call our intermediary
	*(void**)(hookShellcodeBuffer + 2) = intermediary;

	// Overwrite the first [size] instructions with our hook.
	BOOLEAN success = mem_write_ro(target, hookShellcodeBuffer, hookScSize);
	if (!success) {
		LOG_ERROR("Could not write 0x%p -> 0x%p of size %u.", hookShellcodeBuffer, target, hookScSize);
		return info;
	}

	info.function = target;
	info.intermediary = intermediary;
	info.overwritten_offset = sizeof(itmShellcodeInit) + sizeof(itmShellcodePre);
	info.overwritten_size = hookScSize;
	return info;
}

BOOLEAN syspatch_remove_prologue(void* function, function_hook_t hook) {
	NULL_CHECK_RETZERO(function);
	NULL_CHECK_RETZERO(hook.intermediary);

	uint8_t* original = ((uint8_t*)hook.intermediary) + hook.overwritten_offset;
	mem_write_ro(function, original, hook.overwritten_size);

	LOG_INFO("Hook in function 0x%p has been removed.", function);
	return TRUE;
}