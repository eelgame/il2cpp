using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.Contexts;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Phases
{
	internal static class SetupPhase
	{
		public static void Run(AssemblyConversionContext context, ReadOnlyCollection<AssemblyDefinition> assemblies, bool includeWindowsRuntime = true)
		{
			using (MiniProfiler.Section("SetupPhase"))
			{
				SetupSteps.UpdateCodeConversionCache(context);
				SetupSteps.RegisterCorlib(context, includeWindowsRuntime);
				SetupSteps.CreateDataDirectory(context);
				SetupSteps.PreProcessIL(context, assemblies);
				SetupSteps.WriteResources(context, assemblies);
				SetupSteps.CopyEtcFolder(context);
				using (MiniProfiler.Section("Build Results"))
				{
					context.Results.SetSetupPhaseResults(new AssemblyConversionResults.SetupPhase(context.Collectors.RuntimeImplementedMethodWriterCollector.Complete()));
				}
			}
		}
	}
}
