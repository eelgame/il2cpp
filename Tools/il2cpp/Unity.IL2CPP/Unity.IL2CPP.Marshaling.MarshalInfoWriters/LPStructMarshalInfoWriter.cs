using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public class LPStructMarshalInfoWriter : DefaultMarshalInfoWriter
	{
		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public LPStructMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType)
			: base(context, type)
		{
			string text = context.Global.Services.Naming.ForVariable(_typeRef);
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(text, text + "*")
			};
		}

		public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			return sourceVariable.LoadAddress();
		}

		public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
		{
			return Emit.Dereference(variableName);
		}
	}
}
