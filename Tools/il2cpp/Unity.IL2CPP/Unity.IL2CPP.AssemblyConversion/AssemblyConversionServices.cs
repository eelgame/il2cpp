using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.AssemblyConversion
{
	public class AssemblyConversionServices : IGlobalContextServicesProvider, IUnrestrictedContextServicesProvider
	{
		public readonly ICallMappingComponent ICallMapping = new ICallMappingComponent();

		internal readonly GuidProviderComponent GuidProvider = new GuidProviderComponent();

		public readonly TypeProviderComponent TypeProvider = new TypeProviderComponent();

		public readonly AssemblyLoaderComponent AssemblyLoader;

		public readonly AssemblyDependenciesComponent AssemblyDependencies = new AssemblyDependenciesComponent();

		public readonly WindowsRuntimeProjectionsComponent WindowsRuntimeProjections = new WindowsRuntimeProjectionsComponent();

		public readonly ObjectFactoryComponent Factory;

		public readonly PathFactoryComponent PathFactory = new PathFactoryComponent();

		public readonly ContextScopeServiceComponent ContextScope = new ContextScopeServiceComponent();

		ICallMappingComponent IUnrestrictedContextServicesProvider.ICallMapping => ICallMapping;

		GuidProviderComponent IUnrestrictedContextServicesProvider.GuidProvider => GuidProvider;

		TypeProviderComponent IUnrestrictedContextServicesProvider.TypeProvider => TypeProvider;

		AssemblyLoaderComponent IUnrestrictedContextServicesProvider.AssemblyLoader => AssemblyLoader;

		AssemblyDependenciesComponent IUnrestrictedContextServicesProvider.AssemblyDependencies => AssemblyDependencies;

		PathFactoryComponent IUnrestrictedContextServicesProvider.PathFactory => PathFactory;

		IPathFactoryService IGlobalContextServicesProvider.PathFactory => PathFactory;

		WindowsRuntimeProjectionsComponent IUnrestrictedContextServicesProvider.WindowsRuntimeProjections => WindowsRuntimeProjections;

		ObjectFactoryComponent IUnrestrictedContextServicesProvider.Factory => Factory;

		IGuidProvider IGlobalContextServicesProvider.GuidProvider => GuidProvider;

		ITypeProviderService IGlobalContextServicesProvider.TypeProvider => TypeProvider;

		IObjectFactory IGlobalContextServicesProvider.Factory => Factory;

		IWindowsRuntimeProjections IGlobalContextServicesProvider.WindowsRuntime => WindowsRuntimeProjections;

		IICallMappingService IGlobalContextServicesProvider.ICallMapping => ICallMapping;

		IAssemblyDependencyResults IGlobalContextServicesProvider.AssemblyDependencies => AssemblyDependencies;

		IContextScopeService IGlobalContextServicesProvider.ContextScope => ContextScope;

		ContextScopeServiceComponent IUnrestrictedContextServicesProvider.ContextScope => ContextScope;

		public AssemblyConversionServices(LateContextAccess<TinyWriteContext> tinyContextAccess, AssemblyConversionInputData inputData)
		{
			AssemblyLoader = new AssemblyLoaderComponent(inputData);
			Factory = new ObjectFactoryComponent(tinyContextAccess);
		}
	}
}
