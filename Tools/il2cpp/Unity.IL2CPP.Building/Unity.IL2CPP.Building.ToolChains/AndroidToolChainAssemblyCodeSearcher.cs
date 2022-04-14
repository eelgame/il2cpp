using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class AndroidToolChainAssemblyCodeSearcher : SourceCodeSearcher
	{
		public override string StartRegexForMethodInCode(string methodName)
		{
			methodName = methodName.Replace("::", "_");
			return "^" + methodName + ".*: ";
		}

		public override string EndRegexForMethodInCode(string methodName)
		{
			return "^\\s*\\.fnend";
		}
	}
}
