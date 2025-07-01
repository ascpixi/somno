#pragma once

#include <ntddk.h>

int eh_report_exception(
	EXCEPTION_POINTERS* exInfo,
	ULONG_PTR one,
	ULONG_PTR two,
	ULONG_PTR three
);