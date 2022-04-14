using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;

namespace Unity.IL2CPP
{
	public static class ChunkExtensions
	{
		public static ReadOnlyCollection<ReadOnlyCollection<GenericInstanceType>> ChunkByCodeSize(this ICollection<GenericInstanceType> collection, int sizeOfChunks)
		{
			return ChunkBySize(collection, sizeOfChunks, (GenericInstanceType t) => t.SumOfMethodCodeSize());
		}

		public static ReadOnlyCollection<ReadOnlyCollection<GenericInstanceType>> ChunkByNumberOfChunks(this ICollection<GenericInstanceType> collection, int numberOfChunks)
		{
			return ChunkByNumberOfChunks(collection, numberOfChunks, (GenericInstanceType t) => t.SumOfMethodCodeSize());
		}

		public static ReadOnlyCollection<ReadOnlyCollection<TypeReference>> ChunkByCodeSize(this ICollection<TypeReference> collection, int sizeOfChunks)
		{
			return ChunkBySize(collection, sizeOfChunks, (TypeReference t) => t.SumOfMethodCodeSize());
		}

		public static ReadOnlyCollection<ReadOnlyCollection<TypeDefinition>> ChunkByCodeSize(this ICollection<TypeDefinition> collection, int sizeOfChunks)
		{
			return ChunkBySize(collection, sizeOfChunks, (TypeDefinition t) => t.SumOfMethodCodeSize());
		}

		public static ReadOnlyCollection<ReadOnlyCollection<GenericInstanceMethod>> ChunkByCodeSize(this ICollection<GenericInstanceMethod> collection, int sizeOfChunks)
		{
			return ChunkBySize(collection, sizeOfChunks, (GenericInstanceMethod m) => m.CodeSize());
		}

		public static ReadOnlyCollection<ReadOnlyCollection<GenericInstanceMethod>> ChunkByNumberOfChunks(this ICollection<GenericInstanceMethod> collection, int numberOfChunks)
		{
			return ChunkByNumberOfChunks(collection, numberOfChunks, (GenericInstanceMethod m) => m.CodeSize());
		}

		private static ReadOnlyCollection<ReadOnlyCollection<T>> ChunkBySize<T>(ICollection<T> collection, int sizeOfChunks, Func<T, int> getSize)
		{
			List<ReadOnlyCollection<T>> list = new List<ReadOnlyCollection<T>>();
			List<T> list2 = new List<T>();
			int num = 0;
			foreach (T item in collection)
			{
				num += getSize(item);
				list2.Add(item);
				if (num > sizeOfChunks)
				{
					list.Add(list2.ToList().AsReadOnly());
					list2.Clear();
					num = 0;
				}
			}
			list.Add(list2.ToList().AsReadOnly());
			return list.AsReadOnly();
		}

		private static ReadOnlyCollection<ReadOnlyCollection<T>> ChunkByNumberOfChunks<T>(ICollection<T> collection, int numberOfChunks, Func<T, int> getSize)
		{
			List<ReadOnlyCollection<T>> list = new List<ReadOnlyCollection<T>>();
			List<T> list2 = new List<T>();
			int num = 0;
			foreach (T item in collection)
			{
				num += getSize(item);
			}
			int num2 = num / numberOfChunks;
			int i = 1;
			int num3 = 0;
			foreach (T item2 in collection)
			{
				num3 += getSize(item2);
				list2.Add(item2);
				if (num3 > num2 && i <= numberOfChunks)
				{
					list.Add(list2.ToList().AsReadOnly());
					list2.Clear();
					num3 = 0;
					i++;
				}
			}
			list.Add(list2.ToList().AsReadOnly());
			for (; i < numberOfChunks; i++)
			{
				list.Add(new List<T>().AsReadOnly());
			}
			return list.AsReadOnly();
		}
	}
}
