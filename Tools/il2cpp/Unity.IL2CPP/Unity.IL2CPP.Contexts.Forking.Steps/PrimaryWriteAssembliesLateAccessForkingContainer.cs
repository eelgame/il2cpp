namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class PrimaryWriteAssembliesLateAccessForkingContainer : LateAccessForkingContainer
	{
		public readonly LateContextAccess<TinyWriteContext> TinyWriteContext;

		public PrimaryWriteAssembliesLateAccessForkingContainer(LateContextAccess<TinyWriteContext> tinyWriteContext, LateContextAccess<ReadOnlyContext> readOnlyContext)
			: base(readOnlyContext)
		{
			TinyWriteContext = tinyWriteContext;
		}
	}
}
