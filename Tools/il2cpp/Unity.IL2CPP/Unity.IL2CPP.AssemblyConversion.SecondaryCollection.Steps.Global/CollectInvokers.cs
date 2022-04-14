using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryCollection.Steps.Global
{
	public class CollectInvokers : PerAssemblyAndGenericsScheduledStepFuncWithGlobalPostProcessingFunc<GlobalSecondaryCollectionContext, ReadOnlyCollection<Il2CppMethodSpec>, InvokerCollection, ReadOnlyInvokerCollection>
	{
		protected override string Name => "Collect Invokers";

		protected override string PostProcessingSectionName => "Merge Invokers";

		protected override bool Skip(GlobalSecondaryCollectionContext context)
		{
			return context.Parameters.UsingTinyBackend;
		}

		protected override InvokerCollection ProcessItem(GlobalSecondaryCollectionContext context, AssemblyDefinition item)
		{
			InvokerCollection invokerCollection = new InvokerCollection();
			SecondaryCollectionContext secondaryCollectionContext = context.CreateCollectionContext();
			foreach (TypeDefinition allType in item.MainModule.GetAllTypes())
			{
				foreach (MethodDefinition item2 in allType.Methods.Where((MethodDefinition method) => !method.DeclaringType.HasGenericParameters && !method.HasGenericParameters))
				{
					invokerCollection.Add(secondaryCollectionContext, item2);
				}
			}
			return invokerCollection;
		}

		protected override InvokerCollection ProcessItem(GlobalSecondaryCollectionContext context, ReadOnlyCollection<Il2CppMethodSpec> item)
		{
			InvokerCollection invokerCollection = new InvokerCollection();
			SecondaryCollectionContext secondaryCollectionContext = context.CreateCollectionContext();
			Il2CppMethodSpec[] array = context.Results.PrimaryWrite.GenericMethods.UnsortedKeys.Where(MethodTables.MethodNeedsTable).ToArray();
			foreach (Il2CppMethodSpec il2CppMethodSpec in array)
			{
				invokerCollection.Add(secondaryCollectionContext, il2CppMethodSpec.GenericMethod);
			}
			return invokerCollection;
		}

		protected override string ProfilerDetailsForItem2(ReadOnlyCollection<Il2CppMethodSpec> workerItem)
		{
			return "Generic Methods";
		}

		protected override ReadOnlyInvokerCollection CreateEmptyResult()
		{
			return null;
		}

		protected override ReadOnlyCollection<object> OrderItemsForScheduling(ReadOnlyCollection<AssemblyDefinition> items, ReadOnlyCollection<ReadOnlyCollection<Il2CppMethodSpec>> items2)
		{
			return items2.Concat(items.Cast<object>()).ToList().AsReadOnly();
		}

		protected override ReadOnlyInvokerCollection PostProcess(GlobalSecondaryCollectionContext context, ReadOnlyCollection<InvokerCollection> data)
		{
			InvokerCollection invokerCollection = new InvokerCollection();
			foreach (InvokerCollection datum in data)
			{
				invokerCollection.Add(datum);
			}
			return invokerCollection.Complete();
		}
	}
}
