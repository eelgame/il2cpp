using System.Collections.Generic;
using System.Globalization;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class FixedArrayMarshalInfoWriter : ArrayMarshalInfoWriter
	{
		public FixedArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType arrayType, MarshalType marshalType, MarshalInfo marshalInfo)
			: base(context, arrayType, marshalType, marshalInfo)
		{
		}

		public override void WriteFieldDeclaration(IGeneratedCodeWriter writer, FieldReference field, string fieldNameSuffix = null)
		{
			string text = _context.Global.Services.Naming.ForField(field) + fieldNameSuffix;
			writer.WriteLine("{0} {1}[{2}];", _elementTypeMarshalInfoWriter.MarshaledTypes[0].DecoratedName, text, _arraySize);
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", sourceVariable.Load(), "NULL");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("if ({0} > ({1})->max_length)", _arraySize, sourceVariable.Load());
				using (new BlockWriter(writer))
				{
					writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_argument_exception(\"\", \"Type could not be marshaled because the length of an embedded array instance does not match the declared length in the layout.\")"));
				}
				writer.WriteLine();
				WriteMarshalToNativeLoop(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, delegate
				{
					int arraySize = _arraySize;
					return arraySize.ToString(CultureInfo.InvariantCulture);
				});
			}
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			string arraySize = MarshaledArraySizeFor(variableName, methodParameters);
			AllocateAndStoreManagedArray(writer, destinationVariable, metadataAccess, arraySize);
			WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => arraySize);
		}

		public override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
		{
			WriteMarshalFromNativeLoop(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess, (IGeneratedCodeWriter bodyWriter) => MarshaledArraySizeFor(variableName, methodParameters));
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			WriteCleanupLoop(writer, variableName, metadataAccess, delegate
			{
				int arraySize = _arraySize;
				return arraySize.ToString(CultureInfo.InvariantCulture);
			});
		}

		public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			WriteCleanupOutVariableLoop(writer, variableName, metadataAccess, delegate
			{
				int arraySize = _arraySize;
				return arraySize.ToString(CultureInfo.InvariantCulture);
			});
		}

		public override void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
			base.WriteIncludesForFieldDeclaration(writer);
			writer.AddIncludeForTypeDefinition(_elementType);
		}
	}
}
