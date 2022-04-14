using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Results;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class PerAssemblyScheduledStepFunc<TWorkerContext, TWorkerResult> : ScheduledItemsStepFunc<TWorkerContext, AssemblyDefinition, TWorkerResult, PerAssemblyPendingResults<TWorkerResult>, ReadOnlyPerAssemblyPendingResults<TWorkerResult>>
	{
		protected override PerAssemblyPendingResults<TWorkerResult> CreateScheduleResult()
		{
			return new PerAssemblyPendingResults<TWorkerResult>();
		}

		protected override ReadOnlyPerAssemblyPendingResults<TWorkerResult> CreateReadOnlyPendingResults(PerAssemblyPendingResults<TWorkerResult> pendingResults)
		{
			return new ReadOnlyPerAssemblyPendingResults<TWorkerResult>(pendingResults);
		}

		protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
		{
			return workerItem.Name.Name;
		}
	}
}
