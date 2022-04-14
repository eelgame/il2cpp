using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly
{
	public class WritePerAssemblyCodeMetadata : PerAssemblyScheduledStepFuncWithGlobalPostProcessing<GlobalWriteContext, string>
	{
		protected override string Name => "Write Assembly Code Metadata";

		protected override string PostProcessingSectionName => "Write Global Code Metadata";

		protected override bool Skip(GlobalWriteContext context)
		{
			return context.Parameters.UsingTinyBackend;
		}

		protected override string ProcessItem(GlobalWriteContext context, AssemblyDefinition item)
		{
			return PerAssemblyCodeMetadataWriter.Write(context.CreateSourceWritingContext(), item, context.Results.SecondaryCollection.GenericContextCollections[item], null, null);
		}

		protected override void PostProcess(GlobalWriteContext context, ReadOnlyCollection<ResultData<AssemblyDefinition, string>> data)
		{
			SourceWritingContext sourceWritingContext = context.CreateSourceWritingContext();
			CodeRegistrationWriter.WriteCodeRegistration(sourceWritingContext, sourceWritingContext.Global.Results.SecondaryCollection.MethodTables, sourceWritingContext.Global.Results.SecondaryWritePart3.UnresolvedVirtualsTablesInfo, sourceWritingContext.Global.Results.SecondaryCollection.Invokers, data.Select((ResultData<AssemblyDefinition, string> d) => d.Result).ToArray().ToSortedCollection());
		}
	}
}
