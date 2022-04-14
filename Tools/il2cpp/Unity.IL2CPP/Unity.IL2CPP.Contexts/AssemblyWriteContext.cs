using Mono.Cecil;
using Unity.IL2CPP.Debugger;

namespace Unity.IL2CPP.Contexts
{
	public class AssemblyWriteContext
	{
		public readonly GlobalWriteContext Global;

		public readonly SourceWritingContext SourceWritingContext;

		public readonly AssemblyDefinition AssemblyDefinition;

		public readonly ISequencePointProvider SequencePoints;

		public AssemblyWriteContext(SourceWritingContext context, AssemblyDefinition assemblyDefinition)
		{
			AssemblyDefinition = assemblyDefinition;
			SourceWritingContext = context;
			Global = context.Global;
			SequencePoints = Global.PrimaryCollectionResults.SequencePoints.GetProvider(AssemblyDefinition);
		}

		public static AssemblyWriteContext From(SourceWritingContext context, MethodReference method)
		{
			return new AssemblyWriteContext(context, method.Resolve().Module.Assembly);
		}

		public MinimalContext AsMinimal()
		{
			return Global.CreateMinimalContext();
		}

		public ReadOnlyContext AsReadonly()
		{
			return Global.GetReadOnlyContext();
		}

		public static implicit operator ReadOnlyContext(AssemblyWriteContext c)
		{
			return c.AsReadonly();
		}

		public static implicit operator MinimalContext(AssemblyWriteContext c)
		{
			return c.AsMinimal();
		}
	}
}
