using System.Runtime.InteropServices;

namespace Unity.IL2CPP.Building.ToolChains.MsvcVersions.VisualStudioAPI
{
	[ComImport]
	[Guid("6380BCFF-41D3-4B2E-8B2E-BF8A6810C848")]
	[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	public interface IEnumSetupInstances
	{
		void Next(int celt, [Out][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.IUnknown)] object[] rgelt, out int pceltFetched);

		void Skip(int celt);

		void Reset();

		IEnumSetupInstances Clone();
	}
}
