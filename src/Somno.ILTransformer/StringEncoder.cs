using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Somno.ILTransformer
{
    internal static class StringEncoder
    {
        const string KeySalt = "!_Encr@ptKey_!";

        public static string Encode(string s, string k)
        {
            char[] chars = new char[s.Length];
            for (int i = 0; i < s.Length; i++) {
                chars[i] = (char)(s[i] ^ k[i % k.Length]);
            }

            return new string(chars);
        }

        public static string GetXorKey(MethodReference method, Instruction instruction)
        {
            return new string(
                MD5.HashData(Encoding.ASCII.GetBytes(
                    KeySalt + method.FullName + instruction.Offset
                ))
                    .Select(x => (char)x)
                    .ToArray()
            );
        }

        public static void EncodeAllStrings(ModuleDefinition module)
        {
            var decodeMethod = module.GetTypes()
                .First(x => x.Name == "StringDecoder")
                .Methods
                .First(x => x.Name == "Decode");

            int stringsEncoded = 0;

            foreach (var type in module.GetTypes().Distinct()) {
                foreach (var method in type.GetMethods().Distinct()) {
                    if(!method.HasBody) {
                        continue;
                    }

                    var body = method.Body.Instructions;

                    for (int i = 0; i < body.Count; i++) {
                        var inst = body[i];
                        if (inst.OpCode.Code != Code.Ldstr) {
                            continue;
                        }

                        var operand = (string)inst.Operand;

                        if(operand.Length <= 3) {
                            continue; // No point in encoding such a short string.
                        }

                        var key = GetXorKey(method, inst);
                        var encoded = Encode(operand, key);

                        body.Remove(inst);
                        body.Insert(i, Instruction.Create(OpCodes.Ldstr, key));
                        body.Insert(i + 1, Instruction.Create(OpCodes.Ldstr, encoded));
                        body.Insert(i + 2, Instruction.Create(OpCodes.Call, decodeMethod));

                        i += 2; // we've inserted two more instructions
                        stringsEncoded++;
                    }
                }
            }

            Console.WriteLine($"<StringEncoder> Encoded {stringsEncoded} strings in total.");
        }
    }
}
