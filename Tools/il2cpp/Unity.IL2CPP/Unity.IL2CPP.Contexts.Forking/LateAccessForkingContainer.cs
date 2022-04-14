namespace Unity.IL2CPP.Contexts.Forking
{
	public abstract class LateAccessForkingContainer
	{
		public readonly LateContextAccess<ReadOnlyContext> ReadOnlyContext;

		protected LateAccessForkingContainer(LateContextAccess<ReadOnlyContext> readOnlyContext)
		{
			ReadOnlyContext = readOnlyContext;
		}
	}
}
