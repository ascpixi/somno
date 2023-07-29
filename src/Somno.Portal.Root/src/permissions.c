#include <ntifs.h>
#include <ntddk.h>
#include <inttypes.h>

#include "logging.h"

NTSTATUS create_admin_security_descriptor(PACL* dacl, PSECURITY_DESCRIPTOR out) {
	NTSTATUS status = STATUS_SUCCESS;

	status = RtlCreateSecurityDescriptor(out, SECURITY_DESCRIPTOR_REVISION);
	if (!NT_SUCCESS(status)) {
		LOG_ERROR("Couldn't create a security descriptor, error 0x" PRIx32 ".", status);
		return status;
	}

	uint32_t daclLength =
		sizeof(ACL) + (sizeof(ACCESS_ALLOWED_ACE) * 3) +
		RtlLengthSid(SeExports->SeLocalSystemSid) +
		RtlLengthSid(SeExports->SeAliasAdminsSid) +
		RtlLengthSid(SeExports->SeWorldSid);

	PACL createdDACL = ExAllocatePoolWithTag(PagedPool, daclLength, 'lcaD');
	if (createdDACL == NULL) {
		LOG_ERROR("Couldn't allocate memory for the DACL.");
		return status;
	}

	status = RtlCreateAcl(createdDACL, daclLength, ACL_REVISION);
	if (!NT_SUCCESS(status)) {
		ExFreePool(createdDACL);
		LOG_ERROR("Couldn't create an ACL, error 0x" PRIx32 ".", status);
		return status;
	}

	// Allow access for administrators
	status = RtlAddAccessAllowedAce(createdDACL, ACL_REVISION, FILE_ALL_ACCESS, SeExports->SeAliasAdminsSid);
	if (!NT_SUCCESS(status)) {
		ExFreePool(createdDACL);
		LOG_ERROR("Couldn't allow access for admins in the ACL, error 0x" PRIx32 ".", status);
		return status;
	}

	// Allow access for the LocalSystem account
	status = RtlAddAccessAllowedAce(createdDACL, ACL_REVISION, FILE_ALL_ACCESS, SeExports->SeLocalSystemSid);
	if (!NT_SUCCESS(status)) {
		ExFreePool(createdDACL);
		LOG_ERROR("Couldn't allow access for the local system in the ACL, error 0x" PRIx32 ".", status);
		return status;
	}

	// Set the DACL of the security descriptor
	status = RtlSetDaclSecurityDescriptor(
		out,
		TRUE,	// DACL is present
		createdDACL,
		FALSE	// DACL isn't defaulted
	);

	if (!NT_SUCCESS(status)) {
		ExFreePool(createdDACL);
		LOG_ERROR("Couldn't set the DACL of the security descriptor, error 0x" PRIx32 ".", status);
		return status;
	}

	*dacl = createdDACL;
	return STATUS_SUCCESS;
}