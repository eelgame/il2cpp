using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results.Phases;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Components
{
	public class VirtualCallCollector : ItemsWithMetadataIndexCollector<IIl2CppRuntimeType[], IVirtualCallCollectorResults, IVirtualCallCollector, VirtualCallCollector>, IVirtualCallCollector
	{
		private class Results : MetadataIndexTableResults<IIl2CppRuntimeType[]>, IVirtualCallCollectorResults, IMetadataIndexTableResults<IIl2CppRuntimeType[]>, ITableResults<IIl2CppRuntimeType[], uint>
		{
			public Results(ReadOnlyCollection<IIl2CppRuntimeType[]> sortedItems, ReadOnlyDictionary<IIl2CppRuntimeType[], uint> table, ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType[], uint>> sortedTable)
				: base(sortedItems, table, sortedTable)
			{
			}
		}

		private class NotAvailable : IVirtualCallCollector
		{
			public void Add(SourceWritingContext context, MethodReference method)
			{
				throw new NotSupportedException();
			}

			public void AddRange(SourceWritingContext context, IEnumerable<MethodReference> methods)
			{
				throw new NotSupportedException();
			}
		}

		public VirtualCallCollector()
			: base((IEqualityComparer<IIl2CppRuntimeType[]>)new Il2CppRuntimeTypeArrayEqualityComparer())
		{
		}

		protected override IVirtualCallCollectorResults CreateResultObject(ReadOnlyCollection<IIl2CppRuntimeType[]> sortedItems, ReadOnlyDictionary<IIl2CppRuntimeType[], uint> table, ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType[], uint>> sortedTable)
		{
			return new Results(sortedItems, table, sortedTable);
		}

		public void Add(SourceWritingContext context, MethodReference method)
		{
			if (!method.IsGenericInstance)
			{
				IIl2CppRuntimeType[] item = MethodToSignature(context, method);
				AddInternal(item);
			}
		}

		public void AddRange(SourceWritingContext context, IEnumerable<MethodReference> methods)
		{
			foreach (MethodReference method in methods)
			{
				Add(context, method);
			}
		}

		private static IIl2CppRuntimeType[] MethodToSignature(SourceWritingContext context, MethodReference method)
		{
			if (GenericSharingAnalysis.CanShareMethod(context, method))
			{
				method = GenericSharingAnalysis.GetSharedMethod(context, method);
			}
			TypeResolver typeResolver = new TypeResolver(method.DeclaringType as GenericInstanceType, method as GenericInstanceMethod);
			IIl2CppRuntimeType[] array = new IIl2CppRuntimeType[method.Parameters.Count + 1];
			array[0] = context.Global.Collectors.Types.Add(TypeFor(typeResolver.ResolveReturnType(method)));
			for (int i = 0; i < method.Parameters.Count; i++)
			{
				array[i + 1] = context.Global.Collectors.Types.Add(TypeFor(typeResolver.ResolveParameterType(method, method.Parameters[i])));
			}
			return array;
		}

		private static TypeReference TypeFor(TypeReference type)
		{
			if (type.IsByReference || !type.IsValueType())
			{
				return type.Module.TypeSystem.Object;
			}
			if (type.MetadataType == MetadataType.Boolean)
			{
				return type.Module.TypeSystem.SByte;
			}
			if (type.MetadataType == MetadataType.Char)
			{
				return type.Module.TypeSystem.Int16;
			}
			if (type.IsEnum())
			{
				return type.GetUnderlyingEnumType();
			}
			return type;
		}

		protected override ReadOnlyCollection<IIl2CppRuntimeType[]> SortItems(IEnumerable<IIl2CppRuntimeType[]> items)
		{
			return items.ToSortedCollection();
		}

		protected override VirtualCallCollector CreateEmptyInstance()
		{
			return new VirtualCallCollector();
		}

		protected override VirtualCallCollector ThisAsFull()
		{
			return this;
		}

		protected override IVirtualCallCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IVirtualCallCollector writer, out object reader, out VirtualCallCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IVirtualCallCollector writer, out object reader, out VirtualCallCollector full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IVirtualCallCollector writer, out object reader, out VirtualCallCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IVirtualCallCollector writer, out object reader, out VirtualCallCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}
	}
}
