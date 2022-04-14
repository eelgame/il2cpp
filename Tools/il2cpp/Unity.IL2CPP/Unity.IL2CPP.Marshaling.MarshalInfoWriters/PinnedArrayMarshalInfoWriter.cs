using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal class PinnedArrayMarshalInfoWriter : ArrayMarshalInfoWriter
	{
		public PinnedArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType arrayType, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharset = false)
			: base(context, arrayType, marshalType, marshalInfo, useUnicodeCharset)
		{
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", sourceVariable.Load(), "NULL");
			using (new BlockWriter(writer))
			{
				if (_arraySizeSelection == ArraySizeOptions.UseFirstMarshaledType)
				{
					writer.WriteLine("{0}{1} = static_cast<uint32_t>(({2})->max_length);", destinationVariable, MarshaledTypes[0].VariableName, sourceVariable.Load());
				}
				writer.WriteLine("{0} = reinterpret_cast<{1}>(({2})->{3}(0));", destinationVariable, _arrayMarshaledTypeName, sourceVariable.Load(), ArrayNaming.ForArrayItemAddressGetter(useArrayBoundsCheck: false));
			}
		}

		public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", sourceVariable.Load(), "NULL");
			using (new BlockWriter(writer))
			{
				WriteMarshalToNativeLoop(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteArraySizeFromManagedArray(bodyWriter, sourceVariable, destinationVariable));
			}
		}

		public override string WriteMarshalReturnValueToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{sourceVariable.GetNiceName()}_marshaled";
			WriteNativeVariableDeclarationOfType(writer, text);
			writer.WriteLine("if ({0} != {1})", sourceVariable.Load(), "NULL");
			using (new BlockWriter(writer))
			{
				string arraySizeVariable = WriteArraySizeFromManagedArray(writer, sourceVariable, text);
				AllocateAndStoreNativeArray(writer, text, arraySizeVariable);
				WriteMarshalToNativeLoop(writer, sourceVariable, text, null, metadataAccess, (IGeneratedCodeWriter bodyWriter) => arraySizeVariable);
				return text;
			}
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				string arraySize = MarshaledArraySizeFor(variableName, methodParameters);
				AllocateAndStoreManagedArray(writer, destinationVariable, metadataAccess, arraySize);
				WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => arraySize);
			}
		}

		public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
		{
			if (isIn)
			{
				return;
			}
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteArraySizeFromManagedArray(bodyWriter, destinationVariable, variableName));
			}
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
		}

		public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			writer.WriteLine("il2cpp_codegen_marshal_free({0});", variableName);
			writer.WriteLine("{0} = {1};", variableName, "NULL");
		}
	}
}
