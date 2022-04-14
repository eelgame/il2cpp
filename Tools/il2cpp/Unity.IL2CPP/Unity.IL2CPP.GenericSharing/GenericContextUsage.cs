using System;

namespace Unity.IL2CPP.GenericSharing
{
	[Flags]
	public enum GenericContextUsage
	{
		None = 0,
		Type = 1,
		Method = 2,
		Both = 3
	}
}
