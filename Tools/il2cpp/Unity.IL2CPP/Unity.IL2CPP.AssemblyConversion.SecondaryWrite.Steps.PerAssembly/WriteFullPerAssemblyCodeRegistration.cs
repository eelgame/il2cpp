using System;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.PerAssembly
{
	public class WriteFullPerAssemblyCodeRegistration : SimpleScheduledStep<GlobalWriteContext>
	{
		protected override string Name => "Write Per Assembly Code Registration";

		protected override bool Skip(GlobalWriteContext context)
		{
			return false;
		}

		protected override void Worker(GlobalWriteContext context)
		{
			SourceWritingContext sourceWritingContext = context.CreateSourceWritingContext();
			CodeRegistrationWriter.WriteCodeRegistration(sourceWritingContext, sourceWritingContext.Global.Results.SecondaryCollection.MethodTables, sourceWritingContext.Global.Results.SecondaryWritePart3.UnresolvedVirtualsTablesInfo, sourceWritingContext.Global.Results.SecondaryCollection.Invokers, Array.Empty<string>().AsReadOnly());
		}
	}
}
