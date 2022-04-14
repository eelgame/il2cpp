using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.GenericsCollection;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Generics
{
	public class WindowsRuntimeDataCollectionForGenerics : StepAction<GlobalPrimaryCollectionContext>
	{
		private readonly ReadOnlyInflatedCollectionCollector _genericsCollectionData;

		protected override string Name => "Add Windows Runtime type names";

		public WindowsRuntimeDataCollectionForGenerics(ReadOnlyInflatedCollectionCollector genericsCollectionData)
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
			IWindowsRuntimeTypeWithNameCollector windowsRuntimeTypeWithNames = context.Collectors.WindowsRuntimeTypeWithNames;
			foreach (GenericInstanceType typeDeclaration in _genericsCollectionData.TypeDeclarations)
			{
				TypeDefinition typeDefinition = typeDeclaration.Resolve();
				if ((typeDefinition == context.Services.TypeProvider.IReferenceType || typeDefinition == context.Services.TypeProvider.IReferenceArrayType) && typeDeclaration.IsComOrWindowsRuntimeInterface(primaryCollectionContext))
				{
					windowsRuntimeTypeWithNames.AddWindowsRuntimeTypeWithName(primaryCollectionContext, typeDeclaration, typeDeclaration.GetWindowsRuntimeTypeName(primaryCollectionContext));
					context.Collectors.Stats.RecordWindowsRuntimeBoxedType();
					continue;
				}
				TypeReference typeReference = context.Services.WindowsRuntime.ProjectToWindowsRuntime(typeDeclaration);
				if (typeDeclaration != typeReference && (typeReference.IsComOrWindowsRuntimeInterface(primaryCollectionContext) || typeReference.IsWindowsRuntimeDelegate(primaryCollectionContext)))
				{
					windowsRuntimeTypeWithNames.AddWindowsRuntimeTypeWithName(primaryCollectionContext, typeDeclaration, typeReference.GetWindowsRuntimeTypeName(primaryCollectionContext));
				}
			}
		}
	}
}
