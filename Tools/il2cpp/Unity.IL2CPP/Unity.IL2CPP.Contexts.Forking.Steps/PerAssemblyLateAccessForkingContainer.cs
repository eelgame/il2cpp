namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class PerAssemblyLateAccessForkingContainer : LateAccessForkingContainer
	{
		public readonly LateContextAccess<TinyWriteContext> TinyWriteContext;

		public PerAssemblyLateAccessForkingContainer(LateContextAccess<TinyWriteContext> tinyWriteContext, LateContextAccess<ReadOnlyContext> readOnlyContext)
			: base(readOnlyContext)
		{
			TinyWriteContext = tinyWriteContext;
		}
	}
}
