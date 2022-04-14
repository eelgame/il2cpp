using System;
using System.Collections.Generic;
using System.Globalization;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public abstract class ArrayMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		protected enum ArraySizeOptions
		{
			UseArraySize,
			UseSizeParameterIndex,
			UseFirstMarshaledType
		}

		protected readonly ArrayType _arrayType;

		protected readonly int _arraySize;

		protected readonly int _sizeParameterIndex;

		protected readonly ArraySizeOptions _arraySizeSelection;

		protected readonly TypeReference _elementType;

		protected readonly MarshalInfo _marshalInfo;

		protected readonly MarshalType _marshalType;

		protected readonly DefaultMarshalInfoWriter _elementTypeMarshalInfoWriter;

		protected readonly NativeType _nativeElementType;

		protected readonly string _arrayMarshaledTypeName;

		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public override string NativeSize => "-1";

		protected bool NeedsTrailingNullElement
		{
			get
			{
				if (_elementTypeMarshalInfoWriter is StringMarshalInfoWriter)
				{
					return _marshalType != MarshalType.WindowsRuntime;
				}
				return false;
			}
		}

		protected ArrayMarshalInfoWriter(ReadOnlyContext context, ArrayType type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharset = false)
			: base(context, type)
		{
			_marshalInfo = marshalInfo;
			_marshalType = marshalType;
			_arrayType = type;
			_elementType = type.ElementType;
			MarshalInfo marshalInfo2 = null;
			ArrayMarshalInfo arrayMarshalInfo = marshalInfo as ArrayMarshalInfo;
			FixedArrayMarshalInfo fixedArrayMarshalInfo = marshalInfo as FixedArrayMarshalInfo;
			_arraySize = 1;
			_nativeElementType = NativeType.None;
			if (arrayMarshalInfo != null)
			{
				_arraySize = arrayMarshalInfo.Size;
				_sizeParameterIndex = arrayMarshalInfo.SizeParameterIndex;
				if (_arraySize == 0 || (_arraySize == -1 && _sizeParameterIndex >= 0))
				{
					_arraySizeSelection = ArraySizeOptions.UseSizeParameterIndex;
				}
				else
				{
					_arraySizeSelection = ArraySizeOptions.UseArraySize;
				}
				_nativeElementType = arrayMarshalInfo.ElementType;
				marshalInfo2 = new MarshalInfo(_nativeElementType);
			}
			else if (fixedArrayMarshalInfo != null)
			{
				_arraySize = fixedArrayMarshalInfo.Size;
				_nativeElementType = fixedArrayMarshalInfo.ElementType;
				marshalInfo2 = new MarshalInfo(_nativeElementType);
			}
			if (_arraySize == -1)
			{
				_arraySize = 1;
			}
			_elementTypeMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, _elementType, marshalType, marshalInfo2, useUnicodeCharset, forByReferenceType: false, forFieldMarshaling: true);
			if (_elementTypeMarshalInfoWriter.MarshaledTypes.Length > 1)
			{
				throw new InvalidOperationException($"ArrayMarshalInfoWriter cannot marshal arrays of {_elementType.FullName}.");
			}
			_arrayMarshaledTypeName = _elementTypeMarshalInfoWriter.MarshaledTypes[0].DecoratedName + "*";
			if (marshalType == MarshalType.WindowsRuntime)
			{
				string text = context.Global.Services.Naming.ForVariable(context.Global.Services.TypeProvider.UInt32TypeReference);
				_arraySizeSelection = ArraySizeOptions.UseFirstMarshaledType;
				_marshaledTypes = new MarshaledType[2]
				{
					new MarshaledType(text, text, "ArraySize"),
					new MarshaledType(_arrayMarshaledTypeName, _arrayMarshaledTypeName)
				};
			}
			else
			{
				_marshaledTypes = new MarshaledType[1]
				{
					new MarshaledType(_arrayMarshaledTypeName, _arrayMarshaledTypeName)
				};
			}
			if (_elementTypeMarshalInfoWriter is StringMarshalInfoWriter stringMarshalInfoWriter)
			{
				_nativeElementType = stringMarshalInfoWriter.NativeType;
			}
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
			writer.AddIncludeForTypeDefinition(_arrayType);
			_elementTypeMarshalInfoWriter.WriteIncludesForMarshaling(writer);
			base.WriteIncludesForMarshaling(writer);
		}

		public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
		{
			string text = $"_{variableName.GetNiceName()}_marshaled";
			WriteNativeVariableDeclarationOfType(writer, text);
			writer.WriteLine("if ({0} != {1})", variableName.Load(), "NULL");
			using (new BlockWriter(writer))
			{
				string text2 = WriteArraySizeFromManagedArray(writer, variableName, text);
				string text3 = (NeedsTrailingNullElement ? $"({text2} + 1)" : text2);
				writer.WriteLine("{0} = il2cpp_codegen_marshal_allocate_array<{1}>({2});", text, _elementTypeMarshalInfoWriter.MarshaledTypes[0].DecoratedName, text3);
				writer.WriteStatement(Emit.Memset(writer.Context, text, 0, text3 + " * sizeof(" + _elementTypeMarshalInfoWriter.MarshaledTypes[0].DecoratedName + ")"));
				return text;
			}
		}

		public override void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			if (_marshalType != MarshalType.WindowsRuntime)
			{
				return;
			}
			writer.WriteLine("if ({0} != {1})", sourceVariable.Load(), "NULL");
			using (new BlockWriter(writer))
			{
				WriteMarshalToNativeLoop(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, (IGeneratedCodeWriter bodyWriter) => MarshaledArraySizeFor(destinationVariable, methodParameters));
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				WriteAssignNullArray(writer, destinationVariable);
			}
		}

		public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{CleanVariableName(variableName)}_empty";
			ManagedMarshalValue managedMarshalValue = new ManagedMarshalValue(_context, text);
			writer.WriteVariable(_typeRef, text);
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				string length = MarshaledArraySizeFor(variableName, methodParameters);
				writer.WriteLine(managedMarshalValue.Store("reinterpret_cast<" + _context.Global.Services.Naming.ForVariable(_arrayType) + ">(" + Emit.NewSZArray(_context, _arrayType, _elementType, length, metadataAccess) + ")"));
				return text;
			}
		}

		private void WriteLoop(IGeneratedMethodCodeWriter outerWriter, Func<IGeneratedMethodCodeWriter, string> writeLoopCountVariable, Action<IGeneratedMethodCodeWriter> writeLoopBody)
		{
			outerWriter.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine("for (int32_t i = 0; i < ARRAY_LENGTH_AS_INT32({0}); i++)", writeLoopCountVariable(bodyWriter));
				bodyWriter.BeginBlock();
			}, writeLoopBody, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.EndBlock();
			});
		}

		protected void WriteMarshalToNativeLoop(IGeneratedMethodCodeWriter outerWriter, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess, Func<IGeneratedCodeWriter, string> writeLoopCountVariable)
		{
			WriteLoop(outerWriter, writeLoopCountVariable, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				_elementTypeMarshalInfoWriter.WriteMarshalVariableToNative(bodyWriter, new ManagedMarshalValue(_context, sourceVariable, "i"), _elementTypeMarshalInfoWriter.UndecorateVariable($"({destinationVariable})[i]"), managedVariableName, metadataAccess);
			});
		}

		protected void WriteMarshalFromNativeLoop(IGeneratedMethodCodeWriter outerWriter, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess, Func<IGeneratedCodeWriter, string> writeLoopCountVariable)
		{
			WriteLoop(outerWriter, writeLoopCountVariable, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				string variableName2 = _elementTypeMarshalInfoWriter.UndecorateVariable($"({variableName})[i]");
				string value = _elementTypeMarshalInfoWriter.WriteMarshalVariableFromNative(bodyWriter, variableName2, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, metadataAccess);
				bodyWriter.WriteLine("{0};", Emit.StoreArrayElement(destinationVariable.Load(), "i", value, useArrayBoundsCheck: false));
			});
		}

		protected void WriteCleanupLoop(IGeneratedMethodCodeWriter outerWriter, string variableName, IRuntimeMetadataAccess metadataAccess, Func<IGeneratedCodeWriter, string> writeLoopCountVariable)
		{
			WriteLoop(outerWriter, writeLoopCountVariable, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				_elementTypeMarshalInfoWriter.WriteMarshalCleanupVariable(bodyWriter, _elementTypeMarshalInfoWriter.UndecorateVariable($"({variableName})[i]"), metadataAccess);
			});
		}

		protected void WriteCleanupOutVariableLoop(IGeneratedMethodCodeWriter outerWriter, string variableName, IRuntimeMetadataAccess metadataAccess, Func<IGeneratedCodeWriter, string> writeLoopCountVariable)
		{
			WriteLoop(outerWriter, writeLoopCountVariable, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				_elementTypeMarshalInfoWriter.WriteMarshalCleanupOutVariable(bodyWriter, _elementTypeMarshalInfoWriter.UndecorateVariable($"({variableName})[i]"), metadataAccess);
			});
		}

		protected void AllocateAndStoreManagedArray(ICodeWriter writer, ManagedMarshalValue destinationVariable, IRuntimeMetadataAccess metadataAccess, string arraySizeVariable)
		{
			writer.WriteLine(destinationVariable.Store("reinterpret_cast<" + _context.Global.Services.Naming.ForVariable(_arrayType) + ">(" + Emit.NewSZArray(_context, _arrayType, _elementType, arraySizeVariable, metadataAccess) + ")"));
		}

		protected void AllocateAndStoreNativeArray(ICodeWriter writer, string destinationVariable, string arraySize)
		{
			if (NeedsTrailingNullElement)
			{
				writer.WriteLine("{0} = il2cpp_codegen_marshal_allocate_array<{1}>({2} + 1);", destinationVariable, _elementTypeMarshalInfoWriter.MarshaledTypes[0].DecoratedName, arraySize);
				writer.WriteLine("({0})[{1}] = {2};", destinationVariable, arraySize, "NULL");
			}
			else
			{
				writer.WriteLine("{0} = il2cpp_codegen_marshal_allocate_array<{1}>({2});", destinationVariable, _elementTypeMarshalInfoWriter.MarshaledTypes[0].DecoratedName, arraySize);
			}
		}

		protected void WriteAssignNullArray(ICodeWriter writer, string destinationVariable)
		{
			if (_arraySizeSelection == ArraySizeOptions.UseFirstMarshaledType)
			{
				writer.WriteLine("{0}{1} = 0;", destinationVariable, _marshaledTypes[0].VariableName);
			}
			writer.WriteLine("{0} = {1};", destinationVariable, "NULL");
		}

		protected string WriteArraySizeFromManagedArray(IGeneratedCodeWriter writer, ManagedMarshalValue managedArray, string nativeArray)
		{
			string text;
			if (_arraySizeSelection != ArraySizeOptions.UseFirstMarshaledType)
			{
				text = $"_{managedArray.GetNiceName()}_Length";
				writer.WriteLine("{0} {1} = ({2})->max_length;", "il2cpp_array_size_t", text, managedArray.Load());
				return text;
			}
			text = nativeArray + _marshaledTypes[0].VariableName;
			writer.WriteLine("{0} = static_cast<uint32_t>(({1})->max_length);", text, managedArray.Load());
			return $"static_cast<int32_t>({text})";
		}

		public abstract override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess);

		public abstract override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess);

		public abstract override void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess);

		public abstract override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null);

		protected string MarshaledArraySizeFor(string nativeArray, IList<MarshaledParameter> methodParameters)
		{
			switch (_arraySizeSelection)
			{
			case ArraySizeOptions.UseArraySize:
			{
				int arraySize = _arraySize;
				return arraySize.ToString(CultureInfo.InvariantCulture);
			}
			case ArraySizeOptions.UseSizeParameterIndex:
			{
				if (methodParameters == null)
				{
					int arraySize = _arraySize;
					return arraySize.ToString(CultureInfo.InvariantCulture);
				}
				MarshaledParameter marshaledParameter = methodParameters[_sizeParameterIndex];
				if (marshaledParameter.ParameterType.MetadataType != MetadataType.Int32)
				{
					if (marshaledParameter.ParameterType.MetadataType == MetadataType.ByReference)
					{
						return $"static_cast<int32_t>({Emit.Dereference(marshaledParameter.NameInGeneratedCode)})";
					}
					return $"static_cast<int32_t>({marshaledParameter.NameInGeneratedCode})";
				}
				return marshaledParameter.NameInGeneratedCode;
			}
			case ArraySizeOptions.UseFirstMarshaledType:
				return $"static_cast<int32_t>({nativeArray}{MarshaledTypes[0].VariableName})";
			default:
				throw new InvalidOperationException($"Unknown ArraySizeOptions: {_arraySizeSelection}");
			}
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
