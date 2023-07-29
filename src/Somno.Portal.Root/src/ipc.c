#include <ntstatus.h>
#include <ntdef.h>
#include <ntifs.h>
#include <ntddk.h>
#include <inttypes.h>

#include "logging.h"
#include "permissions.h"

static PACL mapDACL;
static SECURITY_DESCRIPTOR mapSecurityDescriptor;

NTSTATUS ipc_open_memory() {
	NTSTATUS status = STATUS_SUCCESS;

	SECURITY_DESCRIPTOR securityDesc = { 0 };
	status = create_admin_security_descriptor(&mapDACL, &securityDesc);
	if (!NT_SUCCESS(status)) {
		LOG_ERROR("Could not create the security descriptor for the shared IPC memory.");
		return status;
	}

	OBJECT_ATTRIBUTES objAttr;
	UNICODE_STRING sectionName;
	RtlInitUnicodeString(&sectionName, )
}