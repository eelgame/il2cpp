using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly
{
	public class AssemblyCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, CollectedAssemblyData>
	{
		protected override string Name => "Collect Assembly Data";

		protected override bool Skip(GlobalPrimaryCollectionContext context)
		{
			return false;
		}

		protected override CollectedAssemblyData ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
		{
			if (!context.Parameters.UsingTinyBackend)
			{
				CollectGenericMethodsFromVTableSlots(context, item);
			}
			return new CollectedAssemblyData();
		}

		private static void CollectGenericMethodsFromVTableSlots(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
		{
			foreach (TypeDefinition item2 in item.AllDefinedTypes())
			{
				if (item2.IsInterface && !item2.IsComOrWindowsRuntimeType(context.GetReadOnlyContext()) && context.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(item2) == null)
				{
					continue;
				}
				foreach (MethodReference slot in context.Collectors.VTable.VTableFor(context.GetReadOnlyContext(), item2).Slots)
				{
					if (slot != null && (slot.IsGenericInstance || slot.DeclaringType.IsGenericInstance))
					{
						context.Collectors.GenericMethods.Add(context.CreateCollectionContext(), slot);
					}
				}
			}
		}
	}
}
