using System;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions.VisualStudioAPI
{
	[Flags]
	public enum InstanceState : uint
	{
		None = 0u,
		Local = 1u,
		Registered = 2u,
		NoRebootRequired = 4u,
		Complete = uint.MaxValue
	}
}
