using System;

namespace Somno;

/// <summary>
/// Provides methods to patch the bytes of a given binary file.
/// </summary>
internal static class BytePatch
{
    /// <summary>
    /// Replaces all instances of the string <paramref name="searchFor"/> with
    /// <paramref name="replaceWith"/>. The strings must not contain characters
    /// outside the standard ASCII range. The length of <paramref name="replaceWith"/>
    /// cannot exceed the length of <paramref name="searchFor"/>.
    /// </summary>
    /// <param name="bytes">The bytes to operate on. This array will be written to.</param>
    /// <param name="searchFor">The ASCII character sequence to search for. Must be greater or equal in length to <paramref name="searchFor"/>.</param>
    /// <param name="replaceWith">The ASCII characters to replace the found strings with. Must be lesser or equal in length to <paramref name="replaceWith"/>.</param>
    /// <exception cref="ArgumentException">Thrown when the length of one of the parameters is invalid.</exception>
    public static void Patch(byte[] bytes, string searchFor, string replaceWith)
    {
        if(searchFor.Length < replaceWith.Length)
            throw new ArgumentException($"Cannot replace '{searchFor}' with '{replaceWith}', as the buffer is too small to hold the replacement value.");

        int length = bytes.Length - searchFor.Length;
        for (int i = 0; i < length; i++) {
            for (int j = 0; j < searchFor.Length; j++) {
                if (bytes[i + j] != (byte)searchFor[j]) {
                    goto continueLoop;
                }
            }

            for (int j = 0; j < replaceWith.Length; j++) {
                bytes[i + j] = (byte)replaceWith[j];
            }

            for (int j = replaceWith.Length; j < searchFor.Length; j++) {
                bytes[i + j] = 0x00;
            }

            continueLoop:;
        }
    }
}
