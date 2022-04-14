using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Metadata;

namespace Unity.IL2CPP
{
	public class InvokerCollector
	{
		public static ReadOnlyInvokerCollection Collect(ReadOnlyContext context, IEnumerable<AssemblyDefinition> assemblies, IGenericMethodCollectorResults genericMethods)
		{
			InvokerCollection invokerCollection = new InvokerCollection();
			foreach (AssemblyDefinition assembly in assemblies)
			{
				foreach (TypeDefinition allType in assembly.MainModule.GetAllTypes())
				{
					foreach (MethodDefinition item in allType.Methods.Where((MethodDefinition method) => !method.DeclaringType.HasGenericParameters && !method.HasGenericParameters))
					{
						invokerCollection.Add(context, item);
					}
				}
			}
			Il2CppMethodSpec[] array = genericMethods.UnsortedKeys.Where(MethodTables.MethodNeedsTable).ToArray();
			foreach (Il2CppMethodSpec il2CppMethodSpec in array)
			{
				invokerCollection.Add(context, il2CppMethodSpec.GenericMethod);
			}
			return invokerCollection.Complete();
		}
	}
}
