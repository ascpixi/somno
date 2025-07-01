#pragma once

#include <ntdef.h>
#include "memory.h"

/// <summary>
/// Represents a hook in a function.
/// </summary>
typedef struct function_hook {
	void* function;
	void* intermediary;
	uint32_t overwritten_offset;
	uint32_t overwritten_size;
} function_hook_t;

/// <summary>
/// Gets the base address of a system module.
/// </summary>
/// <param name="searchFor">- The full path to the target system module.</param>
/// <returns>The base address of the system module, or NULL if the operation did not succeed.</returns>
void* syspatch_get_module_base(const char* searchFor);

/// <summary>
/// Gets an exported symbol from a system module by its name.
/// </summary>
/// <param name="moduleName">- The full path to the system module.</param>
/// <param name="routineName">- The name of the symbol.</param>
/// <returns>The address of the symbol, or NULL if the operation did not succeed.</returns>
void* syspatch_get_module_export(const char* moduleName, const char* routineName);

/// <summary>
/// Hooks into a function, making it call the given callback before
/// executing any of its instructions. The callback can accept the same
/// parameters as the hooked function, which will intercept the parameters
/// of the function. The parameters can only be value types that can fit
/// in one native register slot.
/// 
/// The callback function MUST return a BOOLEAN - if the function returns
/// FALSE, the original code of the hooked function is not executed. If the
/// function signature specifies a return value, a value of 0 is always returned.
/// </summary>
/// <param name="target">- A pointer to the function to hook into.</param>
/// <param name="callback">- The function to call before each call to the target function.</param>
/// <param name="itmRegion">- A pointer to a memregion_t structure to store allocation information in.</param>
/// <returns>Information about the created hook.</returns>
function_hook_t syspatch_add_prologue(void* target, void* callback, memregion_t* itmRegion);

BOOLEAN syspatch_remove_prologue(void* function, function_hook_t hook);