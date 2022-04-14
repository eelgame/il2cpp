using System;
using NiceIO;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Diagnostics;
using Unity.IL2CPP.Options;

namespace Unity.IL2CPP.Contexts.Components
{
	public class DiagnosticsComponent : ComponentBase<object, IDiagnosticsService, DiagnosticsComponent>, IDiagnosticsService
	{
		private class NotAvailable : IDiagnosticsService
		{
			public IDisposable BeginCollectorStateDump(GlobalFullyForkedContext context, string captureName)
			{
				throw new NotSupportedException();
			}
		}

		private class ActionOnDisposeContext : IDisposable
		{
			private readonly Action _action;

			public ActionOnDisposeContext(Action action)
			{
				_action = action;
			}

			public void Dispose()
			{
				_action();
			}
		}

		private class DisabledContext : IDisposable
		{
			public void Dispose()
			{
			}
		}

		private readonly CollectorStateDumper _collectorStateDumper;

		public bool Enabled { get; private set; }

		public NPath OutputDirectory { get; private set; }

		public DiagnosticsComponent()
		{
			_collectorStateDumper = new CollectorStateDumper();
		}

		private DiagnosticsComponent(DiagnosticsComponent other)
		{
			_collectorStateDumper = new CollectorStateDumper(other._collectorStateDumper);
			Enabled = other.Enabled;
			OutputDirectory = other.OutputDirectory;
		}

		public void Initialize(AssemblyConversionContext context)
		{
			if (context.Parameters.DiagnosticOptions.HasFlag(DiagnosticOptions.EnableDiagnostics))
			{
				Enabled = true;
				OutputDirectory = context.InputData.OutputDir.Combine("il2cpp_Diagnostics").EnsureDirectoryExists();
				ConsoleOutput.Info.WriteLine($"Diagnostics Directory : {OutputDirectory}");
			}
		}

		public IDisposable BeginCollectorStateDump(AssemblyConversionContext context, string captureName)
		{
			return BeginCollectorStateDump(context.GlobalReadOnlyContext.GetReadOnlyContext(), context.Collectors, context.StatefulServices, captureName);
		}

		public IDisposable BeginCollectorStateDump(GlobalFullyForkedContext context, string captureName)
		{
			return BeginCollectorStateDump(context.GlobalReadOnlyContext.GetReadOnlyContext(), context.Collectors, context.StatefulServices, captureName);
		}

		private IDisposable BeginCollectorStateDump(ReadOnlyContext context, IUnrestrictedContextCollectorProvider collectors, IUnrestrictedContextStatefulServicesProvider statefulServices, string captureName)
		{
			if (!Enabled)
			{
				return new DisabledContext();
			}
			return new ActionOnDisposeContext(delegate
			{
				_collectorStateDumper.DumpAll(context, collectors, statefulServices, captureName, OutputDirectory);
			});
		}

		protected override void HandleMergeForAdd(DiagnosticsComponent forked)
		{
			throw new NotSupportedException();
		}

		protected override void HandleMergeForMergeValues(DiagnosticsComponent forked)
		{
			_collectorStateDumper.FastForwardDumpCounters(forked._collectorStateDumper);
		}

		protected override DiagnosticsComponent CreateEmptyInstance()
		{
			return new DiagnosticsComponent();
		}

		protected override DiagnosticsComponent CreateCopyInstance()
		{
			return new DiagnosticsComponent(this);
		}

		protected override DiagnosticsComponent ThisAsFull()
		{
			return this;
		}

		protected override IDiagnosticsService ThisAsRead()
		{
			return this;
		}

		protected override object GetNotAvailableWrite()
		{
			throw new NotSupportedException();
		}

		protected override IDiagnosticsService GetNotAvailableRead()
		{
			return new NotAvailable();
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out object writer, out IDiagnosticsService reader, out DiagnosticsComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out object writer, out IDiagnosticsService reader, out DiagnosticsComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out object writer, out IDiagnosticsService reader, out DiagnosticsComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out object writer, out IDiagnosticsService reader, out DiagnosticsComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForFullPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out object writer, out IDiagnosticsService reader, out DiagnosticsComponent full)
		{
			((ComponentBase<object, IDiagnosticsService, DiagnosticsComponent>)this).ReadOnlyFork(out writer, out reader, out full, ForkMode.Copy);
		}

		protected override void ForkForPartialPerAssembly(PerAssemblyLateAccessForkingContainer lateAccess, out object writer, out IDiagnosticsService reader, out DiagnosticsComponent full)
		{
			((ComponentBase<object, IDiagnosticsService, DiagnosticsComponent>)this).ReadOnlyForkWithMergeAbility(out writer, out reader, out full, ForkMode.Copy, MergeMode.MergeValues);
		}
	}
}
