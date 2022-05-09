using System.IO;
using System.Runtime.CompilerServices;
using Mono.Cecil;

public class Program
{
    public static int Main(string[] args)
    {
        var dlls = new[] {"il2cpp.dll", "Unity.IL2CPP.dll", "Unity.IL2CPP.Building.dll"};

        foreach (var dll in dlls)
        {
            var readerParameters = new ReaderParameters();
            var assemblyResolver = new DefaultAssemblyResolver();

            var dir = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            assemblyResolver.AddSearchDirectory(dir);

            readerParameters.InMemory = true;
            readerParameters.AssemblyResolver = assemblyResolver;

            using (var assemblyDefinition =
                   AssemblyDefinition.ReadAssembly(Path.Combine(dir, dll), readerParameters))
            {
                var ca = new CustomAttribute(
                    assemblyDefinition.MainModule.ImportReference(
                        typeof(InternalsVisibleToAttribute).GetConstructor(new[] {typeof(string)})));
                ca.ConstructorArguments.Add(new CustomAttributeArgument(assemblyDefinition.MainModule.TypeSystem.String,
                    "huatuo-il2cpp"));
                assemblyDefinition.CustomAttributes.Add(ca);
                assemblyDefinition.Write(Path.Combine(dir, assemblyDefinition.MainModule.Name));
            }
        }

        return 0;
    }
}