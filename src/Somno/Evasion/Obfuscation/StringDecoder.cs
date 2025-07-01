using System.Runtime.CompilerServices;

namespace Somno.Evasion.Obfuscation;

internal static class StringDecoder
{
    /// <summary>
    /// Decodes a XOR-encoded string. Calls to this method should only
    /// be made by the IL transformer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Decode(string key, string str)
    {
        char[] chArray = new char[str.Length];
        for (int index = 0; index < str.Length; index++) {
            chArray[index] = (char)(str[index] ^ (uint)key[index % key.Length]);
        }

        return new string(chArray);
    }
}
