#pragma once

#include <ntddk.h>

/// <summary>
/// Creates a SECURITY_DESCRIPTOR that allows access for any administrator
/// account to a given object.
/// </summary>
/// <param name="dacl">
///		A pointer to a DACL pointer. This pointer needs to manually be
///		freed after all objects with the SECURITY_DESCRIPTOR are disposed.
/// </param>
/// <param name="out">
///		A pointer to the security descriptor to initialize.
/// </param>
/// <returns>A value, determining whether the operation had succeeded or not.</returns>
NTSTATUS create_admin_security_descriptor(
	PACL* dacl,
	PSECURITY_DESCRIPTOR out
);