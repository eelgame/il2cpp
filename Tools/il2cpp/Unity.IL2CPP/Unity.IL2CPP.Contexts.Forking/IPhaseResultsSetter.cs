using System.Collections.ObjectModel;

namespace Unity.IL2CPP.Contexts.Forking
{
	public interface IPhaseResultsSetter<TContext>
	{
		void SetPhaseResults(ReadOnlyCollection<TContext> forkedContexts);
	}
}
