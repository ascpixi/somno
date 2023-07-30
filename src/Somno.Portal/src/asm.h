#pragma once

#include "inttypes.h"

/// <summary>
/// Gets the size of the instruction at the given address. This function
/// only supports a small sub-set of the x86-64 ISA, and is designed to
/// analyze procedure prologues.
/// </summary>
/// <param name="inst">- The start address of the instruction.</param>
/// <returns>The size of the instruction, or 0 if the size could not be reliably determined.</returns>
uint32_t asm_get_instruction_size(uint8_t* inst);
