using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class ByReferenceMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		private readonly TypeReference _elementType;

		private readonly DefaultMarshalInfoWriter _elementTypeMarshalInfoWriter;

		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public ByReferenceMarshalInfoWriter(ReadOnlyContext context, ByReferenceType type, MarshalType marshalType, MarshalInfo marshalInfo, bool forNativeToManagedWrapper)
			: base(context, type)
		{
			_elementType = type.ElementType;
			_elementTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, type.ElementType, marshalType, marshalInfo, useUnicodeCharSet: false, forByReferenceType: true, forFieldMarshaling: false, forReturnValue: false, forNativeToManagedWrapper);
			_marshaledTypes = _elementTypeMarshalInfoWriter.MarshaledTypes.Select((MarshaledType t) => new MarshaledType(t.Name + "*", t.DecoratedName + "*", t.VariableName)).ToArray();
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			_elementTypeMarshalInfoWriter.WriteMarshaledTypeForwardDeclaration(writer);
		}

		public override void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
			_elementTypeMarshalInfoWriter.WriteMarshaledTypeForwardDeclaration(writer);
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			_elementTypeMarshalInfoWriter.WriteIncludesForMarshaling(writer);
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"{CleanVariableName(destinationVariable)}_dereferenced";
			_elementTypeMarshalInfoWriter.WriteNativeVariableDeclarationOfType(writer, text);
			_elementTypeMarshalInfoWriter.WriteMarshalVariableToNative(writer, sourceVariable.Dereferenced, text, managedVariableName, metadataAccess);
			writer.WriteLine("{0} = &{1};", destinationVariable, text);
		}

		public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
		{
			string text = $"_{variableName.GetNiceName()}_marshaled";
			if (_elementType.MetadataType == MetadataType.Class && _elementTypeMarshalInfoWriter is CustomMarshalInfoWriter)
			{
				WriteNativeVariableDeclarationOfType(writer, text);
			}
			else
			{
				string text2 = $"_{variableName.GetNiceName()}_empty";
				_elementTypeMarshalInfoWriter.WriteNativeVariableDeclarationOfType(writer, text2);
				MarshaledType[] marshaledTypes = MarshaledTypes;
				foreach (MarshaledType marshaledType in marshaledTypes)
				{
					writer.WriteLine("{0} {1} = &{2};", marshaledType.Name, text + marshaledType.VariableName, text2 + marshaledType.VariableName);
				}
			}
			return text;
		}

		public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			_elementTypeMarshalInfoWriter.WriteMarshalVariableToNative(writer, sourceVariable.Dereferenced, Emit.Dereference(UndecorateVariable(destinationVariable)), managedVariableName, metadataAccess);
		}

		public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{CleanVariableName(variableName)}_unmarshaled_dereferenced";
			_elementTypeMarshalInfoWriter.WriteDeclareAndAllocateObject(writer, text, variableName, metadataAccess);
			_elementTypeMarshalInfoWriter.WriteMarshalVariableFromNative(writer, Emit.Dereference(variableName), new ManagedMarshalValue(_context, text), methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
			return Emit.AddressOf(text);
		}

		public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{CleanVariableName(variableName)}_unmarshaled_dereferenced";
			_elementTypeMarshalInfoWriter.WriteDeclareAndAllocateObject(writer, text, variableName, metadataAccess);
			_elementTypeMarshalInfoWriter.WriteMarshalVariableFromNative(writer, Emit.Dereference(variableName), new ManagedMarshalValue(_context, text), methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
			writer.WriteLine(destinationVariable.Dereferenced.Store(text));
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{CleanVariableName(variableName)}_unmarshaled_dereferenced";
			_elementTypeMarshalInfoWriter.WriteDeclareAndAllocateObject(writer, text, variableName, metadataAccess);
			_elementTypeMarshalInfoWriter.WriteMarshalVariableFromNative(writer, Emit.Dereference(variableName), new ManagedMarshalValue(_context, text), methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
			_elementTypeMarshalInfoWriter.WriteMarshalCleanupVariable(writer, Emit.Dereference(variableName), metadataAccess, destinationVariable.Dereferenced.Load());
			writer.WriteLine(destinationVariable.Dereferenced.Store(text));
		}

		public override void WriteDeclareAndAllocateObject(IGeneratedCodeWriter writer, string unmarshaledVariableName, string marshaledVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			string text = unmarshaledVariableName + "_dereferenced";
			_elementTypeMarshalInfoWriter.WriteDeclareAndAllocateObject(writer, text, Emit.Dereference(marshaledVariableName), metadataAccess);
			writer.WriteLine("{0} {1} = &{2};", _context.Global.Services.Naming.ForVariable(_typeRef), unmarshaledVariableName, text);
		}

		public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{CleanVariableName(variableName)}_empty";
			writer.WriteVariable(_elementType, text);
			return Emit.AddressOf(text);
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
		}

		public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			_elementTypeMarshalInfoWriter.WriteMarshalCleanupOutVariable(writer, Emit.Dereference(variableName), metadataAccess, (managedVariableName != null) ? Emit.Dereference(managedVariableName) : null);
		}

		public override string DecorateVariable(string unmarshaledParameterName, string marshaledVariableName)
		{
			return _elementTypeMarshalInfoWriter.DecorateVariable(unmarshaledParameterName, marshaledVariableName);
		}

		public override string UndecorateVariable(string variableName)
		{
			return _elementTypeMarshalInfoWriter.UndecorateVariable(variableName);
		}

		public override bool CanMarshalTypeToNative()
		{
			return _elementTypeMarshalInfoWriter.CanMarshalTypeToNative();
		}

		public override bool CanMarshalTypeFromNative()
		{
			return _elementTypeMarshalInfoWriter.CanMarshalTypeFromNative();
		}

		public override string GetMarshalingException()
		{
			return _elementTypeMarshalInfoWriter.GetMarshalingException();
		}
	}
}
