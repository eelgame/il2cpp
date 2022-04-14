using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Marshaling;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly
{
	public class CCWMarshalingFunctionCollection : PerAssemblyScheduledStepAction<GlobalPrimaryCollectionContext>
	{
		protected override string Name => "Collect CCW Marshaling Functions";

		protected override bool Skip(GlobalPrimaryCollectionContext context)
		{
			return false;
		}

		protected override void ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
		{
			PrimaryCollectionContext primaryCollectionContext = context.CreateCollectionContext();
			ICCWMarshallingFunctionCollector cCWMarshallingFunctionCollector = context.Collectors.CCWMarshallingFunctionCollector;
			foreach (TypeDefinition item2 in item.AllDefinedTypes())
			{
				if (MethodWriter.TypeMethodsCanBeDirectlyCalled(primaryCollectionContext, item2) && (item2.NeedsComCallableWrapper(primaryCollectionContext) || NeedsComCallableWrapperForMarshaledType(primaryCollectionContext, item2)))
				{
					cCWMarshallingFunctionCollector.Add(primaryCollectionContext, item2);
				}
			}
		}

		internal static bool NeedsComCallableWrapperForMarshaledType(ReadOnlyContext context, TypeReference type)
		{
			MarshalType[] marshalTypesForMarshaledType = MarshalingUtils.GetMarshalTypesForMarshaledType(context, type);
			for (int i = 0; i < marshalTypesForMarshaledType.Length; i++)
			{
				if (marshalTypesForMarshaledType[i] == MarshalType.WindowsRuntime && type.IsDelegate() && (!(type is TypeSpecification) || type is GenericInstanceType))
				{
					return true;
				}
			}
			return false;
		}
	}
}
