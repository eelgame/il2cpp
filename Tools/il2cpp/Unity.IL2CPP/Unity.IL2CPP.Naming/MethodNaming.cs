using Mono.Cecil;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Naming
{
	public static class MethodNaming
	{
		public static string ForMethod(this INamingService naming, MethodReference method)
		{
			return naming.ForMethodNameOnly(method);
		}

		public static string ForMethodAdjustorThunk(this INamingService naming, MethodReference method)
		{
			return naming.ForMethod(method) + "_AdjustorThunk";
		}
	}
}
