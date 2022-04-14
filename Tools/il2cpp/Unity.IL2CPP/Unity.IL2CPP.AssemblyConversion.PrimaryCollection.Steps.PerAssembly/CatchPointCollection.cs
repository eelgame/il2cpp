using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome.CFG;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Debugger;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.PerAssembly
{
	public class CatchPointCollection : PerAssemblyScheduledStepFunc<GlobalPrimaryCollectionContext, ICatchPointProvider>
	{
		protected override string Name => "Collect Catch Points";

		protected override bool Skip(GlobalPrimaryCollectionContext context)
		{
			return !context.Parameters.EnableDebugger;
		}

		protected override ICatchPointProvider ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item)
		{
			PrimaryCollectionContext context2 = context.CreateCollectionContext();
			CatchPointCollector catchPointCollector = new CatchPointCollector();
			foreach (TypeDefinition allType in item.MainModule.GetAllTypes())
			{
				foreach (MethodDefinition item2 in allType.Methods.Where((MethodDefinition m) => m.HasBody && m.Body.Instructions.Count > 0))
				{
					MethodWriter.AddRetInstructionAtTheEndIfNeeded(item2);
					ControlFlowGraph controlFlowGraph = ControlFlowGraph.Create(item2);
					ExceptionSupport exceptionSupport = new ExceptionSupport(context.GetReadOnlyContext(), item2, controlFlowGraph.Blocks, null);
					exceptionSupport.AssignIdsForDebugger();
					Queue<ExceptionSupport.Node> queue = new Queue<ExceptionSupport.Node>();
					queue.Enqueue(exceptionSupport.FlowTree.Children);
					while (queue.Count > 0)
					{
						ExceptionSupport.Node node = queue.Dequeue();
						queue.Enqueue(node.Children);
						if (node.Type == ExceptionSupport.NodeType.Catch)
						{
							catchPointCollector.AddCatchPoint(context2, item2, node);
						}
					}
				}
			}
			return catchPointCollector;
		}
	}
}
