using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Generic
{
	public class WriteGenericsPseudoCodeGenModule : SimpleScheduledStep<GlobalWriteContext>
	{
		private readonly string _cleanName;

		protected override string Name => "Write Generics Pseudo Code Gen Module";

		public WriteGenericsPseudoCodeGenModule(string cleanName)
		{
			_cleanName = cleanName;
		}

		protected override bool Skip(GlobalWriteContext context)
		{
			return false;
		}

		protected override void Worker(GlobalWriteContext context)
		{
			PerAssemblyCodeMetadataWriter.WriteGenericsPseudoCodeGenModule(context.CreateSourceWritingContext(), _cleanName, MetadataCacheWriter.RegistrationTableName(context.GetReadOnlyContext()), CodeRegistrationWriter.CodeRegistrationTableName(context.GetReadOnlyContext()));
		}
	}
}
