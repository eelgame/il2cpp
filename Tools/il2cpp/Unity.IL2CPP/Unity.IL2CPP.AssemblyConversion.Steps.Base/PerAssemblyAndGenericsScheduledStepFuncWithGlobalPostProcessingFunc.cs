using Mono.Cecil;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class PerAssemblyAndGenericsScheduledStepFuncWithGlobalPostProcessingFunc<TWorkerContext, TGenericsItem, TWorkerResult, TPostProcessResult> : ScheduledTwoInItemsStepFuncWithContinueFunc<TWorkerContext, AssemblyDefinition, TGenericsItem, TWorkerResult, TPostProcessResult>
	{
		protected override string ProfilerDetailsForItem(AssemblyDefinition workerItem)
		{
			return workerItem.Name.Name;
		}
	}
}
