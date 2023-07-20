.data
	EXTERN ss_syscall_trampoline: QWORD

.code

; NTSTATUS ss_nt_delayexecution(bool alertable, PLARGE_INTEGER interval);
PUBLIC ss_nt_delayexecution
ALIGN 16
ss_nt_delayexecution PROC
	mov   r9, [rsp]			; uint64_t temp = (caller)
	xor   r9, 06F002867h	; (naive XOR encryption)

		; (obfuscation)
		jmp		$ + 6
		db 0EBh
		db 0FEh
		db 048h
		db 0B8h

	push  r9				; Push (temp) onto the stack
	push  rbx				; Push original RBX (used by JMP RBX, it's non-volatile)
	xor   r9, r9			; temp = 0

	mov	[rsp + 16], r9		; (caller) = temp (0)

		; (obfuscation)
		jmp		$ + 6
		db 0CDh
		db 054h
		db 048h
		db 0B8h

	sub rsp, 4096

	mov eax, 034h			; 0x34 = NtDelayExecution

	; Execute a system call using code in a foreign PAGE_EXECUTE region.
	; This is done by "SYSCALL" followed by "JMP RBX".
	mov rbx, landing
	jmp qword ptr ss_syscall_trampoline

	; ...
	; syscall
	; jmp rbx ; (landing)
	; ...

	landing:
		add rsp, 4096
		pop rbx						; Restore old RBX (as it is non-volatile)
		pop r9						; Restore old R9, which holds the real caller
		xor r9, 06F002867h			; (naive XOR encryption)
		mov [rsp], qword ptr r9		; Change the current stack value to the caller,
		ret							; and then return using said stack value.
ss_nt_delayexecution ENDP

END