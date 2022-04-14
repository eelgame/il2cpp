namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class SecondaryWriteLateAccessForkingContainer : LateAccessForkingContainer
	{
		public readonly LateContextAccess<TinyWriteContext> TinyWriteContext;

		public SecondaryWriteLateAccessForkingContainer(LateContextAccess<TinyWriteContext> tinyWriteContext, LateContextAccess<ReadOnlyContext> readOnlyContext)
			: base(readOnlyContext)
		{
			TinyWriteContext = tinyWriteContext;
		}
	}
}
