using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Generics;

namespace Unity.IL2CPP
{
	public sealed class ReadOnlyMethodTables
	{
		public readonly ReadOnlyCollection<CollectMethodTables.GenericMethodPointerTableEntry> SortedGenericMethodPointerTableValues;

		public readonly ReadOnlyCollection<CollectMethodTables.GenericMethodAdjustorThunkTableEntry> SortedGenericMethodAdjustorThunkTableValues;

		public readonly ReadOnlyCollection<CollectMethodTables.GenericMethodTableEntry> SortedGenericMethodTableValues;

		public ReadOnlyMethodTables(List<CollectMethodTables.GenericMethodPointerTableEntry> pointerTableValues, List<CollectMethodTables.GenericMethodAdjustorThunkTableEntry> adjustorThunkTableValues, List<CollectMethodTables.GenericMethodTableEntry> methodTableValues)
		{
			SortedGenericMethodPointerTableValues = pointerTableValues.AsReadOnly();
			SortedGenericMethodAdjustorThunkTableValues = adjustorThunkTableValues.AsReadOnly();
			SortedGenericMethodTableValues = methodTableValues.AsReadOnly();
		}
	}
}
