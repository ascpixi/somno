using Mono.Cecil;

namespace Somno.ILTransformer;

internal class NuGetAssemblyResolver : BaseAssemblyResolver
{
    readonly DefaultAssemblyResolver defaultResolver = new();

    public override AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        AssemblyDefinition? assembly;

        try {
            assembly = defaultResolver.Resolve(name);
            assembly ??= ResolveByNuGet(name);
        }
        catch (AssemblyResolutionException) {
            assembly = ResolveByNuGet(name);
        }
        
        if (assembly == null) {
            throw new AssemblyResolutionException(name);
        }

        return assembly;
    }

    static AssemblyDefinition? ResolveByNuGet(AssemblyNameReference name)
    {
        var libraryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".nuget/packages",
            name.Name,
            name.Version.ToString(),
            "lib"
        );

        foreach (var frameworkDllFolder in Directory.GetDirectories(libraryPath)) {
            var frameworkDll = Path.Combine(frameworkDllFolder, $"{name.Name}.dll");
            var assembly = AssemblyDefinition.ReadAssembly(frameworkDll);

            if (assembly.FullName == name.FullName) {
                return assembly;
            }
        }

        return null;
    }
}
