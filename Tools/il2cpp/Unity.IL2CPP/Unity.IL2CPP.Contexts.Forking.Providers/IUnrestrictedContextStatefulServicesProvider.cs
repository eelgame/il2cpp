using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IUnrestrictedContextStatefulServicesProvider
	{
		SourceAnnotationWriterComponent SourceAnnotationWriter { get; }

		NamingComponent Naming { get; }

		ErrorInformation ErrorInformation { get; }

		ImmediateSchedulerComponent Scheduler { get; }

		DiagnosticsComponent Diagnostics { get; }

		MessageLoggerComponent MessageLogger { get; }
	}
}
