using System;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class ScheduledStep<TWorkerContext> : BaseStep<TWorkerContext>
	{
		protected IDisposable CreateProfilerSectionAroundScheduling(bool parallelEnabled)
		{
			if (parallelEnabled)
			{
				return new DisabledSection();
			}
			return MiniProfiler.Section(StepUtilities.FormatAllStepName(Name));
		}
	}
}
