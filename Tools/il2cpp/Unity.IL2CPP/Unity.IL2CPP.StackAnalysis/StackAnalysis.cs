using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.CFG;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.StackAnalysis
{
	public class StackAnalysis
	{
		private readonly ReadOnlyContext _context;

		private readonly TypeResolver _typeResolver;

		private readonly ControlFlowGraph _cfg;

		private readonly MethodDefinition _methodDefinition;

		private readonly Dictionary<InstructionBlock, StackState> _ins = new Dictionary<InstructionBlock, StackState>();

		private readonly Dictionary<InstructionBlock, StackState> _outs = new Dictionary<InstructionBlock, StackState>();

		private GlobalVariable[] _globalGlobalVariables;

		public GlobalVariable[] Globals => _globalGlobalVariables ?? (_globalGlobalVariables = ComputeGlobalVariables());

		public static StackAnalysis Analyze(MethodWriteContext context, ControlFlowGraph cfg)
		{
			return Analyze(context, context.MethodDefinition, context.TypeResolver, cfg);
		}

		public static StackAnalysis Analyze(ReadOnlyContext context, MethodDefinition method, TypeResolver typeResolver, ControlFlowGraph cfg)
		{
			StackAnalysis stackAnalysis = new StackAnalysis(context, method, typeResolver, cfg);
			stackAnalysis.Analyze();
			return stackAnalysis;
		}

		private StackAnalysis(ReadOnlyContext context, MethodDefinition method, TypeResolver typeResolver, ControlFlowGraph cfg)
		{
			_context = context;
			_methodDefinition = method;
			_cfg = cfg;
			_typeResolver = typeResolver;
		}

		private void Analyze()
		{
			InstructionBlock[] blocks = _cfg.Blocks;
			foreach (InstructionBlock instructionBlock in blocks)
			{
				if (!_ins.TryGetValue(instructionBlock, out var value))
				{
					value = new StackState();
				}
				StackState stackState = StackStateBuilder.StackStateFor(_context, _methodDefinition, _typeResolver, instructionBlock, value);
				_outs.Add(instructionBlock, stackState.Clone());
				foreach (InstructionBlock successor in instructionBlock.Successors)
				{
					if (!_ins.ContainsKey(successor))
					{
						_ins[successor] = new StackState();
					}
					_ins[successor].Merge(stackState);
				}
			}
		}

		public StackState InputStackStateFor(InstructionBlock block)
		{
			if (!_ins.TryGetValue(block, out var value))
			{
				return new StackState();
			}
			return value;
		}

		public StackState OutputStackStateFor(InstructionBlock block)
		{
			if (!_outs.TryGetValue(block, out var value))
			{
				return new StackState();
			}
			return value;
		}

		public GlobalVariable GlobalInputVariableFor(Entry entry)
		{
			return GlobaVariableFor(_ins, entry);
		}

		public GlobalVariable[] InputVariablesFor(InstructionBlock block)
		{
			return InputStackStateFor(block).Entries.Select(GlobalInputVariableFor).ToArray();
		}

		private GlobalVariable[] ComputeGlobalVariables()
		{
			List<GlobalVariable> list = new List<GlobalVariable>();
			foreach (KeyValuePair<InstructionBlock, StackState> entry in _ins.Where((KeyValuePair<InstructionBlock, StackState> e) => !e.Value.IsEmpty))
			{
				int index = 0;
				list.AddRange(entry.Value.Entries.Select((Entry item) => new GlobalVariable
				{
					BlockIndex = entry.Key.Index,
					Index = index++,
					Type = ComputeType(item)
				}));
			}
			return list.ToArray();
		}

		private TypeReference ComputeType(Entry entry)
		{
			if (entry.Types.Any((TypeReference t) => t.ContainsGenericParameters()))
			{
				throw new NotImplementedException();
			}
			if (entry.Types.Count == 1)
			{
				return entry.Types.Single();
			}
			if (entry.Types.Any((TypeReference t) => t.IsValueType()))
			{
				TypeReference widestValueType = StackAnalysisUtils.GetWidestValueType(entry.Types);
				if (widestValueType != null)
				{
					return widestValueType;
				}
				if (entry.Types.All((TypeReference t) => t.Resolve().IsEnum))
				{
					widestValueType = StackAnalysisUtils.GetWidestValueType(entry.Types.Select((TypeReference t) => t.Resolve().GetUnderlyingEnumType()));
					if (widestValueType != null)
					{
						return widestValueType;
					}
				}
				if (entry.Types.Any((TypeReference t) => t.IsSameType(_context.Global.Services.TypeProvider.SystemUIntPtr)))
				{
					return _context.Global.Services.TypeProvider.SystemUIntPtr;
				}
				if (entry.Types.Any((TypeReference t) => t.IsSameType(_context.Global.Services.TypeProvider.SystemIntPtr)))
				{
					return _context.Global.Services.TypeProvider.SystemIntPtr;
				}
				if (entry.Types.Any((TypeReference t) => t.MetadataType == MetadataType.Var))
				{
					throw new NotImplementedException("Unexpected Entry with Type == MetadataType.Var");
				}
				throw new NotImplementedException();
			}
			if (entry.Types.All((TypeReference t) => t.IsSameType(_context.Global.Services.TypeProvider.SystemIntPtr) || t.IsPointer))
			{
				return _context.Global.Services.TypeProvider.SystemIntPtr;
			}
			if (entry.Types.All((TypeReference t) => t.IsSameType(_context.Global.Services.TypeProvider.SystemUIntPtr) || t.IsPointer))
			{
				return _context.Global.Services.TypeProvider.SystemUIntPtr;
			}
			if (entry.Types.Select((TypeReference t) => t.Resolve()).Any((TypeDefinition res) => res?.IsInterface ?? false))
			{
				return entry.Types.First((TypeReference t) => t.Resolve().IsInterface);
			}
			if (entry.NullValue)
			{
				TypeReference typeReference = entry.Types.FirstOrDefault((TypeReference t) => t.MetadataType != MetadataType.Object);
				if (typeReference != null)
				{
					return typeReference;
				}
				return entry.Types.First();
			}
			TypeReference typeReference2 = entry.Types.FirstOrDefault((TypeReference t) => t.MetadataType == MetadataType.Object);
			if (typeReference2 != null)
			{
				return typeReference2;
			}
			return entry.Types.First();
		}

		private GlobalVariable GlobaVariableFor(Dictionary<InstructionBlock, StackState> stackStates, Entry entry)
		{
			InstructionBlock instructionBlock = BlockFor(stackStates, entry);
			return new GlobalVariable
			{
				BlockIndex = instructionBlock.Index,
				Index = StackIndexFor(stackStates, entry, instructionBlock),
				Type = ComputeType(entry)
			};
		}

		private static int StackIndexFor(IDictionary<InstructionBlock, StackState> stackStates, Entry entry, InstructionBlock block)
		{
			StackState stackState = stackStates[block];
			int num = 0;
			foreach (Entry entry2 in stackState.Entries)
			{
				if (entry2 == entry)
				{
					return num;
				}
				num++;
			}
			throw new ArgumentException("invalid Entry", "entry");
		}

		private static InstructionBlock BlockFor(Dictionary<InstructionBlock, StackState> stackStates, Entry entry)
		{
			return (from s in stackStates
				where s.Value.Entries.Contains(entry)
				select s.Key).FirstOrDefault() ?? throw new ArgumentException("invalid Entry", "entry");
		}
	}
}
