using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.AssemblyConversion
{
	public class AssemblyConversionStatefulServices : IGlobalContextStatefulServicesProvider, IUnrestrictedContextStatefulServicesProvider
	{
		public readonly SourceAnnotationWriterComponent SourceAnnotationWriter = new SourceAnnotationWriterComponent();

		public readonly NamingComponent Naming;

		public readonly ErrorInformation ErrorInformation = new ErrorInformation();

		public readonly ImmediateSchedulerComponent Scheduler = new ImmediateSchedulerComponent();

		public readonly DiagnosticsComponent Diagnostics = new DiagnosticsComponent();

		public readonly MessageLoggerComponent MessageLogger = new MessageLoggerComponent();

		SourceAnnotationWriterComponent IUnrestrictedContextStatefulServicesProvider.SourceAnnotationWriter => SourceAnnotationWriter;

		NamingComponent IUnrestrictedContextStatefulServicesProvider.Naming => Naming;

		ErrorInformation IUnrestrictedContextStatefulServicesProvider.ErrorInformation => ErrorInformation;

		ImmediateSchedulerComponent IUnrestrictedContextStatefulServicesProvider.Scheduler => Scheduler;

		DiagnosticsComponent IUnrestrictedContextStatefulServicesProvider.Diagnostics => Diagnostics;

		MessageLoggerComponent IUnrestrictedContextStatefulServicesProvider.MessageLogger => MessageLogger;

		IDiagnosticsService IGlobalContextStatefulServicesProvider.Diagnostics => Diagnostics;

		IMessageLogger IGlobalContextStatefulServicesProvider.MessageLogger => MessageLogger;

		IWorkScheduler IGlobalContextStatefulServicesProvider.Scheduler => Scheduler;

		INamingService IGlobalContextStatefulServicesProvider.Naming => Naming;

		ISourceAnnotationWriter IGlobalContextStatefulServicesProvider.SourceAnnotationWriter => SourceAnnotationWriter;

		IErrorInformationService IGlobalContextStatefulServicesProvider.ErrorInformation => ErrorInformation;

		public AssemblyConversionStatefulServices(LateContextAccess<ReadOnlyContext> readOnlyContextAccess)
		{
			Naming = new NamingComponent(readOnlyContextAccess);
		}
	}
}
