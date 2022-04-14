using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class StepAction<TContext> : BaseStep<TContext>
	{
		public void Run(TContext context)
		{
			using (MiniProfiler.Section(Name))
			{
				if (!Skip(context))
				{
					Process(context);
				}
			}
		}

		protected abstract void Process(TContext context);
	}
}
