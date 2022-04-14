using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Attributes
{
	public class AttributesSupport
	{
		private readonly IGeneratedMethodCodeWriter _writer;

		private AttributesSupport(IGeneratedMethodCodeWriter writer)
		{
			_writer = writer;
		}

		public static string AttributeGeneratorName(INamingService namingService, AssemblyDefinition assemblyDefinition)
		{
			return "g_" + namingService.ForAssembly(assemblyDefinition) + "_AttributeGenerators";
		}

		public static string AttributeGeneratorType()
		{
			return "const CustomAttributesCacheGenerator";
		}

		public static ReadOnlyAttributeWriterOutput WriteAttributes(INamingService namingService, IGeneratedMethodCodeWriter writer, AssemblyDefinition assemblyDefinition, ReadOnlyCollectedAttributeSupportData attributeData)
		{
			AttributesSupport attributesSupport = new AttributesSupport(writer);
			foreach (AttributeData attributeDatum in attributeData.AttributeData)
			{
				attributesSupport.WriteCustomAttributesCacheGeneratorFor(attributeDatum);
			}
			AttributeCollection attributeCollection = attributeData.AttributeCollection;
			writer.WriteTable(AttributeGeneratorType(), AttributeGeneratorName(namingService, assemblyDefinition), attributeCollection.InitializerFunctionNames, (string a) => a, externTable: true);
			return new ReadOnlyAttributeWriterOutput(assemblyDefinition, attributeCollection.AttributeTypeRanges, attributeCollection.AttributeTypes);
		}

		private void WriteCustomAttributesCacheGeneratorFor(AttributeData data)
		{
			CustomAttribute[] attributeTypes = data.AttributeTypes;
			foreach (CustomAttribute customAttribute in attributeTypes)
			{
				_writer.AddIncludeForTypeDefinition(customAttribute.AttributeType);
				foreach (TypeReference item in ExtractTypeReferencesFromCustomAttributeArguments(customAttribute.ConstructorArguments))
				{
					if (item != null)
					{
						_writer.AddIncludeForTypeDefinition(item);
					}
				}
				foreach (TypeReference item2 in ExtractTypeReferencesFromCustomAttributeArguments(customAttribute.Fields.Select((CustomAttributeNamedArgument f) => f.Argument)))
				{
					if (item2 != null)
					{
						_writer.AddIncludeForTypeDefinition(item2);
					}
				}
				foreach (TypeReference item3 in ExtractTypeReferencesFromCustomAttributeArguments(customAttribute.Properties.Select((CustomAttributeNamedArgument p) => p.Argument)))
				{
					if (item3 != null)
					{
						_writer.AddIncludeForTypeDefinition(item3);
					}
				}
			}
			try
			{
				string functionName = data.FunctionName;
				_writer.WriteMethodWithMetadataInitialization($"static void {functionName}(CustomAttributesCache* cache)", functionName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
				{
					WriteMethodBody(bodyWriter, data.AttributeTypes, metadataAccess);
				}, functionName, null);
			}
			catch (Exception)
			{
				_writer.ErrorOccurred = true;
				throw;
			}
		}

		private static void WriteMethodBody(IGeneratedMethodCodeWriter writer, IEnumerable<CustomAttribute> customAttributes, IRuntimeMetadataAccess metadataAccess)
		{
			int num = 0;
			foreach (CustomAttribute customAttribute in customAttributes)
			{
				writer.BeginBlock();
				DeclareTempLocals(writer, customAttribute, metadataAccess);
				TypeDefinition typeDefinition = customAttribute.AttributeType.Resolve();
				writer.WriteLine("{0} tmp = ({0})cache->attributes[{1}];", writer.Context.Global.Services.Naming.ForVariable(typeDefinition), num);
				writer.WriteLine("{0}({1}, {2});", metadataAccess.Method(customAttribute.Constructor), CustomAttributeConstructorFormattedArgumentsFor(writer.Context, customAttribute, metadataAccess), metadataAccess.HiddenMethodInfo(customAttribute.Constructor));
				List<FieldDefinition> source = GatherFieldsFromTypeAndBaseTypes(typeDefinition);
				foreach (CustomAttributeNamedArgument fieldArgument in customAttribute.Fields)
				{
					writer.WriteLine("tmp->{0}({1});", writer.Context.Global.Services.Naming.ForFieldSetter(source.First((FieldDefinition p) => p.Name == fieldArgument.Name)), FormatAttributeValue(writer.Context, fieldArgument.Argument, TempName(fieldArgument), metadataAccess));
				}
				List<PropertyDefinition> source2 = GatherPropertiesFromTypeAndBaseTypes(typeDefinition);
				if (source2.Any())
				{
					foreach (CustomAttributeNamedArgument propertyArgument in customAttribute.Properties)
					{
						MethodDefinition setMethod = source2.First((PropertyDefinition p) => p.Name == propertyArgument.Name).SetMethod;
						writer.WriteLine("{0}(tmp, {1}, {2});", metadataAccess.Method(setMethod), FormatAttributeValue(writer.Context, propertyArgument.Argument, TempName(propertyArgument), metadataAccess), metadataAccess.HiddenMethodInfo(setMethod));
					}
				}
				writer.EndBlock();
				num++;
			}
		}

		private static List<PropertyDefinition> GatherPropertiesFromTypeAndBaseTypes(TypeDefinition attributeType)
		{
			List<PropertyDefinition> list = new List<PropertyDefinition>();
			for (TypeDefinition typeDefinition = attributeType; typeDefinition != null; typeDefinition = ((typeDefinition.BaseType != null) ? typeDefinition.BaseType.Resolve() : null))
			{
				list.AddRange(typeDefinition.Properties);
			}
			return list;
		}

		private static List<FieldDefinition> GatherFieldsFromTypeAndBaseTypes(TypeDefinition attributeType)
		{
			List<FieldDefinition> list = new List<FieldDefinition>();
			for (TypeDefinition typeDefinition = attributeType; typeDefinition != null; typeDefinition = ((typeDefinition.BaseType != null) ? typeDefinition.BaseType.Resolve() : null))
			{
				list.AddRange(typeDefinition.Fields);
			}
			return list;
		}

		private static void DeclareTempLocals(IGeneratedMethodCodeWriter writer, CustomAttribute attribute, IRuntimeMetadataAccess metadataAccess)
		{
			DeclareTempLocalsForBoxing(writer, attribute, metadataAccess);
			DeclareTempLocalsForArrays(writer, attribute, metadataAccess);
		}

		private static void DeclareTempLocalsForBoxing(IGeneratedMethodCodeWriter writer, CustomAttribute attribute, IRuntimeMetadataAccess metadataAccess)
		{
			for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
			{
				CustomAttributeArgument argument = attribute.ConstructorArguments[i];
				if (ValueNeedsBoxing(argument))
				{
					WriteStoreValueInTempLocal(writer, TempName(argument, i), (CustomAttributeArgument)argument.Value, metadataAccess);
				}
			}
			foreach (CustomAttributeNamedArgument item in attribute.Fields.Where(ValueNeedsBoxing))
			{
				WriteStoreValueInTempLocal(writer, TempName(item), (CustomAttributeArgument)item.Argument.Value, metadataAccess);
			}
			foreach (CustomAttributeNamedArgument item2 in attribute.Properties.Where(ValueNeedsBoxing))
			{
				WriteStoreValueInTempLocal(writer, TempName(item2), (CustomAttributeArgument)item2.Argument.Value, metadataAccess);
			}
		}

		private static bool ValueNeedsBoxing(CustomAttributeNamedArgument namedArgument)
		{
			return ValueNeedsBoxing(namedArgument.Argument);
		}

		private static bool ValueNeedsBoxing(CustomAttributeArgument argument)
		{
			if (argument.Type.MetadataType != MetadataType.Object)
			{
				return false;
			}
			CustomAttributeArgument customAttributeArgument = (CustomAttributeArgument)argument.Value;
			if (customAttributeArgument.Type.MetadataType == MetadataType.String)
			{
				return false;
			}
			if (customAttributeArgument.Type.MetadataType == MetadataType.Class && customAttributeArgument.Value is TypeReference)
			{
				return false;
			}
			return true;
		}

		private static string TempName(CustomAttributeNamedArgument argument)
		{
			return "_tmp_" + argument.Name;
		}

		private static string TempName(CustomAttributeArgument argument, int index)
		{
			return "_tmp_" + index;
		}

		private static void WriteStoreValueInTempLocal(ICodeWriter writer, string variableName, CustomAttributeArgument argument, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("{0} {1} = {2};", StorageTypeFor(argument.Value), variableName, FormatForAssignment(argument.Value, metadataAccess));
		}

		private static void DeclareTempLocalsForArrays(IGeneratedMethodCodeWriter writer, CustomAttribute attribute, IRuntimeMetadataAccess metadataAccess)
		{
			for (int i = 0; i < attribute.ConstructorArguments.Count; i++)
			{
				CustomAttributeArgument customAttributeArgument = attribute.ConstructorArguments[i];
				if (ValueIsArray(customAttributeArgument))
				{
					WriteStoreArrayInTempLocal(writer, TempName(customAttributeArgument, i), customAttributeArgument, metadataAccess);
				}
			}
			foreach (CustomAttributeNamedArgument item in attribute.Fields.Where(ValueIsArray))
			{
				WriteStoreArrayInTempLocal(writer, TempName(item), item.Argument, metadataAccess);
			}
			foreach (CustomAttributeNamedArgument item2 in attribute.Properties.Where(ValueIsArray))
			{
				WriteStoreArrayInTempLocal(writer, TempName(item2), item2.Argument, metadataAccess);
			}
		}

		private static bool ValueIsArray(CustomAttributeNamedArgument namedArgument)
		{
			return ValueIsArray(namedArgument.Argument);
		}

		private static bool ValueIsArray(CustomAttributeArgument argument)
		{
			return argument.Type.MetadataType == MetadataType.Array;
		}

		private static void WriteStoreArrayInTempLocal(IGeneratedMethodCodeWriter writer, string varName, CustomAttributeArgument attributeValue, IRuntimeMetadataAccess metadataAccess)
		{
			CustomAttributeArgument[] array = (CustomAttributeArgument[])attributeValue.Value;
			ArrayType arrayType = (ArrayType)attributeValue.Type;
			if (array == null)
			{
				writer.WriteLine(Statement.Expression(Emit.Assign($"{writer.Context.Global.Services.Naming.ForVariable(arrayType)} {varName}", "NULL")));
				return;
			}
			TypeReference elementType = arrayType.ElementType;
			writer.WriteLine(Statement.Expression(Emit.Assign($"{writer.Context.Global.Services.Naming.ForVariable(arrayType)} {varName}", Emit.NewSZArray(writer.Context, arrayType, elementType, array.Length, metadataAccess))));
			if (elementType.MetadataType == MetadataType.Object)
			{
				WriteInitializeObjectArray(writer, varName, elementType, array, metadataAccess);
			}
			else
			{
				WriteInitializeArray(writer, varName, elementType, array, metadataAccess);
			}
		}

		private static void WriteInitializeObjectArray(IGeneratedMethodCodeWriter writer, string varName, TypeReference elementType, IEnumerable<CustomAttributeArgument> arguments, IRuntimeMetadataAccess metadataAccess)
		{
			int num = 0;
			foreach (CustomAttributeArgument argument2 in arguments)
			{
				CustomAttributeArgument argument = argument2;
				if (argument.Value is CustomAttributeArgument)
				{
					argument = (CustomAttributeArgument)argument.Value;
				}
				if (argument.Type.MetadataType == MetadataType.String)
				{
					WriteStoreArrayElement(writer, varName, elementType, num, FormatForAssignment(argument.Value, metadataAccess));
				}
				else
				{
					string text = varName + "_" + num;
					WriteStoreValueInTempLocal(writer, text, argument, metadataAccess);
					WriteStoreArrayElement(writer, varName, elementType, num, Emit.Box(writer.Context, argument.Type, text, metadataAccess));
				}
				num++;
			}
		}

		private static void WriteInitializeArray(IGeneratedMethodCodeWriter writer, string varName, TypeReference elementType, IEnumerable<CustomAttributeArgument> arguments, IRuntimeMetadataAccess metadataAccess)
		{
			int num = 0;
			using (IEnumerator<CustomAttributeArgument> enumerator = arguments.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					WriteStoreArrayElement(value: FormatForAssignment(enumerator.Current.Value, metadataAccess), writer: writer, varName: varName, elementType: elementType, index: num++);
				}
			}
		}

		private static void WriteStoreArrayElement(ICodeWriter writer, string varName, TypeReference elementType, int index, string value)
		{
			writer.WriteLine("{0};", Emit.StoreArrayElement(varName, index.ToString(), value, useArrayBoundsCheck: false));
		}

		private static string StorageTypeFor(object value)
		{
			if (value is bool)
			{
				return "bool";
			}
			if (value is byte)
			{
				return "uint8_t";
			}
			if (value is sbyte)
			{
				return "int8_t";
			}
			if (value is char)
			{
				return "Il2CppChar";
			}
			if (value is short)
			{
				return "int16_t";
			}
			if (value is ushort)
			{
				return "uint16_t";
			}
			if (value is int)
			{
				return "int32_t";
			}
			if (value is uint)
			{
				return "uint32_t";
			}
			if (value is long)
			{
				return "int64_t";
			}
			if (value is ulong)
			{
				return "uint64_t";
			}
			if (value is float)
			{
				return "float";
			}
			if (value is double)
			{
				return "double";
			}
			if (value is TypeReference)
			{
				return "Type_t*";
			}
			if (value is Array)
			{
				throw new NotSupportedException("IL2CPP does not support attributes with object arguments that are array types.");
			}
			throw new ArgumentException("Unsupported CustomAttribute value of type " + value.GetType().FullName);
		}

		private static string FormatForAssignment(object value, IRuntimeMetadataAccess metadataAccess)
		{
			if (value == null)
			{
				return "NULL";
			}
			if (value is bool)
			{
				return value.ToString().ToLower();
			}
			if (value is byte)
			{
				return value.ToString().ToLower() + "U";
			}
			if (value is sbyte)
			{
				return value.ToString().ToLower();
			}
			if (value is char)
			{
				return "u'" + value?.ToString() + "'";
			}
			if (value is short)
			{
				return value.ToString().ToLower();
			}
			if (value is ushort)
			{
				return value.ToString().ToLower() + "U";
			}
			if (value is int)
			{
				return value.ToString().ToLower();
			}
			if (value is uint)
			{
				return value.ToString().ToLower() + "U";
			}
			if (value is long)
			{
				return value.ToString().ToLower();
			}
			if (value is ulong)
			{
				return value.ToString().ToLower() + "U";
			}
			if (value is float)
			{
				return Formatter.StringRepresentationFor((float)value);
			}
			if (value is double)
			{
				return Formatter.StringRepresentationFor((double)value);
			}
			if (value is string)
			{
				return StringWrapperFor((string)value);
			}
			if (value is TypeReference)
			{
				return TypeWrapperFor((TypeReference)value, metadataAccess);
			}
			throw new ArgumentException("Unsupported CustomAttribute value of type " + value.GetType().FullName);
		}

		private static string StringWrapperFor(string value)
		{
			if (value == null)
			{
				return "NULL";
			}
			if (value.Contains("Microsoft") && value.Contains("Visual") && value.Contains("\0ae"))
			{
				value = value.Replace("\0ae", "®");
			}
			byte[] bytes = Encoding.UTF8.GetBytes(value);
			StringBuilder stringBuilder = new StringBuilder();
			byte[] array = bytes;
			foreach (byte b in array)
			{
				stringBuilder.Append($"\\x{b:X}");
			}
			return $"il2cpp_codegen_string_new_wrapper(\"{stringBuilder}\")";
		}

		private static string TypeWrapperFor(TypeReference typeReference, IRuntimeMetadataAccess metadataAccess)
		{
			if (typeReference == null)
			{
				return "NULL";
			}
			return $"il2cpp_codegen_type_get_object({metadataAccess.Il2CppTypeFor(typeReference)})";
		}

		private static string CustomAttributeConstructorFormattedArgumentsFor(ReadOnlyContext context, CustomAttribute attribute, IRuntimeMetadataAccess metadataAccess)
		{
			return attribute.ConstructorArguments.Select((CustomAttributeArgument a, int index) => FormatAttributeValue(context, a, TempName(a, index), metadataAccess)).Aggregate("tmp", (string buff, string s) => buff + ", " + s);
		}

		private static string FormatAttributeValue(ReadOnlyContext context, CustomAttributeArgument argument, string tempLocalName, IRuntimeMetadataAccess metadataAccess)
		{
			MetadataType metadataType = argument.Type.MetadataType;
			if (argument.Type.MetadataType == MetadataType.Class && argument.Type.IsEnum())
			{
				metadataType = MetadataType.ValueType;
			}
			if (argument.Type.IsEnum())
			{
				TypeReference underlyingEnumType = argument.Type.GetUnderlyingEnumType();
				return FormatAttributeValue(context, underlyingEnumType.MetadataType, MetadataUtils.ChangePrimitiveType(argument.Value, underlyingEnumType), tempLocalName, metadataAccess);
			}
			return FormatAttributeValue(context, metadataType, argument.Value, tempLocalName, metadataAccess);
		}

		private static string FormatAttributeValue(ReadOnlyContext context, MetadataType metadataType, object argumentValue, string tempLocalName, IRuntimeMetadataAccess metadataAccess)
		{
			switch (metadataType)
			{
			case MetadataType.Char:
				return Formatter.FormatChar((char)argumentValue);
			case MetadataType.Boolean:
			case MetadataType.SByte:
			case MetadataType.Byte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
				return argumentValue.ToString().ToLower();
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.Int64:
			case MetadataType.UInt64:
				return argumentValue.ToString().ToLower() + "LL";
			case MetadataType.Single:
				return Formatter.StringRepresentationFor((float)argumentValue);
			case MetadataType.Double:
				return Formatter.StringRepresentationFor((double)argumentValue);
			case MetadataType.String:
				return StringWrapperFor((string)argumentValue);
			case MetadataType.Array:
				return tempLocalName;
			case MetadataType.Object:
			{
				CustomAttributeArgument customAttributeArgument = (CustomAttributeArgument)argumentValue;
				if (customAttributeArgument.Type.MetadataType == MetadataType.String)
				{
					return StringWrapperFor((string)customAttributeArgument.Value);
				}
				if (customAttributeArgument.Type.MetadataType == MetadataType.Class && customAttributeArgument.Value is TypeReference)
				{
					return TypeWrapperFor((TypeReference)customAttributeArgument.Value, metadataAccess);
				}
				return Emit.Box(context, customAttributeArgument.Type, tempLocalName, metadataAccess);
			}
			case MetadataType.Class:
				return TypeWrapperFor((TypeReference)argumentValue, metadataAccess);
			default:
				throw new NotSupportedException("Unsupported constructor argument metadata type: " + metadataType);
			}
		}

		private static IEnumerable<TypeReference> ExtractTypeReferencesFromCustomAttributeArguments(IEnumerable<CustomAttributeArgument> arguments)
		{
			if (arguments == null)
			{
				yield break;
			}
			foreach (CustomAttributeArgument argument in arguments)
			{
				MetadataType metadataType = argument.Type.MetadataType;
				if (argument.Type.MetadataType == MetadataType.Class && argument.Type.IsEnum())
				{
					metadataType = MetadataType.ValueType;
				}
				switch (metadataType)
				{
				case MetadataType.Array:
					yield return (ArrayType)argument.Type;
					yield return ((ArrayType)argument.Type).ElementType;
					foreach (TypeReference item in ExtractTypeReferencesFromCustomAttributeArguments((IEnumerable<CustomAttributeArgument>)argument.Value))
					{
						yield return item;
					}
					break;
				case MetadataType.Object:
				{
					CustomAttributeArgument value = (CustomAttributeArgument)argument.Value;
					if (value.Type.MetadataType == MetadataType.String)
					{
						yield return value.Type;
					}
					yield return value.Type;
					break;
				}
				}
			}
		}
	}
}
