using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Scheduling;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly
{
	public abstract class BasePerAssemblyConverter : BaseAssemblyConverter
	{
		protected ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext> Fork(AssemblyConversionContext context)
		{
			ReadOnlyCollection<BaseConversionContainer> containers = CreateContainers(context);
			return Fork(context, containers, CreateContainerOverrideObjects(containers));
		}

		protected abstract ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext> Fork(AssemblyConversionContext context, ReadOnlyCollection<BaseConversionContainer> containers, ReadOnlyCollection<OverrideObjects> overrideObjects);

		protected abstract ReadOnlyCollection<OverrideObjects> CreateContainerOverrideObjects(ReadOnlyCollection<BaseConversionContainer> containers);

		protected static GenericSharingAnalysisResults RunGenericSharingAnalysis(AssemblyConversionContext context)
		{
			ReadOnlyGlobalPendingResults<GenericSharingAnalysisResults> readOnlyGlobalPendingResults;
			using (PhaseWorkSchedulerNoThreading<GlobalPrimaryCollectionContext> scheduler = new PhaseWorkSchedulerNoThreading<GlobalPrimaryCollectionContext>(context.GlobalPrimaryCollectionContext))
			{
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterSharedGenerics"))
				{
					readOnlyGlobalPendingResults = new GenericSharingAnalysis(includeGenerics: true).Schedule(scheduler, context.Results.Initialize.AllAssembliesOrderedByDependency);
				}
			}
			return readOnlyGlobalPendingResults.Result;
		}

		private static ReadOnlyCollection<BaseConversionContainer> CreateContainers(AssemblyConversionContext context)
		{
			ReadOnlyCollection<AssemblyDefinition> allAssembliesOrderedByDependency = context.Results.Initialize.AllAssembliesOrderedByDependency;
			AssemblyDefinition entryAssembly = context.Results.Initialize.EntryAssembly;
			List<BaseConversionContainer> list = new List<BaseConversionContainer>();
			int num = 0;
			list.Add(new GenericsConversionContainer(allAssembliesOrderedByDependency, num++));
			foreach (AssemblyDefinition item in allAssembliesOrderedByDependency)
			{
				string name = PathFactoryComponent.GenerateFileNamePrefixForAssembly(item);
				string cleanName = context.GlobalReadOnlyContext.Services.Naming.ForCleanAssemblyFileName(item);
				list.Add(new AssemblyConversionContainer(item, item == entryAssembly, name, cleanName, num++));
			}
			return list.AsReadOnly();
		}

		protected static IPhaseWorkScheduler<TContext> CreateHackedScheduler<TContext>(TContext context)
		{
			return new PhaseWorkSchedulerNoThreading<TContext>(context);
		}
	}
}
