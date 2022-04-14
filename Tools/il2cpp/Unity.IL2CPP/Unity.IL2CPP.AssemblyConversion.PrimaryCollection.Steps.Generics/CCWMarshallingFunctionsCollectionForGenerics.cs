using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericsCollection;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Generics
{
	public class CCWMarshallingFunctionsCollectionForGenerics : StepAction<GlobalPrimaryCollectionContext>
	{
		private readonly ReadOnlyInflatedCollectionCollector _genericsCollectionData;

		protected override string Name => "Collect Generic CCWMarshallingFunctions";

		public CCWMarshallingFunctionsCollectionForGenerics(ReadOnlyInflatedCollectionCollector genericsCollectionData)
		{
			_genericsCollectionData = genericsCollectionData;
		}

		protected override bool Skip(GlobalPrimaryCollectionContext context)
		{
			return false;
		}

		protected override void Process(GlobalPrimaryCollectionContext context)
		{
			PrimaryCollectionContext primaryCollectionContext = context.CreateCollectionContext();
			foreach (TypeReference instantiatedGenericsAndArray in _genericsCollectionData.InstantiatedGenericsAndArrays)
			{
				if (instantiatedGenericsAndArray.NeedsComCallableWrapper(primaryCollectionContext))
				{
					context.Collectors.CCWMarshallingFunctionCollector.Add(primaryCollectionContext, instantiatedGenericsAndArray);
				}
			}
			foreach (GenericInstanceType typeDeclaration in _genericsCollectionData.TypeDeclarations)
			{
				if (CCWMarshalingFunctionCollection.NeedsComCallableWrapperForMarshaledType(primaryCollectionContext, typeDeclaration))
				{
					context.Collectors.CCWMarshallingFunctionCollector.Add(primaryCollectionContext, typeDeclaration);
				}
			}
		}
	}
}
