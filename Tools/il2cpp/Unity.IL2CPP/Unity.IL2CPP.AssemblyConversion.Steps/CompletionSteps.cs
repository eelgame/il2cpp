using System.IO;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.Steps
{
	public static class CompletionSteps
	{
		public static void EmitMethodMap(GlobalWriteContext context)
		{
			if (!context.Parameters.EmitMethodMap)
			{
				return;
			}
			NPath symbolsFolder = context.InputData.SymbolsFolder;
			symbolsFolder.EnsureDirectoryExists();
			using (StreamWriter streamWriter = new StreamWriter(symbolsFolder.Combine(context.Services.PathFactory.GetFileName("MethodMap.tsv")).ToString()))
			{
				foreach (MethodReference item in context.Results.PrimaryWrite.Methods.SortedKeys.Concat(context.PrimaryWriteResults.GenericMethods.SortedKeys.Select((Il2CppMethodSpec g) => g.GenericMethod)))
				{
					streamWriter.WriteLine(MethodTables.MethodPointerNameFor(context.GetReadOnlyContext(), item) + "\t" + item.FullName + "\t" + item.DeclaringType.Module.Assembly.Name.Name);
				}
			}
		}

		public static void EmitLineMappingFile(GlobalReadOnlyContext context, ISymbolsCollectorResults symbolsResults, NPath outputPath)
		{
			if (!context.Parameters.EmitSourceMapping)
			{
				return;
			}
			using (MiniProfiler.Section("SymbolsCollection"))
			{
				NPath nPath = outputPath.Combine(context.Services.PathFactory.GetFileName("LineNumberMappings.json"));
				Directory.CreateDirectory(outputPath.ToString());
				using (StreamWriter outputStream = new StreamWriter(nPath.ToString()))
				{
					symbolsResults.SerializeToJson(outputStream);
				}
			}
		}
	}
}
