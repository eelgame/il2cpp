using System;

namespace Unity.IL2CPP.Options
{
	[Flags]
	public enum FileGenerationOptions
	{
		None = 1,
		EmitSourceMapping = 8,
		EmitMethodMap = 0x10
	}
}
