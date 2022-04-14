using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class LPArrayMarshalInfoWriter : ArrayMarshalInfoWriter
	{
		public LPArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType arrayType, MarshalType marshalType, MarshalInfo marshalInfo)
			: base(context, arrayType, marshalType, marshalInfo)
		{
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", sourceVariable.Load(), "NULL");
			using (new BlockWriter(writer))
			{
				string arraySizeVariable = WriteArraySizeFromManagedArray(writer, sourceVariable, destinationVariable);
				AllocateAndStoreNativeArray(writer, destinationVariable, arraySizeVariable);
				WriteMarshalToNativeLoop(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => arraySizeVariable);
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				WriteAssignNullArray(writer, destinationVariable);
			}
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("if ({0} == {1})", destinationVariable.Load(), "NULL");
				using (new BlockWriter(writer))
				{
					AllocateAndStoreManagedArray(writer, destinationVariable, metadataAccess, MarshaledArraySizeFor(variableName, methodParameters));
				}
				WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, delegate
				{
					writer.WriteLine("{0} {1} = ({2})->max_length;", "il2cpp_array_size_t", "_arrayLength", destinationVariable.Load());
					return "_arrayLength";
				});
			}
		}

		public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteArraySizeFromManagedArray(bodyWriter, destinationVariable, variableName));
			}
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				WriteCleanupLoop(writer, variableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteLoopCountVariable(bodyWriter, variableName, managedVariableName));
				writer.WriteLine("il2cpp_codegen_marshal_free({0});", variableName);
				writer.WriteLine("{0} = {1};", variableName, "NULL");
			}
		}

		public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				WriteCleanupOutVariableLoop(writer, variableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => WriteLoopCountVariable(bodyWriter, variableName, managedVariableName));
				writer.WriteLine("il2cpp_codegen_marshal_free({0});", variableName);
				writer.WriteLine("{0} = {1};", variableName, "NULL");
			}
		}

		private string WriteLoopCountVariable(IGeneratedCodeWriter bodyWriter, string variableName, string managedVariableName)
		{
			string arg = CleanVariableName(variableName);
			string text = $"{arg}_CleanupLoopCount";
			string text2 = ((managedVariableName == null) ? MarshaledArraySizeFor(variableName, null) : string.Format("({0} != {1}) ? ({0})->max_length : 0", managedVariableName, "NULL"));
			bodyWriter.WriteLine("const {0} {1} = {2};", "il2cpp_array_size_t", text, text2);
			return text;
		}
	}
}
