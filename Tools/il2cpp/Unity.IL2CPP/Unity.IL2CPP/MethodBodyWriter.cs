using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.CFG;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Debugger;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.StackAnalysis;

namespace Unity.IL2CPP
{
	[DebuggerDisplay("{_methodDefinition}")]
	public class MethodBodyWriter
	{
		private enum Signedness
		{
			Signed,
			Unsigned
		}

		private const string VariableNameForVirtualInvokeDataInDirectVirtualCall = "il2cpp_virtual_invoke_data_";

		private const string VariableNameForTypeInfoInConstrainedCall = "il2cpp_this_typeinfo";

		private int _tempIndex;

		private readonly SharingType _sharingType;

		private readonly Labeler _labeler;

		private readonly IGeneratedMethodCodeWriter _writer;

		private readonly ControlFlowGraph _cfg;

		private readonly TypeResolver _typeResolver;

		private readonly MethodReference _methodReference;

		private readonly MethodDefinition _methodDefinition;

		private readonly MethodBodyWriterDebugOptions _options;

		private readonly Unity.IL2CPP.StackAnalysis.StackAnalysis _stackAnalysis;

		private readonly IRuntimeMetadataAccess _runtimeMetadataAccess;

		private readonly ArrayBoundsCheckSupport _arrayBoundsCheckSupport;

		private readonly DivideByZeroCheckSupport _divideByZeroCheckSupport;

		private readonly IVTableBuilder _vTableBuilder;

		private readonly Stack<StackInfo> _valueStack = new Stack<StackInfo>();

		private readonly HashSet<Instruction> _emittedLabels = new HashSet<Instruction>();

		private readonly HashSet<Instruction> _referencedLabels = new HashSet<Instruction>();

		private readonly HashSet<TypeReference> _classesAlreadyInitializedInBlock = new HashSet<TypeReference>(new TypeReferenceEqualityComparer());

		private readonly ReadOnlyDictionary<Instruction, SequencePoint> _sequencePointMapping;

		private bool _thisInstructionIsVolatile;

		private ExceptionSupport _exceptionSupport;

		private NullChecksSupport _nullCheckSupport;

		private TypeReference _constrainedCallThisType;

		private readonly ISourceAnnotationWriter _sourceAnnotationWriter;

		private ISequencePointProvider _sequencePointProvider;

		private readonly MethodWriteContext _context;

		private bool _shouldEmitDebugInformation;

		private TypeReference Int16TypeReference => _context.Global.Services.TypeProvider.Int16TypeReference;

		private TypeReference UInt16TypeReference => _context.Global.Services.TypeProvider.UInt16TypeReference;

		private TypeReference Int32TypeReference => _context.Global.Services.TypeProvider.Int32TypeReference;

		private TypeReference SByteTypeReference => _context.Global.Services.TypeProvider.SByteTypeReference;

		private TypeReference ByteTypeReference => _context.Global.Services.TypeProvider.ByteTypeReference;

		private TypeReference IntPtrTypeReference => _context.Global.Services.TypeProvider.IntPtrTypeReference;

		private TypeReference UIntPtrTypeReference => _context.Global.Services.TypeProvider.UIntPtrTypeReference;

		private TypeReference Int64TypeReference => _context.Global.Services.TypeProvider.Int64TypeReference;

		private TypeReference UInt32TypeReference => _context.Global.Services.TypeProvider.UInt32TypeReference;

		private TypeReference UInt64TypeReference => _context.Global.Services.TypeProvider.UInt64TypeReference;

		private TypeReference SingleTypeReference => _context.Global.Services.TypeProvider.SingleTypeReference;

		private TypeReference DoubleTypeReference => _context.Global.Services.TypeProvider.DoubleTypeReference;

		private TypeReference ObjectTypeReference => _context.Global.Services.TypeProvider.ObjectTypeReference;

		private TypeReference StringTypeReference => _context.Global.Services.TypeProvider.StringTypeReference;

		private TypeReference SystemIntPtr => _context.Global.Services.TypeProvider.SystemIntPtr;

		private TypeReference SystemUIntPtr => _context.Global.Services.TypeProvider.SystemUIntPtr;

		private TypeReference RuntimeTypeHandleTypeReference => _context.Global.Services.TypeProvider.RuntimeTypeHandleTypeReference;

		private TypeReference RuntimeMethodHandleTypeReference => _context.Global.Services.TypeProvider.RuntimeMethodHandleTypeReference;

		private TypeReference RuntimeFieldHandleTypeReference => _context.Global.Services.TypeProvider.RuntimeFieldHandleTypeReference;

		private TypeReference SystemExceptionTypeReference => _context.Global.Services.TypeProvider.SystemException;

		public MethodBodyWriter(MethodWriteContext context, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			_context = context;
			_methodReference = context.MethodReference;
			_methodDefinition = context.MethodDefinition;
			_nullCheckSupport = new NullChecksSupport(writer, _methodDefinition, context.Global.Parameters.EmitNullChecks);
			_arrayBoundsCheckSupport = new ArrayBoundsCheckSupport(_methodDefinition, context.Global.Parameters.EnableArrayBoundsCheck);
			_divideByZeroCheckSupport = new DivideByZeroCheckSupport(writer, _methodDefinition, context.Global.Parameters.EnableDivideByZeroCheck);
			_sequencePointProvider = context.Assembly.SequencePoints;
			_sequencePointMapping = (_methodDefinition.DebugInformation.HasSequencePoints ? _methodDefinition.DebugInformation.GetSequencePointMapping().AsReadOnly() : null);
			_cfg = ControlFlowGraph.Create(_methodDefinition);
			_writer = writer;
			_typeResolver = context.TypeResolver;
			_vTableBuilder = context.Global.Collectors.VTable;
			_options = new MethodBodyWriterDebugOptions();
			_sourceAnnotationWriter = context.Global.Services.SourceAnnotationWriter;
			_stackAnalysis = Unity.IL2CPP.StackAnalysis.StackAnalysis.Analyze(_context, _cfg);
			DeadBlockAnalysis.MarkBlocksDeadIfNeeded(_cfg.Blocks);
			_labeler = new Labeler(_methodDefinition);
			_runtimeMetadataAccess = metadataAccess;
			_sharingType = (GenericSharingAnalysis.IsSharedMethod(context, _methodReference) ? SharingType.Shared : SharingType.NonShared);
			if (_context.Global.Parameters.EnableDebugger)
			{
				_shouldEmitDebugInformation = DebugWriter.ShouldEmitDebugInformation(context.Global.InputData, _methodDefinition.Module.Assembly);
			}
		}

		public void Generate()
		{
			if (!_methodDefinition.HasBody)
			{
				return;
			}
			if (GenericsUtilities.CheckForMaximumRecursion(_context, _methodReference.DeclaringType as GenericInstanceType) || GenericsUtilities.CheckForMaximumRecursion(_context, _methodReference as GenericInstanceMethod))
			{
				_writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_maximum_nested_generics_exception()"));
				return;
			}
			WriteLocalVariables();
			if (_context.Global.Parameters.EnableDebugger)
			{
				WriteDebuggerSupport();
				if (_sequencePointProvider.TryGetSequencePointAt(_methodDefinition, -1, SequencePointKind.Normal, out var info))
				{
					WriteCheckSequencePoint(info);
				}
				if (_sequencePointProvider.TryGetSequencePointAt(_methodDefinition, 16777215, SequencePointKind.Normal, out info))
				{
					WriteCheckMethodExitSequencePoint(info);
				}
				WriteCheckPausePoint(-1);
			}
			_exceptionSupport = new ExceptionSupport(_context, _methodDefinition, _cfg.Blocks, _writer);
			_exceptionSupport.Prepare();
			CollectUsedLabels();
			foreach (ExceptionHandler exceptionHandler in _methodDefinition.Body.ExceptionHandlers)
			{
				if (exceptionHandler.CatchType != null)
				{
					_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(exceptionHandler.CatchType));
				}
			}
			GlobalVariable[] globals = _stackAnalysis.Globals;
			foreach (GlobalVariable globalVariable in globals)
			{
				_writer.WriteVariable(_typeResolver.Resolve(globalVariable.Type, resolveGenericParameters: true), globalVariable.VariableName);
			}
			ExceptionSupport.Node[] children = _exceptionSupport.FlowTree.Children;
			foreach (ExceptionSupport.Node node in children)
			{
				GenerateCodeRecursive(node);
			}
		}

		private void WriteDebuggerSupport()
		{
			bool flag = _sequencePointProvider.MethodHasSequencePoints(_methodDefinition);
			string text = "NULL";
			if (flag && _methodDefinition.HasThis)
			{
				_runtimeMetadataAccess.Il2CppTypeFor(_methodDefinition.DeclaringType);
				text = _context.Global.Services.Naming.ForMethodExecutionContextThisVariable();
				_writer.WriteStatement("DECLARE_METHOD_THIS(" + _context.Global.Services.Naming.ForMethodExecutionContextThisVariable() + ", " + Emit.AddressOf("__this") + ")");
			}
			string text2 = "NULL";
			if (flag && _methodDefinition.HasParameters)
			{
				List<string> list = new List<string>();
				foreach (ParameterDefinition parameter in _methodDefinition.Parameters)
				{
					list.Add(Emit.AddressOf(_context.Global.Services.Naming.ForParameterName(parameter)));
				}
				text2 = _context.Global.Services.Naming.ForMethodExecutionContextParametersVariable();
				_writer.WriteStatement("DECLARE_METHOD_PARAMS(" + _context.Global.Services.Naming.ForMethodExecutionContextParametersVariable() + ", " + list.AggregateWithComma() + ")");
			}
			string text3 = "NULL";
			if (flag && _methodDefinition.Body.HasVariables)
			{
				List<string> list2 = new List<string>();
				foreach (VariableDefinition variable in _methodDefinition.Body.Variables)
				{
					TypeReference type = _typeResolver.Resolve(variable.VariableType);
					_runtimeMetadataAccess.Il2CppTypeFor(type.GetNonPinnedAndNonByReferenceType());
					if (_methodDefinition.DebugInformation != null && _methodDefinition.DebugInformation.TryGetName(variable, out var _))
					{
						list2.Add(Emit.AddressOf(_context.Global.Services.Naming.ForVariableName(variable)));
					}
				}
				if (list2.Count > 0)
				{
					text3 = _context.Global.Services.Naming.ForMethodExecutionContextLocalsVariable();
					_writer.WriteStatement("DECLARE_METHOD_LOCALS(" + _context.Global.Services.Naming.ForMethodExecutionContextLocalsVariable() + ", " + list2.AggregateWithComma() + ")");
				}
			}
			if (_shouldEmitDebugInformation)
			{
				string text4 = (_methodDefinition.HasGenericParameters ? "method" : _runtimeMetadataAccess.MethodInfo(_methodDefinition));
				_writer.WriteStatement("DECLARE_METHOD_EXEC_CTX(" + _context.Global.Services.Naming.ForMethodExecutionContextVariable() + ", " + text4 + ", " + text + ", " + text2 + ", " + text3 + ")");
			}
		}

		private void WriteLocalVariables()
		{
			foreach (KeyValuePair<string, TypeReference> item in ResolveLocalVariableTypes())
			{
				_writer.AddIncludeForTypeDefinition(item.Value);
				_writer.WriteVariable(item.Value, item.Key);
			}
		}

		private IEnumerable<KeyValuePair<string, TypeReference>> ResolveLocalVariableTypes()
		{
			return _methodDefinition.Body.Variables.Select((VariableDefinition v) => new KeyValuePair<string, TypeReference>(_context.Global.Services.Naming.ForVariableName(v), _typeResolver.Resolve(v.VariableType)));
		}

		private void CollectUsedLabels()
		{
			foreach (InstructionBlock item in _cfg.Blocks.Where((InstructionBlock block) => block.IsBranchTarget))
			{
				_referencedLabels.Add(item.First);
			}
			ExceptionSupport.Node[] children = _exceptionSupport.FlowTree.Children;
			foreach (ExceptionSupport.Node node in children)
			{
				RecursivelyAddLabelsForExceptionNodes(node);
			}
		}

		private void RecursivelyAddLabelsForExceptionNodes(ExceptionSupport.Node node)
		{
			if (node.Type != 0 && node.Type != ExceptionSupport.NodeType.Catch && node.Type != ExceptionSupport.NodeType.Finally && node.Type != ExceptionSupport.NodeType.Fault)
			{
				return;
			}
			if (node.Block != null)
			{
				_referencedLabels.Add(node.Block.First);
			}
			ExceptionSupport.Node[] children = node.Children;
			foreach (ExceptionSupport.Node node2 in children)
			{
				if (node2.Block != null)
				{
					_referencedLabels.Add(node2.Block.First);
				}
				RecursivelyAddLabelsForExceptionNodes(node2);
			}
		}

		private void GenerateCodeRecursive(ExceptionSupport.Node node)
		{
			using (RuntimeMetadataAccessContext.Create(_runtimeMetadataAccess, node))
			{
				InstructionBlock block = node.Block;
				if (block != null)
				{
					if (node.Children.Length != 0)
					{
						throw new NotSupportedException("Node with explicit Block should have no children!");
					}
					if (block.IsDead && !IsLastBlockInNonVoidMethod(block))
					{
						WriteComment("Dead block : {0}", block.First.ToString());
						return;
					}
					_valueStack.Clear();
					foreach (GlobalVariable item in (from v in _stackAnalysis.InputVariablesFor(block)
						orderby v.Index
						select v).Reverse())
					{
						_valueStack.Push(new StackInfo(item.VariableName, _typeResolver.Resolve(item.Type, resolveGenericParameters: true)));
					}
					_exceptionSupport.PushExceptionOnStackIfNeeded(node, _valueStack, _typeResolver, _context.Global.Services.TypeProvider.SystemException);
					if (_options.EmitBlockInfo)
					{
						WriteComment("BLOCK: {0}", block.Index);
					}
					if (_options.EmitInputAndOutputs)
					{
						DumpInsFor(block);
					}
					Instruction ins = block.First;
					WriteLabelForBranchTarget(ins);
					EnterNode(node);
					_classesAlreadyInitializedInBlock.Clear();
					while (true)
					{
						SequencePoint sequencePoint = GetSequencePoint(ins);
						if (sequencePoint != null)
						{
							if (sequencePoint.StartLine != 16707566 && _options.EmitLineNumbers)
							{
								_writer.WriteUnindented("#line {0} \"{1}\"", sequencePoint.StartLine, sequencePoint.Document.Url.Replace("\\", "\\\\"));
							}
							if (ins.OpCode != OpCodes.Nop)
							{
								_sourceAnnotationWriter.EmitAnnotation(_writer, sequencePoint);
							}
							WriteCheckSequencePoint(sequencePoint);
						}
						WriteCheckPausePoint(ins.Offset);
						if (_options.EmitIlCode)
						{
							_writer.WriteUnindented("/* {0} */", ins);
						}
						ProcessInstruction(node, block, ref ins);
						ProcessInstructionOperand(ins.OpCode, ins.Operand);
						if (ins.Next == null || ins == block.Last)
						{
							break;
						}
						ins = ins.Next;
					}
					if ((ins.OpCode.Code < Code.Br_S || ins.OpCode.Code > Code.Blt_Un) && block.Successors.Any() && ins.OpCode.Code != Code.Switch)
					{
						SetupFallthroughVariables(block);
					}
					if (_options.EmitInputAndOutputs)
					{
						DumpOutsFor(block);
					}
					if (_options.EmitBlockInfo)
					{
						if (block.Successors.Any())
						{
							WriteComment("END BLOCK {0} (succ: {1})", block.Index, block.Successors.Select((InstructionBlock b) => b.Index.ToString()).AggregateWithComma());
						}
						else
						{
							WriteComment("END BLOCK {0} (succ: none)", block.Index);
						}
						_writer.WriteLine();
						_writer.WriteLine();
					}
					ExitNode(node);
				}
				else
				{
					if (node.Children.Length == 0)
					{
						throw new NotSupportedException("Unexpected empty node!");
					}
					WriteLabelForBranchTarget(node.Start);
					EnterNode(node);
					ExceptionSupport.Node[] children = node.Children;
					foreach (ExceptionSupport.Node node2 in children)
					{
						GenerateCodeRecursive(node2);
					}
					ExitNode(node);
				}
			}
		}

		private bool IsLastBlockInNonVoidMethod(InstructionBlock block)
		{
			if (_methodDefinition.ReturnType.IsNotVoid())
			{
				return block.Last.Next == null;
			}
			return false;
		}

		private void ProcessInstructionOperand(OpCode opcode, object operand)
		{
			if (operand is MethodReference methodReference && opcode.Code != Code.Ldtoken)
			{
				ProcessMethodReferenceOperand(methodReference);
			}
			if (operand is FieldReference fieldReference)
			{
				ProcessFieldReferenceOperand(fieldReference);
			}
		}

		private void ProcessMethodReferenceOperand(MethodReference methodReference)
		{
			MethodDefinition methodDefinition = methodReference.Resolve();
			if (methodDefinition != null && methodDefinition.IsVirtual)
			{
				_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(methodReference.DeclaringType));
			}
			TypeResolver typeResolver = _typeResolver;
			MethodReference methodReference2 = methodReference;
			if (methodReference is GenericInstanceMethod)
			{
				methodReference2 = _typeResolver.Resolve(methodReference);
				typeResolver = _typeResolver.Nested(methodReference2 as GenericInstanceMethod);
			}
			_writer.AddIncludeForTypeDefinition(typeResolver.Resolve(GenericParameterResolver.ResolveReturnTypeIfNeeded(methodReference2)));
			if (methodReference2.HasThis)
			{
				_writer.AddIncludeForTypeDefinition(typeResolver.Resolve(GenericParameterResolver.ResolveThisTypeIfNeeded(methodReference2)));
			}
			foreach (ParameterDefinition parameter in methodReference2.Parameters)
			{
				_writer.AddIncludeForTypeDefinition(typeResolver.Resolve(GenericParameterResolver.ResolveParameterTypeIfNeeded(methodReference2, parameter)));
			}
		}

		private void ProcessFieldReferenceOperand(FieldReference fieldReference)
		{
			_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(fieldReference.DeclaringType));
			_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(GenericParameterResolver.ResolveFieldTypeIfNeeded(fieldReference)));
		}

		private void SetupFallthroughVariables(InstructionBlock block)
		{
			GlobalVariable[] array = _stackAnalysis.InputVariablesFor(block.Successors.Single());
			WriteAssignGlobalVariables(array);
			_valueStack.Clear();
			foreach (GlobalVariable item in array.OrderBy((GlobalVariable v) => v.Index).Reverse())
			{
				_valueStack.Push(new StackInfo(item.VariableName, _typeResolver.Resolve(item.Type)));
			}
		}

		private void EnterNode(ExceptionSupport.Node node)
		{
			switch (node.Type)
			{
			case ExceptionSupport.NodeType.Block:
				_writer.BeginBlock();
				break;
			case ExceptionSupport.NodeType.Try:
				EnterTry(node);
				break;
			case ExceptionSupport.NodeType.Catch:
				EnterCatch(node);
				break;
			case ExceptionSupport.NodeType.Filter:
				EnterFilter(node);
				break;
			case ExceptionSupport.NodeType.Finally:
				EnterFinally(node);
				break;
			case ExceptionSupport.NodeType.Fault:
				EnterFault(node);
				break;
			default:
				throw new NotImplementedException("Unexpected node type " + node.Type);
			}
		}

		private void EnterTry(ExceptionSupport.Node node)
		{
			EmitTry();
			_writer.BeginBlock($"begin try (depth: {node.Depth})");
			WriteStoreTryId(node);
		}

		private void EmitTry()
		{
			if (!_context.Global.Parameters.UsingTinyBackend)
			{
				_writer.WriteLine("try");
			}
			else
			{
				_writer.WriteLine("//try - Try blocks are not supported with the Tiny profile");
			}
		}

		private void EnterFinally(ExceptionSupport.Node node)
		{
			_writer.BeginBlock($"begin finally (depth: {node.Depth})");
			WriteStoreTryId(node.ParentTryNode);
		}

		private void EnterFault(ExceptionSupport.Node node)
		{
			_writer.BeginBlock($"begin fault (depth: {node.Depth})");
		}

		private void EnterCatch(ExceptionSupport.Node node)
		{
			_writer.BeginBlock(string.Format("begin catch({0})", (node.Handler.HandlerType == ExceptionHandlerType.Catch) ? node.Handler.CatchType.FullName : "filter"));
			WriteStoreTryId(node.ParentTryNode);
		}

		private void EnterFilter(ExceptionSupport.Node node)
		{
			_writer.BeginBlock($"begin filter(depth: {node.Depth})");
			_writer.WriteLine("bool __filter_local = false;");
			EmitTry();
			_writer.BeginBlock("begin implicit try block");
		}

		private void ExitNode(ExceptionSupport.Node node)
		{
			switch (node.Type)
			{
			case ExceptionSupport.NodeType.Block:
				_writer.EndBlock();
				break;
			case ExceptionSupport.NodeType.Try:
				ExitTry(node);
				break;
			case ExceptionSupport.NodeType.Catch:
				ExitCatch(node);
				break;
			case ExceptionSupport.NodeType.Filter:
				ExitFilter(node);
				break;
			case ExceptionSupport.NodeType.Finally:
				ExitFinally(node);
				break;
			case ExceptionSupport.NodeType.Fault:
				ExitFault(node);
				break;
			default:
				throw new NotImplementedException("Unexpected node type " + node.Type);
			}
		}

		private void ExitTry(ExceptionSupport.Node node)
		{
			_writer.EndBlock($"end try (depth: {node.Depth})");
			ExceptionSupport.Node[] catchNodes = node.CatchNodes;
			ExceptionSupport.Node finallyNode = node.FinallyNode;
			ExceptionSupport.Node[] filterNodes = node.FilterNodes;
			ExceptionSupport.Node faultNode = node.FaultNode;
			if (_context.Global.Parameters.UsingTinyBackend)
			{
				_writer.WriteLine("/* Catch blocks are not supported with the Tiny profile");
			}
			_writer.WriteLine("catch(Il2CppExceptionWrapper& e)");
			_writer.BeginBlock();
			if (catchNodes.Length != 0)
			{
				using (RuntimeMetadataAccessContext.CreateForCatchHandlers(_runtimeMetadataAccess))
				{
					_runtimeMetadataAccess.StartInitMetadataInline();
					foreach (ExceptionHandler item in catchNodes.Select((ExceptionSupport.Node n) => n.Handler))
					{
						_writer.WriteLine("if(il2cpp_codegen_class_is_assignable_from ({0}, il2cpp_codegen_object_class(e.ex)))", _runtimeMetadataAccess.TypeInfoFor(item.CatchType));
						using (new BlockWriter(_writer))
						{
							_writer.WriteLine(_exceptionSupport.EmitPushActiveException("e.ex") + ";");
							_writer.WriteLine(_labeler.ForJump(item.HandlerStart));
						}
					}
					_runtimeMetadataAccess.EndInitMetadataInline();
					if (finallyNode != null)
					{
						_writer.WriteLine("{0} = ({1})e.ex;", "__last_unhandled_exception", _context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.SystemException));
						_writer.WriteLine(_labeler.ForJump(finallyNode.Handler.HandlerStart));
					}
					else if (faultNode != null)
					{
						_writer.WriteLine("{0} = ({1})e.ex;", "__last_unhandled_exception", _context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.SystemException));
						_writer.WriteLine(_labeler.ForJump(faultNode.Handler.HandlerStart));
					}
					else
					{
						_writer.WriteLine("throw e;");
					}
				}
			}
			else if (filterNodes.Length != 0)
			{
				_writer.WriteLine(_exceptionSupport.EmitPushActiveException("e.ex") + ";");
			}
			else
			{
				if (finallyNode == null && faultNode == null)
				{
					throw new NotSupportedException("Try block ends without any catch, finally, nor fault handler");
				}
				_writer.WriteLine("{0} = ({1})e.ex;", "__last_unhandled_exception", _context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.SystemException));
				if (finallyNode != null)
				{
					_writer.WriteLine(_labeler.ForJump(finallyNode.Handler.HandlerStart));
				}
				if (faultNode != null)
				{
					_writer.WriteLine(_labeler.ForJump(faultNode.Handler.HandlerStart));
				}
			}
			_writer.EndBlock();
			if (_context.Global.Parameters.UsingTinyBackend)
			{
				_writer.WriteLine("*/");
			}
		}

		private void ExitCatch(ExceptionSupport.Node node)
		{
			_writer.EndBlock($"end catch (depth: {node.Depth})");
		}

		private void ExitFilter(ExceptionSupport.Node node)
		{
			_writer.EndBlock("end implicit try block");
			_writer.WriteLine("catch(Il2CppExceptionWrapper&)");
			_writer.BeginBlock("begin implicit catch block");
			_writer.WriteLine("__filter_local = false;");
			_writer.EndBlock("end implicit catch block");
			_writer.WriteLine("if (__filter_local)");
			_writer.BeginBlock();
			_writer.WriteLine(_labeler.ForJump(node.End.Next));
			_writer.EndBlock();
			_writer.WriteLine("else");
			_writer.BeginBlock();
			_writer.WriteStatement(Emit.RaiseManagedException(_exceptionSupport.EmitGetActiveException(SystemExceptionTypeReference), _runtimeMetadataAccess.MethodInfo(_methodReference)));
			_writer.EndBlock();
			_writer.EndBlock($"end filter (depth: {node.Depth})");
		}

		private void ExitFinally(ExceptionSupport.Node node)
		{
			_writer.EndBlock($"end finally (depth: {node.Depth})");
			_writer.WriteLine("IL2CPP_CLEANUP({0})", node.Start.Offset);
			_writer.BeginBlock();
			_writer.WriteLine("IL2CPP_RETHROW_IF_UNHANDLED({0})", _context.Global.Services.Naming.ForVariable(SystemExceptionTypeReference));
			foreach (Instruction item in _exceptionSupport.LeaveTargetsFor(node))
			{
				ExceptionSupport.Node[] array = node.GetTargetFinallyAndFaultNodesForJump(node.End.Offset, item.Offset).ToArray();
				if (array.Length != 0)
				{
					_writer.WriteLine("IL2CPP_END_CLEANUP(0x{0:X}, {1});", item.Offset, _labeler.FormatOffset(array.First().Start));
				}
				else
				{
					_writer.WriteLine("IL2CPP_JUMP_TBL(0x{0:X}, {1})", item.Offset, _labeler.FormatOffset(item));
				}
			}
			_writer.EndBlock();
		}

		private void ExitFault(ExceptionSupport.Node node)
		{
			_writer.EndBlock("end fault");
			_writer.WriteLine("IL2CPP_CLEANUP({0})", node.Start.Offset);
			_writer.BeginBlock();
			foreach (Instruction item in _exceptionSupport.LeaveTargetsFor(node))
			{
				ExceptionSupport.Node[] array = node.GetTargetFinallyAndFaultNodesForJump(node.End.Offset, item.Offset).ToArray();
				if (array.Length != 0)
				{
					_writer.WriteLine("IL2CPP_END_CLEANUP(0x{0:X}, {1});", item.Offset, _labeler.FormatOffset(array.First().Start));
				}
			}
			_writer.WriteLine("IL2CPP_RETHROW_IF_UNHANDLED({0})", _context.Global.Services.Naming.ForVariable(SystemExceptionTypeReference));
			_writer.EndBlock();
		}

		private void DumpInsFor(InstructionBlock block)
		{
			StackState stackState = _stackAnalysis.InputStackStateFor(block);
			if (stackState.IsEmpty)
			{
				WriteComment("[in: -] empty");
				return;
			}
			List<Entry> list = new List<Entry>(stackState.Entries);
			for (int i = 0; i < list.Count; i++)
			{
				Entry entry = list[i];
				WriteComment("[in: {0}] {1} (null: {2})", i, entry.Types.Select((TypeReference t) => t.FullName).AggregateWithComma(), entry.NullValue);
			}
		}

		private void DumpOutsFor(InstructionBlock block)
		{
			StackState stackState = _stackAnalysis.OutputStackStateFor(block);
			if (stackState.IsEmpty)
			{
				WriteComment("[out: -] empty");
				return;
			}
			List<Entry> list = new List<Entry>(stackState.Entries);
			for (int i = 0; i < list.Count; i++)
			{
				Entry entry = list[i];
				WriteComment("[out: {0}] {1} (null: {2})", i, entry.Types.Select((TypeReference t) => t.FullName).AggregateWithComma(), entry.NullValue);
			}
		}

		private void WriteComment(string message, params object[] args)
		{
			_writer.WriteLine("// {0}", string.Format(message, args));
		}

		private void WriteAssignment(ReadOnlyContext context, string leftName, TypeReference leftType, StackInfo right)
		{
			_writer.WriteStatement(GetAssignment(context, leftName, leftType, right, _sharingType));
		}

		public static string GetAssignment(ReadOnlyContext context, string leftName, TypeReference leftType, StackInfo right, SharingType sharingType = SharingType.NonShared)
		{
			return Emit.Assign(leftName, WriteExpressionAndCastIfNeeded(context, leftType, right, sharingType));
		}

		private static string WriteExpressionAndCastIfNeeded(ReadOnlyContext context, TypeReference leftType, StackInfo right, SharingType sharingType = SharingType.NonShared)
		{
			if (leftType.MetadataType == MetadataType.Boolean && right.Type.IsIntegralType())
			{
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (leftType.IsPointer)
			{
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (leftType.IsIntegralPointerType() && (right.Type.IsByReference || right.Type.IsPointer))
			{
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (leftType.IsGenericParameter())
			{
				return right.Expression;
			}
			if (right.Type.MetadataType == MetadataType.Object && leftType.MetadataType != MetadataType.Object)
			{
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (right.Type.IsArray && leftType.IsArray && right.Type.FullName != leftType.FullName)
			{
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (right.Type.MetadataType == MetadataType.IntPtr && leftType.MetadataType == MetadataType.Int32)
			{
				return string.Format("({0})({1}){2}", context.Global.Services.Naming.ForVariable(leftType), "intptr_t", right.Expression);
			}
			if (right.Type.IsIntegralPointerType() && leftType.MetadataType == MetadataType.IntPtr)
			{
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (right.Type.IsPointer && leftType.IsIntegralPointerType())
			{
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (leftType is ByReferenceType byReferenceType && (byReferenceType.ElementType.IsIntegralPointerType() || byReferenceType.ElementType.MetadataType.IsPrimitiveType()) && right.Type == context.Global.Services.TypeProvider.SystemIntPtr)
			{
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (leftType.IsIntegralType() && right.Type.IsIntegralType() && GetMetadataTypeOrderFor(context, leftType) < GetMetadataTypeOrderFor(context, right.Type))
			{
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (sharingType == SharingType.Shared)
			{
				if (leftType.IsUserDefinedStruct() && !TypeReferenceEqualityComparer.AreEqual(leftType, right.Type))
				{
					return "il2cpp_codegen_cast_struct<" + context.Global.Services.Naming.ForVariable(leftType) + ", " + context.Global.Services.Naming.ForVariable(right.Type) + ">(" + Emit.AddressOf(right.Expression) + ")";
				}
				return EmitCastRightCastToLeftType(context, leftType, right);
			}
			if (!VarianceSupport.IsNeededForConversion(leftType, right.Type))
			{
				return right.Expression;
			}
			return $"{VarianceSupport.Apply(context, leftType, right.Type)}{right.Expression}";
		}

		private static string EmitCastRightCastToLeftType(ReadOnlyContext context, TypeReference leftType, StackInfo right)
		{
			return $"({context.Global.Services.Naming.ForVariable(leftType)}){right.Expression}";
		}

		private SequencePoint GetSequencePoint(Instruction ins)
		{
			if (_sequencePointMapping != null && _sequencePointMapping.TryGetValue(ins, out var value))
			{
				return value;
			}
			return null;
		}

		private void ProcessInstruction(ExceptionSupport.Node node, InstructionBlock block, ref Instruction ins)
		{
			_context.Global.Services.ErrorInformation.CurrentInstruction = ins;
			switch (ins.OpCode.Code)
			{
			case Code.Ldarg_0:
				WriteLdarg(0, block, ins);
				break;
			case Code.Ldarg_1:
				WriteLdarg(1, block, ins);
				break;
			case Code.Ldarg_2:
				WriteLdarg(2, block, ins);
				break;
			case Code.Ldarg_3:
				WriteLdarg(3, block, ins);
				break;
			case Code.Ldloc_0:
				WriteLdloc(0, block, ins);
				break;
			case Code.Ldloc_1:
				WriteLdloc(1, block, ins);
				break;
			case Code.Ldloc_2:
				WriteLdloc(2, block, ins);
				break;
			case Code.Ldloc_3:
				WriteLdloc(3, block, ins);
				break;
			case Code.Stloc_0:
				WriteStloc(0);
				break;
			case Code.Stloc_1:
				WriteStloc(1);
				break;
			case Code.Stloc_2:
				WriteStloc(2);
				break;
			case Code.Stloc_3:
				WriteStloc(3);
				break;
			case Code.Ldarg_S:
			{
				int num4 = ((ParameterReference)ins.Operand).Index;
				if (_methodDefinition.HasThis)
				{
					num4++;
				}
				WriteLdarg(num4, block, ins);
				break;
			}
			case Code.Ldarga_S:
				LoadArgumentAddress((ParameterReference)ins.Operand);
				break;
			case Code.Starg_S:
				StoreArg(ins);
				break;
			case Code.Ldloc_S:
			case Code.Ldloc:
			{
				VariableReference variableReference2 = (VariableReference)ins.Operand;
				WriteLdloc(variableReference2.Index, block, ins);
				break;
			}
			case Code.Ldloca_S:
				LoadLocalAddress((VariableReference)ins.Operand);
				break;
			case Code.Stloc_S:
			case Code.Stloc:
			{
				VariableReference variableReference = (VariableReference)ins.Operand;
				WriteStloc(variableReference.Index);
				break;
			}
			case Code.Ldnull:
				LoadNull();
				break;
			case Code.Ldc_I4_M1:
				LoadInt32Constant(-1);
				break;
			case Code.Ldc_I4_0:
				LoadInt32Constant(0);
				break;
			case Code.Ldc_I4_1:
				LoadInt32Constant(1);
				break;
			case Code.Ldc_I4_2:
				LoadInt32Constant(2);
				break;
			case Code.Ldc_I4_3:
				LoadInt32Constant(3);
				break;
			case Code.Ldc_I4_4:
				LoadInt32Constant(4);
				break;
			case Code.Ldc_I4_5:
				LoadInt32Constant(5);
				break;
			case Code.Ldc_I4_6:
				LoadInt32Constant(6);
				break;
			case Code.Ldc_I4_7:
				LoadInt32Constant(7);
				break;
			case Code.Ldc_I4_8:
				LoadInt32Constant(8);
				break;
			case Code.Ldc_I4_S:
				LoadPrimitiveTypeSByte(ins, Int32TypeReference);
				break;
			case Code.Ldc_I4:
				LoadPrimitiveTypeInt32(ins, Int32TypeReference);
				break;
			case Code.Ldc_I8:
				LoadLong(ins, Int64TypeReference);
				break;
			case Code.Ldc_R4:
				LoadConstant(SingleTypeReference, Formatter.StringRepresentationFor((float)ins.Operand));
				break;
			case Code.Ldc_R8:
				LoadConstant(DoubleTypeReference, Formatter.StringRepresentationFor((double)ins.Operand));
				break;
			case Code.Dup:
				WriteDup();
				break;
			case Code.Pop:
				_valueStack.Pop();
				break;
			case Code.Jmp:
				throw new NotImplementedException();
			case Code.Callvirt:
			{
				MethodReference methodToCall = (MethodReference)ins.Operand;
				bool flag2 = BenchmarkSupport.BeginBenchmark(methodToCall, _writer);
				WriteStoreStepOutSequencePoint(ins);
				if (methodToCall.IsStatic())
				{
					throw new InvalidOperationException("In method '" + _methodReference.FullName + "', an attempt to call the static method '" + methodToCall.FullName + "' with the callvirt opcode is not valid IL. Use the call opcode instead.");
				}
				List<StackInfo> list4 = PopItemsFromStack(methodToCall.Parameters.Count + 1, _valueStack);
				string suffix = "_" + ins.Offset;
				string copyBackBoxedExpr = null;
				StackInfo stackInfo22 = list4[0];
				if (_constrainedCallThisType != null || stackInfo22.BoxedType != null)
				{
					WriteConstrainedCallExpressionFor(ref methodToCall, MethodCallType.Virtual, list4, (string s) => s + suffix, out copyBackBoxedExpr);
					if (copyBackBoxedExpr != null)
					{
						_writer.WriteStatement(copyBackBoxedExpr);
					}
				}
				else
				{
					WriteCallExpressionFor(methodToCall, MethodCallType.Virtual, list4, (string s) => s + suffix, !flag2);
				}
				_constrainedCallThisType = null;
				WriteCheckStepOutSequencePoint(ins);
				BenchmarkSupport.EndBenchmark(flag2, _writer);
				break;
			}
			case Code.Call:
			{
				WriteStoreStepOutSequencePoint(ins);
				if (_constrainedCallThisType != null)
				{
					throw new InvalidOperationException($"Constrained opcode was followed a Call rather than a Callvirt in method '{_methodReference.FullName}' at instruction '{ins}'");
				}
				string suffix2 = "_" + ins.Offset;
				MethodReference methodReference3 = (MethodReference)ins.Operand;
				WriteCallExpressionFor(methodReference3, MethodCallType.Normal, PopItemsFromStack(methodReference3.Parameters.Count + (methodReference3.HasThis ? 1 : 0), _valueStack), (string s) => s + suffix2);
				WriteCheckStepOutSequencePoint(ins);
				break;
			}
			case Code.Calli:
			{
				CallSite callSite = ins.Operand as CallSite;
				_writer.Write("typedef ");
				_writer.Write(_context.Global.Services.Naming.ForVariable(callSite.ReturnType));
				string text5 = "func_" + SemiUniqueStableTokenGenerator.GenerateFor(callSite.FullName);
				string text6 = "";
				switch (callSite.CallingConvention)
				{
				case MethodCallingConvention.C:
					text6 = "CDECL";
					break;
				case MethodCallingConvention.FastCall:
					text6 = "FASTCALL";
					break;
				case MethodCallingConvention.StdCall:
					text6 = "STDCALL";
					break;
				case MethodCallingConvention.ThisCall:
					text6 = "THISCALL";
					break;
				}
				_writer.Write(" ({0} *{1})(", text6, text5);
				if (callSite.HasParameters)
				{
					for (int j = 0; j < callSite.Parameters.Count; j++)
					{
						_writer.Write(_context.Global.Services.Naming.ForVariable(callSite.Parameters[j].ParameterType));
						if (j < callSite.Parameters.Count - 1)
						{
							_writer.Write(",");
						}
					}
				}
				_writer.WriteLine(");");
				StackInfo stackInfo23 = _valueStack.Pop();
				StringBuilder stringBuilder = new StringBuilder();
				if (callSite.HasParameters)
				{
					List<StackInfo> list5 = PopItemsFromStack(callSite.Parameters.Count, _valueStack);
					stringBuilder.AppendFormat("(({0}){1})(", text5, stackInfo23.Expression);
					for (int k = 0; k < list5.Count; k++)
					{
						stringBuilder.Append(Emit.Cast(_context, callSite.Parameters[k].ParameterType, list5[k].Expression));
						if (k < list5.Count - 1)
						{
							stringBuilder.Append(",");
						}
					}
					stringBuilder.Append(")");
				}
				else
				{
					stringBuilder.AppendFormat("(({0}){1})()", text5, stackInfo23.Expression);
				}
				if (callSite.ReturnType.IsVoid())
				{
					_writer.WriteStatement(stringBuilder.ToString());
				}
				else if (ins.Next != null && ins.Next.OpCode.Code == Code.Pop)
				{
					_writer.WriteStatement(stringBuilder.ToString());
					_valueStack.Push(new StackInfo("NULL", ObjectTypeReference));
				}
				else
				{
					StackInfo stackInfo24 = NewTemp(callSite.ReturnType);
					_valueStack.Push(new StackInfo(stackInfo24.Expression, callSite.ReturnType));
					_writer.WriteStatement(Emit.Assign(stackInfo24.GetIdentifierExpression(_context), stringBuilder.ToString()));
				}
				break;
			}
			case Code.Ret:
				WriteReturnStatement();
				break;
			case Code.Br_S:
			case Code.Br:
				WriteUnconditionalJumpTo(block, (Instruction)ins.Operand);
				break;
			case Code.Brfalse_S:
			case Code.Brfalse:
				GenerateConditionalJump(block, ins, isTrue: false);
				break;
			case Code.Brtrue_S:
			case Code.Brtrue:
				GenerateConditionalJump(block, ins, isTrue: true);
				break;
			case Code.Beq_S:
			case Code.Beq:
				GenerateConditionalJump(block, ins, "==", Signedness.Signed);
				break;
			case Code.Bge_S:
			case Code.Bge:
				GenerateConditionalJump(block, ins, ">=", Signedness.Signed);
				break;
			case Code.Bgt_S:
			case Code.Bgt:
				GenerateConditionalJump(block, ins, ">", Signedness.Signed);
				break;
			case Code.Ble_S:
			case Code.Ble:
				GenerateConditionalJump(block, ins, "<=", Signedness.Signed);
				break;
			case Code.Blt_S:
			case Code.Blt:
				GenerateConditionalJump(block, ins, "<", Signedness.Signed);
				break;
			case Code.Bne_Un_S:
			case Code.Bne_Un:
				GenerateConditionalJump(block, ins, "==", Signedness.Unsigned, negate: true);
				break;
			case Code.Bge_Un_S:
			case Code.Bge_Un:
				GenerateConditionalJump(block, ins, "<", Signedness.Unsigned, negate: true);
				break;
			case Code.Bgt_Un_S:
			case Code.Bgt_Un:
				GenerateConditionalJump(block, ins, "<=", Signedness.Unsigned, negate: true);
				break;
			case Code.Ble_Un_S:
			case Code.Ble_Un:
				GenerateConditionalJump(block, ins, ">", Signedness.Unsigned, negate: true);
				break;
			case Code.Blt_Un_S:
			case Code.Blt_Un:
				GenerateConditionalJump(block, ins, ">=", Signedness.Unsigned, negate: true);
				break;
			case Code.Switch:
			{
				StackInfo stackInfo18 = _valueStack.Pop();
				Instruction[] targetInstructions = (Instruction[])ins.Operand;
				int num3 = 0;
				List<InstructionBlock> list = new List<InstructionBlock>(block.Successors);
				InstructionBlock instructionBlock = list.SingleOrDefault((InstructionBlock b) => !targetInstructions.Select((Instruction t) => t.Offset).Contains(b.First.Offset));
				if (instructionBlock != null)
				{
					list.Remove(instructionBlock);
					WriteAssignGlobalVariables(_stackAnalysis.InputVariablesFor(instructionBlock));
				}
				_writer.WriteLine($"switch ({stackInfo18})");
				using (NewBlock())
				{
					Instruction[] array4 = targetInstructions;
					foreach (Instruction targetInstruction in array4)
					{
						_writer.WriteLine($"case {num3++}:");
						using (NewBlock())
						{
							InstructionBlock block2 = list.First((InstructionBlock b) => b.First.Offset == targetInstruction.Offset);
							WriteAssignGlobalVariables(_stackAnalysis.InputVariablesFor(block2));
							WriteJump(targetInstruction);
						}
					}
					break;
				}
			}
			case Code.Ldind_I1:
				LoadIndirect(SByteTypeReference, Int32TypeReference);
				break;
			case Code.Ldind_U1:
				LoadIndirect(ByteTypeReference, Int32TypeReference);
				break;
			case Code.Ldind_I2:
				LoadIndirect(Int16TypeReference, Int32TypeReference);
				break;
			case Code.Ldind_U2:
				LoadIndirect(UInt16TypeReference, Int32TypeReference);
				break;
			case Code.Ldind_I4:
				LoadIndirect(Int32TypeReference, Int32TypeReference);
				break;
			case Code.Ldind_U4:
				LoadIndirect(UInt32TypeReference, Int32TypeReference);
				break;
			case Code.Ldind_I8:
				LoadIndirect(Int64TypeReference, Int64TypeReference);
				break;
			case Code.Ldind_R4:
				LoadIndirect(SingleTypeReference, SingleTypeReference);
				break;
			case Code.Ldind_R8:
				LoadIndirect(DoubleTypeReference, DoubleTypeReference);
				break;
			case Code.Ldind_I:
				LoadIndirectNativeInteger();
				break;
			case Code.Ldind_Ref:
				LoadIndirectReference();
				break;
			case Code.Stind_Ref:
				StoreIndirect(ObjectTypeReference);
				break;
			case Code.Stind_I1:
				StoreIndirect(SByteTypeReference);
				break;
			case Code.Stind_I2:
				StoreIndirect(Int16TypeReference);
				break;
			case Code.Stind_I4:
				StoreIndirect(Int32TypeReference);
				break;
			case Code.Stind_I8:
				StoreIndirect(Int64TypeReference);
				break;
			case Code.Stind_R4:
				StoreIndirect(SingleTypeReference);
				break;
			case Code.Stind_R8:
				StoreIndirect(DoubleTypeReference);
				break;
			case Code.Add:
				ArithmeticOpCodes.Add(_context, _valueStack);
				break;
			case Code.Sub:
				ArithmeticOpCodes.Sub(_context, _valueStack);
				break;
			case Code.Mul:
				ArithmeticOpCodes.Mul(_context, _valueStack);
				break;
			case Code.Div:
				_divideByZeroCheckSupport.WriteDivideByZeroCheckIfNeeded(_valueStack.Peek());
				WriteBinaryOperationUsingLargestOperandTypeAsResultType("/");
				break;
			case Code.Div_Un:
				_divideByZeroCheckSupport.WriteDivideByZeroCheckIfNeeded(_valueStack.Peek());
				WriteUnsignedArithmeticOperation("/");
				break;
			case Code.Rem:
				WriteRemainderOperation();
				break;
			case Code.Rem_Un:
				WriteUnsignedArithmeticOperation("%");
				break;
			case Code.And:
				WriteBinaryOperationUsingLeftOperandTypeAsResultType("&");
				break;
			case Code.Or:
				WriteBinaryOperationUsingLargestOperandTypeAsResultType("|");
				break;
			case Code.Xor:
				WriteBinaryOperationUsingLargestOperandTypeAsResultType("^");
				break;
			case Code.Shl:
				WriteBinaryOperationUsingLeftOperandTypeAsResultType("<<");
				break;
			case Code.Shr:
				WriteBinaryOperationUsingLeftOperandTypeAsResultType(">>");
				break;
			case Code.Shr_Un:
				WriteShrUn();
				break;
			case Code.Neg:
				WriteNegateOperation();
				break;
			case Code.Not:
				WriteNotOperation();
				break;
			case Code.Conv_I1:
				ConversionOpCodes.WriteNumericConversion(_context, _valueStack, SByteTypeReference);
				break;
			case Code.Conv_I2:
				ConversionOpCodes.WriteNumericConversion(_context, _valueStack, Int16TypeReference);
				break;
			case Code.Conv_I4:
				ConversionOpCodes.WriteNumericConversion(_context, _valueStack, Int32TypeReference);
				break;
			case Code.Conv_I8:
				ConversionOpCodes.WriteNumericConversionI8(_context, _valueStack);
				break;
			case Code.Conv_R4:
				ConversionOpCodes.WriteNumericConversionFloat(_context, _valueStack, SingleTypeReference);
				break;
			case Code.Conv_R8:
				ConversionOpCodes.WriteNumericConversionFloat(_context, _valueStack, DoubleTypeReference);
				break;
			case Code.Conv_U4:
				ConversionOpCodes.WriteNumericConversion(_context, _valueStack, UInt32TypeReference, Int32TypeReference);
				break;
			case Code.Conv_U8:
				ConversionOpCodes.WriteNumericConversionU8(_context, _valueStack);
				break;
			case Code.Cpobj:
			{
				StackInfo stackInfo25 = _valueStack.Pop();
				StackInfo stackInfo26 = _valueStack.Pop();
				if (stackInfo26.Type.IsPointer)
				{
					TypeReference elementType = ((TypeSpecification)stackInfo25.Type).ElementType;
					_writer.WriteLine($"il2cpp_codegen_memcpy({stackInfo26}, {stackInfo25}, sizeof({_context.Global.Services.Naming.ForVariable(elementType)}));");
				}
				else
				{
					_writer.WriteStatement(Emit.Assign(Emit.Dereference(stackInfo26.Expression), Emit.Dereference(stackInfo25.Expression)));
				}
				break;
			}
			case Code.Ldobj:
				WriteLoadObject(ins, block);
				break;
			case Code.Ldstr:
			{
				string literal = (string)ins.Operand;
				_methodDefinition.Body.GetInstructionToken(ins, out var token);
				_writer.AddIncludeForTypeDefinition(StringTypeReference);
				_valueStack.Push(new StackInfo(_runtimeMetadataAccess.StringLiteral(literal, token, _methodDefinition.Module.Assembly), StringTypeReference));
				break;
			}
			case Code.Newobj:
			{
				WriteStoreStepOutSequencePoint(ins);
				MethodReference methodReference = (MethodReference)ins.Operand;
				MethodReference methodReference2 = _typeResolver.Resolve(methodReference);
				TypeReference typeReference7 = _typeResolver.Resolve(methodReference2.DeclaringType);
				_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(methodReference.DeclaringType));
				if (_methodReference.DeclaringType is GenericInstanceType typeReference8 && !(methodReference2 is GenericInstanceMethod))
				{
					methodReference2 = TypeResolver.For(typeReference8).Resolve(methodReference2);
				}
				List<TypeReference> parameterTypes = GetParameterTypes(methodReference2, _typeResolver);
				List<StackInfo> list2 = PopItemsFromStack(parameterTypes.Count, _valueStack);
				List<string> list3 = FormatArgumentsForMethodCall(parameterTypes, list2, _sharingType);
				if (typeReference7.IsArray)
				{
					StackInfo local = NewTemp(typeReference7);
					ArrayType arrayType2 = (ArrayType)typeReference7;
					if (arrayType2.Rank < 2)
					{
						throw new NotImplementedException("Attempting to create a multidimensional array of rank lesser than 2");
					}
					string text4 = NewTempName();
					string value3 = ((!_context.Global.Parameters.UsingTinyBackend) ? Emit.Call("GenArrayNew", _runtimeMetadataAccess.TypeInfoFor(methodReference.DeclaringType), text4) : Emit.Call("GenArrayNew", _runtimeMetadataAccess.TypeInfoFor(methodReference.DeclaringType), "sizeof(" + _context.Global.Services.Naming.ForVariable(arrayType2.ElementType) + ")", text4));
					_writer.WriteLine("{0} {1}[] = {{ {2} }};", ArrayNaming.ForArrayIndexType(), text4, Emit.CastEach(ArrayNaming.ForArrayIndexType(), list3).AggregateWithComma());
					_writer.WriteLine("{0};", Emit.Assign(local.GetIdentifierExpression(_context), Emit.Cast(_context, arrayType2, value3)));
					_valueStack.Push(new StackInfo(local));
				}
				else if (typeReference7.IsValueType())
				{
					StackInfo local2 = NewTemp(typeReference7);
					string item = Emit.AddressOf(local2.Expression);
					list3.Insert(0, item);
					if (MethodSignatureWriter.NeedsHiddenMethodInfo(_context, methodReference2, MethodCallType.Normal))
					{
						list3.Add((_context.Global.Parameters.EmitComments ? "/*hidden argument*/" : "") + _runtimeMetadataAccess.HiddenMethodInfo(methodReference));
					}
					_writer.WriteVariable(typeReference7, local2.Expression);
					_writer.WriteStatement(Emit.Call(_runtimeMetadataAccess.Method(methodReference2), list3));
					_valueStack.Push(new StackInfo(local2));
				}
				else if (methodReference2.Name == ".ctor" && typeReference7.MetadataType == MetadataType.String)
				{
					list3.Insert(0, "NULL");
					string suffix3 = "_" + ins.Offset;
					MethodReference createStringMethod = GetCreateStringMethod(methodReference2);
					if (!_context.Global.Parameters.UsingTinyClassLibraries)
					{
						WriteCallExpressionFor(_methodReference, createStringMethod, MethodCallType.Normal, list3, (string s) => s + suffix3, emitNullCheckForInvocation: false);
					}
					else
					{
						WriteCallExpressionFor(createStringMethod, MethodCallType.Normal, list2, (string s) => s + suffix3, emitNullCheckForInvocation: false);
					}
				}
				else
				{
					StackInfo stackInfo20 = NewTemp(typeReference7);
					if (CanOptimizeAwayDelegateAllocation(ins, methodReference2))
					{
						StackInfo stackInfo21 = NewTemp(typeReference7);
						_writer.WriteStatement(_context.Global.Services.Naming.ForTypeNameOnly(typeReference7) + " " + stackInfo21.Expression);
						_writer.WriteStatement(Emit.Memset(_context, Emit.AddressOf(stackInfo21.Expression), 0, "sizeof(" + stackInfo21.Expression + ")"));
						_writer.WriteStatement(Emit.Assign(stackInfo20.GetIdentifierExpression(_context), Emit.AddressOf(stackInfo21.Expression)));
					}
					else if (!_context.Global.Parameters.UsingTinyBackend)
					{
						_writer.WriteStatement(Emit.Assign(stackInfo20.GetIdentifierExpression(_context), Emit.Cast(_context, typeReference7, Emit.Call("il2cpp_codegen_object_new", _runtimeMetadataAccess.Newobj(methodReference)))));
					}
					else
					{
						_writer.WriteStatement(Emit.Assign(stackInfo20.GetIdentifierExpression(_context), Emit.Cast(_context, typeReference7, Emit.Call("il2cpp_codegen_object_new", "sizeof(" + _context.Global.Services.Naming.ForTypeNameOnly(typeReference7) + ")", _runtimeMetadataAccess.TypeInfoFor(typeReference7)))));
					}
					list3.Insert(0, stackInfo20.Expression);
					if (MethodSignatureWriter.NeedsHiddenMethodInfo(_context, methodReference2, MethodCallType.Normal))
					{
						list3.Add((_context.Global.Parameters.EmitComments ? "/*hidden argument*/" : "") + _runtimeMetadataAccess.HiddenMethodInfo(methodReference));
					}
					_writer.WriteStatement(Emit.Call(_runtimeMetadataAccess.Method(methodReference), list3));
					if (methodReference2.DeclaringType.IsDelegate() && methodReference2.Name == ".ctor" && _context.Global.Parameters.UsingTinyClassLibraries)
					{
						MethodDefinition method = methodReference2.DeclaringType.Resolve().Methods.Single((MethodDefinition m) => m.IsConstructor);
						methodReference2 = TypeResolver.For(methodReference2.DeclaringType, methodReference2).Resolve(method);
						EmitTinyDelegateFieldSetters(_writer, methodReference2, stackInfo20, list2);
					}
					_valueStack.Push(new StackInfo(stackInfo20));
				}
				WriteCheckStepOutSequencePoint(ins);
				break;
			}
			case Code.Castclass:
				WriteCastclassOrIsInst((TypeReference)ins.Operand, _valueStack.Pop(), "Castclass");
				break;
			case Code.Isinst:
				WriteCastclassOrIsInst((TypeReference)ins.Operand, _valueStack.Pop(), "IsInst");
				break;
			case Code.Conv_R_Un:
				ConversionOpCodes.WriteNumericConversionToFloatFromUnsigned(_context, _valueStack);
				break;
			case Code.Unbox:
				Unbox(ins);
				break;
			case Code.Throw:
			{
				StackInfo stackInfo19 = _valueStack.Pop();
				if (_context.Global.Parameters.EnableDebugger)
				{
					_writer.WriteStatement(Emit.RaiseManagedException(stackInfo19.ToString(), _runtimeMetadataAccess.MethodInfo(_methodReference)));
				}
				else
				{
					_writer.WriteStatement(Emit.RaiseManagedException(stackInfo19.ToString(), _runtimeMetadataAccess.MethodInfo(_methodReference)));
				}
				break;
			}
			case Code.Ldfld:
				LoadField(ins);
				break;
			case Code.Ldflda:
				LoadField(ins, loadAddress: true);
				break;
			case Code.Stfld:
				StoreField(ins);
				break;
			case Code.Ldsfld:
			case Code.Ldsflda:
			case Code.Stsfld:
				StaticFieldAccess(ins);
				break;
			case Code.Stobj:
				WriteStoreObject((TypeReference)ins.Operand);
				break;
			case Code.Conv_Ovf_I1_Un:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, ByteTypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(sbyte.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_I2_Un:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, Int16TypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(short.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_I4_Un:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, Int32TypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(int.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_I8_Un:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, Int64TypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(long.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U1_Un:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, ByteTypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(byte.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U2_Un:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, UInt16TypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(ushort.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U4_Un:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, UInt32TypeReference, treatInputAsUnsigned: true, AppendLongLongLiteralSuffix(uint.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U8_Un:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, UInt64TypeReference, treatInputAsUnsigned: true, ulong.MaxValue + "ULL", () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_I_Un:
				ConversionOpCodes.ConvertToNaturalIntWithOverflow(_context, _writer, _valueStack, SystemIntPtr, treatInputAsUnsigned: true, "INTPTR_MAX", () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U_Un:
				ConversionOpCodes.ConvertToNaturalIntWithOverflow(_context, _writer, _valueStack, SystemUIntPtr, treatInputAsUnsigned: true, "UINTPTR_MAX", () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Box:
			{
				TypeReference typeReference5 = (TypeReference)ins.Operand;
				TypeReference typeReference6 = _typeResolver.Resolve(typeReference5);
				_writer.AddIncludeForTypeDefinition(typeReference6);
				if (!typeReference6.IsValueType() && !typeReference6.IsPointer)
				{
					break;
				}
				StackInfo stackInfo16 = _valueStack.Pop();
				if (CanApplyValueTypeBoxBranchOptimizationToInstruction(ins, block))
				{
					Instruction next = ins.Next;
					if (next.OpCode.Code == Code.Brtrue || next.OpCode.Code == Code.Brtrue_S)
					{
						if (BothSuccessorsAreTheSame(block))
						{
							WriteGlobalVariableAssignmentForFirstSuccessor(block, (Instruction)next.Operand);
						}
						else
						{
							WriteGlobalVariableAssignmentForRightBranch(block, (Instruction)next.Operand);
						}
						WriteJump((Instruction)next.Operand);
					}
					if (BothSuccessorsAreTheSame(block))
					{
						WriteGlobalVariableAssignmentForFirstSuccessor(block, (Instruction)next.Operand);
					}
					else
					{
						WriteGlobalVariableAssignmentForLeftBranch(block, (Instruction)next.Operand);
					}
					ins = next;
					break;
				}
				bool num2 = typeReference6.MetadataType == MetadataType.IntPtr || typeReference6.MetadataType == MetadataType.UIntPtr;
				bool flag = stackInfo16.Type.IsSameType(SystemIntPtr) || stackInfo16.Type.IsSameType(SystemUIntPtr);
				if (num2 && flag)
				{
					typeReference6 = stackInfo16.Type;
				}
				StackInfo stackInfo17 = NewTemp(typeReference6);
				if (typeReference6.IsPrimitive)
				{
					_writer.WriteLine("{0} = {1};", stackInfo17.GetIdentifierExpression(_context), CastTypeIfNeeded(stackInfo16, stackInfo17.Type));
				}
				else
				{
					WriteAssignment(_context, stackInfo17.GetIdentifierExpression(_context), stackInfo17.Type, stackInfo16);
				}
				if (_context.Global.Parameters.UsingTinyBackend && typeReference6.IsNullable())
				{
					typeReference5 = ((GenericInstanceType)typeReference6).GenericArguments[0];
					StoreLocalAndPush(ObjectTypeReference, $"BoxNullable<{_context.Global.Services.Naming.ForVariable(typeReference6)}, {_context.Global.Services.Naming.ForVariable(typeReference5)}>({_runtimeMetadataAccess.TypeInfoFor(typeReference5)}, &{stackInfo17.Expression})", typeReference5);
				}
				else if (_context.Global.Parameters.UsingTinyBackend)
				{
					StoreLocalAndPush(ObjectTypeReference, $"Box({_runtimeMetadataAccess.TypeInfoFor((TypeReference)ins.Operand)}, (void*)&{stackInfo17.Expression}, sizeof({_context.Global.Services.Naming.ForVariable(stackInfo17.Type)}))", typeReference5);
				}
				else
				{
					StoreLocalAndPush(ObjectTypeReference, $"Box({_runtimeMetadataAccess.TypeInfoFor((TypeReference)ins.Operand)}, &{stackInfo17.Expression})", typeReference5);
				}
				break;
			}
			case Code.Newarr:
			{
				StackInfo stackInfo15 = _valueStack.Pop();
				ArrayType arrayType = new ArrayType(_typeResolver.Resolve((TypeReference)ins.Operand));
				_writer.AddIncludeForTypeDefinition(arrayType);
				string length = $"(uint32_t){stackInfo15.Expression}";
				StoreLocalAndPush(arrayType, Emit.Cast(_context, arrayType, Emit.NewSZArray(_context, arrayType, (TypeReference)ins.Operand, length, _runtimeMetadataAccess)));
				break;
			}
			case Code.Ldlen:
			{
				StackInfo stackInfo14 = _valueStack.Pop();
				_nullCheckSupport.WriteNullCheckIfNeeded(stackInfo14);
				PushExpression(UInt32TypeReference, string.Format("(({0}*){1})->max_length", "RuntimeArray", stackInfo14));
				break;
			}
			case Code.Ldelema:
			{
				StackInfo stackInfo12 = _valueStack.Pop();
				StackInfo stackInfo13 = _valueStack.Pop();
				ByReferenceType typeReference4 = new ByReferenceType(_typeResolver.Resolve((TypeReference)ins.Operand));
				_nullCheckSupport.WriteNullCheckIfNeeded(stackInfo13);
				PushExpression(typeReference4, EmitArrayLoadElementAddress(stackInfo13, stackInfo12.Expression));
				break;
			}
			case Code.Ldelem_I1:
				LoadElemAndPop(SByteTypeReference);
				break;
			case Code.Ldelem_U1:
				LoadElemAndPop(ByteTypeReference);
				break;
			case Code.Ldelem_I2:
				LoadElemAndPop(Int16TypeReference);
				break;
			case Code.Ldelem_U2:
				LoadElemAndPop(UInt16TypeReference);
				break;
			case Code.Ldelem_I4:
				LoadElemAndPop(Int32TypeReference);
				break;
			case Code.Ldelem_U4:
				LoadElemAndPop(UInt32TypeReference);
				break;
			case Code.Ldelem_I8:
				LoadElemAndPop(Int64TypeReference);
				break;
			case Code.Ldelem_I:
				LoadElemAndPop(IntPtrTypeReference);
				break;
			case Code.Ldelem_R4:
				LoadElemAndPop(SingleTypeReference);
				break;
			case Code.Ldelem_R8:
				LoadElemAndPop(DoubleTypeReference);
				break;
			case Code.Ldelem_Ref:
			{
				StackInfo index3 = _valueStack.Pop();
				StackInfo array3 = _valueStack.Pop();
				LoadElem(array3, ArrayUtilities.ArrayElementTypeOf(array3.Type), index3);
				break;
			}
			case Code.Stelem_I:
			case Code.Stelem_I1:
			case Code.Stelem_I2:
			case Code.Stelem_I4:
			case Code.Stelem_I8:
			case Code.Stelem_R4:
			case Code.Stelem_R8:
			case Code.Stelem_Any:
			{
				StackInfo value2 = _valueStack.Pop();
				StackInfo index2 = _valueStack.Pop();
				StackInfo array2 = _valueStack.Pop();
				StoreElement(array2, index2, value2, emitElementTypeCheck: false);
				break;
			}
			case Code.Ldelem_Any:
				LoadElemAndPop(_typeResolver.Resolve((TypeReference)ins.Operand));
				break;
			case Code.Stelem_Ref:
			{
				StackInfo value = _valueStack.Pop();
				StackInfo index = _valueStack.Pop();
				StackInfo array = _valueStack.Pop();
				StoreElement(array, index, value, emitElementTypeCheck: true);
				break;
			}
			case Code.Unbox_Any:
			{
				StackInfo stackInfo11 = _valueStack.Pop();
				TypeReference typeReference3 = _typeResolver.Resolve((TypeReference)ins.Operand);
				_writer.AddIncludeForTypeDefinition(typeReference3);
				if (typeReference3.IsValueType())
				{
					ByReferenceType byReferenceType = new ByReferenceType(typeReference3);
					PushExpression(byReferenceType, Emit.Cast(_context, new PointerType(typeReference3), Unbox((TypeReference)ins.Operand, stackInfo11)));
					PushExpression(typeReference3, Emit.InParentheses(Emit.Dereference(Emit.Cast(value: _valueStack.Pop().Expression, context: _context, type: byReferenceType))));
				}
				else
				{
					WriteCastclassOrIsInst((TypeReference)ins.Operand, stackInfo11, "Castclass");
				}
				break;
			}
			case Code.Conv_Ovf_I1:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, SByteTypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(sbyte.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U1:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, ByteTypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(byte.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_I2:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, Int16TypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(short.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U2:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, UInt16TypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(ushort.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_I4:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, Int32TypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(int.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U4:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, UInt32TypeReference, treatInputAsUnsigned: false, AppendLongLongLiteralSuffix(uint.MaxValue), () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_I8:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, Int64TypeReference, treatInputAsUnsigned: false, "(std::numeric_limits<int64_t>::max)()", () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U8:
				ConversionOpCodes.WriteNumericConversionWithOverflow(_context, _writer, _valueStack, UInt64TypeReference, treatInputAsUnsigned: true, "(std::numeric_limits<uint64_t>::max)()", () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Refanyval:
				throw new NotImplementedException();
			case Code.Ckfinite:
				throw new NotImplementedException();
			case Code.Mkrefany:
				_writer.WriteLine("#pragma message(FIXME \"mkrefany is not supported\")");
				_writer.WriteLine("IL2CPP_ASSERT(false && \"mkrefany is not supported\");");
				_valueStack.Pop();
				StoreLocalAndPush(_context.Global.Services.TypeProvider.TypedReference, "{ 0 }");
				break;
			case Code.Ldtoken:
				EmitLoadToken(ins);
				break;
			case Code.Conv_U2:
				ConversionOpCodes.WriteNumericConversion(_context, _valueStack, UInt16TypeReference, Int32TypeReference);
				break;
			case Code.Conv_U1:
				ConversionOpCodes.WriteNumericConversion(_context, _valueStack, ByteTypeReference, Int32TypeReference);
				break;
			case Code.Conv_I:
				ConversionOpCodes.ConvertToNaturalInt(_context, _valueStack, SystemIntPtr);
				break;
			case Code.Conv_Ovf_I:
				ConversionOpCodes.ConvertToNaturalIntWithOverflow(_context, _writer, _valueStack, SystemIntPtr, treatInputAsUnsigned: false, "INTPTR_MAX", () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Conv_Ovf_U:
				ConversionOpCodes.ConvertToNaturalIntWithOverflow(_context, _writer, _valueStack, SystemUIntPtr, treatInputAsUnsigned: false, "UINTPTR_MAX", () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Add_Ovf:
				ArithmeticOpCodes.Add(_context, _writer, OverflowCheck.Signed, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Add_Ovf_Un:
				ArithmeticOpCodes.Add(_context, _writer, OverflowCheck.Unsigned, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Mul_Ovf:
				ArithmeticOpCodes.Mul(_context, _writer, OverflowCheck.Signed, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Mul_Ovf_Un:
				ArithmeticOpCodes.Mul(_context, _writer, OverflowCheck.Unsigned, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Sub_Ovf:
				ArithmeticOpCodes.Sub(_context, _writer, OverflowCheck.Signed, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Sub_Ovf_Un:
				ArithmeticOpCodes.Sub(_context, _writer, OverflowCheck.Unsigned, _valueStack, () => Emit.RaiseManagedException("il2cpp_codegen_get_overflow_exception()", _runtimeMetadataAccess.MethodInfo(_methodReference)));
				break;
			case Code.Endfinally:
			{
				ExceptionSupport.Node enclosingFinallyOrFaultNode = node.GetEnclosingFinallyOrFaultNode();
				_writer.WriteLine("IL2CPP_END_FINALLY({0})", enclosingFinallyOrFaultNode.Start.Offset);
				break;
			}
			case Code.Leave:
			case Code.Leave_S:
				if (ShouldStripLeaveInstruction(block, ins))
				{
					_writer.WriteLine("; // {0}", ins);
					break;
				}
				switch (node.Type)
				{
				case ExceptionSupport.NodeType.Try:
					EmitCodeForLeaveFromTry(node, ins);
					break;
				case ExceptionSupport.NodeType.Catch:
					EmitCodeForLeaveFromCatch(node, ins);
					break;
				case ExceptionSupport.NodeType.Finally:
				case ExceptionSupport.NodeType.Fault:
					EmitCodeForLeaveFromFinallyOrFault(ins);
					break;
				case ExceptionSupport.NodeType.Block:
				case ExceptionSupport.NodeType.Root:
					EmitCodeForLeaveFromBlock(node, ins);
					break;
				}
				_valueStack.Clear();
				break;
			case Code.Stind_I:
				StoreIndirect(SystemIntPtr);
				break;
			case Code.Conv_U:
				ConversionOpCodes.ConvertToNaturalInt(_context, _valueStack, _context.Global.Services.TypeProvider.SystemUIntPtr);
				break;
			case Code.Arglist:
				_writer.WriteLine("#pragma message(FIXME \"arglist is not supported\")");
				_writer.WriteLine("IL2CPP_ASSERT(false && \"arglist is not supported\");");
				StoreLocalAndPush(_context.Global.Services.TypeProvider.RuntimeArgumentHandleTypeReference, "{ 0 }");
				break;
			case Code.Ceq:
				GenerateConditional("==", Signedness.Signed);
				break;
			case Code.Cgt:
				GenerateConditional(">", Signedness.Signed);
				break;
			case Code.Cgt_Un:
				GenerateConditional("<=", Signedness.Unsigned, negate: true);
				break;
			case Code.Clt:
				GenerateConditional("<", Signedness.Signed);
				break;
			case Code.Clt_Un:
				GenerateConditional(">=", Signedness.Unsigned, negate: true);
				break;
			case Code.Ldftn:
				PushCallToLoadFunction((MethodReference)ins.Operand);
				break;
			case Code.Ldvirtftn:
				LoadVirtualFunction(ins);
				break;
			case Code.Ldarg:
			{
				int num = ((ParameterReference)ins.Operand).Index;
				if (_methodDefinition.HasThis)
				{
					num++;
				}
				WriteLdarg(num, block, ins);
				break;
			}
			case Code.Ldarga:
				LoadArgumentAddress((ParameterReference)ins.Operand);
				break;
			case Code.Starg:
				StoreArg(ins);
				break;
			case Code.Ldloca:
				LoadLocalAddress((VariableReference)ins.Operand);
				break;
			case Code.Localloc:
			{
				StackInfo stackInfo10 = _valueStack.Pop();
				PointerType pointerType = new PointerType(SByteTypeReference);
				string text3 = NewTempName();
				_writer.WriteLine(string.Format("{0} {1} = ({0}) alloca({2});", _context.Global.Services.Naming.ForVariable(pointerType), text3, stackInfo10));
				if (_methodDefinition.Body.InitLocals)
				{
					_writer.WriteStatement(Emit.Memset(_context, text3, 0, stackInfo10.Expression));
				}
				PushExpression(pointerType, text3);
				break;
			}
			case Code.Endfilter:
			{
				StackInfo stackInfo9 = _valueStack.Pop();
				_writer.WriteLine(string.Format("{0} = ({1}) ? true : false;", "__filter_local", stackInfo9));
				break;
			}
			case Code.Volatile:
				AddVolatileStackEntry();
				break;
			case Code.Tail:
				_context.Global.Collectors.Stats.RecordTailCall(_methodDefinition);
				break;
			case Code.Initobj:
			{
				StackInfo stackInfo8 = _valueStack.Pop();
				TypeReference typeReference2 = _typeResolver.Resolve((TypeReference)ins.Operand);
				_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(typeReference2));
				_writer.WriteLine("il2cpp_codegen_initobj(" + stackInfo8.Expression + ", sizeof(" + _context.Global.Services.Naming.ForVariable(typeReference2) + "));");
				break;
			}
			case Code.Constrained:
				_constrainedCallThisType = (TypeReference)ins.Operand;
				_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(_constrainedCallThisType));
				break;
			case Code.Cpblk:
			{
				StackInfo stackInfo5 = _valueStack.Pop();
				StackInfo stackInfo6 = _valueStack.Pop();
				string text = string.Empty;
				if (stackInfo6.Type.MetadataType == MetadataType.IntPtr || stackInfo6.Type.MetadataType == MetadataType.UIntPtr)
				{
					text = "(const void*)";
				}
				StackInfo stackInfo7 = _valueStack.Pop();
				string text2 = string.Empty;
				if (stackInfo7.Type.MetadataType == MetadataType.IntPtr || stackInfo7.Type.MetadataType == MetadataType.UIntPtr)
				{
					text2 = "(void*)";
				}
				_writer.WriteLine($"il2cpp_codegen_memcpy({text2}{stackInfo7}, {text}{stackInfo6}, {stackInfo5});");
				break;
			}
			case Code.Initblk:
			{
				StackInfo stackInfo2 = _valueStack.Pop();
				StackInfo stackInfo3 = _valueStack.Pop();
				StackInfo stackInfo4 = _valueStack.Pop();
				_writer.WriteLine($"il2cpp_codegen_memset({stackInfo4}, {stackInfo3}, {stackInfo2});");
				break;
			}
			case Code.No:
				throw new NotImplementedException();
			case Code.Rethrow:
				if (node.Type == ExceptionSupport.NodeType.Finally)
				{
					_writer.WriteLine("{0} = {1};", "__last_unhandled_exception", _exceptionSupport.EmitGetActiveException(SystemExceptionTypeReference));
					WriteJump(node.Handler.HandlerStart);
				}
				else
				{
					_writer.WriteStatement(Emit.RaiseManagedException(_exceptionSupport.EmitGetActiveException(SystemExceptionTypeReference), _runtimeMetadataAccess.MethodInfo(_methodReference)));
				}
				break;
			case Code.Sizeof:
			{
				TypeReference typeReference = _typeResolver.Resolve((TypeReference)ins.Operand);
				_writer.AddIncludeForTypeDefinition(typeReference);
				StoreLocalAndPush(UInt32TypeReference, "sizeof(" + _context.Global.Services.Naming.ForVariable(typeReference) + ")");
				break;
			}
			case Code.Refanytype:
			{
				StackInfo stackInfo = _valueStack.Pop();
				if (stackInfo.Type.FullName != "System.TypedReference" && stackInfo.Type.Resolve().Module.Name == "mscorlib")
				{
					throw new InvalidOperationException();
				}
				FieldDefinition field = stackInfo.Type.Resolve().Fields.Single((FieldDefinition f) => TypeReferenceEqualityComparer.AreEqual(f.FieldType, RuntimeTypeHandleTypeReference));
				string expression = $"{stackInfo.Expression}.{_context.Global.Services.Naming.ForFieldGetter(field)}()";
				_valueStack.Push(new StackInfo(expression, RuntimeTypeHandleTypeReference));
				break;
			}
			default:
				throw new ArgumentOutOfRangeException();
			case Code.Nop:
			case Code.Break:
			case Code.Unaligned:
			case Code.Readonly:
				break;
			}
		}

		private static bool CanOptimizeAwayDelegateAllocation(Instruction ins, MethodReference method)
		{
			if (method.DeclaringType.IsDelegate())
			{
				Instruction next = ins.Next;
				if (next != null && (next.OpCode == OpCodes.Call || next.OpCode == OpCodes.Callvirt) && next.Operand is GenericInstanceMethod genericInstanceMethod && genericInstanceMethod.DeclaringType.FullName == "Unity.Entities.EntityQueryBuilder" && genericInstanceMethod.Name == "ForEach")
				{
					return true;
				}
			}
			return false;
		}

		private static bool BothSuccessorsAreTheSame(InstructionBlock block)
		{
			if (block.Successors.Count() == 2 && block.Successors.First() == block.Successors.Last())
			{
				return true;
			}
			return false;
		}

		private void WriteCheckSequencePoint(SequencePoint sequencePoint)
		{
			if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation)
			{
				SequencePointInfo sequencePointAt = _sequencePointProvider.GetSequencePointAt(_methodDefinition, sequencePoint.Offset, SequencePointKind.Normal);
				WriteCheckSequencePoint(sequencePointAt);
			}
		}

		private string GetSequencePoint(SequencePointInfo sequencePointInfo)
		{
			int seqPointIndex = _sequencePointProvider.GetSeqPointIndex(sequencePointInfo);
			string sequencePointName = DebugWriter.GetSequencePointName(_context, _methodDefinition.Module.Assembly);
			_writer.AddForwardDeclaration("IL2CPP_EXTERN_C Il2CppSequencePoint " + sequencePointName + "[]");
			return $"({sequencePointName} + {seqPointIndex})";
		}

		private void WriteCheckSequencePoint(SequencePointInfo sequencePointInfo)
		{
			if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation)
			{
				if (sequencePointInfo.IlOffset == -1)
				{
					_writer.WriteLine("CHECK_METHOD_ENTRY_SEQ_POINT(" + _context.Global.Services.Naming.ForMethodExecutionContextVariable() + ", " + GetSequencePoint(sequencePointInfo) + ");");
				}
				else
				{
					_writer.WriteLine("CHECK_SEQ_POINT(" + _context.Global.Services.Naming.ForMethodExecutionContextVariable() + ", " + GetSequencePoint(sequencePointInfo) + ");");
				}
			}
		}

		private void WriteCheckMethodExitSequencePoint(SequencePointInfo sequencePointInfo)
		{
			if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation)
			{
				_writer.WriteLine("CHECK_METHOD_EXIT_SEQ_POINT(" + _context.Global.Services.Naming.ForMethodExitSequencePointChecker() + ", " + _context.Global.Services.Naming.ForMethodExecutionContextVariable() + ", " + GetSequencePoint(sequencePointInfo) + ");");
			}
		}

		private void WriteCheckStepOutSequencePoint(Instruction ins)
		{
			if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation && _sequencePointProvider.TryGetSequencePointAt(_methodDefinition, ins.Offset, SequencePointKind.StepOut, out var info))
			{
				_writer.WriteLine("CHECK_SEQ_POINT(" + _context.Global.Services.Naming.ForMethodExecutionContextVariable() + ", " + GetSequencePoint(info) + ");");
			}
		}

		private void WriteStoreStepOutSequencePoint(Instruction ins)
		{
			if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation && _sequencePointProvider.TryGetSequencePointAt(_methodDefinition, ins.Offset, SequencePointKind.StepOut, out var info))
			{
				_writer.WriteLine("STORE_SEQ_POINT(" + _context.Global.Services.Naming.ForMethodExecutionContextVariable() + ", " + GetSequencePoint(info) + ");");
			}
		}

		private void WriteCheckPausePoint(int offset)
		{
			if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation && _sequencePointProvider.MethodHasPausePointAtOffset(_methodDefinition, offset))
			{
				_writer.WriteLine("CHECK_PAUSE_POINT;");
			}
		}

		private void WriteStoreTryId(ExceptionSupport.Node node)
		{
			if (_context.Global.Parameters.EnableDebugger && _shouldEmitDebugInformation)
			{
				_writer.WriteLine($"STORE_TRY_ID({_context.Global.Services.Naming.ForMethodExecutionContextVariable()}, {node?.Id ?? (-1)});");
			}
		}

		private static string AppendLongLongLiteralSuffix<T>(T value)
		{
			return $"{value}LL";
		}

		private void WriteReturnStatement()
		{
			TypeReference typeReference = _typeResolver.ResolveReturnType(_methodDefinition);
			bool flag = typeReference.IsVoid();
			if ((flag && _valueStack.Count > 0) || (!flag && _valueStack.Count > 1))
			{
				throw new InvalidOperationException($"Attempting to return a value from method '{_methodDefinition.FullName}' when there is no value on the stack. Is this invalid IL code?");
			}
			if (!flag)
			{
				StackInfo right = _valueStack.Pop();
				string text = CastIfPointerType(typeReference);
				string empty = string.Empty;
				CodeWriterExtensions.WriteManagedReturnStatement(returnExpression: (typeReference.FullName != right.Type.FullName && typeReference.Resolve() != null && typeReference.Resolve().IsEnum) ? ("(" + _context.Global.Services.Naming.ForVariable(typeReference) + ")(" + right.Expression + ")") : (string.IsNullOrEmpty(text) ? WriteExpressionAndCastIfNeeded(_context, typeReference, right, _sharingType) : (text + "(" + right.Expression + ")")), writer: _writer);
			}
			else
			{
				_writer.WriteLine("return;");
			}
		}

		private bool CanApplyValueTypeBoxBranchOptimizationToInstruction(Instruction ins, InstructionBlock block)
		{
			if (ins != null && ins.OpCode.Code == Code.Box)
			{
				TypeReference typeReference = _typeResolver.Resolve((TypeReference)ins.Operand);
				if (typeReference.IsValueType() && !typeReference.IsNullable() && ins != block.Last && ins.Next != null)
				{
					if (ins.Next.OpCode.Code != Code.Brtrue && ins.Next.OpCode.Code != Code.Brtrue_S && ins.Next.OpCode.Code != Code.Brfalse)
					{
						return ins.Next.OpCode.Code == Code.Brfalse_S;
					}
					return true;
				}
				return false;
			}
			return false;
		}

		private void WriteConstrainedCallExpressionFor(ref MethodReference methodToCall, MethodCallType callType, List<StackInfo> poppedValues, Func<string, string> addUniqueSuffix, out string copyBackBoxedExpr)
		{
			MethodReference methodReference = _typeResolver.Resolve(methodToCall);
			TypeResolver typeResolver = _typeResolver;
			if (methodReference is GenericInstanceMethod genericInstanceMethod)
			{
				typeResolver = _typeResolver.Nested(genericInstanceMethod);
			}
			StackInfo stackInfo = poppedValues[0];
			TypeReference typeReference = null;
			bool flag = false;
			TypeReference typeReference2 = _constrainedCallThisType;
			if (stackInfo.Type is ByReferenceType byReferenceType)
			{
				typeReference = byReferenceType.ElementType.WithoutModifiers();
			}
			else if (stackInfo.Type is PointerType pointerType)
			{
				typeReference = pointerType.ElementType.WithoutModifiers();
			}
			else if (stackInfo.BoxedType != null)
			{
				flag = true;
				typeReference2 = stackInfo.BoxedType;
				typeReference = _typeResolver.Resolve(typeReference2);
			}
			else if (stackInfo.Type.IsIntegralPointerType())
			{
				typeReference = _typeResolver.Resolve(typeReference2);
				stackInfo = (poppedValues[0] = new StackInfo(Emit.Cast(_writer.Context, typeReference.MakePointerType(), stackInfo.Expression), typeReference.MakePointerType()));
			}
			if (stackInfo.BoxedType == null)
			{
				if (typeReference == null)
				{
					throw new InvalidOperationException("Attempting to constrain an invalid type.");
				}
				TypeReference typeReference3 = _typeResolver.Resolve(typeReference2);
				if (!TypeReferenceEqualityComparer.AreEqual(typeReference.WithoutModifiers(), typeReference3.WithoutModifiers()))
				{
					throw new InvalidOperationException($"Attempting to constrain a value of type '{typeReference}' to type '{typeReference3}'.");
				}
			}
			copyBackBoxedExpr = null;
			if (!typeReference.IsValueType() && !typeReference.IsPointer)
			{
				poppedValues[0] = new StackInfo(Emit.InParentheses(Emit.Dereference(stackInfo.Expression)), typeReference);
				_writer.AddIncludeForTypeDefinition(methodReference.DeclaringType);
				WriteCallExpressionFor(methodToCall, callType, poppedValues, addUniqueSuffix);
				return;
			}
			if (_context.Global.Parameters.CanShareEnumTypes && _sharingType == SharingType.Shared && typeReference.IsEnum())
			{
				List<StackInfo> list = new List<StackInfo>(poppedValues);
				if (flag)
				{
					list[0] = stackInfo;
				}
				else
				{
					string value = FakeBox(typeReference, _runtimeMetadataAccess.TypeInfoFor(_constrainedCallThisType), stackInfo.Expression);
					list[0] = new StackInfo(Emit.AddressOf(value), _context.Global.Services.TypeProvider.ObjectTypeReference);
				}
				WriteCallExpressionFor(methodToCall, callType, list, addUniqueSuffix, emitNullCheckForInvocation: false);
				return;
			}
			MethodReference methodReference2 = _vTableBuilder.GetVirtualMethodTargetMethodForConstrainedCallOnValueType(_context, typeReference, methodReference);
			if (methodReference2 != null && TypeReferenceEqualityComparer.AreEqual(methodReference2.DeclaringType, typeReference))
			{
				if (methodReference.IsGenericInstance)
				{
					methodReference2 = typeResolver.Resolve(methodReference2, resolveGenericParameters: true);
				}
				if (typeReference.IsGenericInstance && typeReference.IsValueType() && _sharingType == SharingType.Shared)
				{
					List<StackInfo> list2 = new List<StackInfo>(poppedValues);
					if (flag)
					{
						list2[0] = stackInfo;
						copyBackBoxedExpr = null;
					}
					else
					{
						string text = FakeBox(typeReference, _runtimeMetadataAccess.TypeInfoFor(typeReference2), stackInfo.Expression);
						list2[0] = new StackInfo(Emit.AddressOf(text), _context.Global.Services.TypeProvider.ObjectTypeReference, typeReference2);
						copyBackBoxedExpr = Emit.Assign(Emit.Dereference(stackInfo.Expression), text + ".m_Value");
					}
					string text2 = addUniqueSuffix("il2cpp_virtual_invoke_data_");
					if (methodReference.DeclaringType.IsInterface())
					{
						if (methodReference.IsGenericInstance)
						{
							if (!flag)
							{
								poppedValues[0] = BoxThisForContraintedCallIntoNewTemp(typeReference, stackInfo, typeReference2);
								string value2 = Unbox(typeReference2, poppedValues[0]);
								copyBackBoxedExpr = Emit.Assign(Emit.Dereference(stackInfo.Expression), Emit.Dereference(Emit.Cast(_context.Global.Services.Naming.ForVariable(typeReference) + "*", value2)));
							}
							WriteCallExpressionFor(methodToCall, callType, poppedValues, addUniqueSuffix);
							return;
						}
						_writer.WriteLine($"const VirtualInvokeData& {text2} = il2cpp_codegen_get_interface_invoke_data({_vTableBuilder.IndexFor(_context, methodToCall.Resolve())}, {list2[0]}, {_runtimeMetadataAccess.TypeInfoFor(methodToCall.DeclaringType)});");
					}
					else
					{
						_writer.WriteLine($"const VirtualInvokeData& {text2} = il2cpp_codegen_get_virtual_invoke_data({_vTableBuilder.IndexFor(_context, methodToCall.Resolve())}, {list2[0]});");
					}
					WriteCallExpressionFor(methodToCall, MethodCallType.DirectVirtual, list2, addUniqueSuffix, emitNullCheckForInvocation: false);
				}
				else
				{
					methodToCall = methodReference2;
					_writer.AddIncludeForTypeDefinition(methodToCall.DeclaringType);
					_writer.AddIncludeForTypeDefinition(_typeResolver.ResolveReturnType(methodToCall));
					callType = MethodCallType.Normal;
					if (flag)
					{
						poppedValues[0] = new StackInfo(Unbox(typeReference, stackInfo), new ByReferenceType(typeReference));
					}
					WriteCallExpressionFor(methodToCall, callType, poppedValues, addUniqueSuffix);
				}
			}
			else if (typeReference.IsEnum() && methodToCall.Name == "GetHashCode")
			{
				MethodDefinition methodDefinition = (MethodDefinition)(methodToCall = typeReference.GetUnderlyingEnumType().Resolve().Methods.Single((MethodDefinition m) => m.Name == "GetHashCode"));
				_writer.AddIncludeForTypeDefinition(methodDefinition.DeclaringType);
				callType = MethodCallType.Normal;
				if (flag)
				{
					poppedValues[0] = new StackInfo(Unbox(typeReference, stackInfo), new ByReferenceType(typeReference));
				}
				WriteCallExpressionFor(methodToCall, callType, poppedValues, addUniqueSuffix);
			}
			else
			{
				poppedValues[0] = (flag ? stackInfo : BoxThisForContraintedCallIntoNewTemp(typeReference, stackInfo, typeReference2));
				if (!flag && !typeReference.IsNullable())
				{
					copyBackBoxedExpr = Emit.Assign(Emit.Dereference(stackInfo.Expression), Emit.Dereference(Emit.Cast(_context.Global.Services.Naming.ForVariable(typeReference) + "*", Emit.Call("UnBox", poppedValues[0].Expression))));
				}
				WriteCallExpressionFor(methodToCall, callType, poppedValues, addUniqueSuffix);
			}
		}

		private string FakeBox(TypeReference thisType, string typeInfoVariable, string pointerToValue)
		{
			string text = NewTempName();
			_writer.WriteLine("Il2CppFakeBox<" + _context.Global.Services.Naming.ForVariable(thisType) + "> " + text + "(" + typeInfoVariable + ", " + pointerToValue + ");");
			return text;
		}

		private StackInfo BoxThisForContraintedCallIntoNewTemp(TypeReference thisType, StackInfo thisValue, TypeReference unresolvedConstrainedType)
		{
			StackInfo stackInfo = NewTemp(_context.Global.Services.TypeProvider.ObjectTypeReference);
			if (_context.Global.Parameters.UsingTinyBackend)
			{
				if (thisType.IsNullable())
				{
					TypeReference typeReference = ((GenericInstanceType)thisType).GenericArguments[0];
					_writer.WriteLine("{0} = {1};", stackInfo.GetIdentifierExpression(_context), Emit.Call("BoxNullable<" + _context.Global.Services.Naming.ForVariable(thisType) + ", " + _context.Global.Services.Naming.ForVariable(typeReference) + ">", _runtimeMetadataAccess.TypeInfoFor(typeReference), thisValue.Expression));
				}
				else
				{
					_writer.WriteLine("{0} = {1};", stackInfo.GetIdentifierExpression(_context), Emit.Call("Box", _runtimeMetadataAccess.TypeInfoFor(unresolvedConstrainedType), thisValue.Expression, "sizeof(" + _context.Global.Services.Naming.ForVariable(_typeResolver.Resolve(unresolvedConstrainedType)) + ")"));
				}
			}
			else
			{
				_writer.WriteLine("{0} = {1};", stackInfo.GetIdentifierExpression(_context), Emit.Call("Box", _runtimeMetadataAccess.TypeInfoFor(unresolvedConstrainedType), thisValue.Expression));
			}
			return new StackInfo(stackInfo.Expression, _context.Global.Services.TypeProvider.ObjectTypeReference, unresolvedConstrainedType);
		}

		private void EmitCodeForLeaveFromTry(ExceptionSupport.Node node, Instruction ins)
		{
			int offset = ((Instruction)ins.Operand).Offset;
			ExceptionSupport.Node[] array = node.GetTargetFinallyNodesForJump(ins.Offset, offset).ToArray();
			if (array.Length != 0)
			{
				ExceptionSupport.Node node2 = array.First();
				ExceptionSupport.Node[] array2 = array;
				foreach (ExceptionSupport.Node finallyNode in array2)
				{
					_exceptionSupport.AddLeaveTarget(finallyNode, ins);
				}
				_writer.WriteLine("IL2CPP_LEAVE(0x{0:X}, {1});", ((Instruction)ins.Operand).Offset, _labeler.FormatOffset(node2.Start));
			}
			else
			{
				_writer.WriteLine(_labeler.ForJump(offset));
			}
		}

		private void EmitCodeForLeaveFromCatch(ExceptionSupport.Node node, Instruction ins)
		{
			int offset = ((Instruction)ins.Operand).Offset;
			ExceptionSupport.Node[] array = node.GetTargetFinallyNodesForJump(ins.Offset, offset).ToArray();
			_writer.WriteLine(_exceptionSupport.EmitPopActiveException() + ";");
			if (array.Length != 0)
			{
				ExceptionSupport.Node node2 = array.First();
				ExceptionSupport.Node[] array2 = array;
				foreach (ExceptionSupport.Node finallyNode in array2)
				{
					_exceptionSupport.AddLeaveTarget(finallyNode, ins);
				}
				_writer.WriteLine("IL2CPP_LEAVE(0x{0:X}, {1});", ((Instruction)ins.Operand).Offset, _labeler.FormatOffset(node2.Start));
			}
			else
			{
				_writer.WriteLine(_labeler.ForJump(offset));
			}
		}

		private void EmitCodeForLeaveFromFinallyOrFault(Instruction ins)
		{
			_writer.WriteLine(_labeler.ForJump(((Instruction)ins.Operand).Offset));
		}

		private void EmitCodeForLeaveFromBlock(ExceptionSupport.Node node, Instruction ins)
		{
			int offset = ((Instruction)ins.Operand).Offset;
			if (!node.IsInTryBlock && !node.IsInCatchBlock)
			{
				_writer.WriteLine(_labeler.ForJump(offset));
				return;
			}
			if (node.IsInCatchBlock)
			{
				_writer.WriteLine(_exceptionSupport.EmitPopActiveException() + ";");
			}
			ExceptionSupport.Node[] array = node.GetTargetFinallyNodesForJump(ins.Offset, offset).ToArray();
			if (array.Length != 0)
			{
				ExceptionSupport.Node node2 = array.First();
				ExceptionSupport.Node[] array2 = array;
				foreach (ExceptionSupport.Node finallyNode in array2)
				{
					_exceptionSupport.AddLeaveTarget(finallyNode, ins);
				}
				_writer.WriteLine("IL2CPP_LEAVE(0x{0:X}, {1});", ((Instruction)ins.Operand).Offset, _labeler.FormatOffset(node2.Start));
			}
			else
			{
				_writer.WriteLine(_labeler.ForJump(offset));
			}
		}

		private bool ShouldStripLeaveInstruction(InstructionBlock block, Instruction ins)
		{
			if (!_labeler.NeedsLabel(ins))
			{
				if (block.First == block.Last && block.First.Previous != null)
				{
					return block.First.Previous.OpCode.Code == Code.Leave;
				}
				return false;
			}
			return false;
		}

		private void PushExpression(TypeReference typeReference, string expression, TypeReference boxedType = null, MethodReference methodExpressionIsPointingTo = null)
		{
			_valueStack.Push(new StackInfo($"({expression})", typeReference, boxedType, methodExpressionIsPointingTo));
		}

		private string EmitArrayLoadElementAddress(StackInfo array, string indexExpression)
		{
			_arrayBoundsCheckSupport.RecordArrayBoundsCheckEmitted(_context);
			return Emit.LoadArrayElementAddress(array.Expression, indexExpression, _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod());
		}

		private void LoadArgumentAddress(ParameterReference parameter)
		{
			string text = ((parameter.Index == -1) ? "__this" : ParameterNameFor(_methodDefinition, parameter.Index));
			_valueStack.Push(new StackInfo("(&" + text + ")", new ByReferenceType(_typeResolver.ResolveParameterType(_methodReference, parameter)).WithoutModifiers()));
		}

		private void WriteLabelForBranchTarget(Instruction ins)
		{
			if (!DidAlreadyEmitLabelFor(ins))
			{
				_emittedLabels.Add(ins);
				string arg = "";
				if (_referencedLabels.Contains(ins))
				{
					_writer.WriteLine();
					_writer.WriteUnindented($"{arg}{_labeler.ForLabel(ins)}");
				}
			}
		}

		private bool DidAlreadyEmitLabelFor(Instruction ins)
		{
			return _emittedLabels.Contains(ins);
		}

		private void WriteJump(Instruction targetInstruction)
		{
			_writer.WriteLine(_labeler.ForJump(targetInstruction));
		}

		private void LoadLocalAddress(VariableReference variableReference)
		{
			_valueStack.Push(new StackInfo("(&" + _context.Global.Services.Naming.ForVariableName(variableReference) + ")", new ByReferenceType(_typeResolver.Resolve(variableReference.VariableType))));
		}

		private void WriteDup()
		{
			StackInfo right = _valueStack.Pop();
			if (right.Expression == "__this")
			{
				_valueStack.Push(new StackInfo("__this", right.Type));
				_valueStack.Push(new StackInfo("__this", right.Type));
				return;
			}
			if (right.Expression == "NULL" && right.Type.IsSystemObject())
			{
				_valueStack.Push(new StackInfo("NULL", ObjectTypeReference));
				_valueStack.Push(new StackInfo("NULL", ObjectTypeReference));
				return;
			}
			StackInfo local = NewTemp(right.Type);
			WriteAssignment(_context, local.GetIdentifierExpression(_context), right.Type, right);
			_valueStack.Push(new StackInfo(local));
			_valueStack.Push(new StackInfo(local));
		}

		private void WriteNotOperation()
		{
			StackInfo stackInfo = _valueStack.Pop();
			PushExpression(stackInfo.Type, $"(~{stackInfo.Expression})");
		}

		private void WriteNegateOperation()
		{
			StackInfo originalValue = _valueStack.Pop();
			TypeReference toType = CalculateResultTypeForNegate(originalValue.Type);
			PushExpression(originalValue.Type, $"(-{CastTypeIfNeeded(originalValue, toType)})");
		}

		private TypeReference CalculateResultTypeForNegate(TypeReference type)
		{
			if (type.IsUnsignedIntegralType())
			{
				if (type.MetadataType == MetadataType.Byte || type.MetadataType == MetadataType.UInt16 || type.MetadataType == MetadataType.UInt32)
				{
					return _context.Global.Services.TypeProvider.Int32TypeReference;
				}
				return _context.Global.Services.TypeProvider.Int64TypeReference;
			}
			return type;
		}

		private void LoadConstant(TypeReference type, string stringValue)
		{
			PushExpression(type, stringValue);
		}

		private void StoreLocalAndPush(TypeReference type, string stringValue)
		{
			StackInfo local = NewTemp(type);
			_writer.WriteLine("{0} = {1};", local.GetIdentifierExpression(_context), stringValue);
			_valueStack.Push(new StackInfo(local));
		}

		private void StoreLocalAndPush(TypeReference type, string stringValue, TypeReference boxedType)
		{
			StackInfo stackInfo = NewTemp(type);
			_writer.WriteLine("{0} = {1};", stackInfo.GetIdentifierExpression(_context), stringValue);
			_valueStack.Push(new StackInfo(stackInfo.Expression, stackInfo.Type, boxedType));
		}

		private void StoreLocalAndPush(TypeReference type, string stringValue, MethodReference methodExpressionIsPointingTo)
		{
			StackInfo stackInfo = NewTemp(type);
			_writer.WriteLine("{0} = {1};", stackInfo.GetIdentifierExpression(_context), stringValue);
			_valueStack.Push(new StackInfo(stackInfo.Expression, stackInfo.Type, null, methodExpressionIsPointingTo));
		}

		private void WriteCallExpressionFor(MethodReference unresolvedMethodToCall, MethodCallType callType, List<StackInfo> poppedValues, Func<string, string> addUniqueSuffix, bool emitNullCheckForInvocation = true)
		{
			MethodReference methodReference = _typeResolver.Resolve(unresolvedMethodToCall);
			TypeResolver typeResolverForMethodToCall = _typeResolver;
			if (methodReference is GenericInstanceMethod genericInstanceMethod)
			{
				typeResolverForMethodToCall = _typeResolver.Nested(genericInstanceMethod);
			}
			List<TypeReference> parameterTypes = GetParameterTypes(methodReference, typeResolverForMethodToCall);
			if (methodReference.HasThis)
			{
				parameterTypes.Insert(0, methodReference.DeclaringType.IsValueType() ? new ByReferenceType(methodReference.DeclaringType) : methodReference.DeclaringType);
			}
			List<string> argsFor = FormatArgumentsForMethodCall(parameterTypes, poppedValues, _sharingType);
			WriteCallExpressionFor(_methodReference, unresolvedMethodToCall, callType, argsFor, addUniqueSuffix, emitNullCheckForInvocation);
		}

		private void WriteCallExpressionFor(MethodReference callingMethod, MethodReference unresolvedMethodToCall, MethodCallType callType, List<string> argsFor, Func<string, string> addUniqueSuffix, bool emitNullCheckForInvocation = true)
		{
			MethodReference methodReference = _typeResolver.Resolve(unresolvedMethodToCall);
			TypeResolver typeResolver = _typeResolver;
			Func<string> getHiddenMethodInfo = () => string.Concat(str1: (callType != MethodCallType.DirectVirtual) ? _runtimeMetadataAccess.HiddenMethodInfo(unresolvedMethodToCall) : (addUniqueSuffix("il2cpp_virtual_invoke_data_") + ".method"), str0: _context.Global.Parameters.EmitComments ? "/*hidden argument*/" : "");
			if (emitNullCheckForInvocation)
			{
				_nullCheckSupport.WriteNullCheckForInvocationIfNeeded(methodReference, argsFor);
			}
			if (GenericSharingAnalysis.ShouldTryToCallStaticConstructorBeforeMethodCall(_context, methodReference, _methodReference))
			{
				WriteCallToClassAndInitializerAndStaticConstructorIfNeeded(unresolvedMethodToCall.DeclaringType, _methodDefinition, _runtimeMetadataAccess);
			}
			string returnVariable = "";
			if (!unresolvedMethodToCall.ReturnType.IsVoid())
			{
				TypeReference type = _typeResolver.ResolveReturnType(unresolvedMethodToCall);
				StackInfo stackInfo = NewTemp(type);
				returnVariable = stackInfo.Expression;
				_valueStack.Push(new StackInfo(stackInfo.Expression, type));
				_writer.WriteStatement(stackInfo.GetIdentifierExpression(_context));
			}
			WriteMethodCallExpression(returnVariable, getHiddenMethodInfo, _writer, callingMethod, methodReference, unresolvedMethodToCall, typeResolver, callType, _runtimeMetadataAccess, _vTableBuilder, argsFor, _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod(), addUniqueSuffix);
		}

		private static void WriteCallAndAssignReturnValue(IGeneratedMethodCodeWriter writer, string returnVariable, string expression)
		{
			if (string.IsNullOrEmpty(returnVariable))
			{
				writer.WriteStatement(expression);
			}
			else
			{
				writer.WriteStatement(returnVariable + " = " + expression);
			}
		}

		private static void WriteCallAndOptionallyAssignReturnValue(IGeneratedMethodCodeWriter writer, string returnVariable, string expression)
		{
			if (string.IsNullOrEmpty(returnVariable) || writer.Context.Global.Parameters.ReturnAsByRefParameter)
			{
				writer.WriteStatement(expression);
			}
			else
			{
				writer.WriteStatement(returnVariable + " = " + expression);
			}
		}

		private static bool TryWriteIntrinsicMethodCall(string returnVariable, IGeneratedMethodCodeWriter writer, MethodReference callingMethod, MethodReference methodToCall, IRuntimeMetadataAccess runtimeMetadataAccess, IEnumerable<string> argumentArray, bool useArrayBoundsCheck)
		{
			if (methodToCall.DeclaringType.IsArray && methodToCall.Name == "Set")
			{
				WriteCallAndAssignReturnValue(writer, returnVariable, GetArraySetCall(argumentArray.First(), argumentArray.Skip(1).AggregateWithComma(), useArrayBoundsCheck));
				return true;
			}
			if (methodToCall.DeclaringType.IsArray && methodToCall.Name == "Get")
			{
				WriteCallAndAssignReturnValue(writer, returnVariable, GetArrayGetCall(argumentArray.First(), argumentArray.Skip(1).AggregateWithComma(), useArrayBoundsCheck));
				return true;
			}
			if (methodToCall.DeclaringType.IsArray && methodToCall.Name == "Address")
			{
				WriteCallAndAssignReturnValue(writer, returnVariable, GetArrayAddressCall(argumentArray.First(), argumentArray.Skip(1).AggregateWithComma(), useArrayBoundsCheck));
				return true;
			}
			if (methodToCall.DeclaringType.IsSystemArray() && methodToCall.Name == "GetGenericValueImpl")
			{
				WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call("ArrayGetGenericValueImpl", argumentArray));
				return true;
			}
			if (methodToCall.DeclaringType.IsSystemArray() && methodToCall.Name == "SetGenericValueImpl")
			{
				WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call("ArraySetGenericValueImpl", argumentArray));
				return true;
			}
			if (GenericsUtilities.IsGenericInstanceOfCompareExchange(methodToCall))
			{
				GenericInstanceMethod genericInstanceMethod = (GenericInstanceMethod)methodToCall;
				string arg = writer.Context.Global.Services.Naming.ForVariable(genericInstanceMethod.GenericArguments[0]);
				WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call($"InterlockedCompareExchangeImpl<{arg}>", argumentArray));
				return true;
			}
			if (GenericsUtilities.IsGenericInstanceOfExchange(methodToCall))
			{
				GenericInstanceMethod genericInstanceMethod2 = (GenericInstanceMethod)methodToCall;
				string arg2 = writer.Context.Global.Services.Naming.ForVariable(genericInstanceMethod2.GenericArguments[0]);
				WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call($"InterlockedExchangeImpl<{arg2}>", argumentArray));
				return true;
			}
			if (IntrinsicRemap.ShouldRemap(writer.Context, methodToCall))
			{
				WriteCallAndAssignReturnValue(writer, returnVariable, Emit.Call(IntrinsicRemap.MappedNameFor(writer.Context, methodToCall), IntrinsicRemap.HasCustomArguments(methodToCall) ? IntrinsicRemap.GetCustomArguments(writer.Context, methodToCall, callingMethod, runtimeMetadataAccess, argumentArray) : argumentArray));
				return true;
			}
			if (IsManagedIntrinsics.IsUnmangedCall(writer.Context, methodToCall))
			{
				WriteCallAndAssignReturnValue(writer, returnVariable, IsManagedIntrinsics.IsArgumentTypeUnmanaged(methodToCall));
				return true;
			}
			if (IsManagedIntrinsics.IsManagedCall(methodToCall))
			{
				WriteCallAndAssignReturnValue(writer, returnVariable, IsManagedIntrinsics.IsArgumentTypeManaged(methodToCall));
				return true;
			}
			return false;
		}

		internal static void WriteMethodCallExpression(string returnVariable, Func<string> getHiddenMethodInfo, IGeneratedMethodCodeWriter writer, MethodReference callingMethod, MethodReference methodToCall, MethodReference unresolvedMethodtoCall, TypeResolver typeResolverForMethodToCall, MethodCallType callType, IRuntimeMetadataAccess runtimeMetadataAccess, IVTableBuilder vTableBuilder, IEnumerable<string> argumentArray, bool useArrayBoundsCheck, Func<string, string> addUniqueSuffix = null)
		{
			if (TryWriteIntrinsicMethodCall(returnVariable, writer, callingMethod, methodToCall, runtimeMetadataAccess, argumentArray, useArrayBoundsCheck))
			{
				return;
			}
			if (methodToCall.Resolve().IsStatic)
			{
				List<string> list = new List<string>(argumentArray);
				if (!string.IsNullOrEmpty(returnVariable) && writer.Context.Global.Parameters.ReturnAsByRefParameter)
				{
					list.Add("&" + returnVariable);
				}
				if (!writer.Context.Global.Parameters.UsingTinyBackend)
				{
					list.Add(getHiddenMethodInfo());
				}
				WriteCallAndOptionallyAssignReturnValue(writer, returnVariable, Emit.Call(runtimeMetadataAccess.Method(unresolvedMethodtoCall), list));
				return;
			}
			switch (callType)
			{
			case MethodCallType.DirectVirtual:
			{
				List<string> list4 = new List<string>(argumentArray);
				if (!string.IsNullOrEmpty(returnVariable) && writer.Context.Global.Parameters.ReturnAsByRefParameter)
				{
					list4.Add("&" + returnVariable);
				}
				if (!writer.Context.Global.Parameters.UsingTinyBackend)
				{
					list4.Add(getHiddenMethodInfo());
				}
				string methodPointerForVTable = MethodSignatureWriter.GetMethodPointerForVTable(writer.Context, methodToCall);
				string text = addUniqueSuffix("il2cpp_virtual_invoke_data_");
				WriteCallAndOptionallyAssignReturnValue(writer, returnVariable, Emit.Call("(" + Emit.Cast(methodPointerForVTable, text + ".methodPtr") + ")", list4));
				break;
			}
			default:
				if (!MethodSignatureWriter.CanDevirtualizeMethodCall(methodToCall.Resolve()))
				{
					if (methodToCall.IsGenericInstance && writer.Context.Global.Parameters.UsingTinyClassLibraries)
					{
						writer.WriteStatement("il2cpp_codegen_raise_generic_virtual_method_exception(\"" + methodToCall.FullName + "\")");
						writer.Context.Global.Services.MessageLogger.LogWarning(ErrorMessageWriter.AppendLocationInformation(writer.Context.Global.Services.ErrorInformation, "Calls to generic virtual methods are unsupported in tiny - call to " + methodToCall.FullName));
						break;
					}
					List<string> list2 = new List<string>(argumentArray);
					if (!string.IsNullOrEmpty(returnVariable) && writer.Context.Global.Parameters.ReturnAsByRefParameter)
					{
						list2.Add("&" + returnVariable);
					}
					WriteCallAndOptionallyAssignReturnValue(writer, returnVariable, VirtualCallFor(writer, methodToCall, unresolvedMethodtoCall, list2, typeResolverForMethodToCall, runtimeMetadataAccess, vTableBuilder));
					break;
				}
				goto case MethodCallType.Normal;
			case MethodCallType.Normal:
			{
				List<string> list3 = new List<string>(argumentArray);
				if (!string.IsNullOrEmpty(returnVariable) && writer.Context.Global.Parameters.ReturnAsByRefParameter)
				{
					list3.Add("&" + returnVariable);
				}
				if (!writer.Context.Global.Parameters.UsingTinyBackend)
				{
					list3.Add(getHiddenMethodInfo());
				}
				if (unresolvedMethodtoCall.DeclaringType.IsValueType())
				{
					WriteCallAndOptionallyAssignReturnValue(writer, returnVariable, Emit.Call(runtimeMetadataAccess.Method(methodToCall), list3));
				}
				else
				{
					WriteCallAndOptionallyAssignReturnValue(writer, returnVariable, Emit.Call(runtimeMetadataAccess.Method(unresolvedMethodtoCall), list3));
				}
				break;
			}
			}
		}

		private static string VirtualCallFor(IGeneratedMethodCodeWriter writer, MethodReference method, MethodReference unresolvedMethod, IEnumerable<string> args, TypeResolver typeResolver, IRuntimeMetadataAccess runtimeMetadataAccess, IVTableBuilder vTableBuilder)
		{
			bool isInterface = method.DeclaringType.Resolve().IsInterface;
			List<string> list = new List<string> { method.IsGenericInstance ? runtimeMetadataAccess.MethodInfo(unresolvedMethod) : (vTableBuilder.IndexFor(writer.Context, method.Resolve()) + " /* " + method.FullName + " */") };
			if (isInterface && !method.IsGenericInstance)
			{
				list.Add(runtimeMetadataAccess.TypeInfoFor(unresolvedMethod.DeclaringType));
			}
			list.AddRange(args);
			return Emit.Call(writer.VirtualCallInvokeMethod(method, typeResolver), list);
		}

		private static string GetArrayAddressCall(string array, string arguments, bool useArrayBoundsCheck)
		{
			return Emit.Call($"({array})->{ArrayNaming.ForArrayItemAddressGetter(useArrayBoundsCheck)}", arguments);
		}

		private static string GetArrayGetCall(string array, string arguments, bool useArrayBoundsCheck)
		{
			return Emit.Call($"({array})->{ArrayNaming.ForArrayItemGetter(useArrayBoundsCheck)}", arguments);
		}

		private static string GetArraySetCall(string array, string arguments, bool useArrayBoundsCheck)
		{
			return Emit.Call($"({array})->{ArrayNaming.ForArrayItemSetter(useArrayBoundsCheck)}", arguments);
		}

		private void WriteUnconditionalJumpTo(InstructionBlock block, Instruction target)
		{
			if (block.Successors.Count() != 1)
			{
				throw new ArgumentException("Expected only one successor for the current block", "target");
			}
			WriteAssignGlobalVariables(_stackAnalysis.InputVariablesFor(block.Successors.Single()));
			WriteJump(target);
		}

		private void WriteCastclassOrIsInst(TypeReference targetType, StackInfo value, string operation)
		{
			TypeReference typeReference = _typeResolver.Resolve(targetType);
			if (value.BoxedType != null && typeReference.IsInterface())
			{
				TypeReference typeReference2 = _typeResolver.Resolve(value.BoxedType);
				if (!typeReference2.IsNullable())
				{
					while (typeReference2 != null)
					{
						foreach (TypeReference @interface in typeReference2.GetInterfaces(_context))
						{
							if (TypeReferenceEqualityComparer.AreEqual(@interface, typeReference))
							{
								_valueStack.Push(value);
								return;
							}
						}
						typeReference2 = typeReference2.GetBaseType(_context);
					}
					if (operation == "IsInst")
					{
						LoadNull();
						return;
					}
				}
			}
			TypeReference type = (typeReference.IsValueType() ? _context.Global.Services.TypeProvider.ObjectTypeReference : typeReference);
			_writer.AddIncludeForTypeDefinition(typeReference);
			string text = Emit.Cast(_context, type, GetCastclassOrIsInstCall(targetType, value, operation, typeReference));
			_valueStack.Push(new StackInfo("(" + text + ")", type, value.BoxedType));
		}

		private string GetCastclassOrIsInstCall(TypeReference targetType, StackInfo value, string operation, TypeReference resolvedTypeReference)
		{
			if (_context.Global.Parameters.UsingTinyBackend && resolvedTypeReference.IsNullable())
			{
				targetType = (resolvedTypeReference = ((GenericInstanceType)resolvedTypeReference).GenericArguments[0]);
			}
			return Emit.Call(operation + GetOptimizedCastclassOrIsInstMethodSuffix(resolvedTypeReference, _sharingType), "(RuntimeObject*)" + value.Expression, _runtimeMetadataAccess.TypeInfoFor(targetType));
		}

		private static string GetOptimizedCastclassOrIsInstMethodSuffix(TypeReference resolvedTypeReference, SharingType sharingType)
		{
			if (sharingType == SharingType.NonShared && !resolvedTypeReference.IsInterface() && !resolvedTypeReference.IsArray && !resolvedTypeReference.IsNullable())
			{
				if (!resolvedTypeReference.Resolve().IsSealed)
				{
					return "Class";
				}
				return "Sealed";
			}
			return string.Empty;
		}

		private MethodReference GetCreateStringMethod(MethodReference method)
		{
			if (method.DeclaringType.Name != "String")
			{
				throw new Exception("method.DeclaringType.Name != \"String\"");
			}
			foreach (MethodDefinition item in method.DeclaringType.Resolve().Methods.Where((MethodDefinition meth) => meth.Name == "CreateString"))
			{
				if (item.Parameters.Count != method.Parameters.Count)
				{
					continue;
				}
				bool flag = false;
				for (int i = 0; i < item.Parameters.Count; i++)
				{
					if (item.Parameters[i].ParameterType.FullName != method.Parameters[i].ParameterType.FullName)
					{
						flag = true;
					}
				}
				if (!flag)
				{
					return item;
				}
			}
			throw new Exception($"Can't find proper CreateString : {method.FullName}");
		}

		private void Unbox(Instruction ins)
		{
			StackInfo boxedValue = _valueStack.Pop();
			TypeReference typeReference = (TypeReference)ins.Operand;
			TypeReference type = _typeResolver.Resolve(typeReference);
			_writer.AddIncludeForTypeDefinition(type);
			PushExpression(new ByReferenceType(type), Emit.Cast(_context, new PointerType(type), Unbox(typeReference, boxedValue)));
		}

		private string Unbox(TypeReference unresolvedType, StackInfo boxedValue)
		{
			string text = WriteExpressionAndCastIfNeeded(_context, ObjectTypeReference, boxedValue);
			TypeReference typeReference = _typeResolver.Resolve(unresolvedType);
			if (typeReference.IsNullable())
			{
				TypeReference typeReference2 = ((GenericInstanceType)typeReference).GenericArguments[0];
				if (_context.Global.Parameters.CanShareEnumTypes && _sharingType == SharingType.Shared && typeReference2.IsEnum() && unresolvedType is GenericInstanceType genericInstanceType)
				{
					typeReference2 = genericInstanceType.GenericArguments[0];
				}
				string text2 = _runtimeMetadataAccess.TypeInfoFor(typeReference2);
				string text3 = NewTempName();
				_writer.WriteLine("void* " + text3 + " = alloca(sizeof(" + _context.Global.Services.Naming.ForVariable(typeReference) + "));");
				if (_context.Global.Parameters.UsingTinyBackend)
				{
					_writer.WriteLine("UnBoxNullable<" + _context.Global.Services.Naming.ForVariable(typeReference2) + ">(" + text + ", " + text2 + ", " + text3 + ");");
				}
				else
				{
					_writer.WriteLine("UnBoxNullable(" + text + ", " + text2 + ", " + text3 + ");");
				}
				return text3;
			}
			string text4 = _runtimeMetadataAccess.TypeInfoFor(unresolvedType);
			return string.Format("UnBox(" + text + ", " + text4 + ")");
		}

		private void WriteUnsignedArithmeticOperation(string op)
		{
			StackInfo stackInfo = _valueStack.Pop();
			StackInfo stackInfo2 = _valueStack.Pop();
			TypeReference typeReference = StackTypeConverter.StackTypeForBinaryOperation(_context, stackInfo2.Type);
			TypeReference typeReference2 = StackTypeConverter.StackTypeForBinaryOperation(_context, stackInfo.Type);
			TypeReference typeReference3 = ((GetMetadataTypeOrderFor(_context, typeReference) < GetMetadataTypeOrderFor(_context, typeReference2)) ? GetUnsignedType(typeReference2) : GetUnsignedType(typeReference));
			WriteBinaryOperation(GetSignedType(typeReference3), $"({_context.Global.Services.Naming.ForVariable(typeReference3)})({_context.Global.Services.Naming.ForVariable(typeReference)})", stackInfo2.Expression, op, $"({_context.Global.Services.Naming.ForVariable(typeReference3)})({_context.Global.Services.Naming.ForVariable(typeReference2)})", stackInfo.Expression);
		}

		private TypeReference GetUnsignedType(TypeReference type)
		{
			if (type.IsSameType(SystemIntPtr) || type.IsSameType(SystemUIntPtr))
			{
				return SystemUIntPtr;
			}
			switch (type.MetadataType)
			{
			case MetadataType.SByte:
			case MetadataType.Byte:
				return ByteTypeReference;
			case MetadataType.Int16:
			case MetadataType.UInt16:
				return UInt16TypeReference;
			case MetadataType.Int32:
			case MetadataType.UInt32:
				return UInt32TypeReference;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				return UInt64TypeReference;
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
				return SystemUIntPtr;
			default:
				return type;
			}
		}

		private TypeReference GetSignedType(TypeReference type)
		{
			if (type.IsSameType(SystemIntPtr) || type.IsSameType(SystemUIntPtr))
			{
				return SystemIntPtr;
			}
			switch (type.MetadataType)
			{
			case MetadataType.SByte:
			case MetadataType.Byte:
				return SByteTypeReference;
			case MetadataType.Int16:
			case MetadataType.UInt16:
				return Int16TypeReference;
			case MetadataType.Int32:
			case MetadataType.UInt32:
				return Int32TypeReference;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				return Int64TypeReference;
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
				return SystemIntPtr;
			default:
				return type;
			}
		}

		private static int GetMetadataTypeOrderFor(ReadOnlyContext context, TypeReference type)
		{
			if (type.IsSameType(context.Global.Services.TypeProvider.SystemIntPtr) || type.IsSameType(context.Global.Services.TypeProvider.SystemUIntPtr))
			{
				return 3;
			}
			switch (type.MetadataType)
			{
			case MetadataType.SByte:
			case MetadataType.Byte:
				return 0;
			case MetadataType.Int16:
			case MetadataType.UInt16:
				return 1;
			case MetadataType.Int32:
			case MetadataType.UInt32:
				return 2;
			case MetadataType.Pointer:
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
				return 3;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				return 4;
			default:
				throw new Exception($"Invalid metadata type for typereference {type}");
			}
		}

		private void StoreField(Instruction ins)
		{
			StackInfo right = _valueStack.Pop();
			StackInfo stackInfo = _valueStack.Pop();
			FieldReference fieldReference = (FieldReference)ins.Operand;
			if (stackInfo.Expression != "__this")
			{
				_nullCheckSupport.WriteNullCheckIfNeeded(stackInfo);
			}
			EmitMemoryBarrierIfNecessary(fieldReference);
			if (fieldReference.Name == "m_value" && TypeReferenceEqualityComparer.AreEqual(fieldReference.DeclaringType, IntPtrTypeReference))
			{
				_writer.WriteLine("*{0} = ({1});", stackInfo, WriteExpressionAndCastIfNeeded(_context, SystemIntPtr, right));
			}
			else if (fieldReference.Name == "_pointer" && TypeReferenceEqualityComparer.AreEqual(fieldReference.DeclaringType, UIntPtrTypeReference))
			{
				_writer.WriteLine("*{0} = ({1});", stackInfo, WriteExpressionAndCastIfNeeded(_context, SystemUIntPtr, right));
			}
			else
			{
				_writer.WriteLine("{0}->{1}({2});", CastReferenceTypeOrNativeIntIfNeeded(stackInfo, _typeResolver.Resolve(fieldReference.DeclaringType)), _context.Global.Services.Naming.ForFieldSetter(fieldReference), WriteExpressionAndCastIfNeeded(_context, _typeResolver.ResolveFieldType(fieldReference), right));
			}
		}

		private string CastReferenceTypeOrNativeIntIfNeeded(StackInfo originalValue, TypeReference toType)
		{
			if (!toType.IsValueType())
			{
				return CastTypeIfNeeded(originalValue, toType);
			}
			if (originalValue.Type.IsIntegralPointerType())
			{
				return CastTypeIfNeeded(originalValue, new ByReferenceType(toType));
			}
			if (originalValue.Type.IsPointer)
			{
				return CastTypeIfNeeded(originalValue, new PointerType(toType));
			}
			return originalValue.Expression;
		}

		private string CastTypeIfNeeded(StackInfo originalValue, TypeReference toType)
		{
			if (!TypeReferenceEqualityComparer.AreEqual(originalValue.Type, toType))
			{
				return $"({Emit.Cast(_context, toType, originalValue.Expression)})";
			}
			return originalValue.Expression;
		}

		private string CastIfPointerType(TypeReference type)
		{
			string result = string.Empty;
			if (type.IsPointer || type.IsByReference)
			{
				result = "(" + _context.Global.Services.Naming.ForVariable(type) + ")";
			}
			return result;
		}

		private void EmitLoadToken(Instruction ins)
		{
			object operand = ins.Operand;
			if (operand is TypeReference type)
			{
				StoreLocalAndPush(RuntimeTypeHandleTypeReference, "{ reinterpret_cast<intptr_t> (" + _runtimeMetadataAccess.Il2CppTypeFor(type) + ") }");
				_writer.AddIncludeForTypeDefinition(RuntimeTypeHandleTypeReference);
				return;
			}
			if (_context.Global.Parameters.UsingTinyBackend)
			{
				throw new InvalidOperationException("Tiny profile does not support loading field and method handles. Offending method: '" + _methodReference.FullName + "' in '" + _methodDefinition.DeclaringType.Module.Name + "'.");
			}
			if (operand is FieldReference field)
			{
				StoreLocalAndPush(RuntimeFieldHandleTypeReference, "{ reinterpret_cast<intptr_t> (" + _runtimeMetadataAccess.FieldInfo(field) + ") }");
				_writer.AddIncludeForTypeDefinition(RuntimeFieldHandleTypeReference);
				return;
			}
			if (operand is MethodReference methodReference)
			{
				StoreLocalAndPush(RuntimeMethodHandleTypeReference, "{ reinterpret_cast<intptr_t> (" + _runtimeMetadataAccess.MethodInfo(methodReference) + ") }");
				_writer.AddIncludeForTypeDefinition(RuntimeMethodHandleTypeReference);
				_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(methodReference.DeclaringType));
				return;
			}
			throw new ArgumentException();
		}

		private void LoadField(Instruction ins, bool loadAddress = false)
		{
			StackInfo stackInfo = _valueStack.Pop();
			FieldReference fieldReference = (FieldReference)ins.Operand;
			TypeReference type = _typeResolver.ResolveFieldType(fieldReference);
			string right;
			if (loadAddress)
			{
				type = new ByReferenceType(type);
				right = _context.Global.Services.Naming.ForFieldAddressGetter(fieldReference);
			}
			else
			{
				if (fieldReference.Name == "m_value" && TypeReferenceEqualityComparer.AreEqual(fieldReference.DeclaringType, IntPtrTypeReference))
				{
					StoreLocalAndPush(SystemIntPtr, stackInfo.Type.IsValueType() ? stackInfo.Expression : Emit.Dereference(stackInfo.Expression));
					return;
				}
				if (fieldReference.Name == "_pointer" && TypeReferenceEqualityComparer.AreEqual(fieldReference.DeclaringType, UIntPtrTypeReference))
				{
					StoreLocalAndPush(SystemUIntPtr, stackInfo.Type.IsValueType() ? stackInfo.Expression : Emit.Dereference(stackInfo.Expression));
					return;
				}
				right = _context.Global.Services.Naming.ForFieldGetter(fieldReference);
			}
			if (stackInfo.Expression != "__this")
			{
				_nullCheckSupport.WriteNullCheckIfNeeded(stackInfo);
			}
			StackInfo local = NewTemp(type);
			_valueStack.Push(new StackInfo(local));
			string text = Emit.Call((stackInfo.Type.IsValueType() && !stackInfo.Type.IsIntegralPointerType()) ? Emit.Dot(stackInfo.Expression, right) : Emit.Arrow(CastReferenceTypeOrNativeIntIfNeeded(stackInfo, _typeResolver.Resolve(fieldReference.DeclaringType)), right));
			if (_sharingType == SharingType.Shared)
			{
				text = Emit.Cast(_context, type, text);
			}
			string text2 = Emit.Assign(local.GetIdentifierExpression(_context), text);
			_writer.WriteLine(text2 + ";");
			EmitMemoryBarrierIfNecessary(fieldReference);
		}

		private void MonoCopyValueGet(FieldReference field, StackInfo src, StackInfo dest)
		{
			if (field.FieldType.IsByReference)
			{
				_writer.WriteLine("void **p = (void**){0}", dest.Expression);
				_writer.WriteLine("*p = &{0}", src.Expression);
				return;
			}
			switch (field.FieldType.MetadataType)
			{
			case MetadataType.Boolean:
			case MetadataType.SByte:
			case MetadataType.Byte:
				_writer.WriteLine("uint8_t *p = (uint8_t*)&{0};", dest.Expression);
				_writer.WriteLine("*p = {0} ? *(uint8_t*){1} : 0;", src.Expression, src.Expression);
				break;
			case MetadataType.Char:
			case MetadataType.Int16:
			case MetadataType.UInt16:
				_writer.WriteLine("uint16_t *p = (uint16_t*)&{0};", dest.Expression);
				_writer.WriteLine("*p = {0} ? *(uint16_t*){1} : 0;", src.Expression, src.Expression);
				break;
			case MetadataType.Int32:
			case MetadataType.UInt32:
				_writer.WriteLine("uint32_t *p = (uint32_t*)&{0};", dest.Expression);
				_writer.WriteLine("*p = {0} ? *(uint32_t*){1} : 0;", src.Expression, src.Expression);
				break;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				_writer.WriteLine("uint64_t *p = (uint64_t*)&{0};", dest.Expression);
				_writer.WriteLine("*p = {0} ? *(uint64_t*){1} : 0;", src.Expression, src.Expression);
				break;
			case MetadataType.Single:
				_writer.WriteLine("float *p = (float*)&{0};", dest.Expression);
				_writer.WriteLine("*p = {0} ? *(float*){1} : 0;", src.Expression, src.Expression);
				break;
			case MetadataType.Double:
				_writer.WriteLine("double *p = (double*)&{0};", dest.Expression);
				_writer.WriteLine("*p = {0} ? *(double*){1} : 0;", src.Expression, src.Expression);
				break;
			case MetadataType.String:
			case MetadataType.Class:
			case MetadataType.Array:
			case MetadataType.Object:
				_writer.WriteLine("{0} = ({1})*(MonoObject**){2};", dest.Expression, _context.Global.Services.Naming.ForVariable(dest.Type), src.Expression);
				break;
			default:
				_writer.WriteLine("mono_copy_value({0}->type, &{1}, {2}, 1);", _runtimeMetadataAccess.FieldInfo(field), dest.Expression, src.Expression);
				break;
			}
		}

		private void MonoCopyValueSet(FieldReference field, StackInfo src, StackInfo dest)
		{
			if (field.FieldType.IsByReference)
			{
				_writer.WriteLine("void **p = (void**){0}", dest.Expression);
				_writer.WriteLine("*p = &{0}", src.Expression);
				return;
			}
			switch (field.FieldType.MetadataType)
			{
			case MetadataType.Boolean:
			case MetadataType.SByte:
			case MetadataType.Byte:
				_writer.WriteLine("uint8_t *p = (uint8_t*){0};", dest.Expression);
				_writer.WriteLine("*p = *(uint8_t*)&{1};", src.Expression, src.Expression);
				break;
			case MetadataType.Char:
			case MetadataType.Int16:
			case MetadataType.UInt16:
				_writer.WriteLine("uint16_t *p = (uint16_t*){0};", dest.Expression);
				_writer.WriteLine("*p = (uint16_t){1};", src.Expression, src.Expression);
				break;
			case MetadataType.Int32:
			case MetadataType.UInt32:
				_writer.WriteLine("uint32_t *p = (uint32_t*){0};", dest.Expression);
				_writer.WriteLine("*p = (uint32_t){1};", src.Expression, src.Expression);
				break;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				_writer.WriteLine("uint64_t *p = (uint64_t*){0};", dest.Expression);
				_writer.WriteLine("*p = (uint64_t){1};", src.Expression, src.Expression);
				break;
			case MetadataType.Single:
				_writer.WriteLine("float *p = (float*){0};", dest.Expression);
				_writer.WriteLine("*p = (float){1};", src.Expression, src.Expression);
				break;
			case MetadataType.Double:
				_writer.WriteLine("double *p = (double*){0};", dest.Expression);
				_writer.WriteLine("*p = (double){1};", src.Expression, src.Expression);
				break;
			case MetadataType.String:
			case MetadataType.Class:
			case MetadataType.Array:
			case MetadataType.Object:
				_writer.WriteLine("*(MonoObject**){0} = (MonoObject*){1};", dest.Expression, src.Expression);
				break;
			default:
				_writer.WriteLine("mono_copy_value({0}->type, {1}, &{2}, 1);", _runtimeMetadataAccess.FieldInfo(field), dest.Expression, src.Expression);
				break;
			}
		}

		private void StaticFieldAccess(Instruction ins)
		{
			FieldReference fieldReference = (FieldReference)ins.Operand;
			if (fieldReference.Resolve().IsLiteral)
			{
				throw new Exception("literal values should always be embedded rather than accessed via the field itself");
			}
			WriteCallToClassAndInitializerAndStaticConstructorIfNeeded(fieldReference.DeclaringType, _methodDefinition, _runtimeMetadataAccess);
			ThrowIfAccessIsForbidden(ins, fieldReference);
			TypeReference typeReference = _typeResolver.ResolveFieldType(fieldReference);
			string arg = TypeStaticsExpressionFor(_context, fieldReference, _typeResolver, _runtimeMetadataAccess);
			if (fieldReference.DeclaringType.IsGenericInstance)
			{
				_ = fieldReference.DeclaringType;
			}
			else
			{
				_typeResolver.Resolve(fieldReference.DeclaringType);
			}
			if (ins.OpCode.Code == Code.Stsfld)
			{
				StackInfo right = _valueStack.Pop();
				EmitMemoryBarrierIfNecessary();
				if (!(fieldReference.Name == "Zero") || (!TypeReferenceEqualityComparer.AreEqual(typeReference, IntPtrTypeReference) && !TypeReferenceEqualityComparer.AreEqual(typeReference, UIntPtrTypeReference)))
				{
					_writer.WriteLine(Statement.Expression(Emit.Call($"{arg}{_context.Global.Services.Naming.ForFieldSetter(fieldReference)}", WriteExpressionAndCastIfNeeded(_context, typeReference, right))));
				}
				return;
			}
			if (ins.OpCode.Code == Code.Ldsflda)
			{
				ByReferenceType typeReference2 = new ByReferenceType(typeReference);
				if (IsFieldAccessDirectlyToValueMetadata(fieldReference))
				{
					if (_context.Global.Parameters.UsingTinyBackend)
					{
						PushExpression(typeReference2, _context.Global.Services.Naming.ForStaticFieldsRVAStructStorage(fieldReference));
					}
					else
					{
						PushExpression(typeReference2, "il2cpp_codegen_get_field_data(" + _runtimeMetadataAccess.FieldInfo(fieldReference) + ")");
					}
				}
				else
				{
					string expression = Emit.Call($"{arg}{_context.Global.Services.Naming.ForFieldAddressGetter(fieldReference)}");
					PushExpression(typeReference2, expression);
				}
			}
			else
			{
				if (fieldReference.Name == "Zero" && TypeReferenceEqualityComparer.AreEqual(typeReference, IntPtrTypeReference))
				{
					PushExpression(SystemIntPtr, "0");
					return;
				}
				if (fieldReference.Name == "Zero" && TypeReferenceEqualityComparer.AreEqual(typeReference, UIntPtrTypeReference))
				{
					PushExpression(SystemUIntPtr, "0");
					return;
				}
				StackInfo local = NewTemp(typeReference);
				_writer.WriteLine("{0};", Emit.Assign(local.GetIdentifierExpression(_context), Emit.Call($"{arg}{_context.Global.Services.Naming.ForFieldGetter(fieldReference)}")));
				_valueStack.Push(new StackInfo(local));
			}
			EmitMemoryBarrierIfNecessary();
		}

		private static bool IsFieldAccessDirectlyToValueMetadata(FieldReference fieldReference)
		{
			FieldDefinition fieldDefinition = fieldReference.Resolve();
			if (fieldDefinition != null)
			{
				return fieldDefinition.RVA != 0;
			}
			return false;
		}

		private void ThrowIfAccessIsForbidden(Instruction ins, FieldReference fieldReference)
		{
		}

		private void WriteCallToClassAndInitializerAndStaticConstructorIfNeeded(TypeReference type, MethodDefinition invokingMethod, IRuntimeMetadataAccess runtimeMetadataAccess)
		{
			if (!_context.Global.Parameters.NoLazyStaticConstructors && type.HasStaticConstructor() && !_classesAlreadyInitializedInBlock.Contains(type))
			{
				_classesAlreadyInitializedInBlock.Add(type);
				TypeDefinition typeDefinition = type.Resolve();
				MethodDefinition methodDefinition = typeDefinition.Methods.Single(Extensions.IsStaticConstructor);
				if ((invokingMethod == null || methodDefinition != invokingMethod) && !CompilerServicesSupport.HasEagerStaticClassConstructionEnabled(typeDefinition))
				{
					_writer.WriteLine(Statement.Expression(Emit.Call("IL2CPP_RUNTIME_CLASS_INIT", runtimeMetadataAccess.StaticData(type))));
				}
			}
		}

		internal static string TypeStaticsExpressionFor(ReadOnlyContext context, FieldReference fieldReference, TypeResolver typeResolver, IRuntimeMetadataAccess runtimeMetadataAccess)
		{
			TypeReference type = typeResolver.Resolve(fieldReference.DeclaringType);
			if (!context.Global.Parameters.UsingTinyBackend)
			{
				string arg = runtimeMetadataAccess.StaticData(fieldReference.DeclaringType);
				if (fieldReference.IsThreadStatic())
				{
					return $"(({context.Global.Services.Naming.ForThreadFieldsStruct(type)}*)il2cpp_codegen_get_thread_static_data({arg}))->";
				}
				return $"(({context.Global.Services.Naming.ForStaticFieldsStruct(type)}*)il2cpp_codegen_static_fields_for({arg}))->";
			}
			if (fieldReference.IsThreadStatic())
			{
				throw new NotSupportedException("There is currently no special handling for thread static variables in Tiny");
			}
			if (fieldReference.IsNormalStatic())
			{
				return "((" + context.Global.Services.Naming.ForStaticFieldsStruct(type) + "*)" + context.Global.Services.Naming.ForStaticFieldsStructStorage(type) + ")->";
			}
			return context.Global.Services.Naming.ForTypeNameOnly(type) + "::";
		}

		private void LoadIndirect(TypeReference valueType, TypeReference storageType)
		{
			StackInfo stackInfo = _valueStack.Pop();
			StackInfo local = NewTemp(storageType);
			_writer.WriteLine("{0} = {1};", local.GetIdentifierExpression(_context), GetLoadIndirectExpression(new PointerType(valueType), stackInfo.Expression));
			if (_thisInstructionIsVolatile)
			{
				EmitMemoryBarrierIfNecessary();
			}
			_valueStack.Push(new StackInfo(local));
		}

		private void LoadIndirectReference()
		{
			StackInfo address = _valueStack.Pop();
			StoreLocalAndPush(GetPointerOrByRefType(address), GetLoadIndirectExpression(address.Type, address.Expression));
		}

		private void LoadIndirectNativeInteger()
		{
			StackInfo address = _valueStack.Pop();
			TypeReference pointerOrByRefType = GetPointerOrByRefType(address);
			if (pointerOrByRefType.IsIntegralPointerType())
			{
				PushExpression(pointerOrByRefType, $"(*({address.Expression}))");
			}
			else
			{
				PushLoadIndirectExpression(SystemIntPtr, new PointerType(SystemIntPtr), address.Expression);
			}
		}

		private void PushLoadIndirectExpression(TypeReference expressionType, TypeReference castType, string expression)
		{
			PushExpression(expressionType, GetLoadIndirectExpression(castType, expression));
		}

		private string GetLoadIndirectExpression(TypeReference castType, string expression)
		{
			return $"*(({_context.Global.Services.Naming.ForVariable(castType)}){expression})";
		}

		private TypeReference GetPointerOrByRefType(StackInfo address)
		{
			if (TypeReferenceEqualityComparer.AreEqual(_context.Global.Services.TypeProvider.SystemIntPtr, address.Type) || TypeReferenceEqualityComparer.AreEqual(_context.Global.Services.TypeProvider.SystemUIntPtr, address.Type))
			{
				return _context.Global.Services.TypeProvider.SystemVoid;
			}
			TypeReference type = address.Type;
			type = type.WithoutModifiers();
			if (type is PointerType pointerType)
			{
				return pointerType.ElementType;
			}
			if (type is ByReferenceType byReferenceType)
			{
				return byReferenceType.ElementType;
			}
			throw new Exception();
		}

		private void LoadElemAndPop(TypeReference typeReference)
		{
			StackInfo index = _valueStack.Pop();
			StackInfo array = _valueStack.Pop();
			LoadElem(array, typeReference, index);
		}

		private void StoreArg(Instruction ins)
		{
			StackInfo right = _valueStack.Pop();
			ParameterReference parameterReference = (ParameterReference)ins.Operand;
			if (parameterReference.Index == -1)
			{
				WriteAssignment(_context, "__this", _typeResolver.ResolveParameterType(_methodReference, parameterReference), right);
			}
			else
			{
				WriteAssignment(_context, _context.Global.Services.Naming.ForParameterName(parameterReference), _typeResolver.ResolveParameterType(_methodReference, parameterReference), right);
			}
		}

		private void LoadElem(StackInfo array, TypeReference objectType, StackInfo index)
		{
			_nullCheckSupport.WriteNullCheckIfNeeded(array);
			StackInfo stackInfo = NewTemp(index.Type);
			_writer.WriteLine("{0} = {1};", stackInfo.GetIdentifierExpression(_context), index.Expression);
			_arrayBoundsCheckSupport.RecordArrayBoundsCheckEmitted(_context);
			string text = Emit.LoadArrayElement(array.Expression, stackInfo.Expression, _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod());
			if (!TypeReferenceEqualityComparer.AreEqual(array.Type.GetElementType(), objectType))
			{
				text = Emit.Cast(_context, objectType, text);
			}
			StoreLocalAndPush(objectType, text);
		}

		private void StoreIndirect(TypeReference type)
		{
			StackInfo stackInfo = _valueStack.Pop();
			StackInfo stackInfo2 = _valueStack.Pop();
			EmitMemoryBarrierIfNecessary();
			string text = Emit.Cast(_context, type.MakePointerType(), stackInfo2.Expression);
			string text2 = Emit.Cast(_context, type, stackInfo.Expression);
			_writer.WriteLine("*({0}) = {1};", text, text2);
			_writer.WriteWriteBarrierIfNeeded(type, text, text2);
		}

		private void StoreElement(StackInfo array, StackInfo index, StackInfo value, bool emitElementTypeCheck)
		{
			_nullCheckSupport.WriteNullCheckIfNeeded(array);
			if (!(array.Expression == "NULL"))
			{
				if (emitElementTypeCheck)
				{
					_writer.WriteLine(Emit.ArrayElementTypeCheck(array.Expression, value.Expression));
				}
				TypeReference type = ArrayUtilities.ArrayElementTypeOf(array.Type);
				_arrayBoundsCheckSupport.RecordArrayBoundsCheckEmitted(_context);
				_writer.WriteLine("{0};", Emit.StoreArrayElement(array.Expression, index.Expression, Emit.Cast(_context, type, value.Expression), _arrayBoundsCheckSupport.ShouldEmitBoundsChecksForMethod()));
			}
		}

		private void LoadNull()
		{
			_valueStack.Push(new StackInfo("NULL", ObjectTypeReference));
		}

		private void WriteLdarg(int index, InstructionBlock block, Instruction ins)
		{
			if (_methodDefinition.HasThis)
			{
				index--;
			}
			if (index < 0)
			{
				TypeReference typeReference = _typeResolver.Resolve(_methodReference.DeclaringType);
				if (typeReference.IsValueType())
				{
					typeReference = new ByReferenceType(typeReference);
				}
				_valueStack.Push(new StackInfo("__this", typeReference));
				return;
			}
			TypeReference type = _typeResolver.ResolveParameterType(_methodReference, _methodReference.Parameters[index]).WithoutModifiers();
			StackInfo local = NewTemp(type);
			string right = _context.Global.Services.Naming.ForParameterName(_methodDefinition.Parameters[index]);
			if (!CanApplyValueTypeBoxBranchOptimizationToInstruction(ins.Next, block) && (ins.Next.OpCode.Code != Code.Ldobj || !CanApplyValueTypeBoxBranchOptimizationToInstruction(ins.Next.Next, block)))
			{
				_writer.WriteLine("{0};", Emit.Assign(local.GetIdentifierExpression(_context), right));
			}
			_valueStack.Push(new StackInfo(local));
		}

		private void WriteNumericConversion(TypeReference inputType, TypeReference outputType)
		{
			StackInfo stackInfo = _valueStack.Pop();
			string text = string.Empty;
			if ((TypeReferenceEqualityComparer.AreEqual(stackInfo.Type, SingleTypeReference) || TypeReferenceEqualityComparer.AreEqual(stackInfo.Type, DoubleTypeReference)) && inputType.IsUnsignedIntegralType())
			{
				PushExpression(outputType, $"il2cpp_codegen_cast_floating_point<{_context.Global.Services.Naming.ForVariable(inputType)}, {_context.Global.Services.Naming.ForVariable(outputType)}, {_context.Global.Services.Naming.ForVariable(stackInfo.Type)}>({stackInfo})");
				return;
			}
			if (stackInfo.Type.MetadataType == MetadataType.Pointer)
			{
				text = "(intptr_t)";
			}
			PushExpression(outputType, $"(({_context.Global.Services.Naming.ForVariable(outputType)})(({_context.Global.Services.Naming.ForVariable(inputType)}){text}{stackInfo}))");
		}

		private void WriteNumericConversion(TypeReference typeReference)
		{
			WriteNumericConversion(typeReference, typeReference);
		}

		private void WriteLdloc(int index, InstructionBlock block, Instruction ins)
		{
			VariableDefinition variable = _methodDefinition.Body.Variables[index];
			TypeReference type = _typeResolver.ResolveVariableType(_methodReference, variable);
			StackInfo local = NewTemp(type);
			_valueStack.Push(new StackInfo(local));
			if (!CanApplyValueTypeBoxBranchOptimizationToInstruction(ins.Next, block))
			{
				_writer.WriteLine("{0};", Emit.Assign(local.GetIdentifierExpression(_context), _context.Global.Services.Naming.ForVariableName(variable)));
			}
		}

		private void WriteStloc(int index)
		{
			StackInfo stackInfo = _valueStack.Pop();
			VariableDefinition variableDefinition = _methodDefinition.Body.Variables[index];
			TypeReference typeReference = variableDefinition.VariableType.WithoutModifiers();
			if (typeReference.IsPointer || typeReference.IsByReference || RequiresContravariantCastToStore(typeReference, stackInfo.Type) || RequiresIntegralCastToStore(typeReference, stackInfo.Type))
			{
				if ((typeReference.IsIntegralType() && stackInfo.Type.IsPointer) || (typeReference.IsPointer && stackInfo.Type.IsIntegralType()))
				{
					_writer.WriteLine("{0} = ({1})(intptr_t){2};", _context.Global.Services.Naming.ForVariableName(variableDefinition), _context.Global.Services.Naming.ForVariable(_typeResolver.Resolve(typeReference)), stackInfo);
				}
				else
				{
					_writer.WriteLine("{0} = ({1}){2};", _context.Global.Services.Naming.ForVariableName(variableDefinition), _context.Global.Services.Naming.ForVariable(_typeResolver.Resolve(typeReference)), stackInfo);
				}
			}
			else
			{
				WriteAssignment(_context, _context.Global.Services.Naming.ForVariableName(variableDefinition), _typeResolver.Resolve(variableDefinition.VariableType), stackInfo);
			}
		}

		private bool RequiresContravariantCastToStore(TypeReference destinationVariable, TypeReference sourceVariableType)
		{
			if (!destinationVariable.IsGenericInstance || !sourceVariableType.IsGenericInstance)
			{
				return false;
			}
			if (destinationVariable.IsValueType() || sourceVariableType.IsValueType())
			{
				return false;
			}
			if (TypeReferenceEqualityComparer.AreEqual(destinationVariable, sourceVariableType))
			{
				return false;
			}
			return true;
		}

		private int IntegralSizeInBytes(TypeReference integerType)
		{
			if (!integerType.IsIntegralType())
			{
				throw new ArgumentException("Input type must be integral type", "integerType");
			}
			switch (integerType.MetadataType)
			{
			case MetadataType.SByte:
			case MetadataType.Byte:
				return 1;
			case MetadataType.Int16:
			case MetadataType.UInt16:
				return 2;
			case MetadataType.Int32:
			case MetadataType.UInt32:
				return 4;
			case MetadataType.Int64:
			case MetadataType.UInt64:
				return 8;
			default:
				return int.MaxValue;
			}
		}

		private bool RequiresIntegralCastToStore(TypeReference destinationVariable, TypeReference sourceVariableType)
		{
			if (TypeReferenceEqualityComparer.AreEqual(destinationVariable, sourceVariableType))
			{
				return false;
			}
			if (!destinationVariable.IsIntegralType())
			{
				return false;
			}
			if (IntegralSizeInBytes(destinationVariable) >= 4)
			{
				return false;
			}
			return true;
		}

		private void GenerateConditional(string op, Signedness signedness, bool negate = false)
		{
			PushExpression(Int32TypeReference, ConditionalExpressionFor(op, signedness, negate) + "? 1 : 0");
		}

		private string ConditionalExpressionFor(string cppOperator, Signedness signedness, bool negate)
		{
			StackInfo stackInfo = _valueStack.Pop();
			StackInfo stackInfo2 = _valueStack.Pop();
			if (stackInfo.Expression == "0" && signedness == Signedness.Unsigned)
			{
				if (cppOperator == "<")
				{
					if (!negate)
					{
						return "false";
					}
					return "true";
				}
				if (cppOperator == ">=")
				{
					if (!negate)
					{
						return "true";
					}
					return "false";
				}
			}
			string text = CastExpressionForOperandOfComparision(signedness, stackInfo2);
			string text2 = CastExpressionForOperandOfComparision(signedness, stackInfo);
			if (IsNonPointerReferenceType(stackInfo) && IsNonPointerReferenceType(stackInfo2))
			{
				text2 = PrependCastToObject(text2);
				text = PrependCastToObject(text);
			}
			string text3 = $"(({text}{stackInfo2.Expression}) {cppOperator} ({text2}{stackInfo.Expression}))";
			if (!negate)
			{
				return text3;
			}
			return $"(!{text3})";
		}

		private static bool IsNonPointerReferenceType(StackInfo stackEntry)
		{
			if (!stackEntry.Type.IsValueType())
			{
				return !stackEntry.Type.IsPointer;
			}
			return false;
		}

		private string PrependCastToObject(string expression)
		{
			return $"({_context.Global.Services.Naming.ForType(_context.Global.Services.TypeProvider.SystemObject)}*){expression}";
		}

		private string CastExpressionForOperandOfComparision(Signedness signedness, StackInfo left)
		{
			return "(" + _context.Global.Services.Naming.ForVariable(TypeForComparison(signedness, left.Type)) + ")";
		}

		private TypeReference TypeForComparison(Signedness signedness, TypeReference type)
		{
			TypeReference typeReference = StackTypeConverter.StackTypeFor(_context, type);
			if (typeReference.IsSameType(_context.Global.Services.TypeProvider.SystemIntPtr))
			{
				if (signedness != 0)
				{
					return SystemUIntPtr;
				}
				return SystemIntPtr;
			}
			switch (typeReference.MetadataType)
			{
			case MetadataType.Int32:
				if (signedness != 0)
				{
					return UInt32TypeReference;
				}
				return Int32TypeReference;
			case MetadataType.Int64:
				if (signedness != 0)
				{
					return UInt64TypeReference;
				}
				return Int64TypeReference;
			case MetadataType.IntPtr:
			case MetadataType.UIntPtr:
				if (signedness != 0)
				{
					return UIntPtrTypeReference;
				}
				return IntPtrTypeReference;
			case MetadataType.Pointer:
			case MetadataType.ByReference:
				if (signedness != 0)
				{
					return SystemUIntPtr;
				}
				return SystemIntPtr;
			default:
				return type;
			}
		}

		private void GenerateConditionalJump(InstructionBlock block, Instruction ins, bool isTrue)
		{
			Instruction targetInstruction = (Instruction)ins.Operand;
			StackInfo stackInfo = _valueStack.Pop();
			if (_valueStack.Count == 0)
			{
				using (NewIfBlock(string.Format("{0}{1}", isTrue ? "" : "!", stackInfo.Expression)))
				{
					WriteJump(targetInstruction);
					return;
				}
			}
			WriteGlobalVariableAssignmentForLeftBranch(block, targetInstruction);
			using (NewIfBlock(string.Format("{0}{1}", isTrue ? "" : "!", stackInfo.Expression)))
			{
				WriteGlobalVariableAssignmentForRightBranch(block, targetInstruction);
				WriteJump(targetInstruction);
			}
		}

		private void WriteGlobalVariableAssignmentForRightBranch(InstructionBlock block, Instruction targetInstruction)
		{
			GlobalVariable[] globalVariables = _stackAnalysis.InputVariablesFor(block.Successors.Single((InstructionBlock b) => b.First.Offset == targetInstruction.Offset));
			WriteAssignGlobalVariables(globalVariables);
		}

		private void WriteGlobalVariableAssignmentForLeftBranch(InstructionBlock block, Instruction targetInstruction)
		{
			GlobalVariable[] globalVariables = _stackAnalysis.InputVariablesFor(block.Successors.Single((InstructionBlock b) => b.First.Offset != targetInstruction.Offset));
			WriteAssignGlobalVariables(globalVariables);
		}

		private void WriteGlobalVariableAssignmentForFirstSuccessor(InstructionBlock block, Instruction targetInstruction)
		{
			GlobalVariable[] globalVariables = _stackAnalysis.InputVariablesFor(block.Successors.First((InstructionBlock b) => b.First.Offset == targetInstruction.Offset));
			WriteAssignGlobalVariables(globalVariables);
		}

		private void GenerateConditionalJump(InstructionBlock block, Instruction ins, string cppOperator, Signedness signedness, bool negate = false)
		{
			string conditional = ConditionalExpressionFor(cppOperator, signedness, negate);
			Instruction targetInstruction = (Instruction)ins.Operand;
			if (_valueStack.Count == 0)
			{
				using (NewIfBlock(conditional))
				{
					WriteJump(targetInstruction);
					return;
				}
			}
			GlobalVariable[] globalVariables = _stackAnalysis.InputVariablesFor(block.Successors.Single((InstructionBlock b) => b.First.Offset != targetInstruction.Offset));
			GlobalVariable[] globalVariables2 = _stackAnalysis.InputVariablesFor(block.Successors.Single((InstructionBlock b) => b.First.Offset == targetInstruction.Offset));
			WriteAssignGlobalVariables(globalVariables);
			using (NewIfBlock(conditional))
			{
				WriteAssignGlobalVariables(globalVariables2);
				WriteJump(targetInstruction);
			}
		}

		private void WriteAssignGlobalVariables(GlobalVariable[] globalVariables)
		{
			if (globalVariables.Length != _valueStack.Count)
			{
				throw new ArgumentException("Invalid global variables count", "globalVariables");
			}
			int stackIndex = 0;
			foreach (StackInfo item in _valueStack)
			{
				GlobalVariable globalVariable = globalVariables.Single((GlobalVariable v) => v.Index == stackIndex);
				if (item.Type.FullName != globalVariable.Type.FullName)
				{
					_writer.WriteLine("{0} = (({1}){2}({3}));", globalVariable.VariableName, _context.Global.Services.Naming.ForVariable(_typeResolver.Resolve(globalVariable.Type)), (item.Type.MetadataType == MetadataType.Pointer) ? "(intptr_t)" : "", item.Expression);
				}
				else
				{
					_writer.WriteLine("{0} = {1};", globalVariable.VariableName, item.Expression);
				}
				stackIndex++;
			}
		}

		private void WriteBinaryOperation(TypeReference destType, string lcast, string left, string op, string rcast, string right)
		{
			PushExpression(destType, $"({_context.Global.Services.Naming.ForVariable(destType)})({lcast}{left}{op}{rcast}{right})");
		}

		private void WriteRemainderOperation()
		{
			StackInfo right = _valueStack.Pop();
			StackInfo left = _valueStack.Pop();
			if (right.Type.MetadataType == MetadataType.Single || left.Type.MetadataType == MetadataType.Single)
			{
				PushExpression(SingleTypeReference, $"fmodf({left.Expression}, {right.Expression})");
			}
			else if (right.Type.MetadataType == MetadataType.Double || left.Type.MetadataType == MetadataType.Double)
			{
				PushExpression(DoubleTypeReference, $"fmod({left.Expression}, {right.Expression})");
			}
			else
			{
				WriteBinaryOperation("%", left, right, left.Type);
			}
		}

		private void WriteBinaryOperationUsingLargestOperandTypeAsResultType(string op)
		{
			StackInfo right = _valueStack.Pop();
			StackInfo left = _valueStack.Pop();
			WriteBinaryOperation(op, left, right, StackAnalysisUtils.CorrectLargestTypeFor(_context, left.Type, right.Type));
		}

		private void WriteBinaryOperationUsingLeftOperandTypeAsResultType(string op)
		{
			StackInfo right = _valueStack.Pop();
			StackInfo left = _valueStack.Pop();
			WriteBinaryOperation(op, left, right, left.Type);
		}

		private void WriteBinaryOperation(string op, StackInfo left, StackInfo right, TypeReference resultType)
		{
			string rcast = CastExpressionForBinaryOperator(right);
			string lcast = CastExpressionForBinaryOperator(left);
			if (!resultType.IsPointer)
			{
				try
				{
					resultType = StackTypeConverter.StackTypeFor(_context, resultType);
				}
				catch (ArgumentException)
				{
				}
			}
			WriteBinaryOperation(resultType, lcast, left.Expression, op, rcast, right.Expression);
		}

		private string CastExpressionForBinaryOperator(StackInfo right)
		{
			if (right.Type.IsPointer)
			{
				return "(" + _context.Global.Services.Naming.ForVariable(StackTypeConverter.StackTypeForBinaryOperation(_context, right.Type)) + ")";
			}
			try
			{
				return "(" + StackTypeConverter.CppStackTypeFor(_context, right.Type) + ")";
			}
			catch (ArgumentException)
			{
				return "";
			}
		}

		private void WriteShrUn()
		{
			StackInfo stackInfo = _valueStack.Pop();
			StackInfo stackInfo2 = _valueStack.Pop();
			string lcast = "";
			TypeReference typeReference = StackTypeConverter.StackTypeFor(_context, stackInfo2.Type);
			if (typeReference.MetadataType == MetadataType.Int32)
			{
				lcast = "(uint32_t)";
			}
			if (typeReference.MetadataType == MetadataType.Int64)
			{
				lcast = "(uint64_t)";
			}
			WriteBinaryOperation(typeReference, lcast, stackInfo2.Expression, ">>", "", stackInfo.Expression);
		}

		private string NewTempName()
		{
			return "L_" + _tempIndex++;
		}

		private StackInfo NewTemp(TypeReference type)
		{
			if (type.ContainsGenericParameters())
			{
				throw new InvalidOperationException("Callers should resolve the type prior to calling this method.");
			}
			return new StackInfo(NewTempName(), type);
		}

		private void LoadPrimitiveTypeSByte(Instruction ins, TypeReference type)
		{
			PushExpression(type, Emit.Cast(_context, type, ((sbyte)ins.Operand).ToString()));
		}

		private void LoadPrimitiveTypeInt32(Instruction ins, TypeReference type)
		{
			int num = (int)ins.Operand;
			string text = num.ToString();
			long num2 = num;
			if (num2 <= int.MinValue || num2 >= int.MaxValue)
			{
				text += "LL";
			}
			PushExpression(type, Emit.Cast(_context, type, text));
		}

		private void LoadLong(Instruction ins, TypeReference type)
		{
			long num = (long)ins.Operand;
			string value = num + "LL";
			if (num == long.MinValue)
			{
				value = "(std::numeric_limits<int64_t>::min)()";
			}
			if (num == long.MaxValue)
			{
				value = "(std::numeric_limits<int64_t>::max)()";
			}
			PushExpression(type, Emit.Cast(_context, type, value));
		}

		private void LoadInt32Constant(int value)
		{
			_valueStack.Push((value < 0) ? new StackInfo($"({value})", Int32TypeReference) : new StackInfo(value.ToString(), Int32TypeReference));
		}

		private List<string> FormatArgumentsForMethodCall(List<TypeReference> parameterTypes, List<StackInfo> stackValues, SharingType sharingType)
		{
			int count = parameterTypes.Count;
			List<string> list = new List<string>();
			for (int i = 0; i < count; i++)
			{
				StringBuilder stringBuilder = new StringBuilder();
				StackInfo right = stackValues[i];
				TypeReference typeReference = parameterTypes[i];
				if (typeReference.IsPointer || typeReference.IsByReference)
				{
					stringBuilder.Append("(" + _context.Global.Services.Naming.ForVariable(typeReference) + ")");
				}
				else if (VarianceSupport.IsNeededForConversion(typeReference, right.Type))
				{
					stringBuilder.Append(VarianceSupport.Apply(_context, typeReference, right.Type));
				}
				stringBuilder.Append(WriteExpressionAndCastIfNeeded(_context, typeReference, right, sharingType));
				list.Add(stringBuilder.ToString());
			}
			return list;
		}

		private static List<TypeReference> GetParameterTypes(MethodReference method, TypeResolver typeResolverForMethodToCall)
		{
			return new List<TypeReference>(method.Parameters.Select((ParameterDefinition parameter) => typeResolverForMethodToCall.Resolve(GenericParameterResolver.ResolveParameterTypeIfNeeded(method, parameter))));
		}

		private static List<StackInfo> PopItemsFromStack(int amount, Stack<StackInfo> valueStack)
		{
			if (amount > valueStack.Count)
			{
				throw new Exception($"Attempting to pop '{amount}' values from a stack of depth '{valueStack.Count}'.");
			}
			List<StackInfo> list = new List<StackInfo>();
			for (int i = 0; i != amount; i++)
			{
				list.Add(valueStack.Pop());
			}
			list.Reverse();
			return list;
		}

		private void EmitTinyDelegateFieldSetters(IGeneratedMethodCodeWriter writer, MethodReference constructor, StackInfo delegateVariable, List<StackInfo> arguments)
		{
			MethodReference methodReference = _typeResolver.Resolve(arguments[1].MethodExpressionIsPointingTo);
			string text = null;
			if (ShouldEmitReversePInvokeWrapper(methodReference))
			{
				ReversePInvokeMethodBodyWriter.Create(writer.Context, methodReference).WriteMethodDeclaration(writer);
				text = writer.Context.Global.Services.Naming.ForReversePInvokeWrapperMethod(methodReference);
			}
			else
			{
				text = "NULL";
			}
			MethodDefinition methodDefinition = constructor.DeclaringType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "Invoke");
			string delegateOpenValue = ((!methodReference.HasThis && methodReference.Parameters.Count == methodDefinition.Parameters.Count) ? "true" : "false");
			DelegateMethodsWriter.EmitTinyDelegateExtraFieldSetters(writer, delegateVariable.Expression, text, delegateOpenValue);
		}

		private bool ShouldEmitReversePInvokeWrapper(MethodReference targetMethod)
		{
			if (ReversePInvokeMethodBodyWriter.IsReversePInvokeWrapperNecessary(_context, targetMethod))
			{
				return true;
			}
			if (_context.Global.Parameters.EmitReversePInvokeWrapperDebuggingHelpers && !targetMethod.DeclaringType.ContainsGenericParameter)
			{
				return !targetMethod.ContainsGenericParameter;
			}
			return false;
		}

		private string ParameterNameFor(MethodDefinition method, int i)
		{
			if (method == null)
			{
				throw new ArgumentNullException("method");
			}
			return _context.Global.Services.Naming.ForParameterName(method.Parameters[i]);
		}

		private IDisposable NewIfBlock(string conditional)
		{
			_writer.WriteLine("if ({0})", conditional);
			return NewBlock();
		}

		private IDisposable NewBlock()
		{
			return new BlockWriter(_writer);
		}

		private void EmitMemoryBarrierIfNecessary(FieldReference fieldReference = null)
		{
			if (_thisInstructionIsVolatile || fieldReference.IsVolatile())
			{
				_context.Global.Collectors.Stats.RecordMemoryBarrierEmitted(_methodDefinition);
				_writer.WriteStatement(Emit.MemoryBarrier());
				_thisInstructionIsVolatile = false;
			}
		}

		private void AddVolatileStackEntry()
		{
			_thisInstructionIsVolatile = true;
		}

		private void WriteLoadObject(Instruction ins, InstructionBlock block)
		{
			StackInfo stackInfo = _valueStack.Pop();
			TypeReference type = _typeResolver.Resolve((TypeReference)ins.Operand);
			PointerType variableType = new PointerType(type);
			if (CanApplyValueTypeBoxBranchOptimizationToInstruction(ins.Next, block))
			{
				_valueStack.Push(new StackInfo($"(*({_context.Global.Services.Naming.ForVariable(variableType)}){stackInfo.Expression})", type));
			}
			else
			{
				_writer.AddIncludeForTypeDefinition(type);
				StackInfo item = NewTemp(type);
				_writer.WriteStatement(Emit.Assign(item.GetIdentifierExpression(_context), $"(*({_context.Global.Services.Naming.ForVariable(variableType)}){stackInfo.Expression})"));
				_valueStack.Push(item);
			}
			EmitMemoryBarrierIfNecessary();
		}

		private void WriteStoreObject(TypeReference type)
		{
			TypeReference typeReference = _typeResolver.Resolve(type);
			StackInfo stackInfo = _valueStack.Pop();
			StackInfo stackInfo2 = _valueStack.Pop();
			EmitMemoryBarrierIfNecessary();
			string text = Emit.Cast(_context, new PointerType(typeReference), stackInfo2.Expression);
			_writer.WriteStatement(Emit.Assign(Emit.Dereference(text), stackInfo.Expression));
			_writer.WriteWriteBarrierIfNeeded(typeReference, text, stackInfo.Expression);
		}

		private void LoadVirtualFunction(Instruction ins)
		{
			MethodReference methodReference = (MethodReference)ins.Operand;
			MethodDefinition methodDefinition = methodReference.Resolve();
			StackInfo stackInfo = _valueStack.Pop();
			if (methodDefinition.IsVirtual)
			{
				PushCallToLoadVirtualFunction(methodReference, methodDefinition, stackInfo.Expression);
			}
			else
			{
				PushCallToLoadFunction(methodReference);
			}
		}

		private void PushCallToLoadFunction(MethodReference methodReference)
		{
			if (!_context.Global.Parameters.UsingTinyBackend)
			{
				_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(methodReference.DeclaringType));
				PushExpression(IntPtrTypeReference, Emit.Cast(_context, IntPtrTypeReference, _runtimeMetadataAccess.MethodInfo(methodReference)), null, methodReference);
				return;
			}
			MethodReference methodReference2 = _typeResolver.Resolve(methodReference);
			if (MethodWriter.HasAdjustorThunk(methodReference2))
			{
				string declaration = MethodWriter.WriteAdjustorThunkMethodSignature(_context, methodReference2, _typeResolver);
				_writer.AddMethodForwardDeclaration(declaration);
				StoreLocalAndPush(IntPtrTypeReference, Emit.Cast(_context, IntPtrTypeReference, _context.Global.Services.Naming.ForMethodAdjustorThunk(methodReference2)), methodReference2);
			}
			else
			{
				_writer.AddIncludeForMethodDeclaration(methodReference2);
				StoreLocalAndPush(IntPtrTypeReference, Emit.Cast(_context, IntPtrTypeReference, _runtimeMetadataAccess.Method(methodReference2)), methodReference2);
			}
		}

		private void PushCallToLoadVirtualFunction(MethodReference methodReference, MethodDefinition methodDefinition, string targetExpression)
		{
			bool flag = methodReference.DeclaringType.IsInterface();
			string value = (methodReference.IsGenericInstance ? (flag ? Emit.Call("il2cpp_codegen_get_generic_interface_method", _runtimeMetadataAccess.MethodInfo(methodReference), targetExpression) : Emit.Call("il2cpp_codegen_get_generic_virtual_method", _runtimeMetadataAccess.MethodInfo(methodReference), targetExpression)) : ((!flag) ? Emit.Call("GetVirtualMethodInfo", targetExpression, _vTableBuilder.IndexFor(_context, methodDefinition).ToString()) : Emit.Call("GetInterfaceMethodInfo", targetExpression, _vTableBuilder.IndexFor(_context, methodDefinition).ToString(), _runtimeMetadataAccess.TypeInfoFor(methodReference.DeclaringType))));
			MethodReference methodExpressionIsPointingTo = _typeResolver.Resolve(methodReference);
			if (!_context.Global.Parameters.UsingTinyBackend)
			{
				_writer.AddIncludeForTypeDefinition(_typeResolver.Resolve(methodReference.DeclaringType));
				PushExpression(IntPtrTypeReference, Emit.Cast(_context, IntPtrTypeReference, value), null, methodExpressionIsPointingTo);
			}
			else
			{
				_writer.AddIncludeForMethodDeclaration(_typeResolver.Resolve(methodReference));
				PushExpression(IntPtrTypeReference, Emit.Cast(_context, IntPtrTypeReference, value), null, methodExpressionIsPointingTo);
			}
		}
	}
}
