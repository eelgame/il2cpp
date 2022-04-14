using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Diagnostics;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Contexts.Components
{
	public class MetadataUsageCollectorComponent : CompletableStatefulComponentBase<IMetadataUsageCollectorResults, IMetadataUsageCollectorWriterService, MetadataUsageCollectorComponent>, IMetadataUsageCollectorWriterService
	{
		private class Results : IMetadataUsageCollectorResults
		{
			private readonly ReadOnlyHashSet<IIl2CppRuntimeType> _types;

			private readonly ReadOnlyHashSet<IIl2CppRuntimeType> _typeInfos;

			private readonly ReadOnlyHashSet<MethodReference> _inflatedMethods;

			private readonly ReadOnlyHashSet<Il2CppRuntimeFieldReference> _fieldInfos;

			private readonly ReadOnlyHashSet<StringMetadataToken> _stringLiterals;

			private readonly ReadOnlyDictionary<string, MethodMetadataUsage> _usages;

			public int UsageCount => _types.Count + _typeInfos.Count + _inflatedMethods.Count + _fieldInfos.Count + _stringLiterals.Count;

			public Results(ReadOnlyHashSet<IIl2CppRuntimeType> types, ReadOnlyHashSet<IIl2CppRuntimeType> typeInfos, ReadOnlyHashSet<MethodReference> inflatedMethods, ReadOnlyHashSet<Il2CppRuntimeFieldReference> fieldInfos, ReadOnlyHashSet<StringMetadataToken> stringLiterals, ReadOnlyDictionary<string, MethodMetadataUsage> usages)
			{
				_types = types;
				_typeInfos = typeInfos;
				_inflatedMethods = inflatedMethods;
				_fieldInfos = fieldInfos;
				_stringLiterals = stringLiterals;
				_usages = usages;
			}

			public ReadOnlyHashSet<IIl2CppRuntimeType> GetTypeInfos()
			{
				return _typeInfos;
			}

			public ReadOnlyHashSet<IIl2CppRuntimeType> GetIl2CppTypes()
			{
				return _types;
			}

			public ReadOnlyHashSet<MethodReference> GetInflatedMethods()
			{
				return _inflatedMethods;
			}

			public ReadOnlyHashSet<Il2CppRuntimeFieldReference> GetFieldInfos()
			{
				return _fieldInfos;
			}

			public ReadOnlyHashSet<StringMetadataToken> GetStringLiterals()
			{
				return _stringLiterals;
			}

			public IReadOnlyCollection<KeyValuePair<string, MethodMetadataUsage>> GetUsages()
			{
				return _usages;
			}
		}

		private class NotAvailable : IMetadataUsageCollectorWriterService
		{
			public void Add(string identifier, MethodMetadataUsage usage)
			{
				throw new NotSupportedException();
			}
		}

		private readonly HashSet<IIl2CppRuntimeType> _types = new HashSet<IIl2CppRuntimeType>(new Il2CppRuntimeTypeEqualityComparer());

		private readonly HashSet<IIl2CppRuntimeType> _typeInfos = new HashSet<IIl2CppRuntimeType>(new Il2CppRuntimeTypeEqualityComparer());

		private readonly HashSet<MethodReference> _inflatedMethods = new HashSet<MethodReference>(new MethodReferenceComparer());

		private readonly HashSet<Il2CppRuntimeFieldReference> _fieldInfos = new HashSet<Il2CppRuntimeFieldReference>(new Il2CppRuntimeFieldReferenceEqualityComparer());

		private readonly HashSet<StringMetadataToken> _stringLiterals = new HashSet<StringMetadataToken>(new StringMetadataTokenComparer());

		private readonly Dictionary<string, MethodMetadataUsage> _usages = new Dictionary<string, MethodMetadataUsage>();

		public void Add(string identifier, MethodMetadataUsage usage)
		{
			_usages.Add(identifier, usage);
			foreach (IIl2CppRuntimeType il2CppType in usage.GetIl2CppTypes())
			{
				_types.Add(il2CppType);
			}
			foreach (IIl2CppRuntimeType typeInfo in usage.GetTypeInfos())
			{
				_typeInfos.Add(typeInfo);
			}
			foreach (MethodReference inflatedMethod in usage.GetInflatedMethods())
			{
				_inflatedMethods.Add(inflatedMethod);
			}
			foreach (Il2CppRuntimeFieldReference fieldInfo in usage.GetFieldInfos())
			{
				_fieldInfos.Add(fieldInfo);
			}
			foreach (StringMetadataToken stringLiteral in usage.GetStringLiterals())
			{
				_stringLiterals.Add(stringLiteral);
			}
		}

		protected override void DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendCollection(builder, "_types", _types.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_types", _typeInfos.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_inflatedMethods", _inflatedMethods.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_fieldInfos", _fieldInfos.Select((Il2CppRuntimeFieldReference f) => f.Field).ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_stringLiterals", _stringLiterals.ToSortedCollectionBy((StringMetadataToken item) => item.Literal));
			CollectorStateDumper.AppendTable(builder, "_usages", _usages.ItemsSortedByKeyToString(), null, (MethodMetadataUsage value) => value.UsageCount.ToString());
		}

		protected override void HandleMergeForAdd(MetadataUsageCollectorComponent forked)
		{
			foreach (KeyValuePair<string, MethodMetadataUsage> usage in forked._usages)
			{
				Add(usage.Key, usage.Value);
			}
		}

		protected override void HandleMergeForMergeValues(MetadataUsageCollectorComponent forked)
		{
			throw new NotSupportedException();
		}

		protected override MetadataUsageCollectorComponent CreateEmptyInstance()
		{
			return new MetadataUsageCollectorComponent();
		}

		protected override MetadataUsageCollectorComponent CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override MetadataUsageCollectorComponent ThisAsFull()
		{
			return this;
		}

		protected override IMetadataUsageCollectorWriterService GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IMetadataUsageCollectorWriterService writer, out object reader, out MetadataUsageCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IMetadataUsageCollectorWriterService writer, out object reader, out MetadataUsageCollectorComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IMetadataUsageCollectorWriterService writer, out object reader, out MetadataUsageCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IMetadataUsageCollectorWriterService writer, out object reader, out MetadataUsageCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override IMetadataUsageCollectorResults GetResults()
		{
			return new Results(_types.AsReadOnly(), _typeInfos.AsReadOnly(), _inflatedMethods.AsReadOnly(), _fieldInfos.AsReadOnly(), _stringLiterals.AsReadOnly(), _usages.AsReadOnly());
		}
	}
}
