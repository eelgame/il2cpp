using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public delegate void WriteRuntimeImplementedMethodBodyDelegate(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess);
}
