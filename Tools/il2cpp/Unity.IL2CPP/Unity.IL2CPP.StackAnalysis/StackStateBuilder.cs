using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.StackAnalysis
{
	public class StackStateBuilder
	{
		private readonly ReadOnlyContext _context;

		private readonly MethodDefinition _methodDefinition;

		private readonly TypeResolver _typeResolver;

		private readonly Stack<Entry> _simulationStack;

		private TypeReference Int32TypeReference => _context.Global.Services.TypeProvider.Int32TypeReference;

		private TypeReference UInt32TypeReference => _context.Global.Services.TypeProvider.UInt32TypeReference;

		private TypeReference SByteTypeReference => _context.Global.Services.TypeProvider.SByteTypeReference;

		private TypeReference IntPtrTypeReference => _context.Global.Services.TypeProvider.IntPtrTypeReference;

		private TypeReference UIntPtrTypeReference => _context.Global.Services.TypeProvider.UIntPtrTypeReference;

		private TypeReference Int64TypeReference => _context.Global.Services.TypeProvider.Int64TypeReference;

		private TypeReference SingleTypeReference => _context.Global.Services.TypeProvider.SingleTypeReference;

		private TypeReference DoubleTypeReference => _context.Global.Services.TypeProvider.DoubleTypeReference;

		private TypeReference ObjectTypeReference => _context.Global.Services.TypeProvider.ObjectTypeReference;

		private TypeReference StringTypeReference => _context.Global.Services.TypeProvider.StringTypeReference;

		private TypeReference SystemIntPtr => _context.Global.Services.TypeProvider.SystemIntPtr;

		private TypeReference SystemUIntPtr => _context.Global.Services.TypeProvider.SystemUIntPtr;

		public static StackState StackStateFor(ReadOnlyContext context, MethodDefinition method, TypeResolver typeResolver, IEnumerable<Instruction> instructions, StackState initialState)
		{
			return new StackStateBuilder(context, method, typeResolver, initialState).Build(instructions);
		}

		private StackStateBuilder(ReadOnlyContext context, MethodDefinition method, TypeResolver typeResolver, StackState initialState)
		{
			_context = context;
			_methodDefinition = method;
			_typeResolver = typeResolver;
			_simulationStack = new Stack<Entry>();
			foreach (Entry item in initialState.Entries.Reverse())
			{
				_simulationStack.Push(item.Clone());
			}
		}

		private StackState Build(IEnumerable<Instruction> instructions)
		{
			StackState stackState = new StackState();
			foreach (Instruction instruction in instructions)
			{
				SetupCatchBlockIfNeeded(instruction);
				switch (instruction.OpCode.Code)
				{
				case Code.Ldarg_0:
					LoadArg(0);
					break;
				case Code.Ldarg_1:
					LoadArg(1);
					break;
				case Code.Ldarg_2:
					LoadArg(2);
					break;
				case Code.Ldarg_3:
					LoadArg(3);
					break;
				case Code.Ldloc_0:
					LoadLocal(0);
					break;
				case Code.Ldloc_1:
					LoadLocal(1);
					break;
				case Code.Ldloc_2:
					LoadLocal(2);
					break;
				case Code.Ldloc_3:
					LoadLocal(3);
					break;
				case Code.Stloc_0:
					PopEntry();
					break;
				case Code.Stloc_1:
					PopEntry();
					break;
				case Code.Stloc_2:
					PopEntry();
					break;
				case Code.Stloc_3:
					PopEntry();
					break;
				case Code.Ldarg_S:
				{
					int num = ((ParameterReference)instruction.Operand).Index;
					if (_methodDefinition.HasThis)
					{
						num++;
					}
					LoadArg(num);
					break;
				}
				case Code.Ldarga_S:
					LoadArgumentAddress((ParameterReference)instruction.Operand);
					break;
				case Code.Starg_S:
					PopEntry();
					break;
				case Code.Ldloc_S:
					LoadLocal(((VariableReference)instruction.Operand).Index);
					break;
				case Code.Ldloca_S:
					LoadLocalAddress((VariableReference)instruction.Operand);
					break;
				case Code.Stloc_S:
					PopEntry();
					break;
				case Code.Ldnull:
					PushNullStackEntry();
					break;
				case Code.Ldc_I4_M1:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_0:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_1:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_2:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_3:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_4:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_5:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_6:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_7:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_8:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4_S:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I4:
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldc_I8:
					PushStackEntry(Int64TypeReference);
					break;
				case Code.Ldc_R4:
					PushStackEntry(SingleTypeReference);
					break;
				case Code.Ldc_R8:
					PushStackEntry(DoubleTypeReference);
					break;
				case Code.Dup:
					_simulationStack.Push(_simulationStack.Peek().Clone());
					break;
				case Code.Pop:
					PopEntry();
					break;
				case Code.Call:
					CallMethod((MethodReference)instruction.Operand);
					break;
				case Code.Calli:
					CallIndirectMethod((CallSite)instruction.Operand);
					break;
				case Code.Ret:
					if (ReturnsValue())
					{
						PopEntry();
					}
					break;
				case Code.Brfalse_S:
					PopEntry();
					break;
				case Code.Brtrue_S:
					PopEntry();
					break;
				case Code.Beq_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Bge_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Bgt_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Ble_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Blt_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Bne_Un_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Bge_Un_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Bgt_Un_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Ble_Un_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Blt_Un_S:
					PopEntry();
					PopEntry();
					break;
				case Code.Brfalse:
					PopEntry();
					break;
				case Code.Brtrue:
					PopEntry();
					break;
				case Code.Beq:
					PopEntry();
					PopEntry();
					break;
				case Code.Bge:
					PopEntry();
					PopEntry();
					break;
				case Code.Bgt:
					PopEntry();
					PopEntry();
					break;
				case Code.Ble:
					PopEntry();
					PopEntry();
					break;
				case Code.Blt:
					PopEntry();
					PopEntry();
					break;
				case Code.Bne_Un:
					PopEntry();
					PopEntry();
					break;
				case Code.Bge_Un:
					PopEntry();
					PopEntry();
					break;
				case Code.Bgt_Un:
					PopEntry();
					PopEntry();
					break;
				case Code.Ble_Un:
					PopEntry();
					PopEntry();
					break;
				case Code.Blt_Un:
					PopEntry();
					PopEntry();
					break;
				case Code.Switch:
					PopEntry();
					break;
				case Code.Ldind_I1:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldind_U1:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldind_I2:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldind_U2:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldind_I4:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldind_U4:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldind_I8:
					PopEntry();
					PushStackEntry(Int64TypeReference);
					break;
				case Code.Ldind_I:
				{
					TypeReference elementType2 = PopEntry().Types.First().GetElementType();
					if (elementType2.IsIntegralPointerType())
					{
						PushStackEntry(elementType2);
					}
					else
					{
						PushStackEntry(SystemIntPtr);
					}
					break;
				}
				case Code.Ldind_R4:
					PopEntry();
					PushStackEntry(SingleTypeReference);
					break;
				case Code.Ldind_R8:
					PopEntry();
					PushStackEntry(DoubleTypeReference);
					break;
				case Code.Ldind_Ref:
				{
					ByReferenceType byReferenceType = (ByReferenceType)PopEntry().Types.First().RemoveInModifier();
					PushStackEntry(byReferenceType.ElementType);
					break;
				}
				case Code.Stind_Ref:
					PopEntry();
					PopEntry();
					break;
				case Code.Stind_I1:
					PopEntry();
					PopEntry();
					break;
				case Code.Stind_I2:
					PopEntry();
					PopEntry();
					break;
				case Code.Stind_I4:
					PopEntry();
					PopEntry();
					break;
				case Code.Stind_I8:
					PopEntry();
					PopEntry();
					break;
				case Code.Stind_R4:
					PopEntry();
					PopEntry();
					break;
				case Code.Stind_R8:
					PopEntry();
					PopEntry();
					break;
				case Code.Add:
					_simulationStack.Push(GetResultEntryUsing(StackAnalysisUtils.ResultTypeForAdd));
					break;
				case Code.Sub:
					_simulationStack.Push(GetResultEntryUsing(StackAnalysisUtils.ResultTypeForSub));
					break;
				case Code.Mul:
					_simulationStack.Push(GetResultEntryUsing(StackAnalysisUtils.ResultTypeForMul));
					break;
				case Code.Div:
				{
					PopEntry();
					Entry entry15 = PopEntry();
					_simulationStack.Push(entry15.Clone());
					break;
				}
				case Code.Div_Un:
					PopEntry();
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Rem:
				{
					PopEntry();
					Entry entry14 = PopEntry();
					_simulationStack.Push(entry14.Clone());
					break;
				}
				case Code.Rem_Un:
					PopEntry();
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.And:
				{
					PopEntry();
					Entry entry13 = PopEntry();
					_simulationStack.Push(entry13.Clone());
					break;
				}
				case Code.Or:
				{
					PopEntry();
					Entry entry12 = PopEntry();
					_simulationStack.Push(entry12.Clone());
					break;
				}
				case Code.Xor:
				{
					PopEntry();
					Entry entry11 = PopEntry();
					_simulationStack.Push(entry11.Clone());
					break;
				}
				case Code.Shl:
				{
					PopEntry();
					Entry entry10 = PopEntry();
					_simulationStack.Push(entry10.Clone());
					break;
				}
				case Code.Shr:
				{
					PopEntry();
					Entry entry9 = PopEntry();
					_simulationStack.Push(entry9.Clone());
					break;
				}
				case Code.Shr_Un:
				{
					PopEntry();
					Entry entry8 = PopEntry();
					_simulationStack.Push(entry8.Clone());
					break;
				}
				case Code.Conv_I1:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_I2:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_I4:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_I8:
					PopEntry();
					PushStackEntry(Int64TypeReference);
					break;
				case Code.Conv_R4:
					PopEntry();
					PushStackEntry(SingleTypeReference);
					break;
				case Code.Conv_R8:
					PopEntry();
					PushStackEntry(DoubleTypeReference);
					break;
				case Code.Conv_U4:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_U8:
					PopEntry();
					PushStackEntry(Int64TypeReference);
					break;
				case Code.Callvirt:
					CallMethod((MethodReference)instruction.Operand);
					break;
				case Code.Cpobj:
					PopEntry();
					PopEntry();
					break;
				case Code.Ldobj:
					PopEntry();
					PushStackEntry(_typeResolver.Resolve((TypeReference)instruction.Operand));
					break;
				case Code.Ldstr:
					PushStackEntry(StringTypeReference);
					break;
				case Code.Newobj:
				{
					MethodReference methodReference = _typeResolver.Resolve((MethodReference)instruction.Operand);
					CallConstructor(methodReference);
					PushStackEntry(_typeResolver.Resolve(methodReference.DeclaringType));
					break;
				}
				case Code.Castclass:
					PopEntry();
					PushStackEntry(_typeResolver.Resolve((TypeReference)instruction.Operand));
					break;
				case Code.Isinst:
					PopEntry();
					PushStackEntry(_typeResolver.Resolve((TypeReference)instruction.Operand));
					break;
				case Code.Conv_R_Un:
					PopEntry();
					PushStackEntry(SingleTypeReference);
					break;
				case Code.Unbox:
					HandleStackStateForUnbox(instruction);
					break;
				case Code.Throw:
					PopEntry();
					break;
				case Code.Ldfld:
					PopEntry();
					PushStackEntry(_typeResolver.ResolveFieldType((FieldReference)instruction.Operand));
					break;
				case Code.Ldflda:
					PopEntry();
					PushStackEntry(new ByReferenceType(_typeResolver.ResolveFieldType((FieldReference)instruction.Operand)));
					break;
				case Code.Stfld:
					PopEntry();
					PopEntry();
					break;
				case Code.Ldsfld:
					PushStackEntry(_typeResolver.ResolveFieldType((FieldReference)instruction.Operand));
					break;
				case Code.Ldsflda:
					PushStackEntry(new ByReferenceType(_typeResolver.ResolveFieldType((FieldReference)instruction.Operand)));
					break;
				case Code.Stsfld:
					PopEntry();
					break;
				case Code.Stobj:
					PopEntry();
					PopEntry();
					break;
				case Code.Conv_Ovf_I1_Un:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_I2_Un:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_I4_Un:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_I8_Un:
					PopEntry();
					PushStackEntry(Int64TypeReference);
					break;
				case Code.Conv_Ovf_U1_Un:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_U2_Un:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_U4_Un:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_U8_Un:
					PopEntry();
					PushStackEntry(Int64TypeReference);
					break;
				case Code.Conv_Ovf_I_Un:
					PopEntry();
					PushStackEntry(SystemIntPtr);
					break;
				case Code.Conv_Ovf_U_Un:
					PopEntry();
					PushStackEntry(SystemUIntPtr);
					break;
				case Code.Box:
					PopEntry();
					PushStackEntry(StackEntryForBoxedType((TypeReference)instruction.Operand));
					break;
				case Code.Newarr:
					PopEntry();
					PushStackEntry(new ArrayType(_typeResolver.Resolve((TypeReference)instruction.Operand)));
					break;
				case Code.Ldlen:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldelema:
					LoadElement(new ByReferenceType(_typeResolver.Resolve((TypeReference)instruction.Operand)));
					break;
				case Code.Ldelem_I1:
					LoadElement(Int32TypeReference);
					break;
				case Code.Ldelem_U1:
					LoadElement(Int32TypeReference);
					break;
				case Code.Ldelem_I2:
					LoadElement(Int32TypeReference);
					break;
				case Code.Ldelem_U2:
					LoadElement(Int32TypeReference);
					break;
				case Code.Ldelem_I4:
					LoadElement(Int32TypeReference);
					break;
				case Code.Ldelem_U4:
					LoadElement(Int32TypeReference);
					break;
				case Code.Ldelem_I8:
					LoadElement(Int64TypeReference);
					break;
				case Code.Ldelem_I:
				{
					PopEntry();
					TypeReference elementType = PopEntry().Types.First().GetElementType();
					if (elementType.IsIntegralPointerType())
					{
						PushStackEntry(elementType);
					}
					else
					{
						PushStackEntry(Int32TypeReference);
					}
					break;
				}
				case Code.Ldelem_R4:
					LoadElement(SingleTypeReference);
					break;
				case Code.Ldelem_R8:
					LoadElement(DoubleTypeReference);
					break;
				case Code.Ldelem_Ref:
				{
					PopEntry();
					Entry entry7 = PopEntry();
					TypeReference typeReference = entry7.Types.Single();
					TypeReference typeReference2 = ((!(typeReference is ArrayType) && !(typeReference is TypeSpecification)) ? typeReference.GetElementType() : ArrayUtilities.ArrayElementTypeOf(entry7.Types.Single()));
					PushStackEntry(typeReference2);
					break;
				}
				case Code.Stelem_I:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Stelem_I1:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Stelem_I2:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Stelem_I4:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Stelem_I8:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Stelem_R4:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Stelem_R8:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Stelem_Ref:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Ldelem_Any:
					PopEntry();
					PopEntry();
					PushStackEntry(_typeResolver.Resolve((TypeReference)instruction.Operand));
					break;
				case Code.Stelem_Any:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Unbox_Any:
					HandleStackStateForUnbox(instruction);
					break;
				case Code.Conv_Ovf_I1:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_U1:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_I2:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_U2:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_I4:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_U4:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_Ovf_I8:
					PopEntry();
					PushStackEntry(Int64TypeReference);
					break;
				case Code.Conv_Ovf_U8:
					PopEntry();
					PushStackEntry(Int64TypeReference);
					break;
				case Code.Refanyval:
					throw new NotImplementedException();
				case Code.Ckfinite:
					throw new NotImplementedException();
				case Code.Mkrefany:
					PopEntry();
					PushStackEntry(_context.Global.Services.TypeProvider.TypedReference);
					break;
				case Code.Ldtoken:
					PushStackEntry(StackEntryForLdToken(instruction.Operand));
					break;
				case Code.Conv_U2:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_U1:
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Conv_I:
					PopEntry();
					PushStackEntry(SystemIntPtr);
					break;
				case Code.Conv_Ovf_I:
					PopEntry();
					PushStackEntry(SystemIntPtr);
					break;
				case Code.Conv_Ovf_U:
					PopEntry();
					PushStackEntry(SystemUIntPtr);
					break;
				case Code.Add_Ovf:
				{
					PopEntry();
					Entry entry6 = PopEntry();
					_simulationStack.Push(entry6.Clone());
					break;
				}
				case Code.Add_Ovf_Un:
				{
					PopEntry();
					Entry entry5 = PopEntry();
					_simulationStack.Push(entry5.Clone());
					break;
				}
				case Code.Mul_Ovf:
				{
					PopEntry();
					Entry entry4 = PopEntry();
					_simulationStack.Push(entry4.Clone());
					break;
				}
				case Code.Mul_Ovf_Un:
				{
					PopEntry();
					Entry entry3 = PopEntry();
					_simulationStack.Push(entry3.Clone());
					break;
				}
				case Code.Sub_Ovf:
				{
					PopEntry();
					Entry entry2 = PopEntry();
					_simulationStack.Push(entry2.Clone());
					break;
				}
				case Code.Sub_Ovf_Un:
				{
					PopEntry();
					Entry entry = PopEntry();
					_simulationStack.Push(entry.Clone());
					break;
				}
				case Code.Leave:
					EmptyStack();
					break;
				case Code.Leave_S:
					EmptyStack();
					break;
				case Code.Stind_I:
					PopEntry();
					PopEntry();
					break;
				case Code.Conv_U:
					PopEntry();
					PushStackEntry(SystemUIntPtr);
					break;
				case Code.Arglist:
					PushStackEntry(SystemIntPtr);
					break;
				case Code.Ceq:
					PopEntry();
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Cgt:
					PopEntry();
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Cgt_Un:
					PopEntry();
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Clt:
					PopEntry();
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Clt_Un:
					PopEntry();
					PopEntry();
					PushStackEntry(Int32TypeReference);
					break;
				case Code.Ldftn:
					PushStackEntry(IntPtrTypeReference);
					break;
				case Code.Ldvirtftn:
					PopEntry();
					PushStackEntry(IntPtrTypeReference);
					break;
				case Code.Ldarg:
					LoadArg(((ParameterReference)instruction.Operand).Index);
					break;
				case Code.Ldarga:
					LoadArgumentAddress((ParameterReference)instruction.Operand);
					break;
				case Code.Starg:
					PopEntry();
					break;
				case Code.Ldloc:
					LoadLocal((VariableReference)instruction.Operand);
					break;
				case Code.Ldloca:
					LoadLocalAddress((VariableReference)instruction.Operand);
					break;
				case Code.Stloc:
					PopEntry();
					break;
				case Code.Localloc:
					PopEntry();
					PushStackEntry(new PointerType(SByteTypeReference));
					break;
				case Code.Endfilter:
					PopEntry();
					break;
				case Code.Initobj:
					PopEntry();
					break;
				case Code.Cpblk:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.Initblk:
					PopEntry();
					PopEntry();
					PopEntry();
					break;
				case Code.No:
					throw new NotImplementedException();
				case Code.Sizeof:
					PushStackEntry(UInt32TypeReference);
					break;
				case Code.Refanytype:
					PopEntry();
					PushStackEntry(_context.Global.Services.TypeProvider.RuntimeTypeHandleTypeReference);
					break;
				}
			}
			foreach (Entry item in _simulationStack.Reverse())
			{
				stackState.Entries.Push(item.Clone());
			}
			return stackState;
		}

		private void HandleStackStateForUnbox(Instruction instruction)
		{
			PopEntry();
			PushStackEntry(_typeResolver.Resolve((TypeReference)instruction.Operand));
		}

		private Entry GetResultEntryUsing(StackAnalysisUtils.ResultTypeAnalysisMethod getResultType)
		{
			Entry entry = PopEntry();
			Entry entry2 = PopEntry();
			return new Entry
			{
				Types = { getResultType(_context, entry2.Types.First(), entry.Types.First()) }
			};
		}

		private TypeReference StackEntryForBoxedType(TypeReference operandType)
		{
			if (operandType == null)
			{
				return ObjectTypeReference;
			}
			if (!(operandType is GenericParameter genericParameter))
			{
				return ObjectTypeReference;
			}
			if (genericParameter.Constraints.Count == 0)
			{
				return ObjectTypeReference;
			}
			TypeReference typeReference = _typeResolver.Resolve(genericParameter);
			if (typeReference.IsValueType())
			{
				return ObjectTypeReference;
			}
			return typeReference;
		}

		private TypeReference StackEntryForLdToken(object operand)
		{
			if (operand is TypeReference)
			{
				return _context.Global.Services.TypeProvider.RuntimeTypeHandleTypeReference;
			}
			if (operand is FieldReference)
			{
				return _context.Global.Services.TypeProvider.RuntimeFieldHandleTypeReference;
			}
			if (operand is MethodReference)
			{
				return _context.Global.Services.TypeProvider.RuntimeMethodHandleTypeReference;
			}
			throw new ArgumentException();
		}

		private void LoadArgumentAddress(ParameterReference parameter)
		{
			PushStackEntry(new ByReferenceType(_typeResolver.ResolveParameterType(_methodDefinition, parameter)));
		}

		private void SetupCatchBlockIfNeeded(Instruction instruction)
		{
			MethodBody body = _methodDefinition.Body;
			if (!body.HasExceptionHandlers)
			{
				return;
			}
			foreach (ExceptionHandler exceptionHandler in body.ExceptionHandlers)
			{
				if (exceptionHandler.HandlerType == ExceptionHandlerType.Catch && exceptionHandler.HandlerStart.Offset == instruction.Offset)
				{
					PushStackEntry(_typeResolver.Resolve(exceptionHandler.CatchType));
				}
				else if (exceptionHandler.HandlerType == ExceptionHandlerType.Filter && (exceptionHandler.FilterStart.Offset == instruction.Offset || exceptionHandler.HandlerStart.Offset == instruction.Offset))
				{
					PushStackEntry(_context.Global.Services.TypeProvider.SystemException);
				}
			}
		}

		private bool ReturnsValue()
		{
			if (_methodDefinition.ReturnType != null)
			{
				return _methodDefinition.ReturnType.IsNotVoid();
			}
			return false;
		}

		private void LoadElement(TypeReference typeReference)
		{
			PopEntry();
			PopEntry();
			PushStackEntry(typeReference);
		}

		private void LoadArg(int index)
		{
			if (_methodDefinition.HasThis)
			{
				index--;
			}
			if (index < 0)
			{
				TypeDefinition declaringType = _methodDefinition.DeclaringType;
				TypeReference typeReference = (declaringType.IsValueType ? ((TypeReference)new ByReferenceType(declaringType)) : ((TypeReference)declaringType));
				PushStackEntry(typeReference);
			}
			else
			{
				PushStackEntry(_typeResolver.ResolveParameterType(_methodDefinition, _methodDefinition.Parameters[index]));
			}
		}

		private void LoadLocal(int index)
		{
			PushStackEntry(_typeResolver.Resolve(_methodDefinition.Body.Variables[index].VariableType));
		}

		private void LoadLocal(VariableReference variable)
		{
			PushStackEntry(_typeResolver.Resolve(variable.VariableType));
		}

		private void LoadLocalAddress(VariableReference variable)
		{
			PushStackEntry(new ByReferenceType(_typeResolver.Resolve(variable.VariableType)));
		}

		private void CallMethod(MethodReference methodReference)
		{
			for (int i = 0; i < methodReference.Parameters.Count; i++)
			{
				PopEntry();
			}
			if (methodReference.HasThis)
			{
				PopEntry();
			}
			TypeReference returnType = methodReference.ReturnType;
			if (returnType != null && returnType.IsNotVoid())
			{
				PushStackEntry(_typeResolver.ResolveReturnType(methodReference));
			}
		}

		private void CallMethod(CallSite callSite)
		{
			for (int i = 0; i < callSite.Parameters.Count; i++)
			{
				PopEntry();
			}
			if (callSite.HasThis)
			{
				PopEntry();
			}
			TypeReference returnType = callSite.ReturnType;
			if (returnType != null && returnType.IsNotVoid())
			{
				PushStackEntry(_typeResolver.ResolveReturnType(callSite));
			}
		}

		private void CallConstructor(MethodReference methodReference)
		{
			for (int i = 0; i < methodReference.Parameters.Count; i++)
			{
				PopEntry();
			}
			TypeReference returnType = methodReference.ReturnType;
			if (returnType != null && returnType.IsNotVoid())
			{
				PushStackEntry(returnType);
			}
		}

		private void CallIndirectMethod(CallSite callSite)
		{
			PopEntry();
			CallMethod(callSite);
		}

		private void PushNullStackEntry()
		{
			_simulationStack.Push(Entry.ForNull(ObjectTypeReference));
		}

		private void PushStackEntry(TypeReference typeReference)
		{
			if (typeReference == null)
			{
				throw new ArgumentNullException("typeReference");
			}
			TypeDefinition typeDefinition = typeReference.Resolve();
			if (typeReference.ContainsGenericParameters() && (typeDefinition == null || !typeDefinition.IsEnum))
			{
				throw new NotImplementedException();
			}
			_simulationStack.Push(Entry.For(typeReference));
		}

		private Entry PopEntry()
		{
			return _simulationStack.Pop();
		}

		private void EmptyStack()
		{
			while (_simulationStack.Count > 0)
			{
				PopEntry();
			}
		}
	}
}
