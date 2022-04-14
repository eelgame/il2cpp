using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.Common.Profiles;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.ILPreProcessor;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps
{
	internal static class SetupSteps
	{
		public static void UpdateCodeConversionCache(AssemblyConversionContext context)
		{
			if (context.Parameters.CodeConversionCache)
			{
				CodeConversionCache codeConversionCache = new CodeConversionCache(context);
				if (!codeConversionCache.IsUpToDate())
				{
					codeConversionCache.Refresh();
				}
			}
		}

		public static void CreateDataDirectory(AssemblyConversionContext context)
		{
			context.InputData.DataFolder.EnsureDirectoryExists();
		}

		public static void CopyEtcFolder(AssemblyConversionContext context)
		{
			if (context.InputData.Profile == Profile.UnityTiny)
			{
				return;
			}
			using (MiniProfiler.Section("CopyEtcFolder"))
			{
				context.InputData.Profile.MonoInstall.EtcDirectory.Copy(context.InputData.DataFolder);
			}
		}

		public static void RegisterCorlib(AssemblyConversionContext context, bool includeWindowsRuntime)
		{
			using (MiniProfiler.Section("RegisterCorlib"))
			{
				context.StatefulServices.Diagnostics.Initialize(context);
				context.Services.TypeProvider.Initialize(context);
				if (includeWindowsRuntime)
				{
					context.Services.WindowsRuntimeProjections.Initialize(context.CreatePrimaryCollectionContext());
				}
				RuntimeImplementedMethods.Register(context.CreatePrimaryCollectionContext());
				context.Services.ICallMapping.Initialize(context);
			}
		}

		public static void PreProcessIL(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			using (MiniProfiler.Section("PreProcessIL"))
			{
				using (MiniProfiler.Section("Inject base types and finalizers into COM and Windows Runtime types"))
				{
					InjectBaseTypesAndFinalizersIntoComAndWindowsRuntimeTypes(context.CreateReadOnlyContext(), assemblies);
				}
				using (MiniProfiler.Section("ApplyDefaultMarshalAsAttribute"))
				{
					ApplyDefaultMarshalAsAttribute(assemblies);
				}
			}
		}

		public static void WriteResources(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			if (context.Parameters.UsingTinyClassLibraries)
			{
				return;
			}
			using (MiniProfiler.Section("WriteResources"))
			{
				WriteEmbeddedResourcesForEachAssembly(context, assemblies);
			}
		}

		private static void WriteEmbeddedResourcesForEachAssembly(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			NPath nPath = context.InputData.DataFolder.Combine("Resources");
			nPath.CreateDirectory();
			foreach (AssemblyDefinition assembly in assemblies)
			{
				if (assembly.MainModule.Resources.Any())
				{
					using (FileStream stream = new FileStream(nPath.Combine($"{assembly.MainModule.Name}-resources.dat").ToString(), FileMode.Create, FileAccess.Write))
					{
						ResourceWriter.WriteEmbeddedResources(assembly, stream);
					}
				}
			}
		}

		private static void InjectBaseTypesAndFinalizersIntoComAndWindowsRuntimeTypes(ReadOnlyContext context, IEnumerable<AssemblyDefinition> assemblies)
		{
			InjectBaseTypesAndFinalizersIntoComAndWindowsRuntimeTypesVisitor injectBaseTypesAndFinalizersIntoComAndWindowsRuntimeTypesVisitor = new InjectBaseTypesAndFinalizersIntoComAndWindowsRuntimeTypesVisitor();
			foreach (AssemblyDefinition assembly in assemblies)
			{
				using (MiniProfiler.Section("ModifyCOMAndWindowsRuntimeTypes in assembly", assembly.Name.Name))
				{
					injectBaseTypesAndFinalizersIntoComAndWindowsRuntimeTypesVisitor.Process(context, assembly);
				}
			}
		}

		private static void ApplyDefaultMarshalAsAttribute(IEnumerable<AssemblyDefinition> assemblies)
		{
			ApplyDefaultMarshalAsAttributeVisitor applyDefaultMarshalAsAttributeVisitor = new ApplyDefaultMarshalAsAttributeVisitor();
			foreach (AssemblyDefinition assembly in assemblies)
			{
				using (MiniProfiler.Section("ApplyDefaultMarshalAsAttributeVisitor in assembly", assembly.Name.Name))
				{
					applyDefaultMarshalAsAttributeVisitor.Process(assembly);
				}
			}
		}
	}
}
