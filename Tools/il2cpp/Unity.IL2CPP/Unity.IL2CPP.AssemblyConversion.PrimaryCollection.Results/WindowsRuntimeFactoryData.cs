using Mono.Cecil;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results
{
	public struct WindowsRuntimeFactoryData
	{
		public readonly TypeDefinition TypeDefinition;

		public readonly IIl2CppRuntimeType RuntimeType;

		public WindowsRuntimeFactoryData(TypeDefinition typeDefinition, IIl2CppRuntimeType runtimeType)
		{
			TypeDefinition = typeDefinition;
			RuntimeType = runtimeType;
		}
	}
}
