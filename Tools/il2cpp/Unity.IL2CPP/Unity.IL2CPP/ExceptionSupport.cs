using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.CFG;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public class ExceptionSupport
	{
		internal enum NodeType
		{
			Try,
			Catch,
			Filter,
			Finally,
			Block,
			Root,
			Fault
		}

		public class Node
		{
			private Node _parent;

			private int _index;

			private readonly NodeType _type;

			private readonly Node[] _children;

			private readonly InstructionBlock _block;

			private readonly ExceptionHandler _handler;

			internal int Id { get; set; }

			internal NodeType Type => _type;

			internal InstructionBlock Block => _block;

			internal Node[] Children => _children;

			internal Node Parent => _parent;

			internal bool IsInTryBlock
			{
				get
				{
					Node node = this;
					while (node != null && node.Type != NodeType.Root)
					{
						node = node.Parent;
						if (node.Type == NodeType.Try)
						{
							return true;
						}
						if (node.Type == NodeType.Catch)
						{
							return false;
						}
						if (node.Type == NodeType.Filter)
						{
							return false;
						}
						if (node.Type == NodeType.Finally)
						{
							return false;
						}
						if (node.Type == NodeType.Fault)
						{
							return false;
						}
					}
					return false;
				}
			}

			internal bool IsInCatchBlock
			{
				get
				{
					Node node = this;
					while (node != null && node.Type != NodeType.Root)
					{
						node = node.Parent;
						if (node.Type == NodeType.Try)
						{
							return false;
						}
						if (node.Type == NodeType.Catch)
						{
							return true;
						}
						if (node.Type == NodeType.Filter)
						{
							return false;
						}
						if (node.Type == NodeType.Finally)
						{
							return false;
						}
						if (node.Type == NodeType.Fault)
						{
							return false;
						}
					}
					return false;
				}
			}

			private Node NextSibling
			{
				get
				{
					if (Parent == null)
					{
						return null;
					}
					if (_index == Parent.Children.Length - 1)
					{
						return null;
					}
					return Parent.Children[_index + 1];
				}
			}

			internal ExceptionHandler Handler => _handler;

			private Node Root
			{
				get
				{
					Node node = this;
					while (node != null && node.Type != NodeType.Root)
					{
						node = node.Parent;
					}
					return node;
				}
			}

			internal int Depth
			{
				get
				{
					int num = 0;
					for (Node parent = _parent; parent != null; parent = parent.Parent)
					{
						num++;
					}
					return num;
				}
			}

			internal Node[] CatchNodes
			{
				get
				{
					if (_type != 0)
					{
						throw new NotSupportedException("Cannot find the related finally handler for a non-try block");
					}
					List<Node> list = new List<Node>();
					Node nextSibling = NextSibling;
					while (nextSibling != null && nextSibling.Type == NodeType.Catch)
					{
						list.Add(nextSibling);
						nextSibling = nextSibling.NextSibling;
					}
					return list.ToArray();
				}
			}

			internal Node[] FilterNodes
			{
				get
				{
					if (_type != 0)
					{
						throw new NotSupportedException("Cannot find the related finally handler for a non-try block");
					}
					List<Node> list = new List<Node>();
					Node nextSibling = NextSibling;
					while (nextSibling != null && nextSibling.Type == NodeType.Filter)
					{
						list.Add(nextSibling);
						nextSibling = nextSibling.NextSibling;
					}
					return list.ToArray();
				}
			}

			internal Node FinallyNode
			{
				get
				{
					if (_type != 0)
					{
						throw new NotSupportedException("Cannot find the related finally handler for a non-try block");
					}
					Node nextSibling = NextSibling;
					if (nextSibling == null || nextSibling.Type != NodeType.Finally)
					{
						return null;
					}
					return nextSibling;
				}
			}

			internal Node FaultNode
			{
				get
				{
					if (_type != 0)
					{
						throw new NotSupportedException("Cannot find the related fault handler for a non-try block");
					}
					Node nextSibling = NextSibling;
					if (nextSibling == null || nextSibling.Type != NodeType.Fault)
					{
						return null;
					}
					return nextSibling;
				}
			}

			internal Node ParentTryNode
			{
				get
				{
					Node parent = _parent;
					while (parent != null && parent.Type != 0)
					{
						parent = parent.Parent;
					}
					return parent;
				}
			}

			internal Instruction Start
			{
				get
				{
					for (Node node = this; node != null; node = node.Children[0])
					{
						if (node.Block != null)
						{
							return node.Block.First;
						}
					}
					throw new NotSupportedException("Unsupported Node (" + this?.ToString() + ") with no children!");
				}
			}

			internal Instruction End
			{
				get
				{
					if (Block != null)
					{
						return Block.Last;
					}
					if (_children.Length != 0)
					{
						return _children[_children.Length - 1].End;
					}
					throw new NotSupportedException("Unsupported Node (" + this?.ToString() + ") with no children!");
				}
			}

			internal Node(NodeType type, InstructionBlock block)
				: this(null, type, block, new Node[0], null)
			{
			}

			internal Node(Node parent, NodeType type, InstructionBlock block, Node[] children, ExceptionHandler handler)
			{
				_parent = parent;
				_type = type;
				_block = block;
				_children = children;
				_handler = handler;
				Id = -1;
				if (_block != null && type != NodeType.Block)
				{
					_block.MarkIsAliveRecursive();
				}
				if (_parent != null && _parent.Type != NodeType.Root)
				{
					_block.MarkIsAliveRecursive();
				}
				bool flag = _type != NodeType.Root;
				for (int i = 0; i < children.Length; i++)
				{
					Node node = children[i];
					node._parent = this;
					node._index = i;
					if (flag && node.Block != null)
					{
						node._block.MarkIsAliveRecursive();
					}
				}
			}

			public bool HasCatchOrFilterNodes()
			{
				if (_type != 0)
				{
					throw new NotSupportedException("Cannot find the related catch or filter handler for a non-try block");
				}
				for (Node nextSibling = NextSibling; nextSibling != null; nextSibling = nextSibling.NextSibling)
				{
					NodeType type = nextSibling.Type;
					if ((uint)(type - 1) <= 1u)
					{
						return true;
					}
				}
				return false;
			}

			internal IEnumerable<Node> GetTargetFinallyNodesForJump(int from, int to)
			{
				return from n in Root.Walk((Node node) => IsTargetFinallyNodeForJump(node, @from, to)).Reverse()
					select n.FinallyNode;
			}

			internal IEnumerable<Node> GetTargetFinallyAndFaultNodesForJump(int from, int to)
			{
				return from n in Root.Walk((Node node) => IsTargetFinallyNodeForJump(node, @from, to) || IsTargetFaultNodeForJump(node, @from, to)).Reverse()
					select n.FinallyNode ?? n.FaultNode;
			}

			private static bool IsTargetFinallyNodeForJump(Node node, int from, int to)
			{
				if (node.Type != 0)
				{
					return false;
				}
				Node finallyNode = node.FinallyNode;
				if (finallyNode == null)
				{
					return false;
				}
				if (finallyNode.Handler.TryStart.Offset > from)
				{
					return false;
				}
				if (finallyNode.Handler.TryEnd.Offset <= from)
				{
					return false;
				}
				if (finallyNode.Handler.TryStart.Offset < to && finallyNode.Handler.TryEnd.Offset >= to)
				{
					return false;
				}
				return true;
			}

			private static bool IsTargetFaultNodeForJump(Node node, int from, int to)
			{
				if (node.Type != 0)
				{
					return false;
				}
				if (node.FaultNode == null)
				{
					return false;
				}
				if (node.FaultNode.Handler.TryStart.Offset > from)
				{
					return false;
				}
				if (node.FaultNode.Handler.TryEnd.Offset <= from)
				{
					return false;
				}
				if (node.FaultNode.Handler.HandlerStart.Offset > to)
				{
					return false;
				}
				return true;
			}

			internal Node GetEnclosingFinallyOrFaultNode()
			{
				for (Node node = this; node != null; node = node.Parent)
				{
					if (node.Type == NodeType.Finally || node.Type == NodeType.Fault)
					{
						return node;
					}
				}
				return null;
			}

			private IEnumerable<Node> Walk(Func<Node, bool> filter)
			{
				Queue<Node> queue = new Queue<Node>();
				queue.Enqueue(this);
				while (queue.Count > 0)
				{
					Node current = queue.Dequeue();
					if (filter(current))
					{
						yield return current;
					}
					Node[] children = current.Children;
					foreach (Node item in children)
					{
						queue.Enqueue(item);
					}
				}
			}

			public override string ToString()
			{
				return $"{Enum.GetName(typeof(NodeType), _type)} children: {_children.Length}, depth: {Depth}";
			}
		}

		public const string ActiveExceptions = "__active_exceptions";

		public const string LeaveTargetName = "__leave_targets";

		public const string LocalFilterName = "__filter_local";

		public const string LastUnhandledExceptionName = "__last_unhandled_exception";

		private readonly ReadOnlyContext _context;

		private readonly Node _flowTree;

		private readonly IGeneratedMethodCodeWriter _writer;

		private readonly MethodBody _methodBody;

		private readonly LazyDictionary<Instruction, TryCatchInfo> _infos = new LazyDictionary<Instruction, TryCatchInfo>(() => new TryCatchInfo());

		private readonly Dictionary<Node, HashSet<Instruction>> _leaveTargets = new Dictionary<Node, HashSet<Instruction>>();

		private int _nextId;

		public Node FlowTree => _flowTree;

		public string EmitGetActiveException(TypeReference exceptionType)
		{
			return "IL2CPP_GET_ACTIVE_EXCEPTION(" + _context.Global.Services.Naming.ForVariable(exceptionType) + ")";
		}

		public string EmitPushActiveException(string exceptionExpression)
		{
			return "IL2CPP_PUSH_ACTIVE_EXCEPTION(" + exceptionExpression + ")";
		}

		public string EmitPopActiveException()
		{
			return "IL2CPP_POP_ACTIVE_EXCEPTION()";
		}

		public ExceptionSupport(ReadOnlyContext context, MethodDefinition methodDefinition, InstructionBlock[] blocks, IGeneratedMethodCodeWriter writer)
		{
			_context = context;
			_writer = writer;
			_methodBody = methodDefinition.Body;
			CollectTryCatchInfos(methodDefinition.Body);
			_flowTree = new TryCatchTreeBuilder(_methodBody, blocks, _infos).Build();
		}

		private void AssignIds(Node node)
		{
			Node[] catchNodes;
			if (node.Type == NodeType.Try && node.Id < 0)
			{
				node.Id = _nextId;
				catchNodes = node.CatchNodes;
				for (int i = 0; i < catchNodes.Length; i++)
				{
					catchNodes[i].Id = _nextId;
				}
				Node finallyNode = node.FinallyNode;
				if (finallyNode != null)
				{
					finallyNode.Id = _nextId;
				}
				_nextId++;
			}
			catchNodes = node.Children;
			foreach (Node node2 in catchNodes)
			{
				AssignIds(node2);
			}
		}

		public void Prepare()
		{
			if (_methodBody.HasExceptionHandlers)
			{
				if (HasFinallyOrFaultBlocks(_methodBody))
				{
					_writer.WriteLine("{0} {1} = 0;", _context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.SystemException), "__last_unhandled_exception");
				}
				int num = MaxTryCatchDepth();
				if (num > 0)
				{
					_writer.WriteLine("il2cpp::utils::ExceptionSupportStack<{0}*, {1}> {2};", "RuntimeObject", num, "__active_exceptions");
				}
				int num2 = NumberOfLeaveInstructionsFor(_methodBody);
				if (num2 > 0)
				{
					_writer.WriteLine("il2cpp::utils::ExceptionSupportStack<int32_t, {0}> {1};", num2, "__leave_targets");
				}
				if (_context.Global.Parameters.EnableDebugger)
				{
					AssignIds(_flowTree);
				}
			}
		}

		public void AssignIdsForDebugger()
		{
			if (_methodBody.HasExceptionHandlers)
			{
				AssignIds(_flowTree);
			}
		}

		private static int NumberOfLeaveInstructionsFor(MethodBody body)
		{
			int num = 0;
			foreach (Instruction instruction in body.Instructions)
			{
				if (instruction.OpCode == OpCodes.Leave || instruction.OpCode == OpCodes.Leave_S)
				{
					num++;
				}
			}
			return num;
		}

		private static bool HasFinallyOrFaultBlocks(MethodBody body)
		{
			if (body.HasExceptionHandlers)
			{
				return body.ExceptionHandlers.Any((ExceptionHandler e) => e.HandlerType == ExceptionHandlerType.Fault || e.HandlerType == ExceptionHandlerType.Finally);
			}
			return false;
		}

		private int MaxTryCatchDepth()
		{
			return MaxTryCatchDepth(_flowTree, 0);
		}

		internal static int MaxTryCatchDepth(Node node)
		{
			return MaxTryCatchDepth(node, 0);
		}

		private static int MaxTryCatchDepth(Node node, int depth)
		{
			switch (node.Type)
			{
			case NodeType.Try:
				if (node.HasCatchOrFilterNodes())
				{
					depth++;
				}
				break;
			case NodeType.Catch:
				depth++;
				break;
			}
			if (node.Children.Length == 0)
			{
				return depth;
			}
			return node.Children.Max((Node n) => MaxTryCatchDepth(n, depth));
		}

		internal void PushExceptionOnStackIfNeeded(Node node, Stack<StackInfo> valueStack, TypeResolver typeResolver, TypeDefinition systemException)
		{
			if (node.Type == NodeType.Catch && node.Block != null)
			{
				PushExceptionOnStack(valueStack, typeResolver.Resolve(node.Handler.CatchType ?? systemException));
			}
			else if (node.Type == NodeType.Filter && node.Block != null)
			{
				PushExceptionOnStack(valueStack, systemException);
			}
			else if (node.Parent.Type == NodeType.Catch)
			{
				if (node.Parent.Children[0] == node)
				{
					PushExceptionOnStack(valueStack, typeResolver.Resolve(node.Parent.Handler.CatchType ?? systemException));
				}
			}
			else if (node.Parent.Type == NodeType.Filter && node.Parent.Handler.FilterStart == node.Start)
			{
				PushExceptionOnStack(valueStack, systemException);
			}
		}

		internal IEnumerable<Instruction> LeaveTargetsFor(Node finallyNode)
		{
			if (!_leaveTargets.TryGetValue(finallyNode, out var value))
			{
				yield break;
			}
			foreach (Instruction item in value)
			{
				yield return item;
			}
		}

		internal void AddLeaveTarget(Node finallyNode, Instruction instruction)
		{
			if (!_leaveTargets.TryGetValue(finallyNode, out var value))
			{
				value = new HashSet<Instruction>();
				_leaveTargets[finallyNode] = value;
			}
			value.Add((Instruction)instruction.Operand);
		}

		private void CollectTryCatchInfos(MethodBody body)
		{
			Mono.Collections.Generic.Collection<Instruction> instructions = body.Instructions;
			BuildTryCatchScopeRecursive(instructions, body.ExceptionHandlers);
		}

		private void BuildTryCatchScopeRecursive(IList<Instruction> instructions, IList<ExceptionHandler> handlers)
		{
			if (handlers.Count == 0)
			{
				return;
			}
			int tryStart = handlers.Min((ExceptionHandler h) => h.TryStart.Offset);
			int tryEnd = handlers.Where((ExceptionHandler h) => h.TryStart.Offset == tryStart).Max((ExceptionHandler eh) => eh.TryEnd.Offset);
			System.Collections.ObjectModel.ReadOnlyCollection<ExceptionHandler> readOnlyCollection = handlers.Where((ExceptionHandler h) => h.TryStart.Offset == tryStart && h.TryEnd.Offset == tryEnd).ToSortedCollectionBy((ExceptionHandler h) => h.TryStart.Offset);
			HashSet<ExceptionHandler> hashSet = new HashSet<ExceptionHandler>(handlers.Where((ExceptionHandler h) => (tryStart <= h.TryStart.Offset && h.TryEnd.Offset < tryEnd) || (tryStart < h.TryStart.Offset && h.TryEnd.Offset <= tryEnd)));
			int j;
			for (j = 0; j < instructions.Count && instructions[j].Offset < tryEnd; j++)
			{
			}
			_infos[instructions.Single((Instruction i) => i.Offset == tryStart)].TryStart++;
			_infos[instructions[j]].TryEnd++;
			BuildTryCatchScopeRecursive(instructions, hashSet.ToList());
			handlers = handlers.Except(hashSet).ToArray();
			foreach (ExceptionHandler h2 in readOnlyCollection)
			{
				int blockNesterHandlers = h2.HandlerEnd.Offset;
				int k;
				for (k = 0; k < instructions.Count && instructions[k].Offset < h2.HandlerStart.Offset; k++)
				{
				}
				int l;
				for (l = 0; l < instructions.Count && instructions[l].Offset < blockNesterHandlers; l++)
				{
				}
				int m = 0;
				int n = 0;
				if (h2.HandlerType == ExceptionHandlerType.Filter)
				{
					for (; m < instructions.Count && instructions[m].Offset < h2.FilterStart.Offset; m++)
					{
					}
					for (; n < instructions.Count && instructions[n].Offset < h2.HandlerStart.Offset; n++)
					{
					}
				}
				HashSet<ExceptionHandler> hashSet2 = new HashSet<ExceptionHandler>(handlers.Where((ExceptionHandler e) => (h2.HandlerStart.Offset <= e.TryStart.Offset && e.TryEnd.Offset < blockNesterHandlers) || (h2.HandlerStart.Offset < e.TryStart.Offset && e.TryEnd.Offset <= blockNesterHandlers)));
				if (h2.HandlerType == ExceptionHandlerType.Catch)
				{
					_infos[instructions[k]].CatchStart++;
					_infos[instructions[l]].CatchEnd++;
				}
				else if (h2.HandlerType == ExceptionHandlerType.Finally)
				{
					_infos[instructions[k]].FinallyStart++;
					_infos[instructions[l]].FinallyEnd++;
				}
				else if (h2.HandlerType == ExceptionHandlerType.Fault)
				{
					_infos[instructions[k]].FaultStart++;
					_infos[instructions[l]].FaultEnd++;
				}
				else
				{
					if (h2.HandlerType != ExceptionHandlerType.Filter)
					{
						throw new InvalidOperationException($"Unexpected handler type '{h2.HandlerType}' encountered.");
					}
					_infos[instructions[k]].CatchStart++;
					_infos[instructions[l]].CatchEnd++;
					_infos[instructions[m]].FilterStart++;
					_infos[instructions[k]].FilterEnd++;
				}
				BuildTryCatchScopeRecursive(instructions, hashSet2.ToList());
				handlers = handlers.Except(hashSet2).ToArray();
			}
			BuildTryCatchScopeRecursive(instructions, handlers.Except(readOnlyCollection).ToArray());
		}

		private void PushExceptionOnStack(Stack<StackInfo> valueStack, TypeReference catchType)
		{
			valueStack.Push(new StackInfo($"(({_context.Global.Services.Naming.ForVariable(catchType)}){EmitGetActiveException(catchType)})", catchType));
		}
	}
}
