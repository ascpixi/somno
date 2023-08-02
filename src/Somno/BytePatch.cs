using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Somno
{
    internal static class BytePatch
    {
        public static void Patch(byte[] bytes, string searchFor, string replaceWith)
        {
            if(searchFor.Length < replaceWith.Length) {
                throw new ArgumentException($"Cannot replace '{searchFor}' with '{replaceWith}', as the buffer is too small to hold the replacement value.");
            }

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
}
