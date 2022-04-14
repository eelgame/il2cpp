using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome.CFG;

namespace Unity.IL2CPP
{
	internal class TryCatchTreeBuilder
	{
		internal enum ContextType
		{
			Root,
			Block,
			Try,
			Filter,
			Catch,
			Finally,
			Fault
		}

		internal class Context
		{
			public ContextType Type;

			public InstructionBlock Block;

			public ExceptionHandler Handler;

			public List<Context> Children = new List<Context>();
		}

		private readonly MethodBody _methodBody;

		private readonly InstructionBlock[] _blocks;

		private readonly Stack<Context> _contextStack = new Stack<Context>();

		private readonly LazyDictionary<Instruction, TryCatchInfo> _tryCatchInfos;

		public TryCatchTreeBuilder(MethodBody methodBody, InstructionBlock[] blocks, LazyDictionary<Instruction, TryCatchInfo> tryCatchInfos)
		{
			_methodBody = methodBody;
			_blocks = blocks;
			_tryCatchInfos = tryCatchInfos;
		}

		internal ExceptionSupport.Node Build()
		{
			if (!_methodBody.HasExceptionHandlers)
			{
				return BuildTreeWithNoExceptionHandlers();
			}
			return BuildTreeWithExceptionHandlers();
		}

		private ExceptionSupport.Node BuildTreeWithNoExceptionHandlers()
		{
			int num = 0;
			ExceptionSupport.Node[] array = new ExceptionSupport.Node[_blocks.Length];
			InstructionBlock[] blocks = _blocks;
			foreach (InstructionBlock block in blocks)
			{
				array[num++] = new ExceptionSupport.Node(ExceptionSupport.NodeType.Block, block);
			}
			return MakeRoot(array);
		}

		private ExceptionSupport.Node BuildTreeWithExceptionHandlers()
		{
			_contextStack.Push(new Context
			{
				Type = ContextType.Root
			});
			InstructionBlock[] blocks = _blocks;
			foreach (InstructionBlock instructionBlock in blocks)
			{
				Instruction firstInstr;
				Instruction key;
				if (instructionBlock.Last.Next == null)
				{
					if (_contextStack.Count == 1)
					{
						_contextStack.Peek().Children.Add(new Context
						{
							Type = ContextType.Block,
							Block = instructionBlock
						});
						break;
					}
					firstInstr = instructionBlock.First;
					key = instructionBlock.Last;
				}
				else
				{
					firstInstr = instructionBlock.First;
					key = instructionBlock.Last.Next;
				}
				TryCatchInfo tryCatchInfo = _tryCatchInfos[firstInstr];
				TryCatchInfo tryCatchInfo2 = _tryCatchInfos[key];
				if (tryCatchInfo.CatchStart != 0 && tryCatchInfo.FinallyStart != 0)
				{
					throw new NotSupportedException("An instruction cannot start both a catch and a finally block!");
				}
				for (int j = 0; j < tryCatchInfo.FinallyStart; j++)
				{
					_contextStack.Push(new Context
					{
						Type = ContextType.Finally,
						Handler = _methodBody.ExceptionHandlers.Single((ExceptionHandler h) => h.HandlerType == ExceptionHandlerType.Finally && h.HandlerStart == firstInstr)
					});
				}
				for (int k = 0; k < tryCatchInfo.FaultStart; k++)
				{
					_contextStack.Push(new Context
					{
						Type = ContextType.Fault,
						Handler = _methodBody.ExceptionHandlers.Single((ExceptionHandler h) => h.HandlerType == ExceptionHandlerType.Fault && h.HandlerStart == firstInstr)
					});
				}
				for (int l = 0; l < tryCatchInfo.FilterStart; l++)
				{
					_contextStack.Push(new Context
					{
						Type = ContextType.Filter,
						Handler = _methodBody.ExceptionHandlers.Single((ExceptionHandler h) => h.HandlerType == ExceptionHandlerType.Filter && h.FilterStart == firstInstr)
					});
				}
				for (int m = 0; m < tryCatchInfo.CatchStart; m++)
				{
					_contextStack.Push(new Context
					{
						Type = ContextType.Catch,
						Handler = _methodBody.ExceptionHandlers.Single((ExceptionHandler h) => (h.HandlerType == ExceptionHandlerType.Catch || h.HandlerType == ExceptionHandlerType.Filter) && h.HandlerStart == firstInstr)
					});
				}
				for (int n = 0; n < tryCatchInfo.TryStart; n++)
				{
					_contextStack.Push(new Context
					{
						Type = ContextType.Try
					});
				}
				_contextStack.Peek().Children.Add(new Context
				{
					Type = ContextType.Block,
					Block = instructionBlock
				});
				for (int num = 0; num < tryCatchInfo2.FinallyEnd; num++)
				{
					Context context = _contextStack.Pop();
					_contextStack.Peek().Children.Add(new Context
					{
						Type = ContextType.Finally,
						Children = context.Children,
						Handler = context.Handler
					});
				}
				for (int num2 = 0; num2 < tryCatchInfo2.FaultEnd; num2++)
				{
					Context context2 = _contextStack.Pop();
					_contextStack.Peek().Children.Add(new Context
					{
						Type = ContextType.Fault,
						Children = context2.Children,
						Handler = context2.Handler
					});
				}
				for (int num3 = 0; num3 < tryCatchInfo2.FilterEnd; num3++)
				{
					Context context3 = _contextStack.Pop();
					_contextStack.Peek().Children.Add(new Context
					{
						Type = ContextType.Filter,
						Children = context3.Children,
						Handler = context3.Handler
					});
				}
				for (int num4 = 0; num4 < tryCatchInfo2.CatchEnd; num4++)
				{
					Context context4 = _contextStack.Pop();
					_contextStack.Peek().Children.Add(new Context
					{
						Type = ContextType.Catch,
						Children = context4.Children,
						Handler = context4.Handler
					});
				}
				for (int num5 = 0; num5 < tryCatchInfo2.TryEnd; num5++)
				{
					Context context5 = _contextStack.Pop();
					_contextStack.Peek().Children.Add(new Context
					{
						Type = ContextType.Try,
						Children = context5.Children
					});
				}
			}
			if (_contextStack.Count > 1)
			{
				throw new NotSupportedException("Mismatched context depth when building try/catch tree!");
			}
			return MergeAndBuildRootNode(_contextStack.Pop());
		}

		private static ExceptionSupport.Node MergeAndBuildRootNode(Context context)
		{
			int num = 0;
			ExceptionSupport.Node[] array = new ExceptionSupport.Node[context.Children.Count];
			foreach (Context child in context.Children)
			{
				array[num++] = MergeAndBuildRootNodeRecursive(child);
			}
			return MakeRoot(array);
		}

		private static ExceptionSupport.Node MergeAndBuildRootNodeRecursive(Context context)
		{
			int num = 0;
			ExceptionSupport.Node[] array = new ExceptionSupport.Node[context.Children.Count];
			foreach (Context child in context.Children)
			{
				array[num++] = MergeAndBuildRootNodeRecursive(child);
			}
			if (array.Length == 1)
			{
				ExceptionSupport.Node node = array[0];
				if (node.Type == ExceptionSupport.NodeType.Block)
				{
					return new ExceptionSupport.Node(null, NodeTypeFor(context), node.Block, new ExceptionSupport.Node[0], ExceptionHandlerFor(context));
				}
			}
			return new ExceptionSupport.Node(null, NodeTypeFor(context), BlockFor(context), array, ExceptionHandlerFor(context));
		}

		private static ExceptionSupport.NodeType NodeTypeFor(Context context)
		{
			switch (context.Type)
			{
			case ContextType.Root:
				return ExceptionSupport.NodeType.Root;
			case ContextType.Try:
				return ExceptionSupport.NodeType.Try;
			case ContextType.Catch:
				return ExceptionSupport.NodeType.Catch;
			case ContextType.Filter:
				return ExceptionSupport.NodeType.Filter;
			case ContextType.Finally:
				return ExceptionSupport.NodeType.Finally;
			case ContextType.Fault:
				return ExceptionSupport.NodeType.Fault;
			default:
				return ExceptionSupport.NodeType.Block;
			}
		}

		private static InstructionBlock BlockFor(Context context)
		{
			if (context.Type != ContextType.Block)
			{
				return null;
			}
			return context.Block;
		}

		private static ExceptionHandler ExceptionHandlerFor(Context context)
		{
			ContextType type = context.Type;
			if ((uint)(type - 3) <= 3u)
			{
				return context.Handler;
			}
			return null;
		}

		private static ExceptionSupport.Node MakeRoot(ExceptionSupport.Node[] children)
		{
			return new ExceptionSupport.Node(null, ExceptionSupport.NodeType.Root, null, children, null);
		}
	}
}
