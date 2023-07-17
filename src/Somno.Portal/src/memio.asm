; memio.asm - Stealthy memory I/O operations

.code

PUBLIC SomnoReadVirtualMemory
ALIGN 16
SomnoReadVirtualMemory PROC
	; Obfuscation stub
	add		r8, 2
	test	rsp, rsp
	jne		$ + 6
	db 0E1h
	db 0E6h
	db 0AAh
	db 080h
	nop

	; System call start
	mov r10, rcx
	mov eax, 3Fh
	sub r8, 2				; Obfuscation
	syscall
	ret
SomnoReadVirtualMemory ENDP

END