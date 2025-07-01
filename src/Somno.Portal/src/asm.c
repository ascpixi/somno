#include "inttypes.h"
#include "logging.h"
#include "safety.h"

#define MODRM_MOD(x) (x >> 6)
#define MODRM_REG(x) ((x >> 3) & 0b111)
#define MODRM_RM(x)	 (x & 0b111)

#define MOD_RAX 0
#define MOD_RCX 1
#define MOD_RDX 2
#define MOD_RBX 3
#define MOD_RSP 4
#define MOD_RBP 5
#define MOD_RSI 6
#define MOD_RDI 7
#define MOD_R8  8
#define MOD_R9	9
#define MOD_R10 10
#define MOD_R11 11
#define MOD_R12 12
#define MOD_R13 13
#define MOD_R14 14
#define MOD_R15 15

uint32_t asm_get_instruction_size(uint8_t* inst) {
	NULL_CHECK_RETZERO(inst);

	BOOLEAN rexW = FALSE, rexR = FALSE, rexX = FALSE, rexB = FALSE;
	BOOLEAN legacy = FALSE;
	uint8_t opOffset = 0;

	if (inst[0] == 0x66) {
		// Legacy prefix
		legacy = TRUE;
		inst++;
		opOffset++;
	}

	if ((inst[0] & 0b11110000) == 0b01000000) {
		// REX prefix
		rexW = inst[0] & (1 << 3);
		rexR = inst[0] & (1 << 2);
		rexX = inst[0] & (1 << 1);
		rexB = inst[0] & (1 << 0);
		opOffset++;
		inst++;
	}

	switch (inst[0]) {
		case 0x8D: /* LEA */
		case 0x89: /* MOV */ {
			uint8_t dest = MODRM_RM(inst[1]);
			uint8_t type = MODRM_MOD(inst[1]);

			if (type == 0b00 || type == 0b11) return opOffset + 2; // (REX?) + MOV + /r
			else if (type == 0b01) {
				if (dest == MOD_RSP || dest == MOD_R12)
					return opOffset + 3 + 1; // (REX?) + MOV + SIB + /r + disp8
				else
					return opOffset + 2 + 1; // (REX?) + MOV + /r + disp8
			}
			else {
				if (dest == MOD_RSP || dest == MOD_R12)
					return opOffset + 3 + 4; // (REX?) + MOV + SIB + /r + disp32
				else
					return opOffset + 2 + 4; // (REX?) + MOV + /r + disp32
			}
		}
		case 0x81: /* SUB (?), imm32 */ {
			uint8_t type = MODRM_MOD(inst[1]);
			if (type == 0b00 || type == 0b11) return opOffset + 2 + 4; // (REX?) + SUB + /r + imm32
			else return 0; // TODO: non-direct operand for SUB m, imm32
		}
		case 0x83: /* SUB (?), imm8 */ {
			uint8_t type = MODRM_MOD(inst[1]);
			if (type == 0b00 || type == 0b11) return opOffset + 2 + 1; // (REX?) + SUB + /r + imm32
			else return 0; // TODO: non-direct operand for SUB m, imm8
		}
		case 0x33: /* XOR r64|r32|r16, r/m64|r/m32|r/m16 */ {
			return opOffset + 2; // (REX?) + XOR + MOD.RM
		}
		default: {
			// PUSH
			if (inst[0] >= 0x50 && inst[0] <= 0x50 + MOD_RDI) {
				return opOffset + 1; // for R8 -> R15 REX.B is required
			}
		}
	}

	LOG_ERROR("Could not get instruction size of 0x%p.", inst);
	return 0;
}