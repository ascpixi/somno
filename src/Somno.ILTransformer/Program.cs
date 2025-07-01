using Mono.Cecil;

namespace Somno.ILTransformer;

internal class Program
{
    static void Main(string[] args)
    {
        // Transform the given module itself
        PerformTransform(args[0], module => {
            StringEncoder.EncodeAllStrings(module);
        });
    }

    static void PerformTransform(string path, Action<ModuleDefinition> transform)
    {
        var workDllPath = $"{path}.tmp.dll";

        if (File.Exists(workDllPath))
            File.Delete(workDllPath);

        File.Copy(path, workDllPath);

        var module = ModuleDefinition.ReadModule(workDllPath, new() {
            AssemblyResolver = new NuGetAssemblyResolver()
        });

        transform(module);

        File.Delete(path);
        module.Write(path);

        Console.WriteLine($"Transformed module {Path.GetFileName(path)}.");
    }
}