using System;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Base
{
	public abstract class BaseStep<TContext>
	{
		protected class DisabledSection : IDisposable
		{
			public void Dispose()
			{
			}
		}

		protected abstract string Name { get; }

		protected abstract bool Skip(TContext context);
	}
}
