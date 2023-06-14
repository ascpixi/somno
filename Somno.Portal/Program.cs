using Somno.Portal.Shellcode;
using System;

namespace Somno.Portal
{
    class Program
    {
        static void Main(string[] args)
        {
            using var em = new X8664Emitter(512);
            em.Move(X86Register.RCX, X86Register.RAX);
            em.Move(X86Register.RDX, 3556);
            em.InsertRandomness(5, 12);
            em.Xor(RegisterREX.R8, RegisterREX.R8); // dwFileOffsetHigh
            em.Xor(RegisterREX.R9, RegisterREX.R9); // dwFileOffsetLow
            em.Move(X86Register.RAX, 7543); // dwNumberOfBytesToMap
            em.Push(X86Register.RAX);

            em.InsertRandomness(6, 43);

            em.Subtract(X86Register.RSP, 0x20);
            em.CallUsingRAX();
            em.Add(X86Register.RSP, 0x28);
            em.Move(RegisterREX.R14, X86Register.RAX);

            foreach (var b in em.GetAssembled()) {
                Console.Write($"{b:X2} ");
            }
        }
    }
}
