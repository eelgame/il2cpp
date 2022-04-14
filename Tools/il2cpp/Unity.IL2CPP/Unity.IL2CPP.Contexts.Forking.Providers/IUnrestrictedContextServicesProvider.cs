using Unity.IL2CPP.Contexts.Components;

namespace Unity.IL2CPP.Contexts.Forking.Providers
{
	public interface IUnrestrictedContextServicesProvider
	{
		ICallMappingComponent ICallMapping { get; }

		GuidProviderComponent GuidProvider { get; }

		TypeProviderComponent TypeProvider { get; }

		AssemblyLoaderComponent AssemblyLoader { get; }

		WindowsRuntimeProjectionsComponent WindowsRuntimeProjections { get; }

		ObjectFactoryComponent Factory { get; }

		AssemblyDependenciesComponent AssemblyDependencies { get; }

		PathFactoryComponent PathFactory { get; }

		ContextScopeServiceComponent ContextScope { get; }
	}
}
