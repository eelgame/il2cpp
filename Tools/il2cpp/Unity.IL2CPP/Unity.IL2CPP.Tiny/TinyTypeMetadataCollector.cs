using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericsCollection;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.Tiny
{
	public class TinyTypeMetadataCollector
	{
		public class Results : ITinyTypeMetadataResults
		{
			private readonly ReadOnlyCollection<TinyTypeEntry> _typeEntries;

			private readonly ReadOnlyDictionary<TypeReference, TinyTypeEntry> _typesDictionary;

			public Results(ReadOnlyCollection<TinyTypeEntry> items, ReadOnlyDictionary<TypeReference, TinyTypeEntry> typesDictionary)
			{
				_typeEntries = items;
				_typesDictionary = typesDictionary;
			}

			public ReadOnlyCollection<TinyTypeEntry> GetAllEntries()
			{
				return _typeEntries;
			}

			public TinyTypeEntry GetTypeEntry(TypeReference type)
			{
				if (_typesDictionary.TryGetValue(type, out var value))
				{
					return value;
				}
				throw new InvalidOperationException($"No type entry found for : {type}");
			}
		}

		private static readonly ReadOnlyDictionary<string, string> graftedOnArrayMethods = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>
		{
			{ "T System.Collections.Generic.IList`1::get_Item(System.Int32)", "T System.Array::InternalArray__get_Item(System.Int32)" },
			{ "System.Void System.Collections.Generic.IList`1::set_Item(System.Int32,T)", "System.Void System.Array::InternalArray__set_Item(System.Int32,T)" },
			{ "System.Int32 System.Collections.Generic.IList`1::IndexOf(T)", "System.Int32 System.Array::InternalArray__IndexOf(T)" },
			{ "System.Void System.Collections.Generic.IList`1::Insert(System.Int32,T)", "System.Void System.Array::InternalArray__Insert(System.Int32,T)" },
			{ "System.Void System.Collections.Generic.IList`1::RemoveAt(System.Int32)", "System.Void System.Array::InternalArray__RemoveAt(System.Int32)" },
			{ "System.Int32 System.Collections.Generic.ICollection`1::get_Count()", "System.Int32 System.Array::InternalArray__ICollection_get_Count()" },
			{ "System.Boolean System.Collections.Generic.ICollection`1::get_IsReadOnly()", "System.Boolean System.Array::InternalArray__ICollection_get_IsReadOnly()" },
			{ "System.Void System.Collections.Generic.ICollection`1::Add(T)", "System.Void System.Array::InternalArray__ICollection_Add(T)" },
			{ "System.Void System.Collections.Generic.ICollection`1::Clear()", "System.Void System.Array::InternalArray__ICollection_Clear()" },
			{ "System.Boolean System.Collections.Generic.ICollection`1::Contains(T)", "System.Boolean System.Array::InternalArray__ICollection_Contains(T)" },
			{ "System.Void System.Collections.Generic.ICollection`1::CopyTo(T[],System.Int32)", "System.Void System.Array::InternalArray__ICollection_CopyTo(T[],System.Int32)" },
			{ "System.Boolean System.Collections.Generic.ICollection`1::Remove(T)", "System.Boolean System.Array::InternalArray__ICollection_Remove(T)" },
			{ "System.Collections.Generic.IEnumerator`1<T> System.Collections.Generic.IEnumerable`1::GetEnumerator()", "System.Collections.Generic.IEnumerator`1<T> System.Array::InternalArray__IEnumerable_GetEnumerator()" },
			{ "T System.Collections.Generic.IReadOnlyList`1::get_Item(System.Int32)", "T System.Array::InternalArray__IReadOnlyList_get_Item(System.Int32)" },
			{ "System.Int32 System.Collections.Generic.IReadOnlyCollection`1::get_Count()", "System.Int32 System.Array::InternalArray__IReadOnlyCollection_get_Count()" }
		});

		public static ITinyTypeMetadataResults Collect(ReadOnlyContext context, ReadOnlyCollection<TypeReference> tinyTypes)
		{
			Dictionary<TypeReference, TinyTypeEntry> dictionary = new Dictionary<TypeReference, TinyTypeEntry>(new TypeReferenceEqualityComparer());
			List<TinyTypeEntry> list = new List<TinyTypeEntry>();
			foreach (TypeReference tinyType in tinyTypes)
			{
				AddType(context, tinyType, dictionary, list);
			}
			return new Results(list.AsReadOnly(), dictionary.AsReadOnly());
		}

		private static void AddType(ReadOnlyContext context, TypeReference type, Dictionary<TypeReference, TinyTypeEntry> typesDictionary, List<TinyTypeEntry> typeEntries)
		{
			if (type.IsGenericParameter)
			{
				throw new InvalidOperationException();
			}
			if (typesDictionary.ContainsKey(type))
			{
				return;
			}
			List<MethodReference> list = new List<MethodReference>();
			ICollection<int> collection = new List<int>();
			if (!type.IsArray && !type.IsInterface() && !type.IsPointer && !type.IsRequiredModifier && !type.IsByReference)
			{
				VTable vTable = new VTableBuilder().VTableFor(context, type);
				if (!type.HasGenericParameters)
				{
					list = vTable.Slots.ToList();
				}
				collection = vTable.InterfaceOffsets.Values;
			}
			if (list.Count > 8192)
			{
				throw new NotSupportedException("The Tiny runtime supports 8,192 virtual methods. The type '" + type.FullName + "' has too many virtual methods.");
			}
			List<TypeReference> list2 = new List<TypeReference>();
			if (!type.IsPointer && !type.IsRequiredModifier && !type.IsByReference)
			{
				foreach (TypeReference @interface in type.GetInterfaces(context))
				{
					list2.Add(@interface);
				}
			}
			if (type.IsArray)
			{
				AddGraftedOnArrayInterfaces(context, type, list2, collection, list);
			}
			List<TypeReference> list3 = new List<TypeReference>();
			for (TypeReference baseType = type.GetBaseType(context); baseType != null; baseType = baseType.GetBaseType(context))
			{
				list3.Add(baseType);
				foreach (TypeReference interface2 in baseType.GetInterfaces(context))
				{
					if (!list2.Contains(interface2))
					{
						list2.Add(interface2);
					}
				}
			}
			if (type.IsInterface())
			{
				collection = new int[list2.Count];
			}
			if (collection.Count() != list2.Count)
			{
				throw new InvalidOperationException("Something went wrong: interfaceOffsets count doesn't equal interfaces count.");
			}
			int num = NumberOfPackedInterfaceOffsetElements(list2.Count, 2);
			int num2 = NumberOfPackedInterfaceOffsetElements(list2.Count, 4);
			uint num3 = (uint)(8 + 4 * (list.Count + list3.Count + list2.Count + num));
			if (context.Global.Parameters.EnableTinyDebugging)
			{
				num3 += 4;
			}
			uint num4 = (uint)(16 + 8 * (list.Count + list3.Count + list2.Count + num2));
			if (context.Global.Parameters.EnableTinyDebugging)
			{
				num4 += 8;
			}
			uint offset = 0u;
			uint offset2 = 0u;
			if (typeEntries.Count != 0)
			{
				TinyTypeEntry tinyTypeEntry = typeEntries[typeEntries.Count - 1];
				offset = tinyTypeEntry.Offset32 + tinyTypeEntry.Size32;
				offset2 = tinyTypeEntry.Offset64 + tinyTypeEntry.Size64;
			}
			byte b = 0;
			if (type.IsInterface())
			{
				b = (byte)(b | 1u);
			}
			if (type.IsAbstract())
			{
				b = (byte)(b | 2u);
			}
			if (type.IsPointer)
			{
				b = (byte)(b | 4u);
			}
			uint packedCounts = (uint)((ushort)((b << 13) + (uint)list.Count) + (list3.Count << 16) + (list2.Count << 24));
			TinyTypeEntry tinyTypeEntry2 = new TinyTypeEntry(type, packedCounts, num3, num4, offset, offset2, list.ToArray(), list3, list2, collection, context.Global.Services.Naming.TinyTypeOffsetNameFor(type));
			typeEntries.Add(tinyTypeEntry2);
			typesDictionary.Add(type, tinyTypeEntry2);
			if (list3.Count > 0)
			{
				AddType(context, list3[0], typesDictionary, typeEntries);
			}
			foreach (TypeReference item in list2)
			{
				AddType(context, item, typesDictionary, typeEntries);
			}
			list3.Reverse();
		}

		private static void AddGraftedOnArrayInterfaces(ReadOnlyContext context, TypeReference type, List<TypeReference> interfaces, ICollection<int> interfaceOffsets, List<MethodReference> virtualMethods)
		{
			int num = 0;
			foreach (GenericInstanceType arrayExtraType in GenericContextAwareVisitor.GetArrayExtraTypes(context, (ArrayType)type))
			{
				interfaces.Add(arrayExtraType);
				interfaceOffsets.Add(num);
				foreach (MethodDefinition method in arrayExtraType.Resolve().Methods)
				{
					MethodReference methodReference = GetGraftedOnArrayMethod(context, method);
					if (methodReference.HasGenericParameters)
					{
						methodReference = Inflater.InflateMethod(GenericContextForGraftedOnArrayMethods(method, arrayExtraType), methodReference.Resolve());
					}
					virtualMethods.Add(methodReference);
					num++;
				}
			}
		}

		private static GenericContext GenericContextForGraftedOnArrayMethods(MethodDefinition method, GenericInstanceType extraInterface)
		{
			GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(method);
			genericInstanceMethod.GenericArguments.Add(extraInterface.GenericArguments[0]);
			return new GenericContext(null, genericInstanceMethod);
		}

		public static int NumberOfPackedInterfaceOffsetElements(int interfacesCount, int numberOfPackedInterfaceOffsetsPerElement)
		{
			return interfacesCount / numberOfPackedInterfaceOffsetsPerElement + ((interfacesCount % numberOfPackedInterfaceOffsetsPerElement != 0) ? 1 : 0);
		}

		private static MethodDefinition GetGraftedOnArrayMethod(ReadOnlyContext context, MethodReference method)
		{
			if (graftedOnArrayMethods.TryGetValue(method.FullName, out var arrayMethodFullName))
			{
				return context.Global.Services.TypeProvider.SystemArray.Methods.Single((MethodDefinition m) => m.FullName == arrayMethodFullName);
			}
			throw new InvalidOperationException("The method '" + method.FullName + "' does not match a grafted-on array method.");
		}
	}
}
