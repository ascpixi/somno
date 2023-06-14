// NOTE: The implementation of X8664Emitter has been taken from my C#
//       operating system project Azerou. Changes in the main Azerou
//       implementation can be merged with this one (note that it
//       includes features such as interrupt handling and operates as a
//       ref struct and on native pointers, which is not needed for this
//       implementation).

namespace Somno.Portal.Shellcode
{
    internal enum X86Register : byte
    {
        RAX = 0,
        RCX = 1,
        RDX = 2,
        RBX = 3,
        RSP = 4,
        RBP = 5,
        RSI = 6,
        RDI = 7,
    }
}
