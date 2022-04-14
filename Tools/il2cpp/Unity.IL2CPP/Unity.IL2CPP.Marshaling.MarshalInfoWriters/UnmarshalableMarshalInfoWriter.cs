using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public sealed class UnmarshalableMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public override string NativeSize => "-1";

		public UnmarshalableMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
			: base(context, type)
		{
			string text = ((!(_typeRef is GenericParameter)) ? context.Global.Services.Naming.ForVariable(_typeRef) : "void*");
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(text, text)
			};
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			throw new InvalidOperationException($"Cannot marshal {_typeRef.FullName} to native!");
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			throw new InvalidOperationException($"Cannot marshal {_typeRef.FullName} from native!");
		}

		public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
		{
			throw new InvalidOperationException($"Cannot marshal {_typeRef.FullName} to native!");
		}

		public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			throw new InvalidOperationException($"Cannot marshal {_typeRef.FullName} from native!");
		}

		public override bool CanMarshalTypeToNative()
		{
			return false;
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			writer.AddForwardDeclaration(_typeRef);
		}

		public override string GetMarshalingException()
		{
			return $"il2cpp_codegen_get_marshal_directive_exception(\"Cannot marshal type '{_typeRef.FullName}'.\")";
		}
	}
}
