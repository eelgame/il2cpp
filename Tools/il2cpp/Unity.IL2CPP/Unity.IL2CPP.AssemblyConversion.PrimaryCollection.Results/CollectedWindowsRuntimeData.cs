using System.Collections.ObjectModel;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results
{
	public class CollectedWindowsRuntimeData
	{
		public readonly ReadOnlyCollection<WindowsRuntimeFactoryData> RuntimeFactories;

		public CollectedWindowsRuntimeData(ReadOnlyCollection<WindowsRuntimeFactoryData> runtimeFactories)
		{
			RuntimeFactories = runtimeFactories;
		}
	}
}
