namespace Unity.IL2CPP.Contexts
{
	public class MinimalContext
	{
		public readonly GlobalMinimalContext Global;

		public MinimalContext(GlobalMinimalContext context)
		{
			Global = context;
		}

		public ReadOnlyContext AsReadonly()
		{
			return Global.GetReadOnlyContext();
		}

		public static implicit operator ReadOnlyContext(MinimalContext d)
		{
			return d.AsReadonly();
		}
	}
}
