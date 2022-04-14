using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal class ExceptionMarshalInfoWriter : DefaultMarshalInfoWriter
	{
		private MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public ExceptionMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
			: base(context, type)
		{
			string text = context.Global.Services.Naming.ForVariable(context.Global.Services.TypeProvider.Int32TypeReference);
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(text, text)
			};
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
		}

		public override void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_context.Global.Services.TypeProvider.SystemException);
		}

		public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
		{
			return "0";
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine(destinationVariable + " = " + WriteMarshalVariableToNative(writer, sourceVariable, managedVariableName, metadataAccess) + ";");
		}

		public override string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			return "(" + sourceVariable.Load() + " != NULL ? reinterpret_cast<RuntimeException*>(" + sourceVariable.Load() + ")->hresult : IL2CPP_S_OK)";
		}

		public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			return "NULL";
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine(destinationVariable.Store(WriteMarshalVariableFromNative(writer, variableName, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess)));
		}

		public override string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
		{
			return "(" + variableName + " != IL2CPP_S_OK ? reinterpret_cast<" + _context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.SystemException) + ">(il2cpp_codegen_com_get_exception(" + variableName + ", false)) : NULL)";
		}
	}
}
