namespace Unity.IL2CPP.Contexts
{
	public class PrimaryCollectionContext
	{
		public readonly GlobalPrimaryCollectionContext Global;

		public PrimaryCollectionContext(GlobalPrimaryCollectionContext context)
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

		public static implicit operator ReadOnlyContext(PrimaryCollectionContext c)
		{
			return c.AsReadonly();
		}

		public static implicit operator MinimalContext(PrimaryCollectionContext c)
		{
			return c.AsMinimal();
		}
	}
}
