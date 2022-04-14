using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Tiny;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.PerAssembly
{
	public class WriteTinyPerAssemblyCode : PerAssemblyScheduledStepFunc<GlobalWriteContext, TinyPrimaryWriteResult>
	{
		protected override string Name => "Write Tiny Code For Assembly";

		protected override bool Skip(GlobalWriteContext context)
		{
			return !context.Parameters.UsingTinyBackend;
		}

		protected override TinyPrimaryWriteResult ProcessItem(GlobalWriteContext context, AssemblyDefinition item)
		{
			SourceWritingContext context2 = context.CreateSourceWritingContext();
			string staticConstructorMethodName = TinyPrimaryWriteWriters.WriteStaticConstructorInvokerForAssembly(context2, item);
			string moduleInitializerMethodName = TinyPrimaryWriteWriters.WriteModuleInitializerInvokerForAssembly(context2, item);
			return new TinyPrimaryWriteResult(staticConstructorMethodName, moduleInitializerMethodName);
		}
	}
}
