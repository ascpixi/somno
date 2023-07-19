#pragma once

#include <Windows.h>
#include "logging.hpp"
#include "winwrapper.h"

bool somno_create_thread(LPTHREAD_START_ROUTINE target, PVOID parameter) {
	NTSTATUS status;

	DWORD oldProtect;
	WwVirtualProtect((LPVOID)0x40000, 0x1000, PAGE_EXECUTE_READWRITE, &oldProtect);

	HANDLE hThread = INVALID_HANDLE_VALUE;
	status = WwNtCreateThreadEx(
		&hThread,
		THREAD_ALL_ACCESS,
		NULL,
		(HANDLE)-1, // current process
		(LPTHREAD_START_ROUTINE)0x40000, // start address
		parameter,
		0x1, // suspended
		0, 0, 0, NULL
	);

	if (status != 0) {
		LOG_ERROR("Couldn't create thread; error %d.", (LONG)status);
		return false;
	}

	BOOL ctxSuccess;

	CONTEXT ctx{};
	ctx.ContextFlags = CONTEXT_ALL;
	ctxSuccess = WwGetThreadContext(hThread, &ctx);

	if (!ctxSuccess) {
		LOG_ERROR("Could not get the context of the newly created thread; error %d.", GetLastError());
		return false;
	}

	ctx.Rcx = (DWORD64)target;

	ctxSuccess = WwSetThreadContext(hThread, &ctx);
	if (!ctxSuccess) {
		LOG_ERROR("Could not set the context of the newly created thread; error %d.", GetLastError());
		return false;
	}

	auto resumed = WwResumeThread(hThread);
	if (resumed == -1) {
		LOG_ERROR("Could not resume the newly created thread; error %d.", GetLastError());
		return false;
	}

	return true;
}