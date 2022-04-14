using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal class ComMethodWithPreOwnedInterfacePointerMethodBodyWriter : ComMethodBodyWriter
	{
		public ComMethodWithPreOwnedInterfacePointerMethodBodyWriter(MinimalContext context, MethodReference interfaceMethod)
			: base(context, interfaceMethod, interfaceMethod)
		{
		}

		protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
		}
	}
}
