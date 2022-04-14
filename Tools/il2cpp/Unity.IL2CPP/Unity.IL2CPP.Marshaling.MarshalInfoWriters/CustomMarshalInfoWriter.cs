using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public abstract class CustomMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		protected readonly TypeDefinition _type;

		protected readonly MarshalType _marshalType;

		private readonly MethodDefinition _defaultConstructor;

		private readonly bool _forFieldMarshaling;

		private readonly bool _forByReferenceType;

		private readonly bool _forReturnValue;

		private readonly bool _forNativeToManagedWrapper;

		protected readonly string _marshaledTypeName;

		protected readonly string _marshaledDecoratedTypeName;

		protected readonly string _marshalToNativeFunctionName;

		protected readonly string _marshalFromNativeFunctionName;

		protected readonly string _marshalCleanupFunctionName;

		protected readonly string _marshalToNativeFunctionDeclaration;

		protected readonly string _marshalFromNativeFunctionDeclaration;

		protected readonly string _marshalCleanupFunctionDeclaration;

		private FieldDefinition[] _fields;

		private DefaultMarshalInfoWriter[] _fieldMarshalInfoWriters;

		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public sealed override string MarshalToNativeFunctionName => _marshalToNativeFunctionName;

		public sealed override string MarshalFromNativeFunctionName => _marshalFromNativeFunctionName;

		public sealed override string MarshalCleanupFunctionName => _marshalCleanupFunctionName;

		public sealed override bool HasNativeStructDefinition => true;

		protected FieldDefinition[] Fields
		{
			get
			{
				if (_fields == null)
				{
					PopulateFields();
				}
				return _fields;
			}
		}

		protected DefaultMarshalInfoWriter[] FieldMarshalInfoWriters
		{
			get
			{
				if (_fieldMarshalInfoWriters == null)
				{
					PopulateFields();
				}
				return _fieldMarshalInfoWriters;
			}
		}

		protected CustomMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType, bool forFieldMarshaling, bool forByReferenceType, bool forReturnValue, bool forNativeToManagedWrapper)
			: base(context, type)
		{
			_type = type;
			_marshalType = marshalType;
			string text = context.Global.Services.Naming.ForTypeNameOnly(type);
			string arg = "_" + MarshalingUtils.MarshalTypeToString(marshalType);
			_forFieldMarshaling = forFieldMarshaling;
			_forByReferenceType = forByReferenceType;
			_forReturnValue = forReturnValue;
			_forNativeToManagedWrapper = forNativeToManagedWrapper;
			_marshaledTypeName = GetMarshaledTypeName(type, marshalType);
			_marshaledDecoratedTypeName = (TreatAsValueType() ? _marshaledTypeName : (_marshaledTypeName + "*"));
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(_marshaledTypeName, _marshaledDecoratedTypeName)
			};
			_marshalToNativeFunctionName = $"{text}_marshal{arg}";
			_marshalFromNativeFunctionName = $"{text}_marshal{arg}_back";
			_marshalCleanupFunctionName = $"{text}_marshal{arg}_cleanup";
			_marshalToNativeFunctionDeclaration = $"IL2CPP_EXTERN_C void {MarshalToNativeFunctionName}(const {text}& unmarshaled, {_marshaledTypeName}& marshaled)";
			_marshalFromNativeFunctionDeclaration = $"IL2CPP_EXTERN_C void {_marshalFromNativeFunctionName}(const {_marshaledTypeName}& marshaled, {text}& unmarshaled)";
			_marshalCleanupFunctionDeclaration = $"IL2CPP_EXTERN_C void {MarshalCleanupFunctionName}({_marshaledTypeName}& marshaled)";
			_defaultConstructor = _type.Methods.SingleOrDefault((MethodDefinition ctor) => ctor.Name == ".ctor" && ctor.Parameters.Count == 0);
		}

		public override bool TreatAsValueType()
		{
			if (!_type.IsValueType)
			{
				if (_type.MetadataType == MetadataType.Class && _marshalType == MarshalType.PInvoke)
				{
					return _forFieldMarshaling;
				}
				return false;
			}
			return true;
		}

		public override void WriteNativeStructDefinition(IGeneratedCodeWriter writer)
		{
			TypeReference baseType = _type.BaseType;
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, baseType, _marshalType);
			string marshaledTypeName = GetMarshaledTypeName(baseType, _marshalType);
			if (baseType.IsGenericInstance && defaultMarshalInfoWriter is CustomMarshalInfoWriter customMarshalInfoWriter)
			{
				marshaledTypeName = customMarshalInfoWriter._marshaledTypeName;
			}
			writer.WriteLine("// Native definition for {0} marshalling of {1}", MarshalingUtils.MarshalTypeToNiceString(_marshalType), _type.FullName);
			foreach (FieldDefinition item in MarshalingUtils.NonStaticFieldsOf(_type))
			{
				MarshalInfoWriterFor(writer.Context, item.FieldType, _marshalType, item.MarshalInfo, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(_type), forFieldMarshaling: true).WriteIncludesForFieldDeclaration(writer);
			}
			bool flag = TypeDefinitionWriter.NeedsPackingForNative(_type) && !_type.IsExplicitLayout;
			if (flag)
			{
				writer.WriteLine("#pragma pack(push, tp, {0})", TypeDefinitionWriter.FieldLayoutPackingSizeFor(_type));
			}
			if (_type.HasGenericParameters)
			{
				writer.WriteLine("#ifndef " + _marshaledTypeName + "_define");
				writer.WriteLine("#define " + _marshaledTypeName + "_define");
			}
			string text = ((baseType != null && !baseType.IsSpecialSystemBaseType() && defaultMarshalInfoWriter.HasNativeStructDefinition) ? $" : public {marshaledTypeName}" : string.Empty);
			writer.WriteLine("struct {0}{1}", _marshaledTypeName, text);
			writer.BeginBlock();
			using (new TypeDefinitionPaddingWriter(writer, _type))
			{
				if (!_type.IsExplicitLayout)
				{
					foreach (FieldDefinition item2 in MarshalingUtils.NonStaticFieldsOf(_type))
					{
						MarshalInfoWriterFor(writer.Context, item2.FieldType, _marshalType, item2.MarshalInfo, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(_type), forFieldMarshaling: true).WriteFieldDeclaration(writer, item2);
					}
				}
				else
				{
					writer.WriteLine("union");
					writer.BeginBlock();
					foreach (FieldDefinition item3 in MarshalingUtils.NonStaticFieldsOf(_type))
					{
						WriteFieldWithExplicitLayout(writer, item3, forAlignmentOnly: false);
						WriteFieldWithExplicitLayout(writer, item3, forAlignmentOnly: true);
					}
					writer.EndBlock(semicolon: true);
				}
			}
			writer.EndBlock(semicolon: true);
			if (_type.HasGenericParameters)
			{
				writer.WriteLine("#endif");
			}
			if (flag)
			{
				writer.WriteLine("#pragma pack(pop, tp)");
			}
		}

		private void WriteFieldWithExplicitLayout(IGeneratedCodeWriter writer, FieldDefinition field, bool forAlignmentOnly)
		{
			int num = TypeDefinitionWriter.AlignmentPackingSizeFor(_type);
			bool flag = (!forAlignmentOnly && TypeDefinitionWriter.NeedsPackingForNative(_type)) || (num != -1 && num != 0);
			string text = (forAlignmentOnly ? "_forAlignmentOnly" : string.Empty);
			int offset = field.Offset;
			if (flag)
			{
				writer.WriteLine("#pragma pack(push, tp, {0})", forAlignmentOnly ? num : TypeDefinitionWriter.FieldLayoutPackingSizeFor(_type));
			}
			writer.WriteLine("struct");
			writer.BeginBlock();
			if (offset > 0)
			{
				writer.WriteLine("char {0}[{1}];", _context.Global.Services.Naming.ForFieldPadding(field) + text, offset);
			}
			MarshalInfoWriterFor(writer.Context, field.FieldType, _marshalType, field.MarshalInfo, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(_type), forFieldMarshaling: true).WriteFieldDeclaration(writer, field, text);
			writer.EndBlock(semicolon: true);
			if (flag)
			{
				writer.WriteLine("#pragma pack(pop, tp)");
			}
		}

		public override void WriteMarshalFunctionDeclarations(IGeneratedMethodCodeWriter writer)
		{
			if (!_type.HasGenericParameters)
			{
				writer.AddForwardDeclaration(_type);
				writer.AddForwardDeclaration($"struct {_marshaledTypeName}");
				writer.WriteLine();
				writer.AddForwardDeclaration($"struct {_context.Global.Services.Naming.ForTypeNameOnly(_type)};");
				writer.AddForwardDeclaration($"struct {_marshaledTypeName};");
				writer.WriteLine();
				writer.AddMethodForwardDeclaration(_marshalToNativeFunctionDeclaration);
				writer.AddMethodForwardDeclaration(_marshalFromNativeFunctionDeclaration);
				writer.AddMethodForwardDeclaration(_marshalCleanupFunctionDeclaration);
			}
		}

		public override void WriteMarshalFunctionDefinitions(IGeneratedMethodCodeWriter writer)
		{
			if (!_type.HasGenericParameters)
			{
				for (int i = 0; i < Fields.Length; i++)
				{
					FieldMarshalInfoWriters[i].WriteIncludesForMarshaling(writer);
				}
				writer.WriteLine("// Conversion methods for marshalling of: {0}", _type.FullName);
				WriteMarshalToNativeMethodDefinition(writer);
				WriteMarshalFromNativeMethodDefinition(writer);
				writer.WriteLine("// Conversion method for clean up from marshalling of: {0}", _type.FullName);
				WriteMarshalCleanupFunction(writer);
				if (_marshalType == MarshalType.PInvoke)
				{
					writer.Context.Global.Collectors.TypeMarshallingFunctions.Add(writer.Context, _type);
				}
			}
		}

		protected abstract void WriteMarshalCleanupFunction(IGeneratedMethodCodeWriter writer);

		protected abstract void WriteMarshalFromNativeMethodDefinition(IGeneratedMethodCodeWriter writer);

		protected abstract void WriteMarshalToNativeMethodDefinition(IGeneratedMethodCodeWriter writer);

		protected static DefaultMarshalInfoWriter MarshalInfoWriterFor(MinimalContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forFieldMarshaling = false)
		{
			return MarshalDataCollector.MarshalInfoWriterFor(context, type, marshalType, marshalInfo, useUnicodeCharSet, forByReferenceType: false, forFieldMarshaling);
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			base.WriteIncludesForMarshaling(writer);
			WriteMarshalFunctionDeclarations(writer);
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			if (_type.IsValueType)
			{
				writer.WriteLine(MarshalToNativeFunctionName + "(" + sourceVariable.Load() + ", " + destinationVariable + ");");
				return;
			}
			writer.WriteLine("if (" + sourceVariable.Load() + " != NULL)");
			using (new BlockWriter(writer))
			{
				if (_forByReferenceType && _forNativeToManagedWrapper)
				{
					writer.WriteLine("if (" + Emit.AddressOf(destinationVariable) + " == NULL)");
					using (new BlockWriter(writer))
					{
						writer.WriteLine(Emit.AddressOf(destinationVariable) + " = il2cpp_codegen_marshal_allocate<" + _marshaledTypeName + ">();");
					}
					writer.WriteLine();
				}
				writer.WriteLine(MarshalToNativeFunctionName + "(" + Emit.Dereference(sourceVariable.Load()) + ", " + destinationVariable + ");");
			}
		}

		public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			if (!TreatAsValueType())
			{
				string text = $"_{CleanVariableName(variableName)}_empty";
				writer.WriteLine(_context.Global.Services.Naming.ForVariable(_type) + " " + text + " = (" + Emit.AddressOf(variableName) + " != NULL)");
				writer.WriteLine("    ? " + Emit.NewObj(_context, _type, metadataAccess));
				writer.WriteLine("    : NULL;");
				return text;
			}
			return base.WriteMarshalEmptyVariableFromNative(writer, variableName, methodParameters, metadataAccess);
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			if (TreatAsValueType())
			{
				if (_type.MetadataType == MetadataType.Class)
				{
					writer.WriteLine(destinationVariable.Store(Emit.NewObj(_context, _type, metadataAccess)));
					if (callConstructor)
					{
						EmitCallToConstructor(writer, _type, _defaultConstructor, destinationVariable, metadataAccess);
					}
					writer.WriteLine("{0}({1}, {2});", MarshalFromNativeFunctionName, variableName, destinationVariable.Dereferenced.Load());
				}
				else
				{
					writer.WriteLine("{0}({1}, {2});", MarshalFromNativeFunctionName, variableName, destinationVariable.Load());
				}
			}
			else
			{
				writer.WriteLine("if ({0} != {1})", destinationVariable.Load(), "NULL");
				writer.BeginBlock();
				if (callConstructor)
				{
					EmitCallToConstructor(writer, _type, _defaultConstructor, destinationVariable, metadataAccess);
				}
				writer.WriteLine("{0}({1}, *{2});", MarshalFromNativeFunctionName, variableName, destinationVariable.Load());
				writer.EndBlock();
			}
		}

		public override string WriteMarshalReturnValueToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, IRuntimeMetadataAccess metadataAccess)
		{
			if (TreatAsValueType())
			{
				return base.WriteMarshalReturnValueToNative(writer, sourceVariable, metadataAccess);
			}
			string text = $"_{sourceVariable.GetNiceName()}_marshaled";
			writer.WriteLine(_marshaledDecoratedTypeName + " " + text + ";");
			writer.WriteLine("if (" + sourceVariable.Load() + " != NULL)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(text + " = il2cpp_codegen_marshal_allocate<" + _marshaledTypeName + ">();");
				writer.WriteLine(MarshalToNativeFunctionName + "(" + Emit.Dereference(sourceVariable.Load()) + ", " + Emit.Dereference(text) + ");");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(text + " = NULL;");
				return text;
			}
		}

		public override void WriteDeclareAndAllocateObject(IGeneratedCodeWriter writer, string unmarshaledVariableName, string marshaledVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			if (_type.IsValueType)
			{
				base.WriteDeclareAndAllocateObject(writer, unmarshaledVariableName, marshaledVariableName, metadataAccess);
			}
			else
			{
				EmitNewObject(writer, _type, unmarshaledVariableName, marshaledVariableName, !TreatAsValueType(), metadataAccess);
			}
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			if (TreatAsValueType())
			{
				writer.WriteLine("{0}({1});", MarshalCleanupFunctionName, variableName);
				return;
			}
			writer.WriteLine("if (" + Emit.AddressOf(variableName) + " != NULL)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(MarshalCleanupFunctionName + "(" + variableName + ");");
			}
		}

		public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			if (TreatAsValueType())
			{
				base.WriteMarshalCleanupOutVariable(writer, variableName, metadataAccess, managedVariableName);
				return;
			}
			writer.WriteLine("if (" + Emit.AddressOf(variableName) + " != NULL)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(MarshalCleanupFunctionName + "(" + variableName + ");");
				if (_forByReferenceType || _forReturnValue)
				{
					writer.WriteLine("il2cpp_codegen_marshal_free(" + Emit.AddressOf(variableName) + ");");
				}
			}
		}

		public override string DecorateVariable(string unmarshaledParameterName, string marshaledVariableName)
		{
			if (!TreatAsValueType())
			{
				if (unmarshaledParameterName == null)
				{
					throw new InvalidOperationException("CustomMarshalInfoWriter does not support decorating return value parameters.");
				}
				if (_forByReferenceType)
				{
					return Emit.AddressOf(marshaledVariableName);
				}
				return string.Format("{0} != {2} ? {1} : {2}", unmarshaledParameterName, Emit.AddressOf(marshaledVariableName), "NULL");
			}
			return marshaledVariableName;
		}

		public override string UndecorateVariable(string variableName)
		{
			if (!TreatAsValueType())
			{
				return "*" + variableName;
			}
			return variableName;
		}

		internal static void EmitCallToConstructor(IGeneratedCodeWriter writer, TypeDefinition typeDefinition, MethodDefinition defaultConstructor, ManagedMarshalValue destinationVariable, IRuntimeMetadataAccess metadataAccess)
		{
			if (defaultConstructor != null)
			{
				if (MethodSignatureWriter.NeedsHiddenMethodInfo(writer.Context, defaultConstructor, MethodCallType.Normal))
				{
					writer.WriteLine("{0}({1}, {2});", metadataAccess.Method(defaultConstructor), destinationVariable.Load(), metadataAccess.HiddenMethodInfo(defaultConstructor));
				}
				else
				{
					writer.WriteLine("{0}({1});", metadataAccess.Method(defaultConstructor), destinationVariable.Load());
				}
			}
			else
			{
				writer.WriteStatement(Emit.RaiseManagedException($"il2cpp_codegen_get_missing_method_exception(\"A parameterless constructor is required for type '{typeDefinition.FullName}'.\")"));
			}
		}

		internal static void EmitNewObject(IGeneratedCodeWriter writer, TypeReference typeReference, string unmarshaledVariableName, string marshaledVariableName, bool emitNullCheck, IRuntimeMetadataAccess metadataAccess)
		{
			if (emitNullCheck)
			{
				writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(typeReference) + " " + unmarshaledVariableName + " = (" + marshaledVariableName + " != NULL)");
				writer.WriteLine("    ? " + Emit.NewObj(writer.Context, typeReference, metadataAccess));
				writer.WriteLine("    : NULL;");
			}
			else
			{
				writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(typeReference) + " " + unmarshaledVariableName + " = " + Emit.NewObj(writer.Context, typeReference, metadataAccess) + ";");
			}
		}

		private string GetMarshaledTypeName(TypeReference type, MarshalType marshalType)
		{
			return $"{_context.Global.Services.Naming.ForTypeNameOnly(type)}_marshaled_{MarshalingUtils.MarshalTypeToString(marshalType)}";
		}

		private void PopulateFields()
		{
			_fields = MarshalingUtils.GetMarshaledFields(_context, _type, _marshalType).ToArray();
			_fieldMarshalInfoWriters = MarshalingUtils.GetFieldMarshalInfoWriters(_context, _type, _marshalType).ToArray();
		}
	}
}
