using System.Collections.Generic;
using Unity.IL2CPP.Collections;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Metadata
{
	public class Il2CppGenericInstCollectorComponent
	{
		public static GenericInstancesCollection Collect(SourceWritingContext context)
		{
			Dictionary<IIl2CppRuntimeType[], uint> dictionary = new Dictionary<IIl2CppRuntimeType[], uint>(new Il2CppRuntimeTypeArrayEqualityComparer());
			ReadOnlyContext context2 = context.AsReadonly();
			foreach (IIl2CppRuntimeType sortedItem in context.Global.Results.SecondaryCollection.Types.SortedItems)
			{
				if (sortedItem.Type.IsGenericInstance)
				{
					Il2CppGenericInstanceRuntimeType il2CppGenericInstanceRuntimeType = (Il2CppGenericInstanceRuntimeType)sortedItem;
					if (!GenericsUtilities.CheckForMaximumRecursion(context2, il2CppGenericInstanceRuntimeType.Type))
					{
						AddChecked(dictionary, il2CppGenericInstanceRuntimeType.GenericArguments);
					}
				}
			}
			foreach (Il2CppMethodSpec sortedKey in context.Global.Results.PrimaryWrite.GenericMethods.SortedKeys)
			{
				AddChecked(dictionary, sortedKey.MethodGenericInstanceData);
				AddChecked(dictionary, sortedKey.TypeGenericInstanceData);
			}
			return new GenericInstancesCollection(dictionary);
		}

		private static void AddChecked(Dictionary<IIl2CppRuntimeType[], uint> allInstances, IIl2CppRuntimeType[] genericArguments)
		{
			if (genericArguments != null && !allInstances.ContainsKey(genericArguments))
			{
				allInstances.Add(genericArguments, (uint)allInstances.Count);
			}
		}
	}
}
