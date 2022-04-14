using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata
{
	public class GenericMethodCollectorComponent : ItemsWithMetadataIndexCollector<Il2CppMethodSpec, IGenericMethodCollectorResults, IGenericMethodCollector, GenericMethodCollectorComponent>, IGenericMethodCollector
	{
		private class Results : MetadataIndexTableResults<Il2CppMethodSpec>, IGenericMethodCollectorResults, IMetadataIndexTableResults<Il2CppMethodSpec>, ITableResults<Il2CppMethodSpec, uint>
		{
			public Results(ReadOnlyCollection<Il2CppMethodSpec> sortedItems, ReadOnlyDictionary<Il2CppMethodSpec, uint> table, ReadOnlyCollection<KeyValuePair<Il2CppMethodSpec, uint>> sortedTable)
				: base(sortedItems, table, sortedTable)
			{
			}

			public uint GetIndex(MethodReference method)
			{
				return GetIndex(new Il2CppMethodSpec(method));
			}

			public bool HasIndex(MethodReference method)
			{
				return HasIndex(new Il2CppMethodSpec(method));
			}

			public bool TryGetValue(MethodReference method, out uint genericMethodIndex)
			{
				return TryGetValue(new Il2CppMethodSpec(method), out genericMethodIndex);
			}
		}

		private class NotAvailable : ItemsWithMetadataIndexCollectorNotAvailable<Il2CppMethodSpec>, IGenericMethodCollector, IGenericMethodCollectorResults, IMetadataIndexTableResults<Il2CppMethodSpec>, ITableResults<Il2CppMethodSpec, uint>
		{
			public uint GetIndex(MethodReference method)
			{
				throw new NotImplementedException();
			}

			public bool HasIndex(MethodReference method)
			{
				throw new NotImplementedException();
			}

			public bool TryGetValue(MethodReference method, out uint genericMethodIndex)
			{
				throw new NotImplementedException();
			}

			public void Add(SourceWritingContext context, MethodReference method)
			{
				throw new NotImplementedException();
			}

			public void Add(PrimaryCollectionContext context, MethodReference method)
			{
				throw new NotImplementedException();
			}

			public void Add(SecondaryCollectionContext context, MethodReference method)
			{
				throw new NotImplementedException();
			}
		}

		public GenericMethodCollectorComponent()
			: base((IEqualityComparer<Il2CppMethodSpec>)new Il2CppMethodSpecEqualityComparer())
		{
		}

		public void Add(SourceWritingContext context, MethodReference method)
		{
			Add(context, context.Global.Collectors.Types, method);
		}

		public void Add(PrimaryCollectionContext context, MethodReference method)
		{
			Add(context, context.Global.Collectors.Types, method);
		}

		public void Add(SecondaryCollectionContext context, MethodReference method)
		{
			Add(context, context.Global.Collectors.Types, method);
		}

		private void Add(ReadOnlyContext context, ITypeCollector typeCollector, MethodReference method)
		{
			Il2CppMethodSpec item = new Il2CppMethodSpec(method);
			if (ContainsInternal(item) || !MetadataUtils.TypeDoesNotExceedMaximumRecursion(context, method.DeclaringType) || (method.IsGenericInstance && !MetadataUtils.TypesDoNotExceedMaximumRecursion(context, ((GenericInstanceMethod)method).GenericArguments)))
			{
				return;
			}
			IIl2CppRuntimeType[] typeGenericInstanceData = null;
			IIl2CppRuntimeType[] methodGenericInstanceData = null;
			if (method.DeclaringType.IsGenericInstance)
			{
				typeGenericInstanceData = ((GenericInstanceType)method.DeclaringType).GenericArguments.Select((TypeReference g) => typeCollector.Add(g)).ToArray();
			}
			if (method.IsGenericInstance)
			{
				methodGenericInstanceData = ((GenericInstanceMethod)method).GenericArguments.Select((TypeReference g) => typeCollector.Add(g)).ToArray();
			}
			AddInternal(new Il2CppMethodSpec(method, methodGenericInstanceData, typeGenericInstanceData));
		}

		protected override GenericMethodCollectorComponent CreateEmptyInstance()
		{
			return new GenericMethodCollectorComponent();
		}

		protected override GenericMethodCollectorComponent ThisAsFull()
		{
			return this;
		}

		protected override IGenericMethodCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IGenericMethodCollector writer, out object reader, out GenericMethodCollectorComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IGenericMethodCollector writer, out object reader, out GenericMethodCollectorComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IGenericMethodCollector writer, out object reader, out GenericMethodCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IGenericMethodCollector writer, out object reader, out GenericMethodCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override ReadOnlyCollection<Il2CppMethodSpec> SortItems(IEnumerable<Il2CppMethodSpec> items)
		{
			return items.ToSortedCollection();
		}

		protected override IGenericMethodCollectorResults CreateResultObject(ReadOnlyCollection<Il2CppMethodSpec> sortedItems, ReadOnlyDictionary<Il2CppMethodSpec, uint> table, ReadOnlyCollection<KeyValuePair<Il2CppMethodSpec, uint>> sortedTable)
		{
			return new Results(sortedItems, table, sortedTable);
		}
	}
}
