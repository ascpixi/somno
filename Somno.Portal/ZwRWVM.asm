.code
 
ZwRVM proc
	mov r10, rcx
	mov eax, 3Fh
	syscall
	ret
ZwRVM endp
 
ZwWVM proc
	mov r10, rcx
	mov eax, 3Ah
	syscall
	ret
ZwWVM endp
 
end