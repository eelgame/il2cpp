using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Components
{
	public class TypeCollector : CompletableStatefulComponentBase<ITypeCollectorResults, ITypeCollector, TypeCollector>, ITypeCollector
	{
		private class NotAvailable : ITypeCollector, ITypeCollectorResults
		{
			ReadOnlyCollection<IIl2CppRuntimeType> ITypeCollectorResults.SortedItems
			{
				get
				{
					throw new NotSupportedException();
				}
			}

			int ITypeCollectorResults.GetIndex(IIl2CppRuntimeType typeData)
			{
				throw new NotSupportedException();
			}

			IIl2CppRuntimeType ITypeCollector.Add(TypeReference type, int attrs)
			{
				throw new NotSupportedException();
			}
		}

		private class Results : ITypeCollectorResults
		{
			private readonly IReadOnlyDictionary<IIl2CppRuntimeType, int> _itemLookup;

			public ReadOnlyCollection<IIl2CppRuntimeType> SortedItems { get; }

			public Results(ReadOnlyCollection<IIl2CppRuntimeType> sortedItems, IReadOnlyDictionary<IIl2CppRuntimeType, int> itemLookup)
			{
				SortedItems = sortedItems;
				_itemLookup = itemLookup;
			}

			int ITypeCollectorResults.GetIndex(IIl2CppRuntimeType typeData)
			{
				if (_itemLookup.TryGetValue(typeData, out var value))
				{
					return value;
				}
				throw new InvalidOperationException($"Il2CppTypeIndexFor type {typeData.Type.FullName} does not exist.");
			}
		}

		private readonly Dictionary<Il2CppTypeData, IIl2CppRuntimeType> _data;

		public TypeCollector()
		{
			_data = new Dictionary<Il2CppTypeData, IIl2CppRuntimeType>(new Il2CppTypeDataEqualityComparer());
		}

		public IIl2CppRuntimeType Add(TypeReference type, int attrs = 0)
		{
			AssertNotComplete();
			type = type.WithoutModifiers();
			Il2CppTypeData key = new Il2CppTypeData(type, attrs);
			if (_data.TryGetValue(key, out var value))
			{
				return value;
			}
			if (type.IsGenericInstance)
			{
				GenericInstanceType genericInstanceType = (GenericInstanceType)type;
				value = new Il2CppGenericInstanceRuntimeType(genericInstanceType, attrs, genericInstanceType.GenericArguments.Select((TypeReference t) => Add(t)).ToArray(), Add(genericInstanceType.Resolve()));
			}
			else if (type.IsArray)
			{
				ArrayType arrayType = (ArrayType)type;
				value = new Il2CppArrayRuntimeType(arrayType, attrs, Add(arrayType.ElementType));
			}
			else if (type.IsPointer)
			{
				PointerType pointerType = (PointerType)type;
				value = new Il2CppPtrRuntimeType(pointerType, attrs, Add(pointerType.ElementType));
			}
			else if (type.IsByReference)
			{
				ByReferenceType byReferenceType = (ByReferenceType)type;
				value = new Il2CppByReferenceRuntimeType(byReferenceType, attrs, Add(byReferenceType.ElementType));
			}
			else
			{
				value = new Il2CppRuntimeType(type, attrs);
			}
			_data.Add(key, value);
			return value;
		}

		public bool WasCollected(TypeReference type, int attrs = 0)
		{
			return _data.ContainsKey(new Il2CppTypeData(type, attrs));
		}

		public IReadOnlyCollection<IIl2CppRuntimeType> GetCollectedItems()
		{
			return _data.Values.ToList().AsReadOnly();
		}

		protected override void DumpState(StringBuilder builder)
		{
			foreach (KeyValuePair<Il2CppTypeData, IIl2CppRuntimeType> item in _data.ItemsSortedByKey())
			{
				builder.AppendLine(item.Key.Type.FullName);
				builder.AppendLine($"  Attrs = {item.Key.Attrs}");
			}
		}

		protected override void HandleMergeForAdd(TypeCollector forked)
		{
			foreach (KeyValuePair<Il2CppTypeData, IIl2CppRuntimeType> datum in forked._data)
			{
				_data[datum.Key] = datum.Value;
			}
		}

		protected override void HandleMergeForMergeValues(TypeCollector forked)
		{
			throw new NotSupportedException();
		}

		protected override TypeCollector CreateEmptyInstance()
		{
			return new TypeCollector();
		}

		protected override TypeCollector CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override TypeCollector ThisAsFull()
		{
			return this;
		}

		protected override ITypeCollectorResults GetResults()
		{
			ReadOnlyCollection<IIl2CppRuntimeType> readOnlyCollection = _data.Values.ToSortedCollection();
			Dictionary<IIl2CppRuntimeType, int> dictionary = new Dictionary<IIl2CppRuntimeType, int>(readOnlyCollection.Count, new Il2CppRuntimeTypeEqualityComparer());
			foreach (IIl2CppRuntimeType item in readOnlyCollection)
			{
				dictionary.Add(item, dictionary.Count);
			}
			return new Results(readOnlyCollection, dictionary);
		}

		protected override ITypeCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out ITypeCollector writer, out object reader, out TypeCollector full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out ITypeCollector writer, out object reader, out TypeCollector full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out ITypeCollector writer, out object reader, out TypeCollector full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out ITypeCollector writer, out object reader, out TypeCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}
	}
}
