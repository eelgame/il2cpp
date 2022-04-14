using System;

namespace Unity.IL2CPP.Options
{
	[Flags]
	public enum GenericsOptions
	{
		None = 1,
		EnableSharing = 2,
		EnableEnumTypeSharing = 4,
		EnablePrimitiveValueTypeGenericSharing = 8
	}
}
