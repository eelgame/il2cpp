using System;

namespace Unity.IL2CPP.Options
{
	[Flags]
	public enum Features
	{
		None = 1,
		EnableReload = 2,
		EnableCodeConversionCache = 4,
		EnableDebugger = 8,
		EnableDeepProfiler = 0x10
	}
}
