using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public abstract class MarshalableMarshalInfoWriter : DefaultMarshalInfoWriter
	{
		protected MarshalableMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
			: base(context, type)
		{
		}

		public sealed override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{sourceVariable.GetNiceName()}_marshaled";
			WriteNativeVariableDeclarationOfType(writer, text);
			WriteMarshalVariableToNative(writer, sourceVariable, text, managedVariableName, metadataAccess);
			return text;
		}

		public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
		{
			string marshaledVariableName = variableName.Replace("*", "");
			string text = $"_{CleanVariableName(variableName)}_unmarshaled";
			WriteDeclareAndAllocateObject(writer, text, marshaledVariableName, metadataAccess);
			WriteMarshalVariableFromNative(writer, variableName, new ManagedMarshalValue(_context, text), methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: false, metadataAccess);
			return text;
		}

		public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
		{
			string text = $"_{variableName.GetNiceName()}_marshaled";
			WriteNativeVariableDeclarationOfType(writer, text);
			return text;
		}
	}
}
