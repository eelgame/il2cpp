namespace Unity.IL2CPP.Contexts.Forking.Steps
{
	public class PrimaryCollectionLateAccessForkingContainer : LateAccessForkingContainer
	{
		public PrimaryCollectionLateAccessForkingContainer(LateContextAccess<ReadOnlyContext> readOnlyContext)
			: base(readOnlyContext)
		{
		}
	}
}
