using System.Globalization;

namespace Somno.Packager
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if(args.Length != 4) {
                Console.WriteLine("usage: Somno.Packager [pack|unpack] <256-bit hex key> <input> <output>");
                return;
            }

            var key = ConvertHexStringToByteArray(args[1]);

            switch (args[0]) {
                case "pack": {
                    var input = File.ReadAllBytes(args[2]);
                    var output = Packaging.CreatePackedFile(input, key);

                    var dir = Path.GetDirectoryName(args[3]);
                    if(dir != null)
                        Directory.CreateDirectory(dir);

                    File.WriteAllBytes(args[3], output);
                    Console.WriteLine($"Packed file '{args[2]}' to '{args[3]}'.");
                    return;
                }
                case "unpack": {
                    var input = File.ReadAllBytes(args[2]);
                    var output = Packaging.OpenPackedFile(input, key);

                    var dir = Path.GetDirectoryName(args[3]);
                    if (dir != null)
                        Directory.CreateDirectory(dir);

                    File.WriteAllBytes(args[3], output);
                    Console.WriteLine($"Unpacked file '{args[2]}' to '{args[3]}'.");
                    return;
                }
                default:
                    Console.WriteLine("usage: Somno.Packager [pack|unpack] <256-bit hex key> <input> <output>");
                    break;
            }

        }

        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            byte[] data = new byte[hexString.Length / 2];
            for (int index = 0; index < data.Length; index++) {
                string byteValue = hexString.Substring(index * 2, 2);
                data[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }
    }
}