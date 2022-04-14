namespace Unity.IL2CPP.Building.BuildDescriptions
{
	public static class DebuggerBuildUtils
	{
		public static DebuggerBuildOptions DetermineBuildOptions(bool enableDebugger, bool debuggerOff)
		{
			if (enableDebugger && debuggerOff)
			{
				return DebuggerBuildOptions.BuildDebuggerRuntimeCodeOnly;
			}
			if (enableDebugger)
			{
				return DebuggerBuildOptions.DebuggerEnabled;
			}
			return DebuggerBuildOptions.DebuggerDisabled;
		}
	}
}
