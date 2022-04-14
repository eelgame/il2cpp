using Unity.IL2CPP.Common;

namespace Unity.IL2CPP
{
	public class GeneratedCppCodeMethodSeacher : SourceCodeSearcher
	{
		public override string StartRegexForMethodInCode(string methodName)
		{
			return "^// .*" + methodName + ".*\n.*\n^{";
		}

		public override string EndRegexForMethodInCode(string methodName)
		{
			return "^}$";
		}
	}
}
