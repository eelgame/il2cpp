namespace Unity.IL2CPP.Contexts
{
	public class ReadOnlyContext
	{
		public readonly GlobalReadOnlyContext Global;

		public ReadOnlyContext(GlobalReadOnlyContext context)
		{
			Global = context;
		}
	}
}
