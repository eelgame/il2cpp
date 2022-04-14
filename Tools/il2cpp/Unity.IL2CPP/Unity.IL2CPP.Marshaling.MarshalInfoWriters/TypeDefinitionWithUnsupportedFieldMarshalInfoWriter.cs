using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public sealed class TypeDefinitionWithUnsupportedFieldMarshalInfoWriter : CustomMarshalInfoWriter
	{
		private readonly FieldDefinition _faultyField;

		public override string NativeSize => "-1";

		public TypeDefinitionWithUnsupportedFieldMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType, FieldDefinition faultyField)
			: base(context, type, marshalType, forFieldMarshaling: false, forByReferenceType: false, forReturnValue: false, forNativeToManagedWrapper: false)
		{
			_faultyField = faultyField;
		}

		private void WriteThrowNotSupportedException(ICodeWriter writer)
		{
			string text = _context.Global.Services.Naming.ForField(_faultyField) + "Exception";
			writer.WriteStatement("Exception_t* " + text + " = " + GetMarshalingException());
			writer.WriteStatement(Emit.RaiseManagedException(text));
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

		public override bool CanMarshalTypeToNative()
		{
			return false;
		}

		public override string GetMarshalingException()
		{
			if (_faultyField.FieldType.MetadataType == MetadataType.Class || (_faultyField.FieldType.IsArray && ((ArrayType)_faultyField.FieldType).ElementType.MetadataType == MetadataType.Class))
			{
				return $"il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal field '{_faultyField.Name}' of type '{_type.Name}': Reference type field marshaling is not supported.\")";
			}
			return $"il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal field '{_faultyField.Name}' of type '{_type.Name}'.\")";
		}
	}
}
