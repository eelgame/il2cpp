using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Phases;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Results.Phases;

namespace Unity.IL2CPP.AssemblyConversion.InProcessPerAssembly
{
	public class PartialConverter : BasePerAssemblyConverter
	{
		private class PartialPerAssemblyPhaseResultsSetter : IPhaseResultsSetter<GlobalFullyForkedContext>
		{
			private readonly IUnrestrictedContextDataProvider _parentContext;

			private readonly int _genericsContainerIndex;

			public PartialPerAssemblyPhaseResultsSetter(IUnrestrictedContextDataProvider parentContext, int genericsContainerIndex)
			{
				_parentContext = parentContext;
				_genericsContainerIndex = genericsContainerIndex;
			}

			public void SetPhaseResults(ReadOnlyCollection<GlobalFullyForkedContext> forkedContexts)
			{
				SetPrimaryCollectionPhaseResults(_parentContext, forkedContexts.Select((GlobalFullyForkedContext c) => c.Results.PrimaryCollection).ToArray(), _genericsContainerIndex);
				SetPrimaryWritePhaseResults(_parentContext, forkedContexts.Select((GlobalFullyForkedContext c) => c.Results.PrimaryWrite).ToArray(), _genericsContainerIndex);
			}

			private static void SetPrimaryCollectionPhaseResults(IUnrestrictedContextDataProvider context, AssemblyConversionResults.PrimaryCollectionPhase[] forkedResults, int genericsContainerIndex)
			{
				context.PhaseResults.SetPrimaryCollectionResults(new AssemblyConversionResults.PrimaryCollectionPhase(SequencePointProviderCollection.Merge(forkedResults.Select((AssemblyConversionResults.PrimaryCollectionPhase r) => r.SequencePoints)), CatchPointCollectorCollection.Merge(forkedResults.Select((AssemblyConversionResults.PrimaryCollectionPhase r) => r.CatchPoints)), forkedResults[genericsContainerIndex].Generics, forkedResults.Select((AssemblyConversionResults.PrimaryCollectionPhase r) => r.AttributeSupportData).MergeNoConflictsAllowed(), context.Collectors.WindowsRuntimeTypeWithNames.Complete(), forkedResults.Select((AssemblyConversionResults.PrimaryCollectionPhase r) => r.WindowsRuntimeData).MergeNoConflictsAllowed(), context.Collectors.CCWMarshallingFunctions.Complete(), forkedResults.First().GenericSharingAnalysis));
			}

			private static void SetPrimaryWritePhaseResults(IUnrestrictedContextDataProvider context, AssemblyConversionResults.PrimaryWritePhase[] forkedResults, int genericsContainerIndex)
			{
				context.PhaseResults.SetPrimaryWritePhaseResults(new AssemblyConversionResults.PrimaryWritePhase(context.Collectors.SharedMethods.Complete(), context.Collectors.Methods.Complete(), forkedResults.Select((AssemblyConversionResults.PrimaryWritePhase r) => r.AttributeWriterOutput).MergeNoConflictsAllowed(), context.Collectors.ReversePInvokeWrappers.Complete(), context.Collectors.TypeMarshallingFunctions.Complete(), context.Collectors.WrappersForDelegateFromManagedToNative.Complete(), context.Collectors.InteropGuids.Complete(), context.Collectors.MetadataUsage.Complete(), forkedResults.Select((AssemblyConversionResults.PrimaryWritePhase r) => r.TinyAssemblyResults).MergeNoConflictsAllowed(), forkedResults[genericsContainerIndex].TinyGenericResults, context.Collectors.TinyTypeCollector.Complete(), context.Collectors.TinyStringCollector.Complete(), context.Collectors.GenericMethodCollector.Complete()));
			}
		}

		public override void Run(AssemblyConversionContext context)
		{
			InitializePhase.Run(context);
			SetupPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
			GenericSharingAnalysisResults genericSharingAnalysisResults = BasePerAssemblyConverter.RunGenericSharingAnalysis(context);
			using (BasePerAssemblyConverter.CreateHackedScheduler(new object()))
			{
				using (ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext> forkedContextScope = Fork(context))
				{
					foreach (ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext>.Data item in forkedContextScope.Items)
					{
						BaseConversionContainer value = item.Value;
						GlobalFullyForkedContext context2 = item.Context;
						value.RunPrimaryCollection(context2, genericSharingAnalysisResults);
						value.RunPrimaryWrite(context2);
					}
				}
			}
			ReadOnlyCollection<AssemblyDefinition> allAssembliesOrderedByDependency = context.Results.Initialize.AllAssembliesOrderedByDependency;
			SecondaryCollectionPhase.Run(context, allAssembliesOrderedByDependency);
			SecondaryWritePhase.Run(context, allAssembliesOrderedByDependency);
			MetadataWritePhase.Run(context);
			CompletionPhase.Run(context);
		}

		protected override ForkedContextScope<BaseConversionContainer, GlobalFullyForkedContext> Fork(AssemblyConversionContext context, ReadOnlyCollection<BaseConversionContainer> containers, ReadOnlyCollection<OverrideObjects> overrideObjects)
		{
			return ContextForker.ForPartialPerAssembly(context, containers, overrideObjects, new PartialPerAssemblyPhaseResultsSetter(context.ContextDataProvider, containers.Single((BaseConversionContainer c) => c is GenericsConversionContainer).Index));
		}

		protected override ReadOnlyCollection<OverrideObjects> CreateContainerOverrideObjects(ReadOnlyCollection<BaseConversionContainer> containers)
		{
			return containers.Select((BaseConversionContainer c) => PerAssemblyUtilities.CreateOverrideObjectsForPartial(c.Name, c.CleanName)).ToList().AsReadOnly();
		}
	}
}
