using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

// NOTE: The implementation of X8664Emitter has been taken from my C#
//       operating system project Azerou. Changes in the main Azerou
//       implementation can be merged with this one (note that it
//       includes features such as interrupt handling and operates as a
//       ref struct and on native pointers, which is not needed for this
//       implementation).

namespace Somno.Portal.Shellcode
{
    /// <summary>
    /// Creates x86-64 procedures by emitting raw instructions.
    /// </summary>
    internal unsafe class X8664Emitter : IDisposable
    {
        readonly byte[] buffer;
        readonly BinaryWriter bw;
        readonly MemoryStream ms;

        /// <summary>
        /// Creates a new instance of the <see cref="X8664Emitter"/> structure,
        /// operating on the given buffer.
        /// </summary>
        public X8664Emitter(int bufferSize)
        {
            buffer = new byte[bufferSize];
            ms = new MemoryStream(buffer);
            bw = new(ms);
        }

        public enum XMMRegister : byte
        {
            XMM0,
            XMM1,
            XMM2,
            XMM3,
            XMM4,
            XMM5,
            XMM6,
            XMM7
        }

        const byte REXPrefix = 0b0100 << 4;
        const byte REX_W = REXPrefix | (1 << 3); // (0x48) When 1, a 64-bit operand size is used.
        const byte REX_R = REXPrefix | (1 << 2); // This 1-bit value is an extension to the MODRM.reg field.
        const byte REX_X = REXPrefix | (1 << 1); // This 1-bit value is an extension to the SIB.index field.
        const byte REX_B = REXPrefix | (1 << 0); // This 1-bit value is an extension to the MODRM.rm field or the SIB.base field.

        const byte OperandSizeOverride = 0x66;
        const byte OpCodeSeq1 = 0x0F;

        static byte ModRM(byte mod, byte reg, byte rm)
            => (byte)((mod << 6) | (reg << 3) | rm);

        static byte SIB(byte scale, byte index, byte baseRegister)
            => (byte)((scale << 6) | (index << 3) | baseRegister);

        public byte[] GetAssembled()
        {
            bw.Flush();
            ms.Flush();

            byte[] shellcode = new byte[ms.Position];
            Array.Copy(buffer, shellcode, ms.Position);
            return shellcode;
        }

        public int CallUsingRAX()
        {
            int begin = (int)ms.Position;
            bw.Write((byte)0xFF);     // CALL r/m64
            bw.Write(ModRM(0b11, 2, (byte)X86Register.RAX));
            return begin;
        }

        /// <summary>
        /// Pushes the given register.
        /// </summary>
        public int Push(X86Register target)
        {
            int begin = (int)ms.Position;
            bw.Write((byte)(target + 0x50));
            return begin;
        }

        /// <summary>
        /// Pushes the given register with a REX prefix.
        /// </summary>
        public int Push(RegisterREX target)
        {
            int begin = (int)ms.Position;
            bw.Write(REX_B);
            bw.Write((byte)(target + 0x50));
            return begin;
        }

        /// <summary>
        /// Pops the given register. 
        /// </summary>
        public int Pop(X86Register target)
        {
            int begin = (int)ms.Position;
            bw.Write((byte)(target + 0x58));
            return begin;
        }

        /// <summary>
        /// Pops the given register with a REX prefix.
        /// </summary>
        public int Pop(RegisterREX target)
        {
            int begin = (int)ms.Position;
            bw.Write(0x41); // REX prefix
            bw.Write((byte)(target + 0x58));
            return begin;
        }

        /// <summary>
        /// Moves data from register <paramref name="src"/> to <paramref name="dst"/>.
        /// </summary>
        public int Move(X86Register dst, X86Register src)
        {
            int begin = (int)ms.Position;
            bw.Write(REX_W); // REX.W
            bw.Write((byte)0x89);  // mov
            bw.Write(ModRM(0b11, (byte)src, (byte)dst));
            return begin;
        }

        public int Move(X86Register dst, RegisterREX src)
        {
            int begin = (int)ms.Position;
            bw.Write(REX_W | (1 << 2)); // REX.W, REX.R
            bw.Write((byte)0x89);       // mov
            bw.Write(ModRM(0b11, (byte)src, (byte)dst));
            return begin;
        }

        public int MoveToAddress(RegisterREX dstAddr, RegisterREX src)
        {
            int begin = (int)ms.Position;
            bw.Write((byte)(REX_W | (1 << 2) | (1 << 0))); // REX.W, REX.R, REX.B
            bw.Write((byte)0x89);  // mov
            bw.Write(ModRM(0b00, (byte)src, (byte)dstAddr));
            return begin;
        }

        /// <summary>
        /// Moves data from register <paramref name="src"/> to <paramref name="dst"/>
        /// with the given displacement.
        /// </summary>
        public int Move(X86Register dst, X86Register src, sbyte displacement)
        {
            int begin = (int)ms.Position;
            bw.Write(REX_W); // REX.W
            bw.Write((byte)0x8B);  // mov
            // mod = 0b10, reg = dst, rm = src
            bw.Write(ModRM(0b01, (byte)src, (byte)dst));
            bw.Write(unchecked((byte)displacement));
            return begin;
        }

        public int Move(RegisterREX dst, X86Register src)
        {
            int begin = (int)ms.Position;
            bw.Write((byte)(REX_W + 1)); // REX.W
            bw.Write((byte)0x89);  // mov
            bw.Write(ModRM(0b11, (byte)src, (byte)dst));
            return begin;
        }

        /// <summary>
        /// Moves an immediate value to <paramref name="dst"/>.
        /// </summary>
        /// <returns>The absolute offset to this instruction.</returns>
        public int Move(X86Register dst, uint immediate)
        {
            int begin = (int)ms.Position;
            bw.Write(REX_W); // REX.W
            bw.Write((byte)0xC7);  // mov
            bw.Write(ModRM(0b11, 0, (byte)dst));
            bw.Write(immediate);
            return begin;
        }

        /// <summary>
        /// Moves an immediate value to <paramref name="dst"/>.
        /// </summary>
        public int Move(X86Register dst, ulong immediate)
        {
            int begin = (int)ms.Position;
            bw.Write(REX_W);
            bw.Write((byte)(0xB8 + (byte)dst));
            bw.Write(immediate);
            return begin;
        }

        /// <summary>
        /// Moves an immediate value to <paramref name="dst"/>.
        /// </summary>
        public int Move(RegisterREX dst, ulong immediate)
        {
            int begin = (int)ms.Position;
            bw.Write((byte)(REX_W + 1));
            bw.Write((byte)(0xB8 + (byte)dst));
            bw.Write(immediate);
            return begin;
        }

        public int RelativeJump(sbyte offsetFromNextInstruction)
        {
            int begin = (int)ms.Position;
            bw.Write((byte)0xEB);
            bw.Write(offsetFromNextInstruction);
            return begin;
        }

        public int NoOperation()
        {
            int begin = (int)ms.Position;
            bw.Write(0xEB);
            return begin;
        }

        /// <summary>
        /// Equivalent to <c>jmp $</c> in NASM.
        /// </summary>
        /// <returns></returns>
        // Name taken from https://twitter.com/x86instructions/status/1029618829314289664 :3
        public int WarmerHalt() => RelativeJump(-2); // a rel 8-bit jump takes up 2 bytes

        public void InsertRandomness(sbyte min, sbyte max)
        {
            var amtRandBytes = Random.Shared.Next(min, max);
            RelativeJump((sbyte)amtRandBytes);
            var buffer = new byte[amtRandBytes];
            Random.Shared.NextBytes(buffer);
            bw.Write(buffer);
        }

        /// <summary>
        /// Loads the effective address of <c>[src + displacement]</c> to <paramref name="dst"/>.
        /// </summary>
        public int LoadEffectiveAddress(X86Register dst, X86Register src, sbyte displacement)
        {
            int begin = (int)ms.Position;

            bw.Write(REX_W); // REX.W
            bw.Write((byte)0x8D);  // lea
            bw.Write(ModRM(0b01, (byte)dst, (byte)src));

            if (src is X86Register.RSP) {
                bw.Write(0x24);
            }

            bw.Write(unchecked((byte)displacement));

            return begin;
        }

        /// <summary>
        /// Adds <paramref name="value"/> to the <paramref name="dst"/> register.
        /// </summary>
        public int Add(X86Register dst, sbyte value)
        {
            int begin = (int)ms.Position;

            // 83 /0 ib
            bw.Write(REX_W); // REX.W
            bw.Write((byte)0x83);  // add
            bw.Write(ModRM(0b11, 0, (byte)dst));
            bw.Write(unchecked((byte)value));

            return begin;
        }

        /// <summary>
        /// Subtracts <paramref name="value"/> from the <paramref name="dst"/> register.
        /// </summary>
        public int Subtract(X86Register dst, sbyte value)
        {
            int begin = (int)ms.Position;

            // 83 /5 ib
            bw.Write(REX_W); // REX.W
            bw.Write((byte)0x83);  // sub
            bw.Write(ModRM(0b11, 5, (byte)dst));
            bw.Write(unchecked((byte)value));

            return begin;
        }

        /// <summary>
        /// r/m64 AND imm8 (sign-extended).
        /// </summary>
        public int And(X86Register dst, sbyte immediate)
        {
            int begin = (int)ms.Position;

            bw.Write(REX_W);
            bw.Write((byte)0x83); // and
            bw.Write(ModRM(0b11, 4, (byte)dst));
            bw.Write(unchecked((byte)immediate));

            return begin;
        }

        // https://www.felixcloutier.com/x86/xor
        public int Xor(X86Register dst, X86Register src)
        {
            int begin = (int)ms.Position;
            bw.Write(REX_W);
            bw.Write((byte)0x31); // xor
            bw.Write(ModRM(0b11, (byte)dst, (byte)src));
            return begin;
        }

        public int Xor(RegisterREX dst, RegisterREX src)
        {
            int begin = (int)ms.Position;
            bw.Write((byte)(REX_W | (1 << 2) | (1 << 0))); // REX.R, REX.B, REX.W
            bw.Write((byte)0x31); // xor
            bw.Write(ModRM(0b11, (byte)dst, (byte)src));
            return begin;
       }

        public void Dispose()
        {
            bw.Dispose();
            ms.Dispose();
        }
    }
}
