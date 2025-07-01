using System;
using System.Runtime.CompilerServices;

namespace Somno.WindowHost;

public static class Vigenere
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static int Mod(int a, int b) => (a % b + b) % b;

    static string Cipher(string input, string key, bool encipher)
    {
        for (int i = 0; i < key.Length; ++i) {
            if (!char.IsLetter(key[i])) {
                throw new ArgumentException("The key must be composed of alphabetical characters only.", nameof(key));
            }
        }

        string output = string.Empty;
        int nonAlphaCharCount = 0;

        for (int i = 0; i < input.Length; ++i) {
            if (char.IsLetter(input[i])) {
                bool cIsUpper = char.IsUpper(input[i]);
                char offset = cIsUpper ? 'A' : 'a';
                int keyIndex = (i - nonAlphaCharCount) % key.Length;
                int k = (cIsUpper ? char.ToUpper(key[keyIndex]) : char.ToLower(key[keyIndex])) - offset;
                k = encipher ? k : -k;
                char ch = (char)((Mod(((input[i] + k) - offset), 26)) + offset);
                output += ch;
            }
            else {
                output += input[i];
                ++nonAlphaCharCount;
            }
        }

        return output;
    }

    public static string Encipher(string input, string key) => Cipher(input, key, true);

    internal static string Decipher(string input, string key) => Cipher(input, key, false);
}
