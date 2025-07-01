using Somno.Native;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Somno.Evasion
{
    internal static class USNJournal
    {
        const uint FSCTLQueryUSNJournal = (((0x00000009) << 16) | ((0) << 14) | ((61) << 2) | (0));
        const uint FSCTLDeleteUSNJournal = (((0x00000009) << 16) | ((0) << 14) | ((62) << 2) | (0));
        const uint USNDeleteFlagDelete = (0x00000001);

        [StructLayout(LayoutKind.Sequential)]
        struct USNJournalData
        {
            public ulong UsnJournalID;      //DWORDLONG
            public ulong FirstUsn;          //USN
            public ulong NextUsn;           //USN
            public ulong LowestValidUsn;    //USN
            public ulong MaxUsn;            //USN
            public ulong MaximumSize;       //DWORDLONG
            public ulong AllocationDelta;   //DWORDLONG
        }

        [StructLayout(LayoutKind.Sequential)]
        struct DeleteUSNJournalData
        {
            public ulong UsnJournalID;
            public uint DeleteFlags;
        }

        public static unsafe void Erase(string volume)
        {
            ArgumentException.ThrowIfNullOrEmpty(volume);

            nint hVolume = Kernel32.CreateFile(
                volume,
                Kernel32.GenericRead | Kernel32.GenericWrite,
                Kernel32.FileShareRead | Kernel32.FileShareWrite,
                default,
                Kernel32.OpenExisting,
                0, default
            );

            if(hVolume is 0 or -1)
                throw new Win32Exception();

            USNJournalData usnData = new();

            uint dwBuffer;
            uint timeSpentWaiting = 0;
            while(!Kernel32.DeviceIoControl(
                hVolume,
                FSCTLQueryUSNJournal,
                null, 0,
                &usnData, (uint)sizeof(USNJournalData),
                out dwBuffer,
                null
            )) {
                // USN journal is busy - wait for it to be available
                if (timeSpentWaiting > 6000) {
                    Kernel32.CloseHandle(hVolume);
                    throw new Win32Exception();
                }

                Thread.Sleep(100);
                timeSpentWaiting += 100;
            }

            var deleteData = new DeleteUSNJournalData();
            deleteData.DeleteFlags = USNDeleteFlagDelete;
            deleteData.UsnJournalID = usnData.UsnJournalID;

            bool didWipeData = (
                Kernel32.DeviceIoControl(
                    hVolume,
                    FSCTLDeleteUSNJournal,
                    &deleteData,
                    (uint)sizeof(DeleteUSNJournalData),
                    null, 0,
                    out dwBuffer, null
                ) == false);

            Kernel32.CloseHandle(hVolume);
        }

        public static void Fill(int entries)
        {
            byte[] buffer = new byte[32];
            var rand = new Random();
            var dir = Directory.CreateTempSubdirectory();
            for (int i = 0; i < entries; i++) {
                rand.NextBytes(buffer);

                File.WriteAllBytes(
                    Path.Combine(dir.FullName, RandomProvider.GenerateString(12)),
                    buffer
               );
            }

            Directory.Delete(dir.FullName, true);
        }
    }
}
