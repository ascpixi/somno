#pragma once

#include <stdint.h>
#include <inttypes.h>
#include <Windows.h>
#include "winwrapper.h"

extern "C" void* ss_syscall_trampoline = 0;

// Calls into the NtDelayExecution, while hiding the true caller of the syscall.
extern "C" NTSTATUS ss_nt_delayexecution(
    bool alertable,
    PLARGE_INTEGER interval
);

// Sleeps for the given amount of milliseconds, while masking the true caller
// of the syscall.
#define ss_sleep(milliseconds, alertable)                   \
    {                                                       \
        LARGE_INTEGER sleepInterval = { 0 };                \
        sleepInterval.QuadPart = -(milliseconds * 10000);   \
        ss_nt_delayexecution(alertable, &sleepInterval);    \
    }

__declspec(noinline) void* ss_scanregion(uint8_t* start, size_t length) {
    if (length <= 0) {
        return nullptr;
    }

    for (size_t i = 0; i < length - 64; i++)
    {
        for (size_t j = 0; j < 64; j++)
        {
            if (start[i + j] != 0x00) {
                goto not_zero;
            }
        }

        return start + i + 16;

    not_zero:
        continue;
    }

    return nullptr;
}

typedef struct ss_region_desc {
    void* regionStart;
    uint8_t* addr;
    size_t regionSize;
} ss_region_desc_t;

__declspec(noinline) ss_region_desc_t ss_search_for_zeroregion(void* omit) {
    HANDLE self = (HANDLE)-1; // current process
    MEMORY_BASIC_INFORMATION mbi;
    SYSTEM_INFO si;
    WwGetSystemInfo(&si);

    void* lpMem = 0;
    while (lpMem < si.lpMaximumApplicationAddress) {
        auto returned = WwVirtualQueryEx(self, lpMem, &mbi, sizeof(mbi));
        if (returned == 0) {
            LOG_ERROR("A call to VirtualQueryEx with parameter %" PRIx64 " failed.", (uint64_t)returned);
            return { nullptr, nullptr, 0 };
        }

        uint64_t start = (uint64_t)mbi.BaseAddress;
        uint64_t end = (uint64_t)mbi.BaseAddress + mbi.RegionSize;
        uint64_t omitNum = (uint64_t)omit;

        if (
            !(start <= omitNum && end >= omitNum) &&
            (mbi.State & MEM_COMMIT) && // is in physical memory
            (mbi.Protect & PAGE_EXECUTE_READ) &&
            (mbi.Type & MEM_IMAGE)
        ) {
            // The page is in physical memory and can be read + executed from.
            // Scan the page for at least 64 bytes of zeros.
            auto result = ss_scanregion((uint8_t*)lpMem, mbi.RegionSize);
            if (result != nullptr) {
                return { mbi.BaseAddress, (uint8_t*)result, mbi.RegionSize };
            }
        }

        lpMem = (void*)((uint64_t)mbi.BaseAddress + mbi.RegionSize);
    }

    return { nullptr, nullptr, 0 };
}

// Initializes the Somno stealth sleep functionality.
bool ss_sleep_init(void* omit) {
    auto region = ss_search_for_zeroregion(omit);
    if (region.addr == nullptr) {
        LOG_ERROR("Could not find a free executable memory region.");
        return false;
    }

    LOG_INFO(
        "Located executable memory region 0x%" PRIx64 " of size %" PRIu64 ", empty @ 0x%" PRIx64,
        (uint64_t)region.regionStart,
        (uint64_t)region.regionSize,
        (uint64_t)region.addr
    );

    DWORD oldProtect;

    // Temporarily change the protection flags of the pages in the region, so
    // that we can write to an otherwise execute-only region
    bool protectSuccess = WwVirtualProtect(
        region.regionStart, region.regionSize,
        PAGE_EXECUTE_READWRITE,
        &oldProtect
    );

    if (!protectSuccess) {
        LOG_ERROR("Could not change the protection flags of the region, error 0x%" PRIx32, GetLastError());
        return false;
    }

    region.addr[0] = 0x0F;	// syscall
    region.addr[1] = 0x05;
    region.addr[2] = 0xFF;	// jmp rbx
    region.addr[3] = 0xE3;

    LOG_INFO("Successfully written to the target memory region.");
    ss_syscall_trampoline = region.addr;

    // Restore the previous protection flags.
    protectSuccess = WwVirtualProtect(
        region.regionStart, region.regionSize,
        oldProtect, &oldProtect
    );

    if (!protectSuccess) {
        LOG_ERROR("Could not restore the protection flags of the region, error 0x%" PRIx32, GetLastError());
        return false;
    }

    return true;
}