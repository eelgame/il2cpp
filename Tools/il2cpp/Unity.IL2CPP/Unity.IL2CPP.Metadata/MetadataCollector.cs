using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Metadata.Fields;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Metadata
{
	public class MetadataCollector : IMetadataCollectionResults
	{
		private MetadataStringsCollector _stringsCollector = new MetadataStringsCollector();

		private readonly Dictionary<FieldDefinition, MetadataFieldInfo> _fields = new Dictionary<FieldDefinition, MetadataFieldInfo>();

		private readonly Dictionary<FieldDefaultValue, int> _fieldDefaultValues = new Dictionary<FieldDefaultValue, int>();

		private readonly Dictionary<ParameterDefaultValue, int> _parameterDefaultValues = new Dictionary<ParameterDefaultValue, int>();

		private readonly List<FieldMarshaledSize> _fieldMarshaledSizes = new List<FieldMarshaledSize>();

		private readonly Dictionary<MethodDefinition, MetadataMethodInfo> _methods = new Dictionary<MethodDefinition, MetadataMethodInfo>();

		private readonly HashSet<MethodReference> _existingMethods = new HashSet<MethodReference>(new MethodReferenceComparer());

		private readonly Dictionary<ParameterDefinition, MetadataParameterInfo> _parameters = new Dictionary<ParameterDefinition, MetadataParameterInfo>();

		private readonly Dictionary<PropertyDefinition, int> _properties = new Dictionary<PropertyDefinition, int>();

		private readonly Dictionary<EventDefinition, MetadataEventInfo> _events = new Dictionary<EventDefinition, MetadataEventInfo>();

		private readonly Dictionary<TypeDefinition, MetadataTypeDefinitionInfo> _typeInfos = new Dictionary<TypeDefinition, MetadataTypeDefinitionInfo>();

		private readonly Dictionary<IGenericParameterProvider, int> _genericContainers = new Dictionary<IGenericParameterProvider, int>();

		private readonly Dictionary<GenericParameter, int> _genericParameters = new Dictionary<GenericParameter, int>();

		private readonly Dictionary<GenericParameter, int> _genericParameterConstraintsStart = new Dictionary<GenericParameter, int>();

		private readonly List<IIl2CppRuntimeType> _genericParameterConstraints = new List<IIl2CppRuntimeType>();

		private readonly Dictionary<TypeDefinition, int> _nestedTypesStart = new Dictionary<TypeDefinition, int>();

		private readonly List<int> _nestedTypes = new List<int>();

		private readonly Dictionary<TypeDefinition, int> _interfacesStart = new Dictionary<TypeDefinition, int>();

		private readonly List<IIl2CppRuntimeType> _interfaces = new List<IIl2CppRuntimeType>();

		private readonly Dictionary<TypeDefinition, int> _vtableMethodsStart = new Dictionary<TypeDefinition, int>();

		private readonly List<MethodReference> _vtableMethods = new List<MethodReference>();

		private readonly Dictionary<TypeDefinition, int> _interfaceOffsetsStart = new Dictionary<TypeDefinition, int>();

		private readonly List<InterfaceOffset> _interfaceOffsets = new List<InterfaceOffset>();

		private readonly List<byte> _defaultValueData = new List<byte>();

		private readonly Dictionary<ModuleDefinition, int> _modules = new Dictionary<ModuleDefinition, int>();

		private readonly Dictionary<ExportedType, int> _exportedTypes = new Dictionary<ExportedType, int>();

		private readonly Dictionary<AssemblyDefinition, int> _assemblies = new Dictionary<AssemblyDefinition, int>();

		private readonly Dictionary<ModuleDefinition, int> _lowestTypeInfoIndexForModule = new Dictionary<ModuleDefinition, int>();

		private readonly Dictionary<AssemblyDefinition, Tuple<int, int>> _firstReferencedAssemblyIndexCache = new Dictionary<AssemblyDefinition, Tuple<int, int>>();

		private readonly List<AssemblyDefinition> _referencedAssemblyTable = new List<AssemblyDefinition>();

		public static IMetadataCollectionResults Collect(SecondaryCollectionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			MetadataCollector metadataCollector = new MetadataCollector();
			metadataCollector.AddAssemblies(context, assemblies);
			return metadataCollector;
		}

		public void AddAssemblies(SecondaryCollectionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			using (MiniProfiler.Section("UpdateEmptyTokens"))
			{
				foreach (AssemblyDefinition assembly in assemblies)
				{
					UpdateEmptyTokens(assembly);
				}
			}
			using (MiniProfiler.Section("ValidateTokens"))
			{
				foreach (AssemblyDefinition assembly2 in assemblies)
				{
					ValidateTokens(assembly2);
				}
			}
			foreach (AssemblyDefinition assembly3 in assemblies)
			{
				AddAssembly(context, assembly3);
			}
			foreach (AssemblyDefinition assembly4 in assemblies)
			{
				AddVTables(context, assembly4.MainModule.Types);
			}
			AddReferencedAssemblyMetadata(context, assemblies);
		}

		public void Add(SecondaryCollectionContext context, AssemblyDefinition assembly)
		{
			using (MiniProfiler.Section("UpdateEmptyTokens"))
			{
				UpdateEmptyTokens(assembly);
			}
			using (MiniProfiler.Section("ValidateTokens"))
			{
				ValidateTokens(assembly);
			}
			AddAssembly(context, assembly);
		}

		public IMetadataCollectionResults Complete(SecondaryCollectionContext context)
		{
			foreach (AssemblyDefinition key in _assemblies.Keys)
			{
				AddVTables(context, key.MainModule.Types);
			}
			AddReferencedAssemblyMetadata(context, _assemblies.Keys);
			return this;
		}

		private static void UpdateEmptyTokens(AssemblyDefinition asm)
		{
			TypeDefinition[] array = asm.MainModule.GetAllTypes().ToArray();
			UpdateTokens(array, TokenType.TypeDef);
			UpdateTokens(array.SelectMany((TypeDefinition t) => t.Interfaces), TokenType.InterfaceImpl);
			UpdateTokens(array.SelectMany((TypeDefinition t) => t.GenericParameters).Concat(array.SelectMany((TypeDefinition t) => t.Methods).SelectMany((MethodDefinition m) => m.GenericParameters)), TokenType.GenericParam);
			UpdateTokens(array.SelectMany((TypeDefinition t) => t.Methods), TokenType.Method);
			UpdateTokens(array.SelectMany((TypeDefinition t) => t.Methods).SelectMany((MethodDefinition m) => m.Parameters), TokenType.Param);
			UpdateTokens(array.SelectMany((TypeDefinition t) => t.Properties), TokenType.Property);
			UpdateTokens(array.SelectMany((TypeDefinition t) => t.Fields), TokenType.Field);
			UpdateTokens(array.SelectMany((TypeDefinition t) => t.Events), TokenType.Event);
		}

		private static void UpdateTokens(IEnumerable<IMetadataTokenProvider> providers, TokenType type)
		{
			uint num = 0u;
			List<IMetadataTokenProvider> list = new List<IMetadataTokenProvider>();
			foreach (IMetadataTokenProvider provider in providers)
			{
				num = Math.Max(num, provider.MetadataToken.RID);
				if (provider.MetadataToken.RID == 0)
				{
					list.Add(provider);
				}
			}
			foreach (IMetadataTokenProvider item in list)
			{
				item.MetadataToken = new MetadataToken(type, ++num);
			}
		}

		private static void ValidateTokens(AssemblyDefinition asm)
		{
			Dictionary<uint, IMetadataTokenProvider> tokens = new Dictionary<uint, IMetadataTokenProvider>();
			TypeDefinition[] array = asm.MainModule.GetAllTypes().ToArray();
			ValidateTokens(tokens, array);
			ValidateTokens(tokens, array.SelectMany((TypeDefinition t) => t.Interfaces));
			ValidateTokens(tokens, array.SelectMany((TypeDefinition t) => t.GenericParameters));
			ValidateTokens(tokens, array.SelectMany((TypeDefinition t) => t.Methods));
			ValidateTokens(tokens, array.SelectMany((TypeDefinition t) => t.Methods).SelectMany((MethodDefinition m) => m.GenericParameters));
			ValidateTokens(tokens, array.SelectMany((TypeDefinition t) => t.Methods).SelectMany((MethodDefinition m) => m.Parameters));
			ValidateTokens(tokens, array.SelectMany((TypeDefinition t) => t.Properties));
			ValidateTokens(tokens, array.SelectMany((TypeDefinition t) => t.Fields));
			ValidateTokens(tokens, array.SelectMany((TypeDefinition t) => t.Events));
		}

		private static void ValidateTokens(Dictionary<uint, IMetadataTokenProvider> tokens, IEnumerable<IMetadataTokenProvider> providers)
		{
			foreach (IMetadataTokenProvider provider in providers)
			{
				if (provider.MetadataToken.RID == 0)
				{
					throw new Exception();
				}
				uint num = provider.MetadataToken.ToUInt32();
				if (tokens.TryGetValue(num, out var value))
				{
					throw new InvalidOperationException($"Duplicate metadata token 0x{num:X} for '{value}' and '{provider}'");
				}
				tokens.Add(num, provider);
			}
		}

		private void AddAssembly(SecondaryCollectionContext context, AssemblyDefinition assemblyDefinition)
		{
			AddUnique(_assemblies, assemblyDefinition);
			AddString(assemblyDefinition.Name.Name);
			AddString(assemblyDefinition.Name.Culture);
			AddBytes(NameOfAssemblyPublicKeyData(assemblyDefinition.Name), EncodeBlob(assemblyDefinition.Name.PublicKey));
			AddUnique(_modules, assemblyDefinition.MainModule, delegate(ModuleDefinition module)
			{
				AddString(Path.GetFileName(module.FileName ?? module.Name));
				AddTypeInfos(context, module.Types);
				AddUnique(_exportedTypes, module.ExportedTypes);
			});
		}

		private static byte[] EncodeBlob(byte[] data)
		{
			int num = data.Length;
			byte[] array = new byte[4];
			uint num2;
			if (num < 128)
			{
				num2 = 1u;
				array[0] = (byte)num;
			}
			else if (num < 16384)
			{
				num2 = 2u;
				array[0] = (byte)((uint)(num >> 8) | 0x80u);
				array[1] = (byte)((uint)num & 0xFFu);
			}
			else
			{
				num2 = 4u;
				array[0] = (byte)((uint)(num >> 24) | 0xC0u);
				array[1] = (byte)((uint)(num >> 16) & 0xFFu);
				array[2] = (byte)((uint)(num >> 8) & 0xFFu);
				array[3] = (byte)((uint)num & 0xFFu);
			}
			byte[] array2 = new byte[num + num2 + 1];
			Array.Copy(array, 0L, array2, 0L, num2);
			Array.Copy(data, 0L, array2, num2, data.Length);
			return array2;
		}

		public int AddString(string str)
		{
			return _stringsCollector.AddString(str);
		}

		public static string NameOfAssemblyPublicKeyData(AssemblyNameDefinition assemblyName)
		{
			return assemblyName.Name + "_PublicKey";
		}

		private void AddBytes(string nameOfData, byte[] data)
		{
			_stringsCollector.AddBytes(nameOfData, data);
		}

		public void AddFields(SecondaryCollectionContext context, IEnumerable<FieldDefinition> fields, MarshalType marshalType)
		{
			ITypeCollector typeCollector = context.Global.Collectors.Types;
			FieldDefinition[] array = fields.ToArray();
			AddUnique(_fields, array, delegate(FieldDefinition field)
			{
				AddString(field.Name);
				return new MetadataFieldInfo(_fields.Count, typeCollector.Add(field.FieldType, (int)field.Attributes));
			});
			AddUnique(_fieldDefaultValues, DefaultValueFromFields(context, this, typeCollector, array));
			AddUnique(_fieldMarshaledSizes, MarshaledSizeFromFields(context, this, typeCollector, array, marshalType));
		}

		private static IEnumerable<FieldDefaultValue> DefaultValueFromFields(SecondaryCollectionContext context, MetadataCollector metadataCollector, ITypeCollector typeCollector, IEnumerable<FieldDefinition> fields)
		{
			foreach (FieldDefinition field in fields)
			{
				if (field.HasConstant)
				{
					yield return new FieldDefaultValue(metadataCollector.GetFieldIndex(field), typeCollector.Add(MetadataUtils.GetUnderlyingType(field.FieldType)), (field.Constant == null) ? (-1) : metadataCollector.AddDefaultValueData(MetadataUtils.ConstantDataFor(context, field, field.FieldType, field.FullName)));
				}
				if (field.InitialValue.Length != 0)
				{
					yield return new FieldDefaultValue(metadataCollector.GetFieldIndex(field), typeCollector.Add(MetadataUtils.GetUnderlyingType(field.FieldType)), metadataCollector.AddDefaultValueData(field.InitialValue));
				}
			}
		}

		private static IEnumerable<FieldMarshaledSize> MarshaledSizeFromFields(SecondaryCollectionContext context, MetadataCollector metadataCollector, ITypeCollector typeCollector, IEnumerable<FieldDefinition> fields, MarshalType marshalType)
		{
			foreach (FieldDefinition field in fields)
			{
				if (field.HasMarshalInfo)
				{
					DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(context, field.FieldType, marshalType, field.MarshalInfo);
					yield return new FieldMarshaledSize(metadataCollector.GetFieldIndex(field), typeCollector.Add(MetadataUtils.GetUnderlyingType(field.FieldType)), defaultMarshalInfoWriter.NativeSizeWithoutPointers);
				}
			}
		}

		public void AddTypeInfos(SecondaryCollectionContext context, IEnumerable<TypeDefinition> types)
		{
			ITypeCollector typeCollector = context.Global.Collectors.Types;
			AddUnique(_typeInfos, types, delegate(TypeDefinition type)
			{
				AddString(type.Name);
				AddString(type.Namespace);
				AddMethods(context, type.Methods);
				AddFields(context, type.Fields, MarshalType.PInvoke);
				AddProperties(type.Properties);
				AddEvents(context, type.Events);
				if (type.HasNestedTypes)
				{
					AddTypeInfos(context, type.NestedTypes);
					_nestedTypesStart.Add(type, _nestedTypes.Count);
					_nestedTypes.AddRange(type.NestedTypes.Select(GetTypeInfoIndex));
				}
				if (type.HasInterfaces)
				{
					_interfacesStart.Add(type, _interfaces.Count);
					_interfaces.AddRange(type.Interfaces.Select((InterfaceImplementation a) => typeCollector.Add(a.InterfaceType)));
				}
				if (type.HasGenericParameters)
				{
					AddGenericContainer(type);
					AddGenericParameters(context, type.GenericParameters);
				}
				TypeReference typeReference = BaseTypeFor(type);
				TypeReference typeReference2 = DeclaringTypeFor(type);
				TypeReference typeReference3 = ElementTypeFor(type);
				int count = _typeInfos.Count;
				if (!_lowestTypeInfoIndexForModule.ContainsKey(type.Module))
				{
					_lowestTypeInfoIndexForModule.Add(type.Module, count);
				}
				else if (_lowestTypeInfoIndexForModule[type.Module] > count)
				{
					_lowestTypeInfoIndexForModule[type.Module] = count;
				}
				return new MetadataTypeDefinitionInfo(count, typeCollector.Add(type), (typeReference != null) ? typeCollector.Add(typeReference) : null, (typeReference2 != null) ? typeCollector.Add(typeReference2) : null, (typeReference3 != null) ? typeCollector.Add(typeReference3) : null);
			});
		}

		private static TypeReference DeclaringTypeFor(TypeDefinition type)
		{
			if (!type.IsNested)
			{
				return null;
			}
			return type.DeclaringType;
		}

		private static TypeReference BaseTypeFor(TypeDefinition type)
		{
			return TypeResolver.For(type).Resolve(type.Resolve().BaseType);
		}

		private static TypeReference ElementTypeFor(TypeDefinition type)
		{
			if (type.IsEnum())
			{
				return type.GetUnderlyingEnumType();
			}
			return type;
		}

		private void AddVTables(SecondaryCollectionContext context, IEnumerable<TypeDefinition> types)
		{
			foreach (TypeDefinition type in types)
			{
				if (type.HasNestedTypes)
				{
					AddVTables(context, type.NestedTypes);
				}
				if (type.IsInterface && !type.IsComOrWindowsRuntimeType(context) && context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(type) == null)
				{
					continue;
				}
				VTable vTable = new VTableBuilder().VTableFor(context, type);
				_vtableMethodsStart.Add(type, _vtableMethods.Count);
				foreach (MethodReference slot in vTable.Slots)
				{
					_vtableMethods.Add(slot);
				}
				_interfaceOffsetsStart.Add(type, _interfaceOffsets.Count);
				_interfaceOffsets.AddRange(vTable.InterfaceOffsets.Select((KeyValuePair<TypeReference, int> pair) => new InterfaceOffset(context.Global.Collectors.Types.Add(pair.Key), pair.Value)));
			}
		}

		private void AddProperties(IEnumerable<PropertyDefinition> properties)
		{
			AddUnique(_properties, properties, delegate(PropertyDefinition property)
			{
				AddString(property.Name);
			});
		}

		private void AddEvents(SecondaryCollectionContext context, IEnumerable<EventDefinition> events)
		{
			AddUnique(_events, events, delegate(EventDefinition evt)
			{
				AddString(evt.Name);
				return new MetadataEventInfo(_events.Count, context.Global.Collectors.Types.Add(evt.EventType));
			});
		}

		private void AddMethods(SecondaryCollectionContext context, IEnumerable<MethodDefinition> methods)
		{
			AddUnique(_methods, methods, delegate(MethodDefinition method)
			{
				_existingMethods.Add(method);
				context.Global.Services.ErrorInformation.CurrentMethod = method;
				AddParameters(context, method.Parameters);
				AddString(method.Name);
				if (method.HasGenericParameters)
				{
					AddGenericContainer(method);
					AddGenericParameters(context, method.GenericParameters);
				}
				return new MetadataMethodInfo(_methods.Count, context.Global.Collectors.Types.Add(method.ReturnType));
			});
		}

		private void AddParameters(SecondaryCollectionContext context, IEnumerable<ParameterDefinition> parameters)
		{
			ParameterDefinition[] array = parameters.ToArray();
			AddUnique(_parameters, array, delegate(ParameterDefinition parameter)
			{
				AddString(parameter.Name);
				return new MetadataParameterInfo(_parameters.Count, context.Global.Collectors.Types.Add(parameter.ParameterType, (int)parameter.Attributes));
			});
			AddUnique(_parameterDefaultValues, FromParameters(context, this, context.Global.Collectors.Types, array));
		}

		private static IEnumerable<ParameterDefaultValue> FromParameters(SecondaryCollectionContext context, MetadataCollector metadataCollector, ITypeCollector typeCollector, IEnumerable<ParameterDefinition> parameters)
		{
			foreach (ParameterDefinition parameter in parameters)
			{
				if (parameter.HasConstant)
				{
					yield return new ParameterDefaultValue(metadataCollector.GetParameterIndex(parameter), typeCollector.Add(MetadataUtils.GetUnderlyingType(parameter.ParameterType)), (parameter.Constant == null) ? (-1) : metadataCollector.AddDefaultValueData(MetadataUtils.ConstantDataFor(context, parameter, parameter.ParameterType, parameter.Name)));
				}
			}
		}

		private void AddGenericContainer(IGenericParameterProvider container)
		{
			AddUnique(_genericContainers, container);
		}

		private void AddGenericParameters(SecondaryCollectionContext context, IEnumerable<GenericParameter> genericParameters)
		{
			AddUnique(_genericParameters, genericParameters, delegate(GenericParameter genericParameter)
			{
				AddString(genericParameter.Name);
				if (genericParameter.Constraints.Count > 0)
				{
					_genericParameterConstraintsStart.Add(genericParameter, _genericParameterConstraints.Count);
					_genericParameterConstraints.AddRange(genericParameter.Constraints.Select((GenericParameterConstraint a) => context.Global.Collectors.Types.Add(a.ConstraintType)));
				}
			});
		}

		private static void AddUnique<T>(Dictionary<T, int> items, IEnumerable<T> itemsToAdd, Action<T> onAdd = null)
		{
			foreach (T item in itemsToAdd)
			{
				AddUnique(items, item, onAdd);
			}
		}

		private static void AddUnique<T, TIndex>(Dictionary<T, TIndex> items, IEnumerable<T> itemsToAdd, Func<T, TIndex> onAdd) where TIndex : MetadataIndex
		{
			foreach (T item in itemsToAdd)
			{
				if (items.TryGetValue(item, out var value))
				{
					throw new Exception($"Attempting to add unique metadata item {item} multiple times.");
				}
				value = onAdd(item);
				items.Add(item, value);
			}
		}

		private static void AddUnique<T>(Dictionary<T, int> items, T item, Action<T> onAdd = null)
		{
			if (items.TryGetValue(item, out var value))
			{
				throw new Exception($"Attempting to add unique metadata item {item} multiple times.");
			}
			value = items.Count;
			items.Add(item, value);
			onAdd?.Invoke(item);
		}

		private static void AddUnique<T>(List<T> items, IEnumerable<T> itemsToAdd)
		{
			foreach (T item in itemsToAdd)
			{
				AddUnique(items, item);
			}
		}

		private static void AddUnique<T>(List<T> items, T item)
		{
			if (items.Contains(item))
			{
				throw new Exception($"Attempting to add unique metadata item {item} multiple times.");
			}
			items.Add(item);
		}

		public ReadOnlyCollection<byte> GetStringData()
		{
			return _stringsCollector.GetStringData();
		}

		public int GetStringIndex(string str)
		{
			return _stringsCollector.GetStringIndex(str);
		}

		public ReadOnlyCollection<KeyValuePair<FieldDefinition, MetadataFieldInfo>> GetFields()
		{
			return _fields.ItemsSortedByValue();
		}

		public int GetFieldIndex(FieldDefinition field)
		{
			return _fields[field].Index;
		}

		private int AddDefaultValueData(byte[] data)
		{
			int count = _defaultValueData.Count;
			_defaultValueData.AddRange(data);
			return count;
		}

		public ReadOnlyCollection<FieldDefaultValue> GetFieldDefaultValues()
		{
			return _fieldDefaultValues.KeysSortedByValue();
		}

		public ReadOnlyCollection<ParameterDefaultValue> GetParameterDefaultValues()
		{
			return _parameterDefaultValues.KeysSortedByValue();
		}

		public ReadOnlyCollection<byte> GetDefaultValueData()
		{
			return _defaultValueData.AsReadOnly();
		}

		public ReadOnlyCollection<FieldMarshaledSize> GetFieldMarshaledSizes()
		{
			return _fieldMarshaledSizes.ToArray().AsReadOnly();
		}

		public ReadOnlyCollection<KeyValuePair<TypeDefinition, MetadataTypeDefinitionInfo>> GetTypeInfos()
		{
			return _typeInfos.ItemsSortedByValue();
		}

		public int GetTypeInfoIndex(TypeDefinition type)
		{
			return _typeInfos[type].Index;
		}

		public ReadOnlyCollection<KeyValuePair<MethodDefinition, MetadataMethodInfo>> GetMethods()
		{
			return _methods.ItemsSortedByValue();
		}

		public int GetMethodIndex(MethodDefinition method)
		{
			return _methods[method].Index;
		}

		public ReadOnlyCollection<KeyValuePair<ParameterDefinition, MetadataParameterInfo>> GetParameters()
		{
			return _parameters.ItemsSortedByValue();
		}

		public int GetParameterIndex(ParameterDefinition parameter)
		{
			return _parameters[parameter].Index;
		}

		public ReadOnlyCollection<PropertyDefinition> GetProperties()
		{
			return _properties.KeysSortedByValue();
		}

		public int GetPropertyIndex(PropertyDefinition property)
		{
			return _properties[property];
		}

		public ReadOnlyCollection<KeyValuePair<EventDefinition, MetadataEventInfo>> GetEvents()
		{
			return _events.ItemsSortedByValue();
		}

		public int GetEventIndex(EventDefinition @event)
		{
			return _events[@event].Index;
		}

		public ReadOnlyCollection<IGenericParameterProvider> GetGenericContainers()
		{
			return _genericContainers.KeysSortedByValue();
		}

		public int GetGenericContainerIndex(IGenericParameterProvider container)
		{
			if (_genericContainers.TryGetValue(container, out var value))
			{
				return value;
			}
			return -1;
		}

		public ReadOnlyCollection<GenericParameter> GetGenericParameters()
		{
			return _genericParameters.KeysSortedByValue();
		}

		public int GetGenericParameterIndex(GenericParameter genericParameter)
		{
			return _genericParameters[genericParameter];
		}

		public ReadOnlyCollection<IIl2CppRuntimeType> GetGenericParameterConstraints()
		{
			return _genericParameterConstraints.ToArray().AsReadOnly();
		}

		public int GetGenericParameterConstraintsStartIndex(GenericParameter genericParameter)
		{
			return _genericParameterConstraintsStart[genericParameter];
		}

		public ReadOnlyCollection<int> GetNestedTypes()
		{
			return _nestedTypes.ToArray().AsReadOnly();
		}

		public int GetNestedTypesStartIndex(TypeDefinition type)
		{
			return _nestedTypesStart[type];
		}

		public ReadOnlyCollection<IIl2CppRuntimeType> GetInterfaces()
		{
			return _interfaces.ToArray().AsReadOnly();
		}

		public int GetInterfacesStartIndex(TypeDefinition type)
		{
			return _interfacesStart[type];
		}

		public ReadOnlyCollection<MethodReference> GetVTableMethods()
		{
			return _vtableMethods.ToArray().AsReadOnly();
		}

		public int GetVTableMethodsStartIndex(TypeDefinition type)
		{
			if (_vtableMethodsStart.TryGetValue(type, out var value))
			{
				return value;
			}
			return -1;
		}

		public ReadOnlyCollection<InterfaceOffset> GetInterfaceOffsets()
		{
			return _interfaceOffsets.ToArray().AsReadOnly();
		}

		public int GetInterfaceOffsetsStartIndex(TypeDefinition type)
		{
			return _interfaceOffsetsStart[type];
		}

		public ReadOnlyCollection<ExportedType> GetExportedTypes()
		{
			return _exportedTypes.KeysSortedByValue();
		}

		public int GetExportedTypeIndex(ExportedType exportedType)
		{
			return _exportedTypes[exportedType];
		}

		public ReadOnlyCollection<ModuleDefinition> GetModules()
		{
			return _modules.KeysSortedByValue();
		}

		public int GetModuleIndex(ModuleDefinition module)
		{
			return _modules[module];
		}

		public ReadOnlyCollection<AssemblyDefinition> GetAssemblies()
		{
			return _assemblies.KeysSortedByValue();
		}

		public int GetAssemblyIndex(AssemblyDefinition assembly)
		{
			return _assemblies[assembly];
		}

		public ReadOnlyCollection<AssemblyDefinition> GetReferencedAssemblyTable()
		{
			return _referencedAssemblyTable.AsReadOnly();
		}

		private void AddReferencedAssemblyMetadata(SecondaryCollectionContext context, ICollection<AssemblyDefinition> assemblies)
		{
			foreach (AssemblyDefinition assembly in assemblies)
			{
				List<AssemblyDefinition> list = context.Global.Services.AssemblyDependencies.GetReferencedAssembliesFor(assembly).ToList();
				if (list.Count == 0)
				{
					_firstReferencedAssemblyIndexCache.Add(assembly, new Tuple<int, int>(-1, 0));
					continue;
				}
				_firstReferencedAssemblyIndexCache.Add(assembly, new Tuple<int, int>(_referencedAssemblyTable.Count, list.Count));
				_referencedAssemblyTable.AddRange(list.Distinct());
			}
		}

		public int GetFirstIndexInReferencedAssemblyTableForAssembly(AssemblyDefinition assembly, out int length)
		{
			Tuple<int, int> tuple = _firstReferencedAssemblyIndexCache[assembly];
			length = tuple.Item2;
			return tuple.Item1;
		}

		public int GetLowestTypeInfoIndexForModule(ModuleDefinition image)
		{
			return _lowestTypeInfoIndexForModule[image];
		}
	}
}
