using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public class BlittableByReferenceMarshalInfoWriter : DefaultMarshalInfoWriter
	{
		private readonly TypeReference _elementType;

		private readonly DefaultMarshalInfoWriter _elementTypeMarshalInfoWriter;

		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public override int NativeSizeWithoutPointers => 0;

		public BlittableByReferenceMarshalInfoWriter(ReadOnlyContext context, ByReferenceType type, MarshalType marshalType, MarshalInfo marshalInfo)
			: base(context, type)
		{
			_elementType = type.ElementType;
			_elementTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, _elementType, marshalType, marshalInfo, useUnicodeCharSet: false, forByReferenceType: true);
			if (_elementTypeMarshalInfoWriter.MarshaledTypes.Length > 1)
			{
				throw new InvalidOperationException($"BlittableByReferenceMarshalInfoWriter cannot marshal {type.ElementType.FullName}&.");
			}
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(_elementTypeMarshalInfoWriter.MarshaledTypes[0].Name + "*", _elementTypeMarshalInfoWriter.MarshaledTypes[0].DecoratedName + "*")
			};
		}

		public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
		{
			return WriteMarshalVariableToNative(variableName);
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("{0} = {1};", destinationVariable, WriteMarshalVariableToNative(sourceVariable));
		}

		public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			return WriteMarshalVariableToNative(sourceVariable);
		}

		private string WriteMarshalVariableToNative(ManagedMarshalValue variableName)
		{
			if (_context.Global.Services.Naming.ForVariable(_elementType) != _elementTypeMarshalInfoWriter.MarshaledTypes[0].Name)
			{
				return "reinterpret_cast<" + _marshaledTypes[0].Name + ">(" + variableName.Load() + ")";
			}
			return variableName.Load();
		}

		public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			_elementTypeMarshalInfoWriter.WriteMarshalVariableToNative(writer, sourceVariable.Dereferenced, Emit.Dereference(UndecorateVariable(destinationVariable)), managedVariableName, metadataAccess);
		}

		public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{CleanVariableName(variableName)}_empty";
			writer.WriteVariable(_elementType, text);
			return Emit.AddressOf(text);
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine(destinationVariable.Store(WriteMarshalVariableFromNative(writer, variableName, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess)));
		}

		public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool returnValue, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
		{
			string text = _context.Global.Services.Naming.ForVariable(_typeRef);
			if (text != _marshaledTypes[0].DecoratedName)
			{
				return $"reinterpret_cast<{text}>({variableName})";
			}
			return variableName;
		}

		public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
		{
			if (!(variableName == WriteMarshalVariableToNative(destinationVariable)))
			{
				_elementTypeMarshalInfoWriter.WriteMarshalVariableFromNative(writer, Emit.Dereference(variableName), destinationVariable.Dereferenced, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
			}
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			_elementTypeMarshalInfoWriter.WriteMarshaledTypeForwardDeclaration(writer);
		}
	}
}
