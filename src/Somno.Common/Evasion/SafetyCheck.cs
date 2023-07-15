using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;

namespace Somno.Evasion
{
    internal static class SafetyCheck
    {
        static readonly byte[] licenseFileHash = new byte[] {
            0x4D, 0xEC, 0x6C, 0x85, 0x82, 0x95, 0x57, 0x4E, 0x71, 0xFD,
            0x4E, 0x4D, 0xED, 0x5B, 0x88, 0x18, 0xBB, 0x94, 0x53, 0x27
        };

        /// <summary>
        /// Forces the application to crash by calling randomized
        /// machine code. This also destroys the call-stack.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe static void ForceCrash()
        {
            byte* machineCode = stackalloc byte[512];

            // trash call-stack
            machineCode[0] = 0x48;  // mov    rsp, rax 
            machineCode[1] = 0x89;
            machineCode[2] = 0xC4;
            machineCode[3] = 0x48;  // mov    rbp, rdx
            machineCode[4] = 0x89;
            machineCode[5] = 0xD8;

            while (true) {
                Random.Shared.NextBytes(new Span<byte>(machineCode + 6, 512 - 6));
                var callableGarbage = (delegate*<void>)machineCode;
                callableGarbage();
            }
        }

        static int VerifyHash(byte[] readContents)
        {
            var data = SHA1.HashData(readContents);

            for (int i = 0; i < licenseFileHash.Length; i++) {
                if (data[i] != licenseFileHash[i]) {
                    return i + 0x334;
                }
            }

            return -1;
        }

        /// <summary>
        /// Verifies that the computer the executable is running on is
        /// authorized to run this software.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void VerifyGenuine()
        {
#if !DEBUG
            if (Debugger.IsAttached) {
                ForceCrash();
            }
#endif

            var r1 = new Random(3697701);
            var r2 = new Random(9950912);
            var r3 = new Random(3927530);
            var r4 = new Random(3455975);
            var r5 = new Random(4646916);
            var r6 = new Random(2100638);

            // Generates a uppercase ASCII letter using the given Random object
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static char N(Random lg) => (char)((lg.Next() % 27) + 65);

            // Generates a special ASCII character using the given Random object
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static char S(Random lg) => (char)((lg.Next() % 53) + 33);

            // "C:/PROGRAM FILES/"
            var p1 = @$"{N(r1)}{S(r3)}{S(r3)}{N(r1)}R{N(r1)}{N(r1)}R{N(r1)}{N(r2)} {N(r2)}{N(r2)}{N(r2)}{N(r2)}{S(r3)}{S(r3)}";

            // "SOMNO/AUT"
            var p2 = $@"{N(r4)}{N(r4)}{N(r4)}{N(r4)}{N(r4)}{S(r5)}{S(r5)}{S(r5)}{S(r5)}";

            // "H.LF"
            var p3 = string.Concat(S(r6), S(r6), S(r6), S(r6));

            if(!File.Exists(p1 + p2 + p3)) {
                ForceCrash();
            }

            var b = File.ReadAllBytes(string.Concat(p1, p2, p3));
            if(VerifyHash(b) > 0x1A) {
                ForceCrash();
            }
        }
    }
}
