// NOTE: The implementation of X8664Emitter has been taken from my C#
//       operating system project Azerou. Changes in the main Azerou
//       implementation can be merged with this one (note that it
//       includes features such as interrupt handling and operates as a
//       ref struct and on native pointers, which is not needed for this
//       implementation).

namespace Somno.Portal.Shellcode
{
    public enum RegisterREX : byte
    {
        R8 = 0,
        R9 = 1,
        R10 = 2,
        R11 = 3,
        R12 = 4,
        R13 = 5,
        R14 = 6,
        R15 = 7,
    }
}
