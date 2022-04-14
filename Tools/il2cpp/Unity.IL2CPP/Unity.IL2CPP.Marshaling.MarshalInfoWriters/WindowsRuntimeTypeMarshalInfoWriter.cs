using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class WindowsRuntimeTypeMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public WindowsRuntimeTypeMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
			: base(context, type)
		{
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType("Il2CppWindowsRuntimeTypeName", "Il2CppWindowsRuntimeTypeName")
			};
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_typeRef);
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("il2cpp_codegen_marshal_type_to_native(" + sourceVariable.Load() + ", " + destinationVariable + ");");
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			writer.WriteLine("il2cpp_codegen_delete_native_type(" + variableName + ");");
		}

		public override bool TreatAsValueType()
		{
			return true;
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine(destinationVariable.Store("il2cpp_codegen_marshal_type_from_native(" + variableName + ")"));
		}
	}
}
