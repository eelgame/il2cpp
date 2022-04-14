using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	internal class ConstructorFactoryMethodBodyWriter : ComCallableWrapperMethodBodyWriter
	{
		public ConstructorFactoryMethodBodyWriter(MinimalContext context, MethodDefinition constructor, MethodReference nativeInterfaceMethod)
			: base(context, constructor, nativeInterfaceMethod, MarshalType.WindowsRuntime)
		{
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			TypeReference declaringType = _managedMethod.DeclaringType;
			INamingService naming = writer.Context.Global.Services.Naming;
			string text = "managedInstance";
			writer.WriteLine(naming.ForVariable(declaringType) + " " + text + " = " + Emit.NewObj(writer.Context, declaringType, metadataAccess) + ";");
			writer.WriteMethodCallStatement(metadataAccess, text, _managedMethod, _managedMethod, MethodCallType.Normal, localVariableNames);
			writer.WriteStatement(naming.ForInteropReturnValue() + " = " + text);
		}
	}
}
