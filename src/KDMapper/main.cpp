#include "kdmapper.hpp"

HANDLE iqvw64e_device_handle;

extern "C" bool kdmapper_main(bool passAllocAddress, uint8_t* drvImage) {
	iqvw64e_device_handle = intel_driver::Load();

	if (iqvw64e_device_handle == INVALID_HANDLE_VALUE)
		return false;

	NTSTATUS exitCode = 0;
	if (!kdmapper::MapDriver(iqvw64e_device_handle, drvImage, 0, 0, false, true, false, passAllocAddress, NULL, &exitCode)) {
		intel_driver::Unload(iqvw64e_device_handle);
		return true;
	}

	if (!intel_driver::Unload(iqvw64e_device_handle)) {
		Log(L"[-] Warning failed to fully unload vulnerable driver " << std::endl);
	}

	Log(L"[+] success" << std::endl);
	return true;
}