using Unity.IL2CPP.Contexts.Components;

namespace Unity.IL2CPP.Contexts.Forking
{
	public class OverrideObjects
	{
		public readonly ImmediateSchedulerComponent Workers;

		public readonly ContextScopeServiceComponent ContextScope;

		public readonly PathFactoryComponent PathFactory;

		public OverrideObjects(ImmediateSchedulerComponent workers)
		{
			Workers = workers;
		}

		public OverrideObjects(PathFactoryComponent pathFactory)
		{
			PathFactory = pathFactory;
		}

		public OverrideObjects(PathFactoryComponent pathFactory, ContextScopeServiceComponent contextScope)
		{
			PathFactory = pathFactory;
			ContextScope = contextScope;
		}
	}
}
