using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Attributes;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.PerAssembly
{
	public class WriteAttributes : PerAssemblyScheduledStepFunc<GlobalWriteContext, ReadOnlyAttributeWriterOutput>
	{
		protected override string Name => "Write Attributes";

		protected override bool Skip(GlobalWriteContext context)
		{
			return context.Parameters.UsingTinyBackend;
		}

		protected override ReadOnlyAttributeWriterOutput ProcessItem(GlobalWriteContext context, AssemblyDefinition item)
		{
			return AttributesWriter.Write(context.CreateSourceWritingContext(), item, context.Results.PrimaryCollection.AttributeSupportData[item]);
		}
	}
}
