using Mono.Cecil.Cil;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Naming
{
	public static class VariableNaming
	{
		public static string ForVariableName(this INamingService naming, VariableReference variable)
		{
			return "V_" + variable.Index;
		}
	}
}
