using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Metadata.Fields;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Results
{
	public interface IMetadataCollectionResults
	{
		ReadOnlyCollection<KeyValuePair<EventDefinition, MetadataEventInfo>> GetEvents();

		ReadOnlyCollection<KeyValuePair<FieldDefinition, MetadataFieldInfo>> GetFields();

		ReadOnlyCollection<FieldDefaultValue> GetFieldDefaultValues();

		ReadOnlyCollection<byte> GetDefaultValueData();

		ReadOnlyCollection<KeyValuePair<MethodDefinition, MetadataMethodInfo>> GetMethods();

		ReadOnlyCollection<KeyValuePair<ParameterDefinition, MetadataParameterInfo>> GetParameters();

		ReadOnlyCollection<PropertyDefinition> GetProperties();

		ReadOnlyCollection<byte> GetStringData();

		ReadOnlyCollection<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>> GetTypeInfos();

		ReadOnlyCollection<IGenericParameterProvider> GetGenericContainers();

		ReadOnlyCollection<GenericParameter> GetGenericParameters();

		ReadOnlyCollection<IIl2CppRuntimeType> GetGenericParameterConstraints();

		ReadOnlyCollection<ParameterDefaultValue> GetParameterDefaultValues();

		ReadOnlyCollection<FieldMarshaledSize> GetFieldMarshaledSizes();

		ReadOnlyCollection<int> GetNestedTypes();

		ReadOnlyCollection<IIl2CppRuntimeType> GetInterfaces();

		ReadOnlyCollection<MethodReference> GetVTableMethods();

		ReadOnlyCollection<InterfaceOffset> GetInterfaceOffsets();

		ReadOnlyCollection<ExportedType> GetExportedTypes();

		ReadOnlyCollection<ModuleDefinition> GetModules();

		ReadOnlyCollection<AssemblyDefinition> GetAssemblies();

		ReadOnlyCollection<AssemblyDefinition> GetReferencedAssemblyTable();

		int GetTypeInfoIndex(TypeDefinition type);

		int GetEventIndex(EventDefinition @event);

		int GetFieldIndex(FieldDefinition field);

		int GetMethodIndex(MethodDefinition method);

		int GetParameterIndex(ParameterDefinition parameter);

		int GetPropertyIndex(PropertyDefinition property);

		int GetStringIndex(string str);

		int GetGenericContainerIndex(IGenericParameterProvider container);

		int GetGenericParameterIndex(GenericParameter genericParameter);

		int GetGenericParameterConstraintsStartIndex(GenericParameter genericParameter);

		int GetNestedTypesStartIndex(TypeDefinition type);

		int GetInterfacesStartIndex(TypeDefinition type);

		int GetVTableMethodsStartIndex(TypeDefinition type);

		int GetInterfaceOffsetsStartIndex(TypeDefinition type);

		int GetExportedTypeIndex(ExportedType exportedType);

		int GetModuleIndex(ModuleDefinition module);

		int GetAssemblyIndex(AssemblyDefinition assembly);

		int GetFirstIndexInReferencedAssemblyTableForAssembly(AssemblyDefinition assembly, out int length);

		int GetLowestTypeInfoIndexForModule(ModuleDefinition image);
	}
}
