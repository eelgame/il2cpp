using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components
{
	public class StatsComponent : StatefulComponentBase<IStatsWriterService, object, StatsComponent>, IStatsWriterService, IStatsResults
	{
		private int _totalNullChecks;

		private readonly Dictionary<string, int> _nullCheckMethodsCount = new Dictionary<string, int>();

		private readonly HashSet<string> _nullChecksMethods = new HashSet<string>();

		private readonly HashSet<string> _arrayBoundsChecksMethods = new HashSet<string>();

		private readonly HashSet<string> _divideByZeroChecksMethods = new HashSet<string>();

		private readonly HashSet<string> _memoryBarrierMethods = new HashSet<string>();

		private readonly HashSet<string> _methodsWithTailCalls = new HashSet<string>();

		private readonly HashSet<string> _sharableMethods = new HashSet<string>();

		private long _metadataTotal;

		private readonly Dictionary<string, long> _metadataStreams = new Dictionary<string, long>();

		private int _windowsRuntimeBoxedTypes;

		private int _windowsRuntimeTypesWithNames;

		private int _interfacesImplementedOnIl2CppComObject;

		private int _comCallableWrappers;

		private int _arrayComCallableWrappers;

		private int _implementedComCallableWrapperMethods;

		private int _strippedComCallableWrapperMethods;

		private int _forwardedToBaseClassComCallableWrapperMethods;

		public long ConversionMilliseconds { get; set; }

		public int JobCount { get; set; }

		public int FilesWritten { get; private set; }

		public int TypesConverted { get; private set; }

		public int StringLiterals { get; private set; }

		public int Methods { get; private set; }

		public int GenericTypeMethods { get; private set; }

		public int GenericMethods { get; private set; }

		public int ShareableMethods => _sharableMethods.Count;

		public bool EnableNullChecksRecording { get; set; }

		public bool EnableArrayBoundsCheckRecording { get; set; }

		public bool EnableDivideByZeroCheckRecording { get; set; }

		public int TailCallsEncountered => _methodsWithTailCalls.Count;

		public int WindowsRuntimeBoxedTypes => _windowsRuntimeBoxedTypes;

		public int WindowsRuntimeTypesWithNames => _windowsRuntimeTypesWithNames;

		public int NativeToManagedInterfaceAdapters => _interfacesImplementedOnIl2CppComObject;

		public int ComCallableWrappers => _comCallableWrappers;

		public int ArrayComCallableWrappers => _arrayComCallableWrappers;

		public int ImplementedComCallableWrapperMethods => _implementedComCallableWrapperMethods;

		public int StrippedComCallableWrapperMethods => _strippedComCallableWrapperMethods;

		public int ForwardedToBaseClassComCallableWrapperMethods => _forwardedToBaseClassComCallableWrapperMethods;

		public long MetadataTotal => _metadataTotal;

		public int TotalNullChecks => _totalNullChecks;

		public ReadOnlyDictionary<string, long> MetadataStreams => _metadataStreams.AsReadOnly();

		public Dictionary<string, int> NullCheckMethodsCount => _nullCheckMethodsCount;

		public HashSet<string> NullChecksMethods => _nullChecksMethods;

		public HashSet<string> ArrayBoundsChecksMethods => _arrayBoundsChecksMethods;

		public HashSet<string> DivideByZeroChecksMethods => _divideByZeroChecksMethods;

		public HashSet<string> MemoryBarrierMethods => _memoryBarrierMethods;

		public HashSet<string> SharableMethods => _sharableMethods;

		public void RecordFileWritten(NPath path)
		{
			FilesWritten++;
		}

		public void RecordSharableMethod(MethodReference method)
		{
			_sharableMethods.Add(method.FullName);
		}

		public void RecordNullCheckEmitted(MethodDefinition methodDefinition)
		{
			_totalNullChecks++;
			if (EnableNullChecksRecording)
			{
				string fullName = methodDefinition.FullName;
				if (!_nullCheckMethodsCount.ContainsKey(fullName))
				{
					_nullCheckMethodsCount[fullName] = 0;
				}
				_nullCheckMethodsCount[fullName]++;
				_nullChecksMethods.Add(fullName);
			}
		}

		public void RecordArrayBoundsCheckEmitted(MethodDefinition methodDefinition)
		{
			if (EnableArrayBoundsCheckRecording)
			{
				_arrayBoundsChecksMethods.Add(methodDefinition.FullName);
			}
		}

		public void RecordDivideByZeroCheckEmitted(MethodDefinition methodDefinition)
		{
			if (EnableDivideByZeroCheckRecording)
			{
				_divideByZeroChecksMethods.Add(methodDefinition.FullName);
			}
		}

		public void RecordTailCall(MethodDefinition methodDefinition)
		{
			_methodsWithTailCalls.Add(methodDefinition.FullName);
		}

		public void RecordMemoryBarrierEmitted(MethodDefinition methodDefinition)
		{
			_memoryBarrierMethods.Add(methodDefinition.FullName);
		}

		public void RecordStringLiteral(string str)
		{
			StringLiterals++;
		}

		public void RecordMethod(MethodReference method)
		{
			Methods++;
			if (method.DeclaringType.IsGenericInstance)
			{
				GenericTypeMethods++;
			}
			if (method.IsGenericInstance)
			{
				GenericMethods++;
			}
		}

		public void RecordMetadataStream(string name, long size)
		{
			_metadataTotal += size;
			_metadataStreams.Add(name, size);
		}

		public void RecordWindowsRuntimeBoxedType()
		{
			_windowsRuntimeBoxedTypes++;
		}

		public void RecordWindowsRuntimeTypeWithName()
		{
			_windowsRuntimeTypesWithNames++;
		}

		public void RecordNativeToManagedInterfaceAdapter()
		{
			_interfacesImplementedOnIl2CppComObject++;
		}

		public void RecordComCallableWrapper()
		{
			_comCallableWrappers++;
		}

		public void RecordArrayComCallableWrapper()
		{
			_arrayComCallableWrappers++;
		}

		public void RecordImplementedComCallableWrapperMethod()
		{
			_implementedComCallableWrapperMethods++;
		}

		public void RecordStrippedComCallableWrapperMethod()
		{
			_strippedComCallableWrapperMethods++;
		}

		public void RecordForwardedToBaseClassComCallableWrapperMethod()
		{
			_forwardedToBaseClassComCallableWrapperMethods++;
		}

		protected override void DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendTable(builder, "_nullCheckMethodsCount", _nullCheckMethodsCount.ItemsSortedByKey());
			CollectorStateDumper.AppendCollection(builder, "_nullChecksMethods", _nullChecksMethods.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_arrayBoundsChecksMethods", _arrayBoundsChecksMethods.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_divideByZeroChecksMethods", _divideByZeroChecksMethods.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_memoryBarrierMethods", _memoryBarrierMethods.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_methodsWithTailCalls", _methodsWithTailCalls.ToSortedCollection());
			CollectorStateDumper.AppendCollection(builder, "_sharableMethods", _sharableMethods.ToSortedCollection());
			CollectorStateDumper.AppendTable(builder, "_metadataStreams", _metadataStreams.ItemsSortedByKey());
			CollectorStateDumper.AppendValue(builder, "_metadataTotal", _metadataTotal);
			CollectorStateDumper.AppendValue(builder, "_totalNullChecks", _totalNullChecks);
			CollectorStateDumper.AppendValue(builder, "_windowsRuntimeBoxedTypes", _windowsRuntimeBoxedTypes);
			CollectorStateDumper.AppendValue(builder, "_windowsRuntimeTypesWithNames", _windowsRuntimeTypesWithNames);
			CollectorStateDumper.AppendValue(builder, "_interfacesImplementedOnIl2CppComObject", _interfacesImplementedOnIl2CppComObject);
			CollectorStateDumper.AppendValue(builder, "_comCallableWrappers", _comCallableWrappers);
			CollectorStateDumper.AppendValue(builder, "_arrayComCallableWrappers", _arrayComCallableWrappers);
			CollectorStateDumper.AppendValue(builder, "_implementedComCallableWrapperMethods", _implementedComCallableWrapperMethods);
			CollectorStateDumper.AppendValue(builder, "_strippedComCallableWrapperMethods", _strippedComCallableWrapperMethods);
			CollectorStateDumper.AppendValue(builder, "_forwardedToBaseClassComCallableWrapperMethods", _forwardedToBaseClassComCallableWrapperMethods);
			CollectorStateDumper.AppendValue(builder, "ConversionMilliseconds", ConversionMilliseconds);
			CollectorStateDumper.AppendValue(builder, "FilesWritten", FilesWritten);
			CollectorStateDumper.AppendValue(builder, "TypesConverted", TypesConverted);
			CollectorStateDumper.AppendValue(builder, "StringLiterals", StringLiterals);
			CollectorStateDumper.AppendValue(builder, "Methods", Methods);
			CollectorStateDumper.AppendValue(builder, "GenericTypeMethods", GenericTypeMethods);
			CollectorStateDumper.AppendValue(builder, "GenericMethods", GenericMethods);
		}

		protected override void HandleMergeForAdd(StatsComponent forked)
		{
			_totalNullChecks += forked._totalNullChecks;
			_nullCheckMethodsCount.MergeWithMergeConflicts(forked._nullCheckMethodsCount, (int p, int f) => p + f);
			_nullChecksMethods.Merge(forked._nullChecksMethods);
			_arrayBoundsChecksMethods.Merge(forked._arrayBoundsChecksMethods);
			_divideByZeroChecksMethods.Merge(forked._divideByZeroChecksMethods);
			_memoryBarrierMethods.Merge(forked._memoryBarrierMethods);
			_methodsWithTailCalls.Merge(forked._methodsWithTailCalls);
			_sharableMethods.Merge(forked._sharableMethods);
			_metadataTotal += forked._metadataTotal;
			_metadataStreams.MergeWithMergeConflicts(forked._metadataStreams, (long p, long f) => p + f);
			_windowsRuntimeBoxedTypes += forked._windowsRuntimeBoxedTypes;
			_windowsRuntimeTypesWithNames += forked._windowsRuntimeTypesWithNames;
			_interfacesImplementedOnIl2CppComObject += forked._interfacesImplementedOnIl2CppComObject;
			_comCallableWrappers += forked._comCallableWrappers;
			_arrayComCallableWrappers += forked._arrayComCallableWrappers;
			_implementedComCallableWrapperMethods += forked._implementedComCallableWrapperMethods;
			_strippedComCallableWrapperMethods += forked._strippedComCallableWrapperMethods;
			_forwardedToBaseClassComCallableWrapperMethods += forked._forwardedToBaseClassComCallableWrapperMethods;
			FilesWritten += forked.FilesWritten;
			TypesConverted += forked.TypesConverted;
			StringLiterals += forked.StringLiterals;
			Methods += forked.Methods;
			GenericTypeMethods += forked.GenericTypeMethods;
			GenericMethods += forked.GenericMethods;
		}

		protected override void HandleMergeForMergeValues(StatsComponent forked)
		{
			throw new NotSupportedException();
		}

		protected override StatsComponent CreateEmptyInstance()
		{
			return new StatsComponent();
		}

		protected override StatsComponent CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override StatsComponent ThisAsFull()
		{
			return this;
		}

		protected override object ThisAsRead()
		{
			throw new NotSupportedException();
		}

		protected override IStatsWriterService GetNotAvailableWrite()
		{
			throw new NotSupportedException();
		}

		protected override object GetNotAvailableRead()
		{
			throw new NotSupportedException();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IStatsWriterService writer, out object reader, out StatsComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IStatsWriterService writer, out object reader, out StatsComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IStatsWriterService writer, out object reader, out StatsComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IStatsWriterService writer, out object reader, out StatsComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out IStatsWriterService writer, out object reader, out StatsComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}
	}
}
