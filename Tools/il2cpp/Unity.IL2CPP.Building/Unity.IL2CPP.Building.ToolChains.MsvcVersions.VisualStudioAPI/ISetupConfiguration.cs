using System.Runtime.InteropServices;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions.VisualStudioAPI
{
	[ComImport]
	[Guid("42843719-DB4C-46C2-8E7C-64F1816EFD5B")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISetupConfiguration
	{
		IEnumSetupInstances EnumInstances();

		[return: MarshalAs(UnmanagedType.IUnknown)]
		object GetInstanceForCurrentProcess();

		[return: MarshalAs(UnmanagedType.IUnknown)]
		object GetInstanceForPath([In][MarshalAs(UnmanagedType.LPWStr)] string path);
	}
}
