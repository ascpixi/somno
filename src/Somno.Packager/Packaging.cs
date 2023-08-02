using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Somno.Packager
{
    public static class Packaging
    {
        /// <summary>
        /// Opens a packed file that was previously packaged with
        /// <see cref="CreatePackedFile(byte[], byte[])"/>.
        /// </summary>
        /// <param name="bytes">The packed bytes.</param>
        /// <param name="key">The decryption key to use.</param>
        public static byte[] OpenPackedFile(byte[] bytes, byte[] key)
        {
            //var reversed = bytes;
            var decrypted = Decrypt(bytes, key);
            //return Decompress(decrypted);
            return decrypted;
        }

        /// <summary>
        /// Creates a packed file, to be later un-packed using
        /// <see cref="OpenPackedFile(byte[], byte[])"/>.
        /// </summary>
        /// <param name="bytes">The raw bytes to pack.</param>
        /// <param name="key">The encryption key to use.</param>
        public static byte[] CreatePackedFile(byte[] bytes, byte[] key)
        {
            //var compressed = Compress(bytes);
            var encrypted = Encrypt(bytes, key);
            return encrypted;
        }

        static byte[] Decompress(byte[] input)
        {
            using (var source = new MemoryStream(input)) {
                byte[] lengthBytes = new byte[4];
                source.Read(lengthBytes, 0, 4);

                var length = BitConverter.ToInt32(lengthBytes, 0);
                using (var decompressionStream = new GZipStream(source,
                    CompressionMode.Decompress)) {
                    var result = new byte[length];
                    decompressionStream.Read(result, 0, length);
                    return result;
                }
            }
        }

        static byte[] Compress(byte[] input)
        {
            using (var result = new MemoryStream()) {
                var lengthBytes = BitConverter.GetBytes(input.Length);
                result.Write(lengthBytes, 0, 4);

                using (var compressionStream = new GZipStream(result,
                    CompressionMode.Compress)) {
                    compressionStream.Write(input, 0, input.Length);
                    compressionStream.Flush();

                }
                return result.ToArray();
            }
        }

        static byte[] Encrypt(byte[] data, byte[] key)
        {
            using var aes = Aes.Create();
            aes.Key = key;
            aes.GenerateIV();

            using var ms = new MemoryStream();
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
                cs.Write(data, 0, data.Length);
            }

            return ms.ToArray();
        }

        static byte[] Decrypt(byte[] encryptedData, byte[] key)
        {
            using var aes = Aes.Create();
            byte[] iv = new byte[aes.BlockSize / 8];
            Buffer.BlockCopy(encryptedData, 0, iv, 0, iv.Length);

            aes.Key = key;
            aes.IV = iv;

            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write)) {
                cs.Write(encryptedData, iv.Length, encryptedData.Length - iv.Length);
            }

            return ms.ToArray();
        }
    }
}
