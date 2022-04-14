using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome.Ordering;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Diagnostics;
using Unity.IL2CPP.GenericSharing;

namespace Unity.IL2CPP.Contexts.Components
{
	public class RuntimeImplementedMethodWriterCollectorComponent : CompletableStatefulComponentBase<IRuntimeImplementedMethodWriterResults, IRuntimeImplementedMethodWriterCollector, RuntimeImplementedMethodWriterCollectorComponent>, IRuntimeImplementedMethodWriterCollector
	{
		private struct RuntimeImplementedMethodData
		{
			public readonly GetGenericSharingDataDelegate GetGenericSharingData;

			public readonly WriteRuntimeImplementedMethodBodyDelegate WriteRuntimeImplementedMethodBody;

			public RuntimeImplementedMethodData(GetGenericSharingDataDelegate getGenericSharingData, WriteRuntimeImplementedMethodBodyDelegate writeRuntimeImplementedMethodBody)
			{
				GetGenericSharingData = getGenericSharingData;
				WriteRuntimeImplementedMethodBody = writeRuntimeImplementedMethodBody;
			}
		}

		private class NotAvailable : IRuntimeImplementedMethodWriterCollector
		{
			public void RegisterMethod(MethodDefinition method, GetGenericSharingDataDelegate getGenericSharingData, WriteRuntimeImplementedMethodBodyDelegate writeMethodBody)
			{
				throw new NotSupportedException();
			}
		}

		private class Results : IRuntimeImplementedMethodWriterResults
		{
			private ReadOnlyDictionary<MethodDefinition, RuntimeImplementedMethodData> _runtimeImplementedMethods;

			public Results(ReadOnlyDictionary<MethodDefinition, RuntimeImplementedMethodData> runtimeImplementedMethods)
			{
				_runtimeImplementedMethods = runtimeImplementedMethods;
			}

			public bool TryGetWriter(MethodDefinition method, out WriteRuntimeImplementedMethodBodyDelegate value)
			{
				if (_runtimeImplementedMethods.TryGetValue(method, out var value2))
				{
					value = value2.WriteRuntimeImplementedMethodBody;
					return true;
				}
				value = null;
				return false;
			}

			public bool TryGetGenericSharingDataFor(MethodDefinition method, out IEnumerable<RuntimeGenericData> value)
			{
				if (_runtimeImplementedMethods.TryGetValue(method, out var value2))
				{
					value = value2.GetGenericSharingData();
					return true;
				}
				value = null;
				return false;
			}
		}

		private Dictionary<MethodDefinition, RuntimeImplementedMethodData> _runtimeImplementedMethods = new Dictionary<MethodDefinition, RuntimeImplementedMethodData>();

		public void RegisterMethod(MethodDefinition method, GetGenericSharingDataDelegate getGenericSharingData, WriteRuntimeImplementedMethodBodyDelegate writeMethodBody)
		{
			_runtimeImplementedMethods.Add(method, new RuntimeImplementedMethodData(getGenericSharingData, writeMethodBody));
		}

		protected override void DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendCollection(builder, "_runtimeImplementedMethods", _runtimeImplementedMethods.Keys.ToSortedCollection());
		}

		protected override void HandleMergeForAdd(RuntimeImplementedMethodWriterCollectorComponent forked)
		{
			throw new NotSupportedException();
		}

		protected override void HandleMergeForMergeValues(RuntimeImplementedMethodWriterCollectorComponent forked)
		{
			throw new NotSupportedException();
		}

		protected override RuntimeImplementedMethodWriterCollectorComponent CreateEmptyInstance()
		{
			return new RuntimeImplementedMethodWriterCollectorComponent();
		}

		protected override RuntimeImplementedMethodWriterCollectorComponent CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override RuntimeImplementedMethodWriterCollectorComponent ThisAsFull()
		{
			return this;
		}

		protected override IRuntimeImplementedMethodWriterCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override IRuntimeImplementedMethodWriterResults GetResults()
		{
			return new Results(_runtimeImplementedMethods.AsReadOnly());
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPartialPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out IRuntimeImplementedMethodWriterCollector writer, out object reader, out RuntimeImplementedMethodWriterCollectorComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}
	}
}
