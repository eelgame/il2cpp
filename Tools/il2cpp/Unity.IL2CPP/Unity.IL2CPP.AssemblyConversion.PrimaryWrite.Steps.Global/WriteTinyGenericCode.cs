using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Tiny;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryWrite.Steps.Global
{
	public class WriteTinyGenericCode : SimpleScheduledStepFunc<GlobalWriteContext, TinyPrimaryWriteResult>
	{
		protected override string Name => "Write Tiny Code For Generics";

		protected override bool Skip(GlobalWriteContext context)
		{
			return !context.Parameters.UsingTinyBackend;
		}

		protected override TinyPrimaryWriteResult CreateEmptyResult()
		{
			return null;
		}

		protected override TinyPrimaryWriteResult Worker(GlobalWriteContext context)
		{
			return new TinyPrimaryWriteResult(TinyPrimaryWriteWriters.WriteStaticConstructorInvokerForGenerics(context.CreateSourceWritingContext(), "Generics", context.Results.PrimaryCollection.Generics.Types), null);
		}
	}
}
