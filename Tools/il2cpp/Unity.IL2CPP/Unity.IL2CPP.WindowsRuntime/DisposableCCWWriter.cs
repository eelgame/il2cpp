using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal class DisposableCCWWriter : IProjectedComCallableWrapperMethodWriter
	{
		public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
		{
		}

		public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference closeMethod)
		{
			TypeDefinition type = closeMethod.DeclaringType.Resolve();
			MethodDefinition managedMethod = context.Global.Services.WindowsRuntime.ProjectToCLR(type).Methods.Single((MethodDefinition m) => m.Name == "Dispose");
			return new ProjectedMethodBodyWriter(context, managedMethod, closeMethod);
		}
	}
}
