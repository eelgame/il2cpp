using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection
{
	internal sealed class ICollectionProjectedMethodBodyWriter
	{
		public delegate IEnumerable<RuntimeGenericData> GetGenericSharingDataForMethodDelegate(MethodDefinition method);

		private struct MapMethodData
		{
			public readonly string MethodName;

			public readonly TypeReference ReturnType;

			public readonly GetGenericSharingDataForMethodDelegate GetGenericSharingDataForMethod;

			public readonly WriteRuntimeImplementedMethodBodyDelegate WriteMethodBodyDelegate;

			public MapMethodData(string methodName, TypeReference returnType, GetGenericSharingDataForMethodDelegate getGenericSharingDataForMethod, WriteRuntimeImplementedMethodBodyDelegate writeMethodBodyDelegate)
			{
				MethodName = methodName;
				ReturnType = returnType;
				GetGenericSharingDataForMethod = getGenericSharingDataForMethod;
				WriteMethodBodyDelegate = writeMethodBodyDelegate;
			}
		}

		private readonly TypeDefinition _iCollectionType;

		private readonly TypeDefinition _iDictionaryType;

		private readonly TypeDefinition _iMapType;

		private readonly TypeDefinition _iVectorType;

		private readonly TypeDefinition _keyValuePairType;

		private readonly PrimaryCollectionContext _context;

		public ICollectionProjectedMethodBodyWriter(PrimaryCollectionContext context, TypeDefinition iCollectionType, TypeDefinition iDictionaryType, TypeDefinition iMapType, TypeDefinition iVectorType)
		{
			_context = context;
			_iCollectionType = iCollectionType;
			_iDictionaryType = iDictionaryType;
			_iVectorType = iVectorType;
			_iMapType = iMapType;
			_keyValuePairType = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "KeyValuePair`2");
		}

		public void WriteAdd(MethodDefinition method)
		{
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			MethodDefinition vectorMethod = _iVectorType?.Methods.Single((MethodDefinition m) => m.Name == "Append");
			MapMethodData mapMethodData = new MapMethodData("AddToIMap", _context.Global.Services.TypeProvider.SystemVoid, GetICollectionGenericSharingData, WriteAddToIMapMethodBody);
			DispatchToVectorOrMapMethod(iLProcessor, null, vectorMethod, mapMethodData);
			if (method.ReturnType.MetadataType != MetadataType.Void)
			{
				MethodDefinition method2 = _iCollectionType.Methods.Single((MethodDefinition m) => m.Name == "get_Count");
				iLProcessor.Emit(OpCodes.Ldarg_0);
				iLProcessor.Emit(OpCodes.Callvirt, method2);
				iLProcessor.Emit(OpCodes.Ldc_I4_1);
				iLProcessor.Emit(OpCodes.Sub);
			}
			iLProcessor.Emit(OpCodes.Ret);
		}

		public void WriteClear(MethodDefinition method)
		{
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			MethodDefinition vectorMethod = _iVectorType?.Methods.Single((MethodDefinition m) => m.Name == "Clear");
			MapMethodData mapMethodData = new MapMethodData("ClearIMap", _context.Global.Services.TypeProvider.SystemVoid, GetICollectionGenericSharingData, WriteClearIMapMethodBody);
			DispatchToVectorOrMapMethod(iLProcessor, null, vectorMethod, mapMethodData);
			iLProcessor.Emit(OpCodes.Ret);
		}

		public void WriteContains(MethodDefinition method)
		{
			method.Body.Variables.Add(new VariableDefinition(_context.Global.Services.TypeProvider.BoolTypeReference));
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			DispatchToVectorOrMapMethod(mapMethodData: new MapMethodData("IMapContains", _context.Global.Services.TypeProvider.BoolTypeReference, GetICollectionContainsGenericSharingData, WriteIMapContainsMethodBody), ilProcessor: iLProcessor, resultVariable: method.Body.Variables[0], emitVectorCall: EmitIVectorContains);
			iLProcessor.Emit(OpCodes.Ldloc_0);
			iLProcessor.Emit(OpCodes.Ret);
		}

		private void EmitIVectorContains(ILProcessor ilProcessor, TypeReference iVectorInstance, VariableDefinition resultVariable)
		{
			MethodDefinition method = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "IndexOf");
			MethodReference method2 = TypeResolver.For(iVectorInstance).Resolve(method);
			VariableDefinition variableDefinition = new VariableDefinition(_context.Global.Services.TypeProvider.UInt32TypeReference);
			ilProcessor.Body.Variables.Add(variableDefinition);
			ilProcessor.Emit(OpCodes.Ldarg_0);
			ilProcessor.Emit(OpCodes.Ldarg_1);
			ilProcessor.Emit(OpCodes.Ldloca, variableDefinition);
			ilProcessor.Emit(OpCodes.Callvirt, method2);
			ilProcessor.Emit(OpCodes.Stloc, resultVariable);
		}

		public void WriteCopyTo(MethodDefinition method)
		{
			GenericParameter collectionElementType = (method.DeclaringType.HasGenericParameters ? method.DeclaringType.GenericParameters[0] : null);
			EmitCopyToLoop(_context, method.Body.GetILProcessor(), collectionElementType, delegate(ILProcessor ilProcessor)
			{
				ilProcessor.Emit(OpCodes.Ldarg_0);
			}, null);
		}

		public static void EmitCopyToLoop(MinimalContext context, ILProcessor ilProcessor, TypeReference collectionElementType, Action<ILProcessor> loadCollection, Action<ILProcessor> postProcessElement)
		{
			MethodDefinition method = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "ArgumentNullException").Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			MethodDefinition method2 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "ArgumentOutOfRangeException").Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			MethodDefinition method3 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "ArgumentException").Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			MethodReference method4 = null;
			GetCopyToHelperMethods(context, collectionElementType, out var iEnumeratorType, out var getEnumeratorMethod, out var moveNextMethod, out var getCurrentMethod, out var iCollectionGetCountMethod, out var iDisposableDisposeMethod);
			ParameterDefinition parameterDefinition = ilProcessor.Body.Method.Parameters[0];
			ArrayType arrayType = parameterDefinition.ParameterType as ArrayType;
			if (arrayType == null)
			{
				TypeDefinition typeDefinition = parameterDefinition.ParameterType.Resolve();
				if (typeDefinition != context.Global.Services.TypeProvider.SystemArray)
				{
					throw new InvalidProgramException("Unrecognized type of the first CopyTo method parameter: " + typeDefinition.FullName);
				}
				method4 = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "SetValue" && m.Parameters.Count == 2 && m.Parameters[1].ParameterType.MetadataType == MetadataType.Int32);
			}
			VariableDefinition variableDefinition = new VariableDefinition(iEnumeratorType);
			ilProcessor.Body.Variables.Add(variableDefinition);
			VariableDefinition variableDefinition2 = new VariableDefinition(context.Global.Services.TypeProvider.Int32TypeReference);
			ilProcessor.Body.Variables.Add(variableDefinition2);
			Instruction instruction = ilProcessor.Create(OpCodes.Nop);
			Instruction instruction2 = ilProcessor.Create(OpCodes.Nop);
			Instruction instruction3 = ilProcessor.Create(OpCodes.Nop);
			Instruction instruction4 = ilProcessor.Create(OpCodes.Nop);
			Instruction instruction5 = ilProcessor.Create(OpCodes.Nop);
			Instruction instruction6 = ilProcessor.Create(OpCodes.Nop);
			Instruction instruction7 = ilProcessor.Create(OpCodes.Nop);
			Instruction instruction8 = ilProcessor.Create(OpCodes.Ret);
			ilProcessor.Emit(OpCodes.Ldarg_1);
			ilProcessor.Emit(OpCodes.Brtrue, instruction2);
			ilProcessor.Emit(OpCodes.Ldstr, "array");
			ilProcessor.Emit(OpCodes.Newobj, method);
			ilProcessor.Emit(OpCodes.Throw);
			ilProcessor.Append(instruction2);
			ilProcessor.Emit(OpCodes.Ldarg_2);
			ilProcessor.Emit(OpCodes.Ldc_I4_0);
			ilProcessor.Emit(OpCodes.Bge, instruction);
			ilProcessor.Emit(OpCodes.Ldstr, "index");
			ilProcessor.Emit(OpCodes.Newobj, method2);
			ilProcessor.Emit(OpCodes.Throw);
			ilProcessor.Append(instruction);
			loadCollection(ilProcessor);
			ilProcessor.Emit(OpCodes.Callvirt, iCollectionGetCountMethod);
			ilProcessor.Emit(OpCodes.Stloc, variableDefinition2);
			ilProcessor.Emit(OpCodes.Ldloc, variableDefinition2);
			ilProcessor.Emit(OpCodes.Brtrue, instruction3);
			ilProcessor.Emit(OpCodes.Ret);
			ilProcessor.Append(instruction3);
			ilProcessor.Emit(OpCodes.Ldarg_2);
			ilProcessor.Emit(OpCodes.Ldarg_1);
			ilProcessor.Emit(OpCodes.Ldlen);
			ilProcessor.Emit(OpCodes.Blt, instruction4);
			ilProcessor.Emit(OpCodes.Ldstr, "The specified index is out of bounds of the specified array.");
			ilProcessor.Emit(OpCodes.Newobj, method3);
			ilProcessor.Emit(OpCodes.Throw);
			ilProcessor.Append(instruction4);
			ilProcessor.Emit(OpCodes.Ldarg_1);
			ilProcessor.Emit(OpCodes.Ldlen);
			ilProcessor.Emit(OpCodes.Ldloc, variableDefinition2);
			ilProcessor.Emit(OpCodes.Sub);
			ilProcessor.Emit(OpCodes.Ldarg_2);
			ilProcessor.Emit(OpCodes.Bge, instruction5);
			ilProcessor.Emit(OpCodes.Ldstr, "The specified space is not sufficient to copy the elements from this Collection.");
			ilProcessor.Emit(OpCodes.Newobj, method3);
			ilProcessor.Emit(OpCodes.Throw);
			ilProcessor.Append(instruction5);
			loadCollection(ilProcessor);
			ilProcessor.Emit(OpCodes.Callvirt, getEnumeratorMethod);
			ilProcessor.Emit(OpCodes.Stloc, variableDefinition);
			ilProcessor.Append(instruction6);
			ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
			ilProcessor.Emit(OpCodes.Callvirt, moveNextMethod);
			ilProcessor.Emit(OpCodes.Brfalse, instruction7);
			ilProcessor.Emit(OpCodes.Ldarg_1);
			if (arrayType == null)
			{
				ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
				ilProcessor.Emit(OpCodes.Callvirt, getCurrentMethod);
				postProcessElement?.Invoke(ilProcessor);
			}
			ilProcessor.Emit(OpCodes.Ldarg_2);
			ilProcessor.Emit(OpCodes.Dup);
			ilProcessor.Emit(OpCodes.Ldc_I4_1);
			ilProcessor.Emit(OpCodes.Add);
			ilProcessor.Emit(OpCodes.Starg, 2);
			if (arrayType != null)
			{
				ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
				ilProcessor.Emit(OpCodes.Callvirt, getCurrentMethod);
				postProcessElement?.Invoke(ilProcessor);
				ilProcessor.Emit(OpCodes.Stelem_Any, arrayType.ElementType);
			}
			else
			{
				ilProcessor.Emit(OpCodes.Call, method4);
			}
			ilProcessor.Emit(OpCodes.Br, instruction6);
			ilProcessor.Append(instruction7);
			if (iDisposableDisposeMethod != null)
			{
				ilProcessor.Emit(OpCodes.Leave, instruction8);
				Instruction instruction9 = ilProcessor.Create(OpCodes.Ldloc, variableDefinition);
				ilProcessor.Append(instruction9);
				ilProcessor.Emit(OpCodes.Callvirt, iDisposableDisposeMethod);
				ilProcessor.Emit(OpCodes.Endfinally);
				ExceptionHandler exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally);
				exceptionHandler.TryStart = instruction6;
				exceptionHandler.TryEnd = instruction9;
				exceptionHandler.HandlerStart = instruction9;
				exceptionHandler.HandlerEnd = instruction8;
				exceptionHandler.CatchType = context.Global.Services.TypeProvider.SystemException;
				ilProcessor.Body.ExceptionHandlers.Add(exceptionHandler);
			}
			ilProcessor.Append(instruction8);
		}

		private static void GetCopyToHelperMethods(MinimalContext context, TypeReference collectionElementType, out TypeReference iEnumeratorType, out MethodReference getEnumeratorMethod, out MethodReference moveNextMethod, out MethodReference getCurrentMethod, out MethodReference iCollectionGetCountMethod, out MethodReference iDisposableDisposeMethod)
		{
			TypeDefinition typeDefinition = ((collectionElementType != null) ? context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "ICollection`1") : context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections", "ICollection"));
			MethodDefinition methodDefinition = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "get_Count" && m.Parameters.Count == 0);
			TypeDefinition typeDefinition2 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections", "IEnumerator");
			moveNextMethod = typeDefinition2.Methods.Single((MethodDefinition m) => m.Name == "MoveNext" && m.Parameters.Count == 0);
			if (collectionElementType != null)
			{
				TypeDefinition typeDefinition3 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "IEnumerable`1");
				GenericInstanceType typeReference = new GenericInstanceType(typeDefinition3)
				{
					GenericArguments = { collectionElementType }
				};
				TypeDefinition typeDefinition4 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "IEnumerator`1");
				GenericInstanceType genericInstanceType = new GenericInstanceType(typeDefinition4)
				{
					GenericArguments = { collectionElementType }
				};
				iEnumeratorType = genericInstanceType;
				MethodDefinition method = typeDefinition3.Methods.Single((MethodDefinition m) => m.Name == "GetEnumerator" && m.Parameters.Count == 0);
				getEnumeratorMethod = TypeResolver.For(typeReference).Resolve(method);
				MethodDefinition method2 = typeDefinition4.Methods.Single((MethodDefinition m) => m.Name == "get_Current" && m.Parameters.Count == 0);
				getCurrentMethod = TypeResolver.For(genericInstanceType).Resolve(method2);
				GenericInstanceType genericInstanceType2 = new GenericInstanceType(typeDefinition);
				genericInstanceType2.GenericArguments.Add(collectionElementType);
				iCollectionGetCountMethod = TypeResolver.For(genericInstanceType2).Resolve(methodDefinition);
				TypeDefinition typeDefinition5 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "IDisposable");
				iDisposableDisposeMethod = typeDefinition5.Methods.Single((MethodDefinition m) => m.Name == "Dispose" && m.Parameters.Count == 0);
			}
			else
			{
				TypeDefinition typeDefinition6 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections", "IEnumerable");
				getEnumeratorMethod = typeDefinition6.Methods.Single((MethodDefinition m) => m.Name == "GetEnumerator" && m.Parameters.Count == 0);
				iEnumeratorType = typeDefinition2;
				getCurrentMethod = typeDefinition2.Methods.Single((MethodDefinition m) => m.Name == "get_Current" && m.Parameters.Count == 0);
				iCollectionGetCountMethod = methodDefinition;
				iDisposableDisposeMethod = null;
			}
		}

		public void WriteGetCount(MethodDefinition method)
		{
			MethodDefinition method2 = _context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "InvalidOperationException").Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			method.Body.Variables.Add(new VariableDefinition(_context.Global.Services.TypeProvider.Int32TypeReference));
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			Instruction instruction = iLProcessor.Create(OpCodes.Ldstr, "The backing collection is too large.");
			MethodDefinition vectorMethod = _iVectorType?.Methods.Single((MethodDefinition m) => m.Name == "get_Size");
			DispatchToVectorOrMapMethod(mapMethodData: new MapMethodData("GetIMapSize", _context.Global.Services.TypeProvider.Int32TypeReference, GetICollectionGenericSharingData, WriteGetIMapSizeMethodBody), ilProcessor: iLProcessor, resultVariable: method.Body.Variables[0], vectorMethod: vectorMethod);
			iLProcessor.Emit(OpCodes.Ldloc_0);
			iLProcessor.Emit(OpCodes.Ldc_I4, int.MaxValue);
			iLProcessor.Emit(OpCodes.Bge_Un, instruction);
			iLProcessor.Emit(OpCodes.Ldloc_0);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Newobj, method2);
			iLProcessor.Emit(OpCodes.Throw);
		}

		public void WriteGetIsReadOnly(MethodDefinition method)
		{
			WriteReturnFalse(method);
		}

		public void WriteGetIsFixedSize(MethodDefinition method)
		{
			WriteReturnFalse(method);
		}

		public void WriteGetIsSynchronized(MethodDefinition method)
		{
			WriteReturnFalse(method);
		}

		private static void WriteReturnFalse(MethodDefinition method)
		{
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldc_I4_0);
			iLProcessor.Emit(OpCodes.Ret);
		}

		public void WriteGetSyncRoot(MethodDefinition method)
		{
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ret);
		}

		public void WriteRemove(MethodDefinition method)
		{
			VariableDefinition variableDefinition = null;
			if (method.ReturnType.MetadataType != MetadataType.Void)
			{
				variableDefinition = new VariableDefinition(method.ReturnType);
				method.Body.Variables.Add(variableDefinition);
			}
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			DispatchToVectorOrMapMethod(mapMethodData: new MapMethodData("RemoveFromIMap", _context.Global.Services.TypeProvider.BoolTypeReference, GetICollectionGenericSharingData, WriteRemoveFromIMapMethodBody), ilProcessor: iLProcessor, resultVariable: variableDefinition, emitVectorCall: EmitIVectorRemove);
			if (variableDefinition != null)
			{
				iLProcessor.Emit(OpCodes.Ldloc_0);
			}
			iLProcessor.Emit(OpCodes.Ret);
		}

		private void EmitIVectorRemove(ILProcessor ilProcessor, TypeReference iVectorInstance, VariableDefinition resultVariable)
		{
			TypeResolver typeResolver = TypeResolver.For(iVectorInstance);
			MethodDefinition method = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "IndexOf");
			MethodReference method2 = typeResolver.Resolve(method);
			MethodDefinition method3 = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "RemoveAt");
			MethodReference method4 = typeResolver.Resolve(method3);
			VariableDefinition variableDefinition = new VariableDefinition(_context.Global.Services.TypeProvider.UInt32TypeReference);
			ilProcessor.Body.Variables.Add(variableDefinition);
			Instruction instruction = ilProcessor.Create(OpCodes.Nop);
			ilProcessor.Emit(OpCodes.Ldarg_0);
			ilProcessor.Emit(OpCodes.Ldarg_1);
			ilProcessor.Emit(OpCodes.Ldloca, variableDefinition);
			ilProcessor.Emit(OpCodes.Callvirt, method2);
			if (resultVariable != null)
			{
				ilProcessor.Emit(OpCodes.Dup);
				ilProcessor.Emit(OpCodes.Stloc, resultVariable);
			}
			ilProcessor.Emit(OpCodes.Brfalse, instruction);
			ilProcessor.Emit(OpCodes.Ldarg_0);
			ilProcessor.Emit(OpCodes.Ldloc, variableDefinition);
			ilProcessor.Emit(OpCodes.Callvirt, method4);
			ilProcessor.Append(instruction);
		}

		private void DispatchToVectorOrMapMethod(ILProcessor ilProcessor, VariableDefinition resultVariable, MethodDefinition vectorMethod, MapMethodData mapMethodData)
		{
			Action<ILProcessor, TypeReference, VariableDefinition> emitVectorCall = delegate(ILProcessor ilProcessorInner, TypeReference iVectorInstance, VariableDefinition resultVariableInner)
			{
				int count = ilProcessorInner.Body.Method.Parameters.Count;
				for (int i = 0; i < count + 1; i++)
				{
					ilProcessorInner.Emit(OpCodes.Ldarg, i);
				}
				ilProcessorInner.Emit(OpCodes.Callvirt, TypeResolver.For(iVectorInstance).Resolve(vectorMethod));
				if (vectorMethod.ReturnType.MetadataType != MetadataType.Void)
				{
					ilProcessor.Emit(OpCodes.Stloc, resultVariableInner);
				}
			};
			DispatchToVectorOrMapMethod(ilProcessor, resultVariable, emitVectorCall, mapMethodData);
		}

		private void DispatchToVectorOrMapMethod(ILProcessor ilProcessor, VariableDefinition resultVariable, Action<ILProcessor, TypeReference, VariableDefinition> emitVectorCall, MapMethodData mapMethodData)
		{
			MethodDefinition method = ilProcessor.Body.Method;
			TypeDefinition declaringType = method.DeclaringType;
			TypeReference typeReference = null;
			MethodReference method2 = null;
			if (_iVectorType != null)
			{
				typeReference = ((!_iVectorType.HasGenericParameters) ? ((TypeReference)_iVectorType) : ((TypeReference)new GenericInstanceType(_iVectorType)
				{
					GenericArguments = { (TypeReference)declaringType.GenericParameters[0] }
				}));
			}
			if (_iMapType != null)
			{
				MethodDefinition mapMethod = new MethodDefinition(mapMethodData.MethodName, MethodAttributes.Private, mapMethodData.ReturnType);
				mapMethod.ImplAttributes = MethodImplAttributes.CodeTypeMask;
				foreach (ParameterDefinition parameter in method.Parameters)
				{
					mapMethod.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, parameter.ParameterType));
				}
				declaringType.Methods.Add(mapMethod);
				_context.Global.Collectors.RuntimeImplementedMethodWriters.RegisterMethod(mapMethod, () => mapMethodData.GetGenericSharingDataForMethod(mapMethod), mapMethodData.WriteMethodBodyDelegate);
				GenericInstanceType genericInstanceType = new GenericInstanceType(declaringType);
				foreach (GenericParameter genericParameter in declaringType.GenericParameters)
				{
					genericInstanceType.GenericArguments.Add(genericParameter);
				}
				method2 = TypeResolver.For(genericInstanceType).Resolve(mapMethod);
			}
			if (_iVectorType != null && _iMapType != null)
			{
				Instruction instruction = ilProcessor.Create(OpCodes.Nop);
				Instruction instruction2 = ilProcessor.Create(OpCodes.Nop);
				ilProcessor.Emit(OpCodes.Ldarg_0);
				ilProcessor.Emit(OpCodes.Isinst, typeReference);
				ilProcessor.Emit(OpCodes.Brfalse, instruction);
				emitVectorCall(ilProcessor, typeReference, resultVariable);
				ilProcessor.Emit(OpCodes.Br, instruction2);
				ilProcessor.Append(instruction);
				for (int i = 0; i < method.Parameters.Count + 1; i++)
				{
					ilProcessor.Emit(OpCodes.Ldarg, i);
				}
				ilProcessor.Emit(OpCodes.Call, method2);
				if (method.ReturnType.MetadataType != MetadataType.Void)
				{
					ilProcessor.Emit(OpCodes.Stloc, resultVariable);
				}
				ilProcessor.Append(instruction2);
			}
			else if (_iVectorType != null)
			{
				emitVectorCall(ilProcessor, typeReference, resultVariable);
			}
			else
			{
				for (int j = 0; j < method.Parameters.Count + 1; j++)
				{
					ilProcessor.Emit(OpCodes.Ldarg, j);
				}
				ilProcessor.Emit(OpCodes.Call, method2);
				if (method.ReturnType.MetadataType != MetadataType.Void)
				{
					ilProcessor.Emit(OpCodes.Stloc, resultVariable);
				}
			}
		}

		private IEnumerable<RuntimeGenericData> GetICollectionGenericSharingData(MethodDefinition method)
		{
			GenericParameter genericType = method.DeclaringType.GenericParameters[0];
			return new RuntimeGenericTypeData[1]
			{
				new RuntimeGenericTypeData(RuntimeGenericContextInfo.Class, genericType)
			};
		}

		private IEnumerable<RuntimeGenericData> GetICollectionContainsGenericSharingData(MethodDefinition method)
		{
			GenericParameter genericParameter = method.DeclaringType.GenericParameters[0];
			TypeDefinition typeDefinition = _context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "EqualityComparer`1");
			TypeResolver typeResolver = TypeResolver.For(new GenericInstanceType(typeDefinition)
			{
				GenericArguments = { (TypeReference)genericParameter }
			});
			MethodReference methodReference = typeResolver.Resolve(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "get_Default" && m.Parameters.Count == 0));
			MethodReference methodReference2 = typeResolver.Resolve(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "Equals" && m.Parameters.Count == 2 && m.HasThis));
			_context.Global.Collectors.GenericMethods.Add(_context, methodReference);
			_context.Global.Collectors.GenericMethods.Add(_context, methodReference2);
			return new RuntimeGenericData[3]
			{
				new RuntimeGenericTypeData(RuntimeGenericContextInfo.Class, genericParameter),
				new RuntimeGenericMethodData(RuntimeGenericContextInfo.Method, methodReference),
				new RuntimeGenericMethodData(RuntimeGenericContextInfo.Method, methodReference2)
			};
		}

		private void WriteGetIMapSizeMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			ForwardCallToMapMethod(writer, method, metadataAccess, "get_Size", forwardKey: false, forwardValue: false);
		}

		private void WriteAddToIMapMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			ForwardCallToDictionaryMethod(writer, method, metadataAccess, "Add", forwardKey: true, forwardValue: true);
		}

		private void WriteClearIMapMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			ForwardCallToMapMethod(writer, method, metadataAccess, "Clear", forwardKey: false, forwardValue: false);
		}

		private void WriteRemoveFromIMapMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			ForwardCallToDictionaryMethod(writer, method, metadataAccess, "Remove", forwardKey: true, forwardValue: false);
		}

		private void WriteIMapContainsMethodBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			if (!ThrowExceptionIfGenericParameterIsNotKeyValuePair(writer, method))
			{
				TypeDefinition typeDefinition = method.DeclaringType.Resolve();
				GenericInstanceType genericInstanceType = (GenericInstanceType)((GenericInstanceType)method.DeclaringType).GenericArguments[0];
				TypeResolver typeResolver = TypeResolver.For(genericInstanceType);
				TypeReference typeReference = genericInstanceType.GenericArguments[0];
				TypeReference typeReference2 = genericInstanceType.GenericArguments[1];
				MethodReference method2 = typeResolver.Resolve(_keyValuePairType.Methods.Single((MethodDefinition m) => m.IsConstructor));
				TypeResolver typeResolver2 = TypeResolver.For(new GenericInstanceType(_iDictionaryType)
				{
					GenericArguments = { typeReference, typeReference2 }
				});
				TypeDefinition typeDefinition2 = writer.Context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "EqualityComparer`1");
				GenericInstanceType genericInstanceType2 = new GenericInstanceType(typeDefinition2);
				genericInstanceType2.GenericArguments.Add(genericInstanceType);
				TypeResolver typeResolver3 = TypeResolver.For(genericInstanceType2);
				MethodReference method3 = typeResolver3.Resolve(typeDefinition2.Methods.Single((MethodDefinition m) => m.Name == "get_Default" && m.Parameters.Count == 0));
				MethodReference method4 = typeResolver3.Resolve(typeDefinition2.Methods.Single((MethodDefinition m) => m.Name == "Equals" && m.Parameters.Count == 2 && m.HasThis));
				MethodDefinition method5 = _iDictionaryType.Methods.Single((MethodDefinition m) => m.Name == "TryGetValue");
				MethodReference methodReference = typeResolver2.Resolve(method5);
				List<string> list = new List<string>
				{
					new VTableBuilder().IndexFor(_context, method5) + " /* " + methodReference.FullName + " */",
					"inflatedInterface",
					"__this",
					"key",
					"&value"
				};
				DeclareAndExtractPropertyFromPair(writer, method, metadataAccess, typeResolver, typeReference, "key", "get_Key");
				string text = metadataAccess.UnresolvedTypeInfoFor(_iDictionaryType);
				writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(typeReference2) + " value;");
				writer.WriteLine("RuntimeClass* keyValuePairClass = " + metadataAccess.TypeInfoFor(typeDefinition.GenericParameters[0]) + ";");
				writer.WriteLine("RuntimeClass* inflatedInterface = InitializedTypeInfo(il2cpp_codegen_inflate_generic_class(" + text + ", il2cpp_codegen_get_generic_class_inst(keyValuePairClass)));");
				TypeReference returnType = methodReference.ReturnType;
				writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(returnType) + " tryGetValueReturnValue;");
				if (writer.Context.Global.Parameters.ReturnAsByRefParameter)
				{
					list.Add("&tryGetValueReturnValue");
				}
				else
				{
					writer.Write("tryGetValueReturnValue = ");
				}
				writer.WriteStatement(Emit.Call(writer.VirtualCallInvokeMethod(methodReference, typeResolver2), list));
				writer.WriteLine("if (!tryGetValueReturnValue)");
				using (new BlockWriter(writer))
				{
					writer.WriteManagedReturnStatement("false");
				}
				writer.WriteLine();
				writer.AddIncludeForTypeDefinition(genericInstanceType2);
				writer.AddIncludeForMethodDeclaration(method2);
				writer.AddIncludeForMethodDeclaration(method3);
				writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(genericInstanceType) + " comparisonPair;");
				writer.WriteMethodCallStatement(metadataAccess, "&comparisonPair", method, method2, MethodCallType.Normal, "key", "value");
				writer.WriteLine();
				writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(genericInstanceType2) + " comparer;");
				writer.WriteMethodCallWithResultStatement(metadataAccess, "NULL", method, method3, MethodCallType.Normal, "comparer");
				writer.WriteLine("bool result;");
				writer.WriteMethodCallWithResultStatement(metadataAccess, "comparer", method, method4, MethodCallType.Virtual, "result", writer.Context.Global.Services.Naming.ForParameterName(method.Parameters[0]), "comparisonPair");
				writer.WriteManagedReturnStatement("result");
			}
		}

		private void ForwardCallToMapMethod(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess, string mapMethodName, bool forwardKey, bool forwardValue)
		{
			ForwardKeyValuePairCallToMethod(writer, method, metadataAccess, _iMapType, mapMethodName, forwardKey, forwardValue);
		}

		private void ForwardCallToDictionaryMethod(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess, string dictionaryMethodName, bool forwardKey, bool forwardValue)
		{
			ForwardKeyValuePairCallToMethod(writer, method, metadataAccess, _iDictionaryType, dictionaryMethodName, forwardKey, forwardValue);
		}

		private void ForwardKeyValuePairCallToMethod(IGeneratedMethodCodeWriter writer, MethodReference currentMethod, IRuntimeMetadataAccess metadataAccess, TypeDefinition methodDeclaringTypeDef, string methodName, bool forwardKey, bool forwardValue)
		{
			if (ThrowExceptionIfGenericParameterIsNotKeyValuePair(writer, currentMethod))
			{
				return;
			}
			GenericInstanceType obj = (GenericInstanceType)((GenericInstanceType)currentMethod.DeclaringType).GenericArguments[0];
			TypeResolver keyValuePairResolver = TypeResolver.For(obj);
			TypeReference typeReference = obj.GenericArguments[0];
			TypeReference typeReference2 = obj.GenericArguments[1];
			GenericInstanceType typeReference3 = new GenericInstanceType(methodDeclaringTypeDef)
			{
				GenericArguments = { typeReference, typeReference2 }
			};
			MethodDefinition method = methodDeclaringTypeDef.Methods.Single((MethodDefinition m) => m.Name == methodName);
			TypeResolver typeResolver = TypeResolver.For(typeReference3);
			MethodReference methodReference = typeResolver.Resolve(method);
			List<string> list = new List<string>
			{
				new VTableBuilder().IndexFor(_context, method) + " /* " + methodReference.FullName + " */",
				"inflatedInterface",
				"__this"
			};
			string text = metadataAccess.UnresolvedTypeInfoFor(methodDeclaringTypeDef);
			writer.WriteLine("RuntimeClass* keyValuePairClass = " + metadataAccess.TypeInfoFor(currentMethod.DeclaringType.Resolve().GenericParameters[0]) + ";");
			writer.WriteLine("RuntimeClass* inflatedInterface = InitializedTypeInfo(il2cpp_codegen_inflate_generic_class(" + text + ", il2cpp_codegen_get_generic_class_inst(keyValuePairClass)));");
			if (forwardKey)
			{
				DeclareAndExtractPropertyFromPair(writer, currentMethod, metadataAccess, keyValuePairResolver, typeReference, "key", "get_Key");
				list.Add("key");
			}
			if (forwardValue)
			{
				DeclareAndExtractPropertyFromPair(writer, currentMethod, metadataAccess, keyValuePairResolver, typeReference2, "value", "get_Value");
				list.Add("value");
			}
			TypeReference typeReference4 = typeResolver.Resolve(methodReference.ReturnType);
			if (typeReference4.MetadataType != MetadataType.Void)
			{
				writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(typeReference4) + " forwardReturnValue;");
				if (!writer.Context.Global.Parameters.ReturnAsByRefParameter)
				{
					writer.Write("forwardReturnValue = ");
				}
				else
				{
					list.Add("&forwardReturnValue");
				}
			}
			writer.WriteLine(Emit.Call(writer.VirtualCallInvokeMethod(methodReference, typeResolver), list) + ";");
			if (typeReference4.MetadataType != MetadataType.Void)
			{
				writer.WriteManagedReturnStatement("forwardReturnValue");
			}
		}

		private void DeclareAndExtractPropertyFromPair(IGeneratedMethodCodeWriter writer, MethodReference currentMethod, IRuntimeMetadataAccess metadataAccess, TypeResolver keyValuePairResolver, TypeReference propertyType, string variableName, string getterName)
		{
			MethodReference method = keyValuePairResolver.Resolve(_keyValuePairType.Methods.Single((MethodDefinition m) => m.Name == getterName));
			writer.AddIncludeForMethodDeclaration(method);
			string thisVariableName = Emit.AddressOf(writer.Context.Global.Services.Naming.ForParameterName(currentMethod.Parameters[0]));
			writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(propertyType) + " " + variableName + ";");
			writer.WriteMethodCallWithResultStatement(metadataAccess, thisVariableName, currentMethod, method, MethodCallType.Normal, variableName);
		}

		private bool ThrowExceptionIfGenericParameterIsNotKeyValuePair(IGeneratedMethodCodeWriter writer, MethodReference method)
		{
			TypeReference typeReference = ((GenericInstanceType)method.DeclaringType).GenericArguments[0];
			if (_keyValuePairType == null || _keyValuePairType != typeReference.Resolve())
			{
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_invalid_cast_exception(\"\")"));
				return true;
			}
			return false;
		}
	}
}
