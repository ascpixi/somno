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

	; Move 0x3F to EAX
	mov eax, 26h
	add eax, 24
	or  eax, 1

	sub r8, 2				; Obfuscation
	syscall
	ret
SomnoReadVirtualMemory ENDP

END