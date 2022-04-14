using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public class ComVariantMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public ComVariantMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
			: base(context, type)
		{
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType("Il2CppVariant", "Il2CppVariant")
			};
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
		}

		public override void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("il2cpp_codegen_com_marshal_variant((RuntimeObject*)({0}), &({1}));", sourceVariable.Load(), destinationVariable);
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine(destinationVariable.Store("(RuntimeObject*)il2cpp_codegen_com_marshal_variant_result(&({0}))", variableName));
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			writer.WriteLine("il2cpp_codegen_com_destroy_variant(&({0}));", variableName);
		}
	}
}
