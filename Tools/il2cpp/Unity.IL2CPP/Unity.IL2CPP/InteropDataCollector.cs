using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP
{
	internal static class InteropDataCollector
	{
		public static ReadOnlyCollection<KeyValuePair<IIl2CppRuntimeType, InteropData>> Collect(SourceWritingContext context)
		{
			Dictionary<IIl2CppRuntimeType, InteropData> dictionary = new Dictionary<IIl2CppRuntimeType, InteropData>(new Il2CppRuntimeTypeEqualityComparer());
			foreach (IIl2CppRuntimeType cCWMarshalingFunction in context.Global.Results.PrimaryCollection.CCWMarshalingFunctions)
			{
				InteropData interopData = new InteropData();
				interopData.HasCreateCCWFunction = true;
				dictionary.Add(cCWMarshalingFunction, interopData);
			}
			foreach (IIl2CppRuntimeType typeMarshallingFunction in context.Global.Results.PrimaryWrite.TypeMarshallingFunctions)
			{
				if (!dictionary.TryGetValue(typeMarshallingFunction, out var value))
				{
					value = new InteropData();
					dictionary.Add(typeMarshallingFunction, value);
				}
				value.HasPInvokeMarshalingFunctions = true;
			}
			foreach (IIl2CppRuntimeType item in context.Global.Results.PrimaryWrite.WrappersForDelegateFromManagedToNative)
			{
				if (!dictionary.TryGetValue(item, out var value2))
				{
					value2 = new InteropData();
					dictionary.Add(item, value2);
				}
				value2.HasDelegatePInvokeWrapperMethod = true;
			}
			foreach (IIl2CppRuntimeType interopGuid in context.Global.Results.PrimaryWrite.InteropGuids)
			{
				if (!dictionary.TryGetValue(interopGuid, out var value3))
				{
					value3 = new InteropData();
					dictionary.Add(interopGuid, value3);
				}
				value3.HasGuid = true;
			}
			return dictionary.ItemsSortedByKey();
		}
	}
}
