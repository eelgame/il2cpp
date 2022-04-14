using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class TypeDefinitionWithMarshalSizeOfOnlyMarshalInfoWriter : TypeDefinitionMarshalInfoWriter
	{
		public TypeDefinitionWithMarshalSizeOfOnlyMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType, bool forFieldMarshaling, bool forByReferenceType, bool forReturnValue, bool forNativeToManagedWrapper)
			: base(context, type, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper)
		{
		}

		private void WriteThrowNotSupportedException(ICodeWriter writer)
		{
			writer.WriteStatement("Exception_t* _marshalingException = " + GetMarshalingException());
			writer.WriteStatement(Emit.RaiseManagedException("_marshalingException"));
		}

		protected override void WriteMarshalToNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine(_marshalToNativeFunctionDeclaration);
			writer.BeginBlock();
			WriteThrowNotSupportedException(writer);
			writer.EndBlock();
		}

		protected override void WriteMarshalFromNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine(_marshalFromNativeFunctionDeclaration);
			writer.BeginBlock();
			WriteThrowNotSupportedException(writer);
			writer.EndBlock();
		}

		protected override void WriteMarshalCleanupFunction(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine(_marshalCleanupFunctionDeclaration);
			writer.WriteLine("{");
			writer.WriteLine("}");
		}

		public override bool CanMarshalTypeFromNative()
		{
			return false;
		}

		public override bool CanMarshalTypeToNative()
		{
			return false;
		}

		public override string GetMarshalingException()
		{
			return $"il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal type '{_typeRef.FullName}'.\")";
		}
	}
}
