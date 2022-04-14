using System.Collections.Generic;
using Unity.Cecil.Awesome.Ordering;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results
{
	public class WindowsRuntimeFactoryDataComparer : IComparer<WindowsRuntimeFactoryData>
	{
		public int Compare(WindowsRuntimeFactoryData x, WindowsRuntimeFactoryData y)
		{
			return x.TypeDefinition.Compare(y.TypeDefinition);
		}
	}
}
