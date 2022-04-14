using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.Contexts;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Phases
{
	internal static class CompletionPhase
	{
		public static void Run(AssemblyConversionContext context)
		{
			using (MiniProfiler.Section("CompletionPhase"))
			{
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterEmitMethodMap"))
				{
					CompletionSteps.EmitMethodMap(context.GlobalWriteContext);
				}
				using (context.StatefulServices.Diagnostics.BeginCollectorStateDump(context, "AfterEmitLineMapping"))
				{
					CompletionSteps.EmitLineMappingFile(context.GlobalReadOnlyContext, context.Results.SecondaryWrite.Symbols, context.InputData.SymbolsFolder);
				}
				using (MiniProfiler.Section("Build Results"))
				{
					context.Results.SetCompletionPhaseResults(new AssemblyConversionResults.CompletionPhase(context.Collectors.Stats, context.Collectors.MatchedAssemblyMethodSourceFiles.Complete(), context.StatefulServices.MessageLogger.Complete()));
				}
			}
		}
	}
}
