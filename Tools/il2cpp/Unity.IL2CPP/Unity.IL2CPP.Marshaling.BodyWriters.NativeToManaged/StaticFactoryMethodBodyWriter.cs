using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	internal class StaticFactoryMethodBodyWriter : ProjectedMethodBodyWriter
	{
		protected override string ManagedObjectExpression => "NULL";

		public StaticFactoryMethodBodyWriter(MinimalContext context, MethodReference managedMethod, MethodReference nativeInterfaceMethod)
			: base(context, managedMethod, nativeInterfaceMethod)
		{
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void)
			{
				WriteMethodCallStatement(metadataAccess, ManagedObjectExpression, localVariableNames, writer, writer.Context.Global.Services.Naming.ForInteropReturnValue());
			}
			else
			{
				WriteMethodCallStatement(metadataAccess, ManagedObjectExpression, localVariableNames, writer);
			}
		}
	}
}
