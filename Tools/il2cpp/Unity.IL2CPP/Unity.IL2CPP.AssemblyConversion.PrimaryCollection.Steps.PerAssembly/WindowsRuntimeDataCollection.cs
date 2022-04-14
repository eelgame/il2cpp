using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly
{
	public class WindowsRuntimeDataCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, CollectedWindowsRuntimeData>
	{
		protected override string Name => "Collect Windows Runtime Data";

		protected override bool Skip(GlobalPrimaryCollectionContext context)
		{
			return context.Parameters.UsingTinyBackend;
		}

		protected override CollectedWindowsRuntimeData ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
		{
			PrimaryCollectionContext context2 = context.CreateCollectionContext();
			IWindowsRuntimeTypeWithNameCollector windowsRuntimeTypeWithNames = context.Collectors.WindowsRuntimeTypeWithNames;
			List<WindowsRuntimeFactoryData> list = new List<WindowsRuntimeFactoryData>();
			foreach (TypeDefinition item2 in item.AllDefinedTypes())
			{
				if (item2.NeedsWindowsRuntimeFactory())
				{
					list.Add(new WindowsRuntimeFactoryData(item2, context.Collectors.Types.Add(item2)));
				}
				string fullName;
				if (item2.IsExposedToWindowsRuntime())
				{
					if (item2.HasGenericParameters || item2.MetadataType != MetadataType.Class || item2.IsInterface)
					{
						continue;
					}
					fullName = item2.FullName;
				}
				else
				{
					TypeDefinition typeDefinition = context.Services.WindowsRuntime.ProjectToWindowsRuntime(item2);
					if (item2 == typeDefinition)
					{
						continue;
					}
					fullName = typeDefinition.FullName;
				}
				windowsRuntimeTypeWithNames.AddWindowsRuntimeTypeWithName(context2, item2, fullName);
			}
			return new CollectedWindowsRuntimeData(list.ToSortedCollection(new WindowsRuntimeFactoryDataComparer()));
		}
	}
}
