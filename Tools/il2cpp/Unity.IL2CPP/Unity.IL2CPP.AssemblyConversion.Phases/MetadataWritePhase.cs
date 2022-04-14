using Unity.IL2CPP.AssemblyConversion.Steps;
using Unity.IL2CPP.Contexts;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Phases
{
	internal static class MetadataWritePhase
	{
		public static void Run(AssemblyConversionContext context, bool includeMetadata = true)
		{
			using (MiniProfiler.Section("MetadataWritePhase"))
			{
				MetadataWriteSteps.WriteGlobalMetadataDatFile(context.GlobalMetadataWriteContext);
			}
		}
	}
}
