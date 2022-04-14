using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public class MsvcToolChainAssemblyCodeSearcher : SourceCodeSearcher
	{
		public override string StartRegexForMethodInCode(string methodName)
		{
			methodName = methodName.Replace("::", "_");
			return "^" + methodName + ".* PROC";
		}

		public override string EndRegexForMethodInCode(string methodName)
		{
			methodName = methodName.Replace("::", "_");
			return "^" + methodName + ".* ENDP";
		}
	}
}
