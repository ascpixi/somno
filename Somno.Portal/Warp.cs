using Somno.Portal.Native;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Somno.Portal
{
    internal static class Warp
    {
        public static bool Initialize()
        {
            var mutex = Kernel32.CreateMutex(0, true, @"Global\SMNWARPMutex");
            if (Marshal.GetLastWin32Error() == Win32Status.ERROR_ALREADY_EXISTS) {
                // Security: An instance of either the installer or the client is already running, terminate now
                Environment.Exit(1);
            }

            // TODO: Make sure no anti-cheats are running here.

            if(!SetPrivilege("SeDebugPrivilege")) {
                return false;
            }

            // Get the PID of LSASS (TODO: maybe we can get a less intrusive process?)
            var lsassPIDs = ProcessUtility.GetPIDs("lsass.exe");
            if(lsassPIDs.Count == 0) {
                return false;
            }

            lsassPIDs.Sort();
            var pivotPID = lsassPIDs[0];
            if(pivotPID == 0) {
                return false;
            }


        }

        static bool SetPrivilege(string lpszPrivilege, bool bEnablePrivilege = true)
        {
            IntPtr hToken;
            if (!AdvAPI32.OpenProcessToken(Kernel32.GetCurrentProcess(), Kernel32.TokenAdjustPrivilidges, out hToken)) {
                if (hToken != default) {
                    Kernel32.CloseHandle(hToken);
                }

                return false;
            }

            LUID luid = default;
            if (!AdvAPI32.LookupPrivilegeValue(null, lpszPrivilege, ref luid)) {
                if (hToken != default) {
                    Kernel32.CloseHandle(hToken);
                }

                return false;
            }

            TokenPrivilidges priv = default;

            priv.PrivilegeCount = 1;
            priv.Privileges[0].Luid = luid;
            priv.Privileges[0].Attributes = bEnablePrivilege ? AdvAPI32.SE_PRIVILEGE_ENABLED : AdvAPI32.SE_PRIVILEGE_REMOVED;
            
            if (!AdvAPI32.AdjustTokenPrivileges(hToken, false, ref priv, 0, default, default)) {
                if (hToken != default) {
                    Kernel32.CloseHandle(hToken);
                }

                return false;
            }

            if (hToken != default) {
                Kernel32.CloseHandle(hToken);

            }

            return true;
        }
    }
}
