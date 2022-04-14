using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP.Contexts
{
	public class TinyWriteContext
	{
		public readonly TinyGlobalWriteContext Global;

		public TinyWriteContext(TinyGlobalWriteContext context)
		{
			Global = context;
		}

		public ReadOnlyContext AsReadonly()
		{
			return Global.GetReadOnlyContext();
		}

		public MinimalContext AsMinimal()
		{
			return Global.CreateMinimalContext();
		}

		public static implicit operator ReadOnlyContext(TinyWriteContext c)
		{
			return c.AsReadonly();
		}

		public static implicit operator MinimalContext(TinyWriteContext c)
		{
			return c.AsMinimal();
		}

		public ICppCodeWriter CreateProfiledSourceWriterInOutputDirectory(string filename)
		{
			return Global.AsBig().CreateProfiledSourceWriterInOutputDirectory(filename);
		}
	}
}
