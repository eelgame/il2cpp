using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions.VisualStudioAPI
{
	[ComImport]
	[Guid("89143C9A-05AF-49B0-B717-72E218A2185C")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface ISetupInstance2
	{
		[return: MarshalAs(UnmanagedType.BStr)]
		string GetInstanceId();

		[return: MarshalAs(UnmanagedType.Struct)]
		FILETIME GetInstallDate();

		[return: MarshalAs(UnmanagedType.BStr)]
		string GetInstallationName();

		[return: MarshalAs(UnmanagedType.BStr)]
		string GetInstallationPath();

		[return: MarshalAs(UnmanagedType.BStr)]
		string GetInstallationVersion();

		[return: MarshalAs(UnmanagedType.BStr)]
		string GetDisplayName([In][MarshalAs(UnmanagedType.U4)] int lcid = 0);

		[return: MarshalAs(UnmanagedType.BStr)]
		string GetDescription([In][MarshalAs(UnmanagedType.U4)] int lcid = 0);

		[return: MarshalAs(UnmanagedType.BStr)]
		string ResolvePath([In][MarshalAs(UnmanagedType.LPWStr)] string pwszRelativePath = null);

		[return: MarshalAs(UnmanagedType.U4)]
		InstanceState GetState();

		[return: MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_UNKNOWN)]
		ISetupPackageReference[] GetPackages();

		ISetupPackageReference GetProduct();

		[return: MarshalAs(UnmanagedType.BStr)]
		string GetProductPath();

		[return: MarshalAs(UnmanagedType.IUnknown)]
		object GetErrors();
	}
}
