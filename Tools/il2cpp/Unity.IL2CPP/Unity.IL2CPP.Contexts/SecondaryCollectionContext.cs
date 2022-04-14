namespace Unity.IL2CPP.Contexts
{
	public class SecondaryCollectionContext
	{
		public readonly GlobalSecondaryCollectionContext Global;

		public SecondaryCollectionContext(GlobalSecondaryCollectionContext context)
		{
			Global = context;
		}

		public MinimalContext AsMinimal()
		{
			return new MinimalContext(Global.AsMinimal());
		}

		public ReadOnlyContext AsReadonly()
		{
			return Global.GetReadOnlyContext();
		}

		public static implicit operator ReadOnlyContext(SecondaryCollectionContext c)
		{
			return c.AsReadonly();
		}

		public static implicit operator MinimalContext(SecondaryCollectionContext c)
		{
			return c.AsMinimal();
		}
	}
}
