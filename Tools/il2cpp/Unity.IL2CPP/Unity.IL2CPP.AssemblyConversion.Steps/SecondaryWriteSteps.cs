using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Debugger;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps
{
	internal static class SecondaryWriteSteps
	{
		public static void WriteDebuggerTables(GlobalWriteContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			if (!context.Parameters.EnableDebugger)
			{
				return;
			}
			using (MiniProfiler.Section("Write debugger tables"))
			{
				foreach (AssemblyDefinition assembly in assemblies)
				{
					DebugWriter.WriteDebugMetadata(context.CreateSourceWritingContext(), assembly, (SequencePointCollector)context.PrimaryCollectionResults.SequencePoints.GetProvider(assembly), context.PrimaryCollectionResults.CatchPoints.GetCollector(assembly));
				}
			}
		}

		public static void WriteUnresolvedVirtualCalls(GlobalWriteContext context, out UnresolvedVirtualsTablesInfo virtualCallTables)
		{
			if (context.Parameters.UsingTinyBackend)
			{
				virtualCallTables = default(UnresolvedVirtualsTablesInfo);
				return;
			}
			using (MiniProfiler.Section("WriteUnresolvedStubs"))
			{
				virtualCallTables = UnresolvedVirtualCallStubWriter.WriteUnresolvedStubs(context.CreateSourceWritingContext());
			}
		}
	}
}
