namespace Unity.IL2CPP.Tiny
{
	public class TinyPrimaryWriteResult
	{
		public readonly string StaticConstructorMethodName;

		public readonly string ModuleInitializerMethodName;

		public TinyPrimaryWriteResult(string staticConstructorMethodName, string moduleInitializerMethodName)
		{
			StaticConstructorMethodName = staticConstructorMethodName;
			ModuleInitializerMethodName = moduleInitializerMethodName;
		}
	}
}
