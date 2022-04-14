using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface IWindowsRuntimeProjections
	{
		TypeReference ProjectToWindowsRuntime(TypeReference type);

		TypeDefinition ProjectToWindowsRuntime(TypeDefinition type);

		TypeReference ProjectToCLR(TypeReference type);

		TypeDefinition ProjectToCLR(TypeDefinition type);

		IProjectedComCallableWrapperMethodWriter GetProjectedComCallableWrapperMethodWriterFor(TypeDefinition type);

		TypeDefinition GetNativeToManagedAdapterClassFor(TypeDefinition interfaceType);

		IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> GetClrToWindowsRuntimeProjectedTypes();

		IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> GetNativeToManagedInterfaceAdapterClasses();
	}
}
