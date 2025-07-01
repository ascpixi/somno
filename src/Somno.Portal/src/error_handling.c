#include <ntddk.h>
#include "inttypes.h"
#include "logging.h"

int eh_report_exception(EXCEPTION_POINTERS* exInfo, ULONG_PTR one, ULONG_PTR two, ULONG_PTR three) {
	if (exInfo) {
		EXCEPTION_RECORD* ex = exInfo->ExceptionRecord;
		if (ex) {
			LOG_ERROR("An exception was thrown with the ID of 0x%" PRIx32 ".", ex->ExceptionCode);
			LOG_ERROR("   thrown at: 0x%p", ex->ExceptionAddress);

			for (size_t i = 0; i < ex->NumberParameters; i++) {
				LOG_ERROR("   - info: 0x%" PRIx64, ex->ExceptionInformation[i]);
			}
		}
		else {
			LOG_ERROR("An exception was thrown, but its record pointer is NULL.");
		}

		LOG_ERROR(
			"Offending call: (sig=0x%" PRIx64 ", pid=0x%" PRIx64 ", control=0x%" PRIx64 ")",
			one, two, three
		);
	}
	else {
		LOG_ERROR(
			"An unknown exception was thrown. (sig=0x%" PRIx64 ", pid=0x%" PRIx64 ", control=0x%" PRIx64 ")",
			one, two, three
		);
	}

	return EXCEPTION_EXECUTE_HANDLER;
}