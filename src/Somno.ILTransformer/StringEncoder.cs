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

        static IEnumerable<MethodDefinition> GetAllMethods(TypeDefinition type)
        {
            IEnumerable<MethodDefinition> allMethods;
            var methodsAndCtors = type.GetMethods().Concat(type.GetConstructors());
            
            var cctor = type.GetStaticConstructor();
            if(cctor != null) {
                allMethods = methodsAndCtors.Append(cctor);
            } else {
                allMethods = methodsAndCtors;
            }

            return allMethods.Distinct();
        }

        public static void EncodeAllStrings(ModuleDefinition module)
        {
            var decodeMethod = module.GetTypes()
                .First(x => x.Name == "StringDecoder")
                .Methods
                .First(x => x.Name == "Decode");

            int stringsEncoded = 0;

            foreach (var type in module.GetTypes().Distinct()) {
                foreach (var method in GetAllMethods(type)) {
                    if(!method.HasBody) {
                        continue;
                    }

                    method.Body.SimplifyMacros();

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

                        var first = Instruction.Create(OpCodes.Ldstr, key);
                        body.Insert(i, first);
                        body.Insert(i + 1, Instruction.Create(OpCodes.Ldstr, encoded));
                        body.Insert(i + 2, Instruction.Create(OpCodes.Call, decodeMethod));

                        // Find branches that were pointing to the instruction we deleted,
                        // and re-route them to the first instruction of our injected snippet.
                        var targetsPendingCorrection =
                            body.Where(x => x.Operand is Instruction)
                                .Where(x => x.Operand == inst);

                        foreach (var item in targetsPendingCorrection) {
                            item.Operand = first;
                        }

                        var multiTargetsPendingCorrection =
                            body.Where(x => x.Operand is Instruction[])
                                .Select(x =>
                                    (inst: x, shouldInclude: ((Instruction[])x.Operand).Any(x => x == inst))
                                )
                                .Where(x => x.shouldInclude)
                                .Select(x => x.inst);

                        foreach (var item in multiTargetsPendingCorrection) {
                            var targets = (Instruction[])item.Operand;

                            for (int j = 0; j < targets.Length; j++) {
                                if (targets[j] == inst) {
                                    targets[j] = first;
                                }
                            }
                        }

                        foreach (var eh in method.Body.ExceptionHandlers) {
                            if (eh.HandlerStart == inst) eh.HandlerStart = first;
                            if (eh.HandlerEnd == inst) eh.HandlerEnd = first;

                            if (eh.TryStart == inst) eh.TryStart = first;
                            if (eh.TryEnd == inst) eh.TryEnd = first;

                            if (eh.FilterStart == inst) eh.FilterStart = first;
                        }

                        i += 2; // we've inserted two more instructions
                        stringsEncoded++;
                    }

                    method.Body.OptimizeMacros();
                }
            }

            Console.WriteLine($"<StringEncoder> Encoded {stringsEncoded} strings in total.");
        }
    }
}
