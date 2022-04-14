using System;

namespace Unity.IL2CPP.Options
{
	[Flags]
	public enum TestingOptions
	{
		None = 1,
		EnableErrorMessageTest = 2,
		EnableGoogleBenchmark = 4,
		BlockCompiling = 8
	}
}
