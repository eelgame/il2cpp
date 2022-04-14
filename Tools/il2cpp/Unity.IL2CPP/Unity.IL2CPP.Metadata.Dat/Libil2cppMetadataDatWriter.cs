using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Metadata.Fields;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.StringLiterals;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Metadata.Dat
{
	public class Libil2cppMetadataDatWriter : MetadataDatWriterBase
	{
		protected override int Version => 27;

		public Libil2cppMetadataDatWriter(MetadataWriteContext context)
			: base(context)
		{
		}

		protected override void WriteToStream(Stream binary, MemoryStream headerStream, MemoryStream dataStream)
		{
			IStringLiteralCollection stringLiterals = _context.Global.Results.SecondaryWrite.StringLiterals;
			IMetadataCollectionResults metadataCollector = _context.Global.Results.SecondaryCollection.Metadata;
			IVTableBuilder vTableBuilder = _context.Global.Collectors.VTable;
			ReadOnlyDictionary<AssemblyDefinition, ReadOnlyAttributeWriterOutput> attributeCollections = _context.Global.Results.PrimaryWrite.AttributeWriterOutput;
			UnresolvedVirtualsTablesInfo virtualCallTables = _context.Global.Results.SecondaryWritePart3.UnresolvedVirtualsTablesInfo;
			ITypeCollectorResults typeResults = _context.Global.Results.SecondaryCollection.Types;
			IGenericMethodCollectorResults genericMethods = _context.Global.Results.PrimaryWrite.GenericMethods;
			KeyValuePair<int, int>[] fieldRefs = (from item in _context.Global.Results.SecondaryWrite.FieldReferences.Fields.ItemsSortedByValue()
				select new KeyValuePair<int, int>(typeResults.GetIndex(item.Key.DeclaringTypeData), _context.Global.Services.Naming.GetFieldIndex(item.Key.Field))).ToArray();
			using (MiniProfiler.Section("StringLiteralWriter"))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					using (MemoryStream memoryStream2 = new MemoryStream())
					{
						new StringLiteralWriter().Write(_context, memoryStream, memoryStream2, stringLiterals);
						memoryStream2.AlignTo(4);
						AddStreamAndRecordHeader("String Literals", headerStream, 256, dataStream, memoryStream);
						AddStreamAndRecordHeader("String Literal Data", headerStream, 256, dataStream, memoryStream2);
					}
				}
			}
			WriteMetadataToStream("Metadata Strings", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				byte[] array5 = metadataCollector.GetStringData().ToArray();
				stream.Write(array5, 0, array5.Length);
				stream.AlignTo(4);
			});
			WriteMetadataToStream("Events", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (var (eventDefinition2, metadataEventInfo2) in metadataCollector.GetEvents())
				{
					stream.WriteInt(metadataCollector.GetStringIndex(eventDefinition2.Name));
					stream.WriteInt(typeResults.GetIndex(metadataEventInfo2.EventType));
					stream.WriteInt((eventDefinition2.AddMethod != null) ? (metadataCollector.GetMethodIndex(eventDefinition2.AddMethod) - metadataCollector.GetMethodIndex(eventDefinition2.DeclaringType.Methods[0])) : (-1));
					stream.WriteInt((eventDefinition2.RemoveMethod != null) ? (metadataCollector.GetMethodIndex(eventDefinition2.RemoveMethod) - metadataCollector.GetMethodIndex(eventDefinition2.DeclaringType.Methods[0])) : (-1));
					stream.WriteInt((eventDefinition2.InvokeMethod != null) ? (metadataCollector.GetMethodIndex(eventDefinition2.InvokeMethod) - metadataCollector.GetMethodIndex(eventDefinition2.DeclaringType.Methods[0])) : (-1));
					stream.WriteUInt(eventDefinition2.MetadataToken.ToUInt32());
				}
			});
			WriteMetadataToStream("Properties", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (PropertyDefinition property in metadataCollector.GetProperties())
				{
					stream.WriteInt(metadataCollector.GetStringIndex(property.Name));
					stream.WriteInt((property.GetMethod != null) ? (metadataCollector.GetMethodIndex(property.GetMethod) - metadataCollector.GetMethodIndex(property.DeclaringType.Methods[0])) : (-1));
					stream.WriteInt((property.SetMethod != null) ? (metadataCollector.GetMethodIndex(property.SetMethod) - metadataCollector.GetMethodIndex(property.DeclaringType.Methods[0])) : (-1));
					stream.WriteInt((int)property.Attributes);
					stream.WriteUInt(property.MetadataToken.ToUInt32());
				}
			});
			WriteMetadataToStream("Methods", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (var (methodDefinition2, metadataMethodInfo2) in metadataCollector.GetMethods())
				{
					stream.WriteInt(metadataCollector.GetStringIndex(methodDefinition2.Name));
					stream.WriteInt(metadataCollector.GetTypeInfoIndex(methodDefinition2.DeclaringType));
					stream.WriteInt(typeResults.GetIndex(metadataMethodInfo2.ReturnType));
					stream.WriteInt(methodDefinition2.HasParameters ? metadataCollector.GetParameterIndex(methodDefinition2.Parameters[0]) : (-1));
					stream.WriteInt(metadataCollector.GetGenericContainerIndex(methodDefinition2));
					stream.WriteUInt(methodDefinition2.MetadataToken.ToUInt32());
					stream.WriteUShort((ushort)methodDefinition2.Attributes);
					stream.WriteUShort((ushort)methodDefinition2.ImplAttributes);
					stream.WriteUShort(methodDefinition2.IsStripped() ? ushort.MaxValue : ((ushort)vTableBuilder.IndexFor(_context, methodDefinition2)));
					stream.WriteUShort((ushort)methodDefinition2.Parameters.Count);
				}
			});
			WriteMetadataToStream("Parameter Default Values", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (ParameterDefaultValue parameterDefaultValue in metadataCollector.GetParameterDefaultValues())
				{
					stream.WriteInt(parameterDefaultValue.ParameterIndex);
					stream.WriteInt(typeResults.GetIndex(parameterDefaultValue.DeclaringType));
					stream.WriteInt(parameterDefaultValue.DataIndex);
				}
			});
			WriteMetadataToStream("Field Default Values", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (FieldDefaultValue fieldDefaultValue in metadataCollector.GetFieldDefaultValues())
				{
					stream.WriteInt(fieldDefaultValue.FieldIndex);
					stream.WriteInt(typeResults.GetIndex(fieldDefaultValue.RuntimeType));
					stream.WriteInt(fieldDefaultValue.DataIndex);
				}
			});
			WriteMetadataToStream("Field and Parameter Default Values Data", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				byte[] array4 = metadataCollector.GetDefaultValueData().ToArray();
				stream.Write(array4, 0, array4.Length);
				stream.AlignTo(4);
			});
			WriteMetadataToStream("Field Marshaled Sizes", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (FieldMarshaledSize fieldMarshaledSize in metadataCollector.GetFieldMarshaledSizes())
				{
					stream.WriteInt(fieldMarshaledSize.FieldIndex);
					stream.WriteInt(typeResults.GetIndex(fieldMarshaledSize.RuntimeType));
					stream.WriteInt(fieldMarshaledSize.Size);
				}
			});
			WriteMetadataToStream("Parameters", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (var (parameterDefinition2, metadataParameterInfo2) in metadataCollector.GetParameters())
				{
					stream.WriteInt(metadataCollector.GetStringIndex(parameterDefinition2.Name));
					stream.WriteUInt(parameterDefinition2.MetadataToken.ToUInt32());
					stream.WriteInt(typeResults.GetIndex(metadataParameterInfo2.ParameterType));
				}
			});
			WriteMetadataToStream("Fields", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (var (fieldDefinition2, metadataFieldInfo2) in metadataCollector.GetFields())
				{
					stream.WriteInt(metadataCollector.GetStringIndex(fieldDefinition2.Name));
					stream.WriteInt(typeResults.GetIndex(metadataFieldInfo2.FieldType));
					stream.WriteUInt(fieldDefinition2.MetadataToken.ToUInt32());
				}
			});
			WriteMetadataToStream("Generic Parameters", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (GenericParameter genericParameter in metadataCollector.GetGenericParameters())
				{
					stream.WriteInt(metadataCollector.GetGenericContainerIndex(genericParameter.Owner));
					stream.WriteInt(metadataCollector.GetStringIndex(genericParameter.Name));
					stream.WriteShort((short)((genericParameter.Constraints.Count > 0) ? metadataCollector.GetGenericParameterConstraintsStartIndex(genericParameter) : 0));
					stream.WriteShort((short)genericParameter.Constraints.Count);
					stream.WriteUShort((ushort)genericParameter.Position);
					stream.WriteUShort((ushort)genericParameter.Attributes);
				}
			});
			WriteMetadataToStream("Generic Parameter Constraints", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (IIl2CppRuntimeType genericParameterConstraint in metadataCollector.GetGenericParameterConstraints())
				{
					stream.WriteInt(typeResults.GetIndex(genericParameterConstraint));
				}
			});
			WriteMetadataToStream("Generic Containers", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (IGenericParameterProvider genericContainer in metadataCollector.GetGenericContainers())
				{
					stream.WriteInt((genericContainer.GenericParameterType == GenericParameterType.Method) ? metadataCollector.GetMethodIndex((MethodDefinition)genericContainer) : metadataCollector.GetTypeInfoIndex((TypeDefinition)genericContainer));
					stream.WriteInt(genericContainer.GenericParameters.Count);
					stream.WriteInt((genericContainer.GenericParameterType == GenericParameterType.Method) ? 1 : 0);
					stream.WriteInt(metadataCollector.GetGenericParameterIndex(genericContainer.GenericParameters[0]));
				}
			});
			WriteMetadataToStream("Nested Types", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (int nestedType in metadataCollector.GetNestedTypes())
				{
					stream.WriteInt(nestedType);
				}
			});
			WriteMetadataToStream("Interfaces", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (IIl2CppRuntimeType @interface in metadataCollector.GetInterfaces())
				{
					stream.WriteInt(typeResults.GetIndex(@interface));
				}
			});
			WriteMetadataToStream("VTables", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (MethodReference vTableMethod in metadataCollector.GetVTableMethods())
				{
					stream.WriteUInt((vTableMethod != null) ? MetadataUtils.GetEncodedMethodMetadataUsageIndex(vTableMethod, metadataCollector, genericMethods) : 0u);
				}
			});
			WriteMetadataToStream("Interface Offsets", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (InterfaceOffset interfaceOffset in metadataCollector.GetInterfaceOffsets())
				{
					stream.WriteInt(typeResults.GetIndex(interfaceOffset.RuntimeType));
					stream.WriteInt(interfaceOffset.Offset);
				}
			});
			WriteMetadataToStream("Type Definitions", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo> typeInfo in metadataCollector.GetTypeInfos())
				{
					Extensions.Deconstruct(typeInfo, out var key, out var value2);
					TypeDefinition typeDefinition2 = key;
					MetadataTypeDefinitionInfo metadataTypeDefinitionInfo = value2;
					int value3 = 0;
					int num4 = 0;
					if (!typeDefinition2.IsInterface || typeDefinition2.IsComOrWindowsRuntimeType(_context) || _context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(typeDefinition2) != null)
					{
						VTable vTable = vTableBuilder.VTableFor(_context, typeDefinition2);
						value3 = vTable.Slots.Count;
						num4 = vTable.InterfaceOffsets.Count;
					}
					int count = typeDefinition2.Methods.Count;
					int value4 = ((count > 0) ? metadataCollector.GetMethodIndex(typeDefinition2.Methods[0]) : (-1));
					stream.WriteInt(metadataCollector.GetStringIndex(typeDefinition2.Name));
					stream.WriteInt(metadataCollector.GetStringIndex(typeDefinition2.Namespace));
					stream.WriteInt(typeResults.GetIndex(metadataTypeDefinitionInfo.RuntimeType));
					stream.WriteInt((metadataTypeDefinitionInfo.DeclaringRuntimeType != null) ? typeResults.GetIndex(metadataTypeDefinitionInfo.DeclaringRuntimeType) : (-1));
					stream.WriteInt((metadataTypeDefinitionInfo.BaseRuntimeType != null) ? typeResults.GetIndex(metadataTypeDefinitionInfo.BaseRuntimeType) : (-1));
					stream.WriteInt((metadataTypeDefinitionInfo.ElementRuntimeType != null) ? typeResults.GetIndex(metadataTypeDefinitionInfo.ElementRuntimeType) : (-1));
					stream.WriteInt(metadataCollector.GetGenericContainerIndex(typeDefinition2));
					stream.WriteUInt((uint)typeDefinition2.Attributes);
					stream.WriteInt(typeDefinition2.HasFields ? metadataCollector.GetFieldIndex(typeDefinition2.Fields[0]) : (-1));
					stream.WriteInt(value4);
					stream.WriteInt(typeDefinition2.HasEvents ? metadataCollector.GetEventIndex(typeDefinition2.Events[0]) : (-1));
					stream.WriteInt(typeDefinition2.HasProperties ? metadataCollector.GetPropertyIndex(typeDefinition2.Properties[0]) : (-1));
					stream.WriteInt(typeDefinition2.HasNestedTypes ? metadataCollector.GetNestedTypesStartIndex(typeDefinition2) : (-1));
					stream.WriteInt(typeDefinition2.HasInterfaces ? metadataCollector.GetInterfacesStartIndex(typeDefinition2) : (-1));
					stream.WriteInt(metadataCollector.GetVTableMethodsStartIndex(typeDefinition2));
					stream.WriteInt((num4 > 0) ? metadataCollector.GetInterfaceOffsetsStartIndex(typeDefinition2) : (-1));
					stream.WriteIntAsUShort(count);
					stream.WriteIntAsUShort(typeDefinition2.Properties.Count);
					stream.WriteIntAsUShort(typeDefinition2.Fields.Count);
					stream.WriteIntAsUShort(typeDefinition2.Events.Count);
					stream.WriteIntAsUShort(typeDefinition2.NestedTypes.Count);
					stream.WriteIntAsUShort(value3);
					stream.WriteIntAsUShort(typeDefinition2.Interfaces.Count);
					stream.WriteIntAsUShort(num4);
					int num5 = 0;
					num5 |= (typeDefinition2.IsValueType ? 1 : 0);
					num5 |= (typeDefinition2.IsEnum ? 1 : 0) << 1;
					num5 |= (typeDefinition2.HasFinalizer() ? 1 : 0) << 2;
					num5 |= (typeDefinition2.HasStaticConstructor() ? 1 : 0) << 3;
					num5 |= (MarshalingUtils.IsBlittable(typeDefinition2, null, MarshalType.PInvoke, useUnicodeCharset: false) ? 1 : 0) << 4;
					num5 |= (typeDefinition2.IsComOrWindowsRuntimeType(_context) ? 1 : 0) << 5;
					int num6 = TypeDefinitionWriter.FieldLayoutPackingSizeFor(typeDefinition2);
					if (num6 != -1)
					{
						num5 |= (int)MetadataDatWriterBase.ConvertPackingSizeToCompressedEnum(num6) << 6;
					}
					num5 |= (int)(((typeDefinition2.PackingSize == -1) ? 1u : 0u) << 10);
					num5 |= (int)(((typeDefinition2.ClassSize == -1) ? 1u : 0u) << 11);
					short packingSize = typeDefinition2.PackingSize;
					if (packingSize != -1)
					{
						num5 |= (int)MetadataDatWriterBase.ConvertPackingSizeToCompressedEnum(packingSize) << 12;
					}
					stream.WriteInt(num5);
					stream.WriteUInt(typeDefinition2.MetadataToken.ToUInt32());
				}
			});
			List<ReadOnlyAttributeWriterOutput> attributeCollectionsInModuleOrder = new List<ReadOnlyAttributeWriterOutput>(attributeCollections.Count);
			WriteMetadataToStream("Images", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				int num3 = 0;
				foreach (ModuleDefinition module in metadataCollector.GetModules())
				{
					stream.WriteInt(metadataCollector.GetStringIndex(Path.GetFileName(module.FileName ?? module.Name)));
					stream.WriteInt(metadataCollector.GetAssemblyIndex(module.Assembly));
					stream.WriteInt(metadataCollector.GetLowestTypeInfoIndexForModule(module));
					stream.WriteInt(module.GetAllTypes().Count());
					stream.WriteInt(module.HasExportedTypes ? metadataCollector.GetExportedTypeIndex(module.ExportedTypes[0]) : (-1));
					stream.WriteInt(module.ExportedTypes.Count);
					stream.WriteInt((module.Assembly.EntryPoint == null) ? (-1) : metadataCollector.GetMethodIndex(module.Assembly.EntryPoint));
					stream.WriteUInt(module.MetadataToken.ToUInt32());
					ReadOnlyAttributeWriterOutput item2 = attributeCollections[module.Assembly];
					stream.WriteInt(num3);
					stream.WriteInt(item2.AttributeTypeRanges.Count);
					attributeCollectionsInModuleOrder.Add(item2);
					num3 += item2.AttributeTypeRanges.Count;
				}
			});
			WriteMetadataToStream("Assemblies", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (AssemblyDefinition assembly in metadataCollector.GetAssemblies())
				{
					stream.WriteInt(metadataCollector.GetModuleIndex(assembly.MainModule));
					stream.WriteUInt(assembly.MetadataToken.ToUInt32());
					int length;
					int firstIndexInReferencedAssemblyTableForAssembly = metadataCollector.GetFirstIndexInReferencedAssemblyTableForAssembly(assembly, out length);
					stream.WriteInt(firstIndexInReferencedAssemblyTableForAssembly);
					stream.WriteInt(length);
					stream.WriteInt(metadataCollector.GetStringIndex(assembly.Name.Name));
					stream.WriteInt(metadataCollector.GetStringIndex(assembly.Name.Culture));
					stream.WriteInt(metadataCollector.GetStringIndex(MetadataCollector.NameOfAssemblyPublicKeyData(assembly.Name)));
					stream.WriteUInt((uint)assembly.Name.HashAlgorithm);
					stream.WriteInt(assembly.Name.Hash.Length);
					stream.WriteUInt((uint)assembly.Name.Attributes);
					stream.WriteInt(assembly.Name.Version.Major);
					stream.WriteInt(assembly.Name.Version.Minor);
					stream.WriteInt(assembly.Name.Version.Build);
					stream.WriteInt(assembly.Name.Version.Revision);
					byte[] array3 = ((assembly.Name.PublicKeyToken.Length != 0) ? assembly.Name.PublicKeyToken : new byte[8]);
					foreach (byte value in array3)
					{
						stream.WriteByte(value);
					}
				}
			});
			WriteMetadataToStream("Field Refs", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				KeyValuePair<int, int>[] array2 = fieldRefs;
				for (int i = 0; i < array2.Length; i++)
				{
					KeyValuePair<int, int> keyValuePair = array2[i];
					stream.WriteInt(keyValuePair.Key);
					stream.WriteInt(keyValuePair.Value);
				}
			});
			WriteMetadataToStream("Referenced Assemblies", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (AssemblyDefinition item3 in metadataCollector.GetReferencedAssemblyTable())
				{
					stream.WriteInt(metadataCollector.GetAssemblyIndex(item3));
				}
			});
			WriteMetadataToStream("Attribute Types Ranges", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				int num2 = 0;
				foreach (ReadOnlyAttributeWriterOutput item4 in attributeCollectionsInModuleOrder)
				{
					foreach (AttributeClassTypeRange attributeTypeRange in item4.AttributeTypeRanges)
					{
						stream.WriteUInt(attributeTypeRange.MetadataToken);
						stream.WriteInt(attributeTypeRange.Start + num2);
						stream.WriteInt(attributeTypeRange.Count);
					}
					num2 += item4.AttributeTypes.Count;
				}
			});
			WriteMetadataToStream("Attribute Types", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				ITypeCollectorResults typeCollectorResults2 = typeResults;
				foreach (ReadOnlyAttributeWriterOutput item5 in attributeCollectionsInModuleOrder)
				{
					foreach (IIl2CppRuntimeType attributeType in item5.AttributeTypes)
					{
						stream.WriteInt(typeCollectorResults2.GetIndex(attributeType));
					}
				}
			});
			WriteMetadataToStream("Unresolved Virtual Call Parameter Types", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				ITypeCollectorResults typeCollectorResults = typeResults;
				foreach (IIl2CppRuntimeType item6 in virtualCallTables.SignatureTypes.SelectMany((IIl2CppRuntimeType[] s) => s))
				{
					stream.WriteInt(typeCollectorResults.GetIndex(item6));
				}
			});
			WriteMetadataToStream("Unresolved Virtual Call Parameter Ranges", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				int num = 0;
				foreach (IIl2CppRuntimeType[] signatureType in virtualCallTables.SignatureTypes)
				{
					stream.WriteInt(num);
					stream.WriteInt(signatureType.Length);
					num += signatureType.Length;
				}
			});
			MetadataStringsCollector windowsRuntimeNames = new MetadataStringsCollector();
			WriteMetadataToStream("Windows Runtime type names", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (Tuple<IIl2CppRuntimeType, string> windowsRuntimeTypeWithName in _context.Global.Results.PrimaryCollection.WindowsRuntimeTypeWithNames)
				{
					stream.WriteInt(windowsRuntimeNames.AddString(windowsRuntimeTypeWithName.Item2));
					stream.WriteInt(typeResults.GetIndex(windowsRuntimeTypeWithName.Item1));
				}
			});
			WriteMetadataToStream("Windows Runtime strings", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				byte[] array = windowsRuntimeNames.GetStringData().ToArray();
				stream.Write(array, 0, array.Length);
				stream.AlignTo(4);
			});
			WriteMetadataToStream("Exported Types", headerStream, 256, dataStream, delegate(MemoryStream stream)
			{
				foreach (ExportedType exportedType in metadataCollector.GetExportedTypes())
				{
					TypeDefinition typeDefinition = exportedType.Resolve();
					stream.WriteInt((typeDefinition != null) ? metadataCollector.GetTypeInfoIndex(typeDefinition) : (-1));
				}
			});
		}
	}
}
