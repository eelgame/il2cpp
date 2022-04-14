using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP
{
	public class MethodMetadataUsage
	{
		private readonly Dictionary<IIl2CppRuntimeType, bool> _types = new Dictionary<IIl2CppRuntimeType, bool>(new Il2CppRuntimeTypeEqualityComparer());

		private readonly Dictionary<IIl2CppRuntimeType, bool> _typeInfos = new Dictionary<IIl2CppRuntimeType, bool>(new Il2CppRuntimeTypeEqualityComparer());

		private readonly Dictionary<MethodReference, bool> _inflatedMethods = new Dictionary<MethodReference, bool>(new MethodReferenceComparer());

		private readonly Dictionary<Il2CppRuntimeFieldReference, bool> _fieldInfos = new Dictionary<Il2CppRuntimeFieldReference, bool>(new Il2CppRuntimeFieldReferenceEqualityComparer());

		private readonly Dictionary<StringMetadataToken, bool> _stringLiterals = new Dictionary<StringMetadataToken, bool>(new StringMetadataTokenComparer());

		public bool UsesMetadata
		{
			get
			{
				if (_types.Count <= 0 && _typeInfos.Count <= 0 && _inflatedMethods.Count <= 0 && _fieldInfos.Count <= 0)
				{
					return _stringLiterals.Count > 0;
				}
				return true;
			}
		}

		public int UsageCount => _types.Count + _typeInfos.Count + _inflatedMethods.Count + _fieldInfos.Count + _stringLiterals.Count;

		public void AddTypeInfo(IIl2CppRuntimeType type, bool inlinedInitialization = false)
		{
			AddItem(_typeInfos, type, inlinedInitialization);
		}

		public void AddIl2CppType(IIl2CppRuntimeType type, bool inlinedInitialization = false)
		{
			AddItem(_types, type, inlinedInitialization);
		}

		public void AddInflatedMethod(MethodReference method, bool inlinedInitialization = false)
		{
			AddItem(_inflatedMethods, method, inlinedInitialization);
		}

		public void AddFieldInfo(Il2CppRuntimeFieldReference field, bool inlinedInitialization = false)
		{
			AddItem(_fieldInfos, field, inlinedInitialization);
		}

		public void AddStringLiteral(string literal, AssemblyDefinition assemblyDefinition, MetadataToken metadataToken, bool inlinedInitialization = false)
		{
			AddItem(_stringLiterals, new StringMetadataToken(literal, assemblyDefinition, metadataToken), inlinedInitialization);
		}

		public IEnumerable<IIl2CppRuntimeType> GetTypeInfos()
		{
			return _typeInfos.Keys;
		}

		public IEnumerable<IIl2CppRuntimeType> GetIl2CppTypes()
		{
			return _types.Keys;
		}

		public IEnumerable<MethodReference> GetInflatedMethods()
		{
			return _inflatedMethods.Keys;
		}

		public IEnumerable<Il2CppRuntimeFieldReference> GetFieldInfos()
		{
			return _fieldInfos.Keys;
		}

		public IEnumerable<StringMetadataToken> GetStringLiterals()
		{
			return _stringLiterals.Keys;
		}

		public IEnumerable<IIl2CppRuntimeType> GetTypeInfosNeedingInit()
		{
			return GetItemsNeedingInit(_typeInfos);
		}

		public IEnumerable<IIl2CppRuntimeType> GetIl2CppTypesNeedingInit()
		{
			return GetItemsNeedingInit(_types);
		}

		public IEnumerable<MethodReference> GetInflatedMethodsNeedingInit()
		{
			return GetItemsNeedingInit(_inflatedMethods);
		}

		public IEnumerable<Il2CppRuntimeFieldReference> GetFieldInfosNeedingInit()
		{
			return GetItemsNeedingInit(_fieldInfos);
		}

		public IEnumerable<StringMetadataToken> GetStringLiteralsNeedingInit()
		{
			return GetItemsNeedingInit(_stringLiterals);
		}

		private static void AddItem<T>(Dictionary<T, bool> list, T item, bool inlinedInitialization)
		{
			if (!list.ContainsKey(item))
			{
				list.Add(item, inlinedInitialization);
			}
			else if (!inlinedInitialization)
			{
				list[item] = false;
			}
		}

		private static T[] GetItemsNeedingInit<T>(Dictionary<T, bool> items)
		{
			return (from t in items
				where !t.Value
				select t.Key).ToArray();
		}
	}
}
