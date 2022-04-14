using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public class TypeDefinitionWriter
	{
		public enum FieldType
		{
			Instance,
			Static,
			ThreadStatic
		}

		private struct FieldWriteInstruction
		{
			public FieldDefinition Field { get; private set; }

			public string FieldName { get; private set; }

			public string FieldTypeName { get; private set; }

			public TypeReference FieldType { get; private set; }

			public FieldWriteInstruction(ReadOnlyContext context, FieldDefinition field, string fieldTypeName, TypeReference fieldType)
			{
				this = default(FieldWriteInstruction);
				Field = field;
				FieldName = context.Global.Services.Naming.ForField(field);
				FieldTypeName = fieldTypeName;
				FieldType = fieldType;
			}
		}

		private struct ComFieldWriteInstruction
		{
			public TypeReference InterfaceType { get; private set; }

			public ComFieldWriteInstruction(TypeReference interfaceType)
			{
				this = default(ComFieldWriteInstruction);
				InterfaceType = interfaceType;
			}
		}

		private const char kArrayFirstIndexName = 'i';

		private const string kMonoItemsName = "vector";

		public void WriteTypeDefinitionFor(SourceWritingContext context, TypeReference type, IGeneratedCodeWriter writer)
		{
			TypeDefinition typeDefinition = type.Resolve();
			context.Global.Services.ErrorInformation.CurrentType = typeDefinition;
			if (context.Global.Parameters.EnableErrorMessageTest)
			{
				ErrorTypeAndMethod.ThrowIfIsErrorType(context, type.Resolve());
			}
			VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinition(typeDefinition);
			CollectIncludes(writer, type, typeDefinition);
			if (type is TypeDefinition typeDefinition2 && typeDefinition2.HasGenericParameters)
			{
				return;
			}
			writer.WriteLine();
			writer.WriteCommentedLine(type.FullName);
			bool flag = false;
			if (!type.IsSystemObject() && !type.IsSystemArray() && !type.IsIl2CppComObject(context))
			{
				flag = MarshalingUtils.IsBlittable(type, null, MarshalType.ManagedLayout, useUnicodeCharset: true);
				bool flag2 = !typeDefinition.IsExplicitLayout && NeedsPackingForManaged(typeDefinition, flag);
				if (flag2)
				{
					writer.WriteLine("#pragma pack(push, tp, {0})", AlignmentPackingSizeFor(typeDefinition));
				}
				writer.WriteLine("struct {0} {1}", context.Global.Services.Naming.ForTypeNameOnly(type), GetBaseTypeDeclaration(context, type));
				writer.BeginBlock();
				WriteGuid(writer, type);
				WriteFieldsWithAccessors(context, writer, type, flag);
				writer.EndBlock(semicolon: true);
				if (flag2)
				{
					writer.WriteLine("#pragma pack(pop, tp)");
				}
			}
			if (typeDefinition.Fields.Any((FieldDefinition f) => f.IsNormalStatic()) || typeDefinition.StoresNonFieldsInStaticFields())
			{
				writer.WriteLine();
				if (context.Global.Parameters.UsingTinyClassLibraries)
				{
					writer.WriteLine("extern void* {0};", context.Global.Services.Naming.ForStaticFieldsStructStorage(type));
				}
				writer.WriteLine("struct {0}", context.Global.Services.Naming.ForStaticFieldsStruct(type));
				writer.BeginBlock();
				WriteFieldsWithAccessors(context, writer, type, flag, FieldType.Static);
				writer.EndBlock(semicolon: true);
			}
			if (typeDefinition.Fields.Any((FieldDefinition f) => f.IsThreadStatic()))
			{
				writer.WriteLine();
				writer.WriteLine("struct {0}", context.Global.Services.Naming.ForThreadFieldsStruct(type));
				writer.BeginBlock();
				WriteFieldsWithAccessors(context, writer, type, flag, FieldType.ThreadStatic);
				writer.EndBlock(semicolon: true);
			}
			writer.WriteLine();
			WriteNativeStructDefinitions(type, writer);
		}

		private static void VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinition(TypeDefinition typeDefinition)
		{
			VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinitionRecusrive(typeDefinition, new HashSet<TypeDefinition>());
		}

		private static void VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinitionRecusrive(TypeDefinition typeDefinition, HashSet<TypeDefinition> parentTypes)
		{
			if (typeDefinition == null || parentTypes.Contains(typeDefinition) || !typeDefinition.IsValueType)
			{
				return;
			}
			foreach (FieldDefinition field in typeDefinition.Fields)
			{
				parentTypes.Add(typeDefinition);
				if (field.IsStatic && field.FieldType.IsNullable())
				{
					GenericInstanceType genericInstanceType = (GenericInstanceType)field.FieldType;
					if (parentTypes.Contains(genericInstanceType.GenericArguments[0]))
					{
						throw new NotSupportedException($"The type '{typeDefinition}' contains a static field which has a type of '{field.FieldType}'. IL2CPP does not support conversion of this recursively defined type.");
					}
				}
				else
				{
					VerifyTypeDoesNotHaveRecursiveStaticNullableFieldDefinitionRecusrive(field.FieldType.Resolve(), parentTypes);
				}
				parentTypes.Remove(typeDefinition);
			}
		}

		public static void WriteArrayTypeDefinition(SourceWritingContext context, ArrayType type, ICodeWriter writer)
		{
			context.Global.Services.ErrorInformation.CurrentType = type.Resolve();
			if (context.Global.Parameters.EnableErrorMessageTest)
			{
				ErrorTypeAndMethod.ThrowIfIsErrorType(context, type.Resolve());
			}
			writer.WriteCommentedLine(type.FullName);
			writer.WriteLine("struct {0} {1}", context.Global.Services.Naming.ForTypeNameOnly(type), GetBaseTypeDeclaration(context, type));
			writer.BeginBlock();
			WriteArrayFieldsWithAccessors(writer, type);
			writer.EndBlock(semicolon: true);
		}

		private static void WriteNativeStructDefinitions(TypeReference type, IGeneratedCodeWriter writer)
		{
			MarshalType[] marshalTypesForMarshaledType = MarshalingUtils.GetMarshalTypesForMarshaledType(writer.Context, type);
			foreach (MarshalType marshalType in marshalTypesForMarshaledType)
			{
				MarshalDataCollector.MarshalInfoWriterFor(writer.Context, type, marshalType, null, MarshalingUtils.UseUnicodeAsDefaultMarshalingForFields(type)).WriteNativeStructDefinition(writer);
			}
		}

		private void CollectIncludes(IGeneratedCodeWriter writer, TypeReference type, TypeDefinition typeDefinition)
		{
			if (type.HasGenericParameters)
			{
				return;
			}
			if (type is ArrayType)
			{
				writer.AddIncludeForTypeDefinition(writer.Context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Array"));
				return;
			}
			TypeResolver typeResolver = new TypeResolver(type as GenericInstanceType);
			TypeReference typeReference = typeResolver.Resolve(typeDefinition.BaseType);
			if (typeReference != null)
			{
				writer.AddIncludeForTypeDefinition(typeReference);
			}
			foreach (FieldDefinition field in typeDefinition.Fields)
			{
				TypeReference typeReference2 = typeResolver.Resolve(field.FieldType);
				if (!TypeReferenceEqualityComparer.AreEqual(typeReference2, type) && !typeReference2.IsPointer)
				{
					writer.AddIncludesForTypeReference(typeReference2);
				}
			}
			foreach (FieldDefinition field2 in typeDefinition.Fields)
			{
				if (field2.FieldType is PointerType pointerType)
				{
					writer.AddForwardDeclaration(typeResolver.Resolve(pointerType.ElementType));
				}
			}
			foreach (TypeReference allFactoryType in type.GetAllFactoryTypes(writer.Context))
			{
				writer.AddForwardDeclaration(allFactoryType);
			}
			if (!typeDefinition.IsDelegate())
			{
				return;
			}
			List<MethodDefinition> list = new List<MethodDefinition>(3);
			list.Add(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "Invoke"));
			MethodDefinition methodDefinition = typeDefinition.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "BeginInvoke");
			MethodDefinition methodDefinition2 = typeDefinition.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "EndInvoke");
			if (methodDefinition != null)
			{
				list.Add(methodDefinition);
			}
			if (methodDefinition2 != null)
			{
				list.Add(methodDefinition2);
			}
			foreach (MethodDefinition item in list)
			{
				writer.AddIncludesForTypeReference(typeResolver.Resolve(GenericParameterResolver.ResolveReturnTypeIfNeeded(item)));
				foreach (ParameterDefinition parameter in item.Parameters)
				{
					TypeReference typeReference3 = typeResolver.Resolve(GenericParameterResolver.ResolveParameterTypeIfNeeded(item, parameter));
					writer.AddIncludesForTypeReference(typeReference3);
					if (typeReference3.IsByReference)
					{
						ByReferenceType byReferenceType = (ByReferenceType)typeReference3;
						if (byReferenceType.ElementType.IsValueType())
						{
							typeReference3 = byReferenceType.ElementType;
						}
					}
					if (typeReference3.IsValueType())
					{
						writer.AddIncludeForTypeDefinition(typeReference3);
					}
				}
			}
		}

		private static bool NeedsPackingForManaged(TypeDefinition typeDefinition, bool isUnmanaged)
		{
			if (isUnmanaged)
			{
				return NeedsPacking(typeDefinition);
			}
			return false;
		}

		internal static bool NeedsPackingForNative(TypeDefinition typeDefinition)
		{
			if (!NeedsPacking(typeDefinition))
			{
				return typeDefinition.IsExplicitLayout;
			}
			return true;
		}

		private static bool NeedsPacking(TypeDefinition typeDefinition)
		{
			if ((typeDefinition.IsSequentialLayout || typeDefinition.IsExplicitLayout) && typeDefinition.PackingSize != 0)
			{
				return typeDefinition.PackingSize != -1;
			}
			return false;
		}

		internal static int FieldLayoutPackingSizeFor(TypeDefinition typeDefinition)
		{
			if (typeDefinition.IsExplicitLayout)
			{
				return 1;
			}
			return typeDefinition.PackingSize;
		}

		internal static int AlignmentPackingSizeFor(TypeDefinition typeDefinition)
		{
			return typeDefinition.PackingSize;
		}

		private static void WriteGuid(IGeneratedCodeWriter writer, TypeReference type)
		{
			if (type.HasCLSID() || type.HasIID(writer.Context))
			{
				WriteAccessSpecifier(writer, "public");
				string text = (type.HasCLSID() ? "CLSID" : "IID");
				writer.WriteLine("static const Il2CppGuid " + text + ";");
				writer.WriteLine();
				writer.Context.Global.Collectors.InteropGuids.Add(writer.Context, type);
			}
		}

		private static bool FieldMatches(FieldDefinition field, FieldType fieldType)
		{
			switch (fieldType)
			{
			case FieldType.Static:
				return field.IsNormalStatic();
			case FieldType.ThreadStatic:
				return field.IsThreadStatic();
			default:
				return !field.IsStatic;
			}
		}

		private static void WriteFieldsWithAccessors(SourceWritingContext context, IGeneratedCodeWriter writer, TypeReference type, bool isUnmanagedType, FieldType fieldType = FieldType.Instance)
		{
			TypeDefinition typeDefinition = type.Resolve();
			List<FieldWriteInstruction> fieldWriteInstructions = MakeFieldWriteInstructionsForType(context, type, typeDefinition, fieldType);
			List<ComFieldWriteInstruction> list = MakeComFieldWriteInstructionsForType(context, type, typeDefinition, fieldType);
			WriteAccessSpecifier(writer, "public");
			if (fieldType == FieldType.Instance)
			{
				using (new TypeDefinitionPaddingWriter(writer, typeDefinition))
				{
					WriteFields(writer, typeDefinition, isUnmanagedType, fieldType, fieldWriteInstructions, list);
				}
			}
			else
			{
				WriteFields(writer, typeDefinition, isUnmanagedType, fieldType, fieldWriteInstructions, list);
			}
			writer.WriteLine();
			WriteAccessSpecifier(writer, "public");
			WriteFieldGettersAndSetters(writer, type, fieldWriteInstructions);
			WriteComFieldGetters(writer, type, list);
		}

		private static void WriteArrayFieldsWithAccessors(ICodeWriter writer, ArrayType arrayType)
		{
			TypeReference elementType = arrayType.ElementType;
			string elementTypeName = writer.Context.Global.Services.Naming.ForVariable(elementType);
			WriteAccessSpecifier(writer, "public");
			writer.WriteLine("ALIGN_FIELD (8) {0} {1}[1];", writer.Context.Global.Services.Naming.ForVariable(arrayType.ElementType), ArrayNaming.ForArrayItems());
			writer.WriteLine();
			WriteAccessSpecifier(writer, "public");
			WriteArrayAccessors(writer, arrayType, elementType, elementTypeName, emitArrayBoundsCheck: true);
			WriteArrayAccessors(writer, arrayType, elementType, elementTypeName, emitArrayBoundsCheck: false);
			if (arrayType.Rank > 1)
			{
				WriteArrayAccessorsForMultiDimensionalArray(writer, arrayType.Rank, elementType, elementTypeName, emitArrayBoundsCheck: true);
				WriteArrayAccessorsForMultiDimensionalArray(writer, arrayType.Rank, elementType, elementTypeName, emitArrayBoundsCheck: false);
			}
		}

		private static void WriteArrayAccessors(ICodeWriter writer, ArrayType arrayType, TypeReference elementType, string elementTypeName, bool emitArrayBoundsCheck)
		{
			writer.WriteLine("inline {0} {1}({2} {3}) const", elementTypeName, ArrayNaming.ForArrayItemGetter(emitArrayBoundsCheck), ArrayNaming.ForArrayIndexType(), ArrayNaming.ForArrayIndexName(), ArrayNaming.ForArrayItems());
			string block = (arrayType.IsVector ? Emit.ArrayBoundsCheck("this", "index") : Emit.MultiDimensionalArrayBoundsCheck(writer.Context, "this", "index", arrayType.Rank));
			using (new BlockWriter(writer))
			{
				if (emitArrayBoundsCheck)
				{
					writer.WriteLine(block);
				}
				writer.WriteLine("return {0}[{1}];", ArrayNaming.ForArrayItems(), ArrayNaming.ForArrayIndexName());
			}
			writer.WriteLine("inline {0}* {1}({2} {3})", elementTypeName, ArrayNaming.ForArrayItemAddressGetter(emitArrayBoundsCheck), ArrayNaming.ForArrayIndexType(), ArrayNaming.ForArrayIndexName(), ArrayNaming.ForArrayItems());
			using (new BlockWriter(writer))
			{
				if (emitArrayBoundsCheck)
				{
					writer.WriteLine(block);
				}
				writer.WriteLine("return {0} + {1};", ArrayNaming.ForArrayItems(), ArrayNaming.ForArrayIndexName());
			}
			writer.WriteLine("inline void {0}({1} {2}, {3} value)", ArrayNaming.ForArrayItemSetter(emitArrayBoundsCheck), ArrayNaming.ForArrayIndexType(), ArrayNaming.ForArrayIndexName(), elementTypeName);
			using (new BlockWriter(writer))
			{
				if (emitArrayBoundsCheck)
				{
					writer.WriteLine(block);
				}
				writer.WriteLine("{0}[{1}] = value;", ArrayNaming.ForArrayItems(), ArrayNaming.ForArrayIndexName());
				writer.WriteWriteBarrierIfNeeded(elementType, $"{ArrayNaming.ForArrayItems()} + {ArrayNaming.ForArrayIndexName()}", "value");
			}
		}

		private static void WriteArrayAccessorsForMultiDimensionalArray(ICodeWriter writer, int rank, TypeReference elementType, string elementTypeName, bool emitArrayBoundsCheck)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string text = BuildArrayIndexParameters(stringBuilder, rank);
			string block = BuildArrayIndexCalculation(stringBuilder, rank);
			string block2 = BuildArrayBoundsVariables(writer.Context, stringBuilder, rank, emitArrayBoundsCheck, writer.IndentationLevel + 1);
			writer.WriteLine("inline {0} {1}({2}) const", elementTypeName, ArrayNaming.ForArrayItemGetter(emitArrayBoundsCheck), text);
			using (new BlockWriter(writer))
			{
				writer.WriteLine(block2);
				writer.WriteLine(block);
				writer.WriteLine("return {0}[{1}];", ArrayNaming.ForArrayItems(), ArrayNaming.ForArrayIndexName());
			}
			writer.WriteLine("inline {0}* {1}({2})", elementTypeName, ArrayNaming.ForArrayItemAddressGetter(emitArrayBoundsCheck), text);
			using (new BlockWriter(writer))
			{
				writer.WriteLine(block2);
				writer.WriteLine(block);
				writer.WriteLine("return {0} + {1};", ArrayNaming.ForArrayItems(), ArrayNaming.ForArrayIndexName());
			}
			writer.WriteLine("inline void {0}({1}, {2} value)", ArrayNaming.ForArrayItemSetter(emitArrayBoundsCheck), text, elementTypeName);
			using (new BlockWriter(writer))
			{
				writer.WriteLine(block2);
				writer.WriteLine(block);
				writer.WriteLine("{0}[{1}] = value;", ArrayNaming.ForArrayItems(), ArrayNaming.ForArrayIndexName());
				writer.WriteWriteBarrierIfNeeded(elementType, $"{ArrayNaming.ForArrayItems()} + {ArrayNaming.ForArrayIndexName()}", "value");
			}
		}

		private static string BuildArrayIndexParameters(StringBuilder stringBuilder, int rank)
		{
			stringBuilder.Clear();
			char c = (char)(105 + rank);
			for (char c2 = 'i'; c2 < c; c2 = (char)(c2 + 1))
			{
				stringBuilder.AppendFormat("{0} {1}", ArrayNaming.ForArrayIndexType(), c2);
				if (c2 != c - 1)
				{
					stringBuilder.Append(", ");
				}
			}
			return stringBuilder.ToString();
		}

		private static string BuildArrayBoundsVariables(ReadOnlyContext context, StringBuilder stringBuilder, int rank, bool emitArrayBoundsCheck, int indentationLevel)
		{
			stringBuilder.Clear();
			string text = new string('\t', indentationLevel);
			bool flag = false;
			for (int i = 0; i < rank; i++)
			{
				if (i != 0 || emitArrayBoundsCheck)
				{
					string text2 = BoundVariableNameFor(i);
					if (flag)
					{
						stringBuilder.Append(text);
					}
					if (!context.Global.Parameters.UsingTinyBackend)
					{
						stringBuilder.AppendFormat("{0} {1} = bounds[{2}].length;{3}", ArrayNaming.ForArrayIndexType(), text2, i, Environment.NewLine);
					}
					else
					{
						stringBuilder.AppendFormat("{0} {1} = bounds[{2}];{3}", ArrayNaming.ForArrayIndexType(), text2, i, Environment.NewLine);
					}
					if (emitArrayBoundsCheck)
					{
						stringBuilder.AppendFormat("{0}{1}{2}", text, Emit.MultiDimensionalArrayBoundsCheck(text2, ((char)(105 + i)).ToString()), Environment.NewLine);
					}
					flag = true;
				}
			}
			return stringBuilder.ToString();
		}

		private static string BoundVariableNameFor(int i)
		{
			return $"{(char)(105 + i)}Bound";
		}

		private static string BuildArrayIndexCalculation(StringBuilder stringBuilder, int rank)
		{
			stringBuilder.Clear();
			stringBuilder.AppendFormat("{0} {1} = ", ArrayNaming.ForArrayIndexType(), ArrayNaming.ForArrayIndexName());
			for (int i = 0; i < rank - 2; i++)
			{
				stringBuilder.Append('(');
			}
			for (int j = 0; j < rank; j++)
			{
				stringBuilder.Append((char)(105 + j));
				if (j != 0 && j != rank - 1)
				{
					stringBuilder.Append(')');
				}
				if (j != rank - 1)
				{
					stringBuilder.AppendFormat(" * {0} + ", BoundVariableNameFor(j + 1));
				}
			}
			stringBuilder.Append(';');
			return stringBuilder.ToString();
		}

		private static List<FieldWriteInstruction> MakeFieldWriteInstructionsForType(SourceWritingContext context, TypeReference type, TypeDefinition typeDefinition, FieldType fieldType)
		{
			List<FieldWriteInstruction> list = new List<FieldWriteInstruction>();
			TypeResolver typeResolver = TypeResolver.For(type);
			foreach (FieldDefinition item in typeDefinition.Fields.Where((FieldDefinition f) => FieldMatches(f, fieldType)))
			{
				string fieldTypeName;
				TypeReference typeReference;
				if (item.DeclaringType.FullName == "System.Delegate" && item.Name == "method_ptr" && !context.Global.Parameters.UsingTinyClassLibraries)
				{
					FieldReference fieldReference = item;
					fieldTypeName = "Il2CppMethodPointer";
					typeReference = item.FieldType;
				}
				else
				{
					FieldReference fieldReference = new FieldReference(item.Name, item.FieldType, type);
					typeReference = typeResolver.ResolveFieldType(fieldReference);
					fieldTypeName = context.Global.Services.Naming.ForVariable(typeReference);
				}
				list.Add(new FieldWriteInstruction(context, item, fieldTypeName, typeReference));
			}
			return list;
		}

		private static List<ComFieldWriteInstruction> MakeComFieldWriteInstructionsForType(ReadOnlyContext context, TypeReference type, TypeDefinition typeDefinition, FieldType fieldType)
		{
			if (fieldType != FieldType.Static || !typeDefinition.IsComOrWindowsRuntimeType(context) || !type.DerivesFrom(context, context.Global.Services.TypeProvider.Il2CppComObjectTypeReference, checkInterfaces: false))
			{
				return new List<ComFieldWriteInstruction>();
			}
			TypeReference[] array = type.GetAllFactoryTypes(context).ToArray();
			List<ComFieldWriteInstruction> list = new List<ComFieldWriteInstruction>(array.Length);
			TypeResolver typeResolver = TypeResolver.For(type);
			bool flag = false;
			TypeReference[] array2 = array;
			foreach (TypeReference typeReference in array2)
			{
				if (typeReference.IsIActivationFactory(context))
				{
					flag = true;
				}
				list.Add(new ComFieldWriteInstruction(typeResolver.Resolve(typeReference)));
			}
			if (!flag && list.Count > 0)
			{
				list.Insert(0, new ComFieldWriteInstruction(context.Global.Services.TypeProvider.IActivationFactoryTypeReference));
			}
			return list;
		}

		private static void WriteFields(ICppCodeWriter writer, TypeDefinition typeDefinition, bool isUnmanagedType, FieldType fieldType, List<FieldWriteInstruction> fieldWriteInstructions, List<ComFieldWriteInstruction> comFieldWriteInstructions)
		{
			bool flag = typeDefinition.IsExplicitLayout && fieldType == FieldType.Instance;
			if (flag)
			{
				writer.WriteLine("union");
				writer.BeginBlock();
			}
			foreach (FieldWriteInstruction fieldWriteInstruction in fieldWriteInstructions)
			{
				WriteFieldInstruction(writer, typeDefinition, isUnmanagedType, flag, fieldWriteInstruction);
				if (flag)
				{
					WriteFieldInstruction(writer, typeDefinition, isUnmanagedType, explicitLayout: true, fieldWriteInstruction, forAlignmentOnly: true);
				}
			}
			if (flag)
			{
				writer.EndBlock(semicolon: true);
			}
			foreach (ComFieldWriteInstruction comFieldWriteInstruction in comFieldWriteInstructions)
			{
				writer.WriteCommentedLine($"Cached pointer to {comFieldWriteInstruction.InterfaceType.FullName}");
				writer.WriteLine("{0}* {1};", writer.Context.Global.Services.Naming.ForTypeNameOnly(comFieldWriteInstruction.InterfaceType), writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldName(comFieldWriteInstruction.InterfaceType));
			}
		}

		private static void WriteFieldInstruction(ICppCodeWriter writer, TypeDefinition typeDefinition, bool isUnmanagedType, bool explicitLayout, FieldWriteInstruction instruction, bool forAlignmentOnly = false)
		{
			int num = AlignmentPackingSizeFor(typeDefinition);
			string text = (forAlignmentOnly ? "_forAlignmentOnly" : string.Empty);
			bool flag = !forAlignmentOnly || NeedsPackingForManaged(typeDefinition, isUnmanagedType);
			if (explicitLayout)
			{
				if (flag)
				{
					writer.WriteLine("#pragma pack(push, tp, {0})", forAlignmentOnly ? num : FieldLayoutPackingSizeFor(typeDefinition));
				}
				writer.WriteLine("struct");
				writer.BeginBlock();
				int offset = instruction.Field.Offset;
				if (offset > 0)
				{
					writer.WriteLine("char {0}[{1}];", writer.Context.Global.Services.Naming.ForFieldPadding(instruction.Field) + text, offset);
				}
			}
			if (!forAlignmentOnly)
			{
				writer.WriteCommentedLine(instruction.Field.FullName);
			}
			writer.WriteStatement(instruction.FieldTypeName + " " + instruction.FieldName + text);
			if (explicitLayout)
			{
				writer.EndBlock(semicolon: true);
				if (flag)
				{
					writer.WriteLine("#pragma pack(pop, tp)");
				}
			}
		}

		private static void WriteFieldGettersAndSetters(ICppCodeWriter writer, TypeReference declaringType, List<FieldWriteInstruction> fieldWriteInstructions)
		{
			for (int i = 0; i < fieldWriteInstructions.Count; i++)
			{
				FieldWriteInstruction fieldWriteInstruction = fieldWriteInstructions[i];
				if (!writer.Context.Global.Parameters.UsingTinyBackend)
				{
					writer.WriteLine("inline static int32_t {0}() {{ return static_cast<int32_t>(offsetof({1}, {2})); }}", writer.Context.Global.Services.Naming.ForFieldOffsetGetter(fieldWriteInstruction.Field), GetDeclaringTypeStructName(writer.Context, declaringType, fieldWriteInstruction.Field), writer.Context.Global.Services.Naming.ForField(fieldWriteInstruction.Field));
				}
				writer.WriteLine("inline " + fieldWriteInstruction.FieldTypeName + " " + writer.Context.Global.Services.Naming.ForFieldGetter(fieldWriteInstruction.Field) + "() const { return " + fieldWriteInstruction.FieldName + "; }");
				writer.WriteLine("inline " + fieldWriteInstruction.FieldTypeName + "* " + writer.Context.Global.Services.Naming.ForFieldAddressGetter(fieldWriteInstruction.Field) + "() { return &" + fieldWriteInstruction.FieldName + "; }");
				writer.WriteLine("inline void " + writer.Context.Global.Services.Naming.ForFieldSetter(fieldWriteInstruction.Field) + "(" + fieldWriteInstruction.FieldTypeName + " value)");
				using (new BlockWriter(writer))
				{
					writer.WriteLine("{0} = value;", fieldWriteInstruction.FieldName);
					writer.WriteWriteBarrierIfNeeded(fieldWriteInstruction.FieldType, Emit.AddressOf(fieldWriteInstruction.FieldName), "value");
				}
				if (i != fieldWriteInstructions.Count - 1)
				{
					writer.WriteLine();
				}
			}
		}

		private static void WriteComFieldGetters(IGeneratedCodeWriter writer, TypeReference declaringType, List<ComFieldWriteInstruction> fieldWriteInstructions)
		{
			for (int i = 0; i < fieldWriteInstructions.Count; i++)
			{
				TypeReference interfaceType = fieldWriteInstructions[i].InterfaceType;
				string text = writer.Context.Global.Services.Naming.ForTypeNameOnly(interfaceType);
				string text2 = writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldName(interfaceType);
				string text3 = writer.Context.Global.Services.Naming.ForInteropReturnValue();
				if (i != 0)
				{
					writer.WriteLine();
				}
				writer.AddIncludeForTypeDefinition(interfaceType);
				writer.WriteLine("inline " + text + "* " + writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(interfaceType) + "()");
				using (new BlockWriter(writer))
				{
					writer.WriteLine(text + "* " + text3 + " = " + text2 + ";");
					writer.WriteLine("if (" + text3 + " == NULL)");
					using (new BlockWriter(writer))
					{
						if (interfaceType.IsIActivationFactory(writer.Context))
						{
							writer.WriteLine("il2cpp::utils::StringView<Il2CppNativeChar> className(IL2CPP_NATIVE_STRING(\"" + declaringType.FullName + "\"));");
							writer.WriteStatement(Emit.Assign(text3, "il2cpp_codegen_windows_runtime_get_activation_factory(className)"));
						}
						else
						{
							string text4 = Emit.Call(writer.Context.Global.Services.Naming.ForComTypeInterfaceFieldGetter(writer.Context.Global.Services.TypeProvider.IActivationFactoryTypeReference));
							string left = string.Format("const il2cpp_hresult_t " + writer.Context.Global.Services.Naming.ForInteropHResultVariable());
							string right = string.Format(text4 + "->QueryInterface(" + text + "::IID, reinterpret_cast<void**>(&" + text3 + "))");
							writer.WriteStatement(Emit.Assign(left, right));
							writer.WriteStatement(Emit.Call("il2cpp_codegen_com_raise_exception_if_failed", writer.Context.Global.Services.Naming.ForInteropHResultVariable(), interfaceType.IsComInterface() ? "true" : "false"));
						}
						writer.WriteLine();
						writer.WriteLine("if (il2cpp_codegen_atomic_compare_exchange_pointer((void**)" + Emit.AddressOf(text2) + ", " + text3 + ", NULL) != NULL)");
						using (new BlockWriter(writer))
						{
							writer.WriteLine(text3 + "->Release();");
							writer.WriteStatement(Emit.Assign(text3, text2));
						}
					}
					writer.WriteLine("return " + text3 + ";");
				}
			}
		}

		private static string GetDeclaringTypeStructName(ReadOnlyContext context, TypeReference declaringType, FieldReference field)
		{
			if (field.IsThreadStatic())
			{
				return context.Global.Services.Naming.ForThreadFieldsStruct(declaringType);
			}
			if (field.IsNormalStatic())
			{
				return context.Global.Services.Naming.ForStaticFieldsStruct(declaringType);
			}
			return context.Global.Services.Naming.ForTypeNameOnly(declaringType);
		}

		private static string GetBaseTypeDeclaration(ReadOnlyContext context, TypeReference type)
		{
			if (type.IsArray)
			{
				ArrayType arrayType = (ArrayType)type;
				if (!context.Global.Parameters.UsingTinyBackend || arrayType.IsVector)
				{
					return " : public RuntimeArray";
				}
				return string.Format(" : public {0}<{1}>", "Il2CppMultidimensionalArray", arrayType.Rank);
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition.BaseType != null && typeDefinition.BaseType.FullName != "System.Enum" && (typeDefinition.BaseType.FullName != "System.ValueType" || typeDefinition.FullName == "System.Enum"))
			{
				TypeResolver typeResolver = TypeResolver.For(type);
				return string.Format(" : public " + context.Global.Services.Naming.ForType(typeResolver.Resolve(typeDefinition.BaseType)));
			}
			return string.Empty;
		}

		private static void WriteAccessSpecifier(ICodeWriter writer, string accessSpecifier)
		{
			writer.Dedent();
			writer.WriteLine("{0}:", accessSpecifier);
			writer.Indent();
		}

		internal static void WriteStaticFieldDefinitionsForTinyProfile(IGeneratedMethodCodeWriter writer, TypeReference type)
		{
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition.Fields.Any((FieldDefinition f) => f.IsNormalStatic()) || typeDefinition.StoresNonFieldsInStaticFields())
			{
				writer.WriteLine("void* {0} = (void*)sizeof({1});", writer.Context.Global.Services.Naming.ForStaticFieldsStructStorage(type), writer.Context.Global.Services.Naming.ForStaticFieldsStruct(type));
			}
		}

		internal static void WriteStaticFieldRVAExternsForTinyProfile(IGeneratedMethodCodeWriter writer, TypeReference type)
		{
			foreach (FieldDefinition field in type.Resolve().Fields)
			{
				if (field.Attributes.HasFlag(FieldAttributes.HasFieldRVA))
				{
					writer.WriteLine("extern const uint8_t " + writer.Context.Global.Services.Naming.ForStaticFieldsRVAStructStorage(field) + "[];");
				}
			}
		}
	}
}
