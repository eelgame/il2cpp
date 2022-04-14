using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.Steps
{
	internal static class MetadataWriteSteps
	{
		public static void WriteGlobalMetadataDatFile(GlobalMetadataWriteContext context)
		{
			context.Services.Factory.CreateMetadataDatWriter(context).Write();
		}
	}
}
