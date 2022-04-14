using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal static class ProjectedComCallableWrapperMethodWriterDriver
	{
		private sealed class NotSupportedMethodBodyWriter : ComCallableWrapperMethodBodyWriter
		{
			public NotSupportedMethodBodyWriter(MinimalContext context, MethodReference method)
				: base(context, method, method, MarshalType.WindowsRuntime)
			{
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				string text = "Cannot call method '" + InteropMethod.FullName + "' from native code. IL2CPP does not yet support calling this projected method.";
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_not_supported_exception(\"" + text + "\")"));
			}
		}

		public static void WriteFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
		{
			TypeResolver typeResolver = TypeResolver.For(interfaceType);
			TypeDefinition typeDefinition = interfaceType.Resolve();
			IProjectedComCallableWrapperMethodWriter projectedComCallableWrapperMethodWriterFor = context.Global.Services.WindowsRuntime.GetProjectedComCallableWrapperMethodWriterFor(typeDefinition);
			projectedComCallableWrapperMethodWriterFor?.WriteDependenciesFor(context, writer, interfaceType);
			writer.AddIncludeForTypeDefinition(interfaceType);
			foreach (MethodDefinition method in typeDefinition.Methods)
			{
				MethodReference methodReference = typeResolver.Resolve(method);
				ComCallableWrapperMethodBodyWriter methodBodyWriter = projectedComCallableWrapperMethodWriterFor?.GetBodyWriter(context, methodReference) ?? new NotSupportedMethodBodyWriter(context, methodReference);
				writer.WriteCommentedLine("Projected COM callable wrapper method for " + methodReference.FullName);
				string methodSignature = MethodSignatureWriter.FormatProjectedComCallableWrapperMethodDeclaration(context, methodReference, typeResolver, MarshalType.WindowsRuntime);
				string text = context.Global.Services.Naming.ForComCallableWrapperProjectedMethod(methodReference);
				writer.WriteMethodWithMetadataInitialization(methodSignature, text, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
				{
					methodBodyWriter.WriteMethodBody(bodyWriter, metadataAccess);
				}, text, methodReference);
			}
		}
	}
}
