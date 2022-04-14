using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IGlobalContextStatefulServicesProvider
	{
		INamingService Naming { get; }

		ISourceAnnotationWriter SourceAnnotationWriter { get; }

		IErrorInformationService ErrorInformation { get; }

		IWorkScheduler Scheduler { get; }

		IDiagnosticsService Diagnostics { get; }

		IMessageLogger MessageLogger { get; }
	}
}
