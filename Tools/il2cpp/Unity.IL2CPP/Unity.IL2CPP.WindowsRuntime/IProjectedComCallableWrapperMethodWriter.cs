using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

namespace Unity.IL2CPP.WindowsRuntime
{
	public interface IProjectedComCallableWrapperMethodWriter
	{
		void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType);

		ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference method);
	}
}
