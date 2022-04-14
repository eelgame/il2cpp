using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions.VisualStudioAPI
{
	[ComImport]
	[Guid("177F0C4A-1CD3-4DE7-A32C-71DBBB9FA36D")]
	public class SetupConfiguration : ISetupConfiguration
	{
		[MethodImpl(MethodImplOptions.InternalCall)]
		public extern IEnumSetupInstances EnumInstances();

		[MethodImpl(MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		public extern object GetInstanceForCurrentProcess();

		[MethodImpl(MethodImplOptions.InternalCall)]
		[return: MarshalAs(UnmanagedType.IUnknown)]
		public extern object GetInstanceForPath([In][MarshalAs(UnmanagedType.LPWStr)] string path);

		// [MethodImpl(MethodImplOptions.InternalCall)]
		// public extern SetupConfiguration();
	}
}
