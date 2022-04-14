using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public class HandleRefMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly TypeDefinition _typeDefinition;

		private readonly bool _forByReferenceType;

		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public HandleRefMarshalInfoWriter(ReadOnlyContext context, TypeReference type, bool forByReferenceType)
			: base(context, type)
		{
			_typeDefinition = type.Resolve();
			_forByReferenceType = forByReferenceType;
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType("void*", "void*")
			};
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			if (!CanMarshalTypeToNative())
			{
				throw new InvalidOperationException("Cannot marshal HandleRef by reference to native code.");
			}
			FieldDefinition fieldDefinition = _typeDefinition.Fields.SingleOrDefault((FieldDefinition f) => f.Name == "m_handle");
			if (fieldDefinition == null)
			{
				throw new InvalidOperationException($"Unable to locate the handle field on {_typeDefinition}");
			}
			writer.WriteLine("{0} = (void*){1}.{2}();", destinationVariable, sourceVariable.Load(), _context.Global.Services.Naming.ForFieldGetter(fieldDefinition));
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			throw new InvalidOperationException("Cannot marshal HandleRef from native code");
		}

		public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
		{
			throw new InvalidOperationException("Cannot marshal HandleRef from native code");
		}

		public override bool CanMarshalTypeToNative()
		{
			return !_forByReferenceType;
		}

		public override bool CanMarshalTypeFromNative()
		{
			return false;
		}

		public override string GetMarshalingException()
		{
			return string.Format("il2cpp_codegen_get_marshal_directive_exception(\"HandleRefs cannot be marshaled ByRef or from unmanaged to managed.\")", _typeRef);
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
		}
	}
}
