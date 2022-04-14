namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class SecondaryCollectionLateAccessForkingContainer : LateAccessForkingContainer
	{
		public SecondaryCollectionLateAccessForkingContainer(LateContextAccess<ReadOnlyContext> readOnlyContext)
			: base(readOnlyContext)
		{
		}
	}
}
