using Unity.IL2CPP.AssemblyConversion.Phases;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.Classic
{
	internal class ClassicConverter : BaseAssemblyConverter
	{
		public override void Run(AssemblyConversionContext context)
		{
			InitializePhase.Run(context);
			SetupPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
			PrimaryCollectionPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
			PrimaryWritePhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency, context.Results.Initialize.EntryAssembly);
			SecondaryCollectionPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
			SecondaryWritePhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency);
			MetadataWritePhase.Run(context);
			CompletionPhase.Run(context);
		}
	}
}
