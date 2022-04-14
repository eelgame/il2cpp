using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal class DelegateMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		protected readonly MarshaledType[] _marshaledTypes;

		protected readonly bool _forFieldMarshaling;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public DelegateMarshalInfoWriter(ReadOnlyContext context, TypeReference type, bool forFieldMarshaling)
			: base(context, type)
		{
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType("Il2CppMethodPointer", "Il2CppMethodPointer")
			};
			_forFieldMarshaling = forFieldMarshaling;
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("{0} = il2cpp_codegen_marshal_delegate(reinterpret_cast<{1}>({2}));", destinationVariable, "MulticastDelegate_t*", sourceVariable.Load());
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine(destinationVariable.Store("il2cpp_codegen_marshal_function_ptr_to_delegate<{0}>({1}, {2})", _context.Global.Services.Naming.ForType(_typeRef), variableName, metadataAccess.TypeInfoFor(_typeRef)));
		}

		public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
		}

		public override void WriteNativeVariableDeclarationOfType(IGeneratedMethodCodeWriter writer, string variableName)
		{
			writer.WriteLine("{0} {1} = NULL;", MarshaledTypes[0].DecoratedName, variableName);
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
		}
	}
}
