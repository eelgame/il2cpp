using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal sealed class DictionaryCollectionTypesGenerator
	{
		public enum CollectionKind
		{
			Key,
			Value
		}

		private readonly MinimalContext _context;

		private readonly TypeDefinition _iDictionaryTypeDef;

		private readonly TypeDefinition _iDisposableTypeDef;

		private readonly TypeDefinition _iEnumerableTypeDef;

		private readonly TypeDefinition _iEnumeratorTypeDef;

		private readonly TypeDefinition _iCollectionTypeDef;

		private readonly TypeDefinition _nonGenericIEnumeratorTypeDef;

		private readonly TypeDefinition _keyValuePairTypeDef;

		private readonly MethodDefinition _notSupportedExceptionCtor;

		private readonly CollectionKind _collectionKind;

		public DictionaryCollectionTypesGenerator(MinimalContext context, TypeDefinition iDictionaryTypeDef, CollectionKind collectionKind)
		{
			_context = context;
			_iDictionaryTypeDef = iDictionaryTypeDef;
			_collectionKind = collectionKind;
			_iDisposableTypeDef = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "IDisposable");
			_iEnumerableTypeDef = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "IEnumerable`1");
			_iEnumeratorTypeDef = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "IEnumerator`1");
			_iCollectionTypeDef = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "ICollection`1");
			_nonGenericIEnumeratorTypeDef = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections", "IEnumerator");
			_keyValuePairTypeDef = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "KeyValuePair`2");
			TypeDefinition typeDefinition = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "NotSupportedException");
			_notSupportedExceptionCtor = typeDefinition.Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		}

		public TypeDefinition EmitDictionaryKeyCollection(ModuleDefinition module, bool implementICollection)
		{
			TypeAttributes attributes = TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
			string name = $"{_iDictionaryTypeDef.Name.Substring(1, _iDictionaryTypeDef.Name.Length - 3)}{_collectionKind}Collection`2";
			TypeDefinition typeDefinition = new TypeDefinition("System.Runtime.InteropServices.WindowsRuntime", name, attributes, _context.Global.Services.TypeProvider.SystemObject);
			module.Types.Add(typeDefinition);
			typeDefinition.GenericParameters.Add(new GenericParameter("K", typeDefinition));
			typeDefinition.GenericParameters.Add(new GenericParameter("V", typeDefinition));
			GenericInstanceType genericInstanceType = new GenericInstanceType(_iEnumerableTypeDef);
			genericInstanceType.GenericArguments.Add(typeDefinition.GenericParameters[(int)_collectionKind]);
			InterfaceUtilities.MakeImplementInterface(typeDefinition, genericInstanceType);
			FieldDefinition field = AddDictionaryField(typeDefinition);
			TypeResolver typeResolver = TypeResolver.For(new GenericInstanceType(typeDefinition)
			{
				GenericArguments = 
				{
					(TypeReference)typeDefinition.GenericParameters[0],
					(TypeReference)typeDefinition.GenericParameters[1]
				}
			});
			FieldReference fieldReference = typeResolver.Resolve(field);
			EmitCollectionConstructor(typeDefinition, fieldReference);
			TypeDefinition enumeratorType = EmitEnumeratorType(module);
			MethodDefinition methodDefinition = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.IEnumerable`1.GetEnumerator");
			EmitIEnumerableOfTGetEnumeratorMethodBody(methodDefinition, enumeratorType, fieldReference);
			MethodDefinition getEnumeratorMethod = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.IEnumerable.GetEnumerator");
			EmitIEnumerableGetEnumeratorMethodBody(getEnumeratorMethod, typeResolver.Resolve(methodDefinition));
			if (implementICollection)
			{
				GenericInstanceType genericInstanceType2 = new GenericInstanceType(_iCollectionTypeDef);
				genericInstanceType2.GenericArguments.Add(typeDefinition.GenericParameters[(int)_collectionKind]);
				InterfaceUtilities.MakeImplementInterface(typeDefinition, genericInstanceType2);
				EmitGetCountMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.get_Count"), fieldReference);
				EmitIsReadOnlyMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.get_IsReadOnly"));
				EmitThrowInvalidMutationExceptionMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.Add"));
				EmitThrowInvalidMutationExceptionMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.Clear"));
				EmitContainsMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.Contains"), fieldReference);
				EmitCopyToMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.CopyTo"), fieldReference);
				EmitThrowInvalidMutationExceptionMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.ICollection`1.Remove"));
			}
			return typeDefinition;
		}

		private void EmitCollectionConstructor(TypeDefinition typeDefinition, FieldReference dictionaryField)
		{
			MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
			MethodDefinition methodDefinition = new MethodDefinition(".ctor", attributes, typeDefinition.Module.ImportReference(_context.Global.Services.TypeProvider.SystemVoid));
			methodDefinition.Parameters.Add(new ParameterDefinition("dictionary", ParameterAttributes.None, dictionaryField.FieldType));
			typeDefinition.Methods.Add(methodDefinition);
			ILProcessor iLProcessor = methodDefinition.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Call, GetObjectConstructor());
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Stfld, dictionaryField);
			iLProcessor.Emit(OpCodes.Ret);
			methodDefinition.Body.OptimizeMacros();
		}

		private void EmitIEnumerableOfTGetEnumeratorMethodBody(MethodDefinition getEnumeratorMethod, TypeDefinition enumeratorType, FieldReference dictionaryField)
		{
			MethodReference method = TypeResolver.For(new GenericInstanceType(enumeratorType)
			{
				GenericArguments = 
				{
					(TypeReference)getEnumeratorMethod.DeclaringType.GenericParameters[0],
					(TypeReference)getEnumeratorMethod.DeclaringType.GenericParameters[1]
				}
			}).Resolve(enumeratorType.Methods.Single((MethodDefinition m) => m.IsConstructor));
			getEnumeratorMethod.Attributes &= ~MethodAttributes.Abstract;
			ILProcessor iLProcessor = getEnumeratorMethod.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, dictionaryField);
			iLProcessor.Emit(OpCodes.Newobj, method);
			iLProcessor.Emit(OpCodes.Ret);
			getEnumeratorMethod.Body.OptimizeMacros();
		}

		private void EmitIEnumerableGetEnumeratorMethodBody(MethodDefinition getEnumeratorMethod, MethodReference iEnumeratorOfTGetEnumeratorMethod)
		{
			getEnumeratorMethod.Attributes &= ~MethodAttributes.Abstract;
			ILProcessor iLProcessor = getEnumeratorMethod.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Call, iEnumeratorOfTGetEnumeratorMethod);
			iLProcessor.Emit(OpCodes.Ret);
			getEnumeratorMethod.Body.OptimizeMacros();
		}

		private void EmitGetCountMethodBody(MethodDefinition getCountMethod, FieldReference dictionaryFieldInstance)
		{
			EmitMethodForwardingToFieldMethodBody(getCountMethod, dictionaryFieldInstance, GetDictionaryGetCountMethod(getCountMethod.DeclaringType));
		}

		private MethodReference GetDictionaryGetCountMethod(TypeDefinition adapterType)
		{
			GenericInstanceType genericInstanceType = new GenericInstanceType(_keyValuePairTypeDef);
			genericInstanceType.GenericArguments.Add(adapterType.GenericParameters[0]);
			genericInstanceType.GenericArguments.Add(adapterType.GenericParameters[1]);
			GenericInstanceType typeReference = new GenericInstanceType(_iCollectionTypeDef)
			{
				GenericArguments = { (TypeReference)genericInstanceType }
			};
			MethodDefinition method = _iCollectionTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Count" && m.Parameters.Count == 0);
			return TypeResolver.For(typeReference).Resolve(method);
		}

		private void EmitIsReadOnlyMethodBody(MethodDefinition isReadOnlyMethod)
		{
			isReadOnlyMethod.Attributes &= ~MethodAttributes.Abstract;
			ILProcessor iLProcessor = isReadOnlyMethod.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldc_I4_1);
			iLProcessor.Emit(OpCodes.Ret);
			isReadOnlyMethod.Body.OptimizeMacros();
		}

		private void EmitContainsMethodBody(MethodDefinition containsMethod, FieldReference dictionaryFieldInstance)
		{
			if (_collectionKind == CollectionKind.Key)
			{
				MethodDefinition method = _iDictionaryTypeDef.Methods.Single((MethodDefinition m) => m.Name == "ContainsKey" && m.Parameters.Count == 1);
				MethodReference methodToForwardTo = TypeResolver.For(dictionaryFieldInstance.DeclaringType).Resolve(method);
				EmitMethodForwardingToFieldMethodBody(containsMethod, dictionaryFieldInstance, methodToForwardTo);
				return;
			}
			containsMethod.Attributes &= ~MethodAttributes.Abstract;
			ILProcessor iLProcessor = containsMethod.Body.GetILProcessor();
			GenericInstanceType obj = (GenericInstanceType)dictionaryFieldInstance.DeclaringType;
			TypeReference item = obj.GenericArguments[0];
			TypeReference item2 = obj.GenericArguments[1];
			GenericInstanceType genericInstanceType = new GenericInstanceType(_keyValuePairTypeDef)
			{
				GenericArguments = { item, item2 }
			};
			GenericInstanceType typeReference = new GenericInstanceType(_iEnumerableTypeDef)
			{
				GenericArguments = { (TypeReference)genericInstanceType }
			};
			GenericInstanceType genericInstanceType2 = new GenericInstanceType(_iEnumeratorTypeDef)
			{
				GenericArguments = { (TypeReference)genericInstanceType }
			};
			TypeResolver typeResolver = TypeResolver.For(genericInstanceType2);
			TypeDefinition typeDefinition = _context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "EqualityComparer`1");
			GenericInstanceType genericInstanceType3 = new GenericInstanceType(typeDefinition)
			{
				GenericArguments = { item2 }
			};
			TypeResolver typeResolver2 = TypeResolver.For(genericInstanceType3);
			MethodDefinition method2 = _iEnumerableTypeDef.Methods.Single((MethodDefinition m) => m.Name == "GetEnumerator" && m.Parameters.Count == 0);
			MethodReference method3 = TypeResolver.For(typeReference).Resolve(method2);
			MethodDefinition method4 = _nonGenericIEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "MoveNext" && m.Parameters.Count == 0);
			MethodDefinition method5 = _iEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Current" && m.Parameters.Count == 0);
			MethodReference method6 = typeResolver.Resolve(method5);
			MethodDefinition method7 = _keyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Value" && m.Parameters.Count == 0);
			MethodReference method8 = TypeResolver.For(genericInstanceType).Resolve(method7);
			MethodDefinition method9 = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "get_Default" && m.Parameters.Count == 0);
			MethodReference method10 = typeResolver2.Resolve(method9);
			MethodDefinition method11 = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "Equals" && m.Parameters.Count == 2);
			MethodReference method12 = typeResolver2.Resolve(method11);
			MethodDefinition method13 = _iDisposableTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Dispose" && m.Parameters.Count == 0);
			containsMethod.Body.Variables.Add(new VariableDefinition(genericInstanceType2));
			containsMethod.Body.Variables.Add(new VariableDefinition(genericInstanceType3));
			containsMethod.Body.Variables.Add(new VariableDefinition(genericInstanceType));
			Instruction instruction = iLProcessor.Create(OpCodes.Nop);
			Instruction instruction2 = iLProcessor.Create(OpCodes.Nop);
			Instruction instruction3 = iLProcessor.Create(OpCodes.Nop);
			Instruction instruction4 = iLProcessor.Create(OpCodes.Nop);
			iLProcessor.Emit(OpCodes.Call, method10);
			iLProcessor.Emit(OpCodes.Stloc_1);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, dictionaryFieldInstance);
			iLProcessor.Emit(OpCodes.Callvirt, method3);
			iLProcessor.Emit(OpCodes.Stloc_0);
			iLProcessor.Append(instruction3);
			iLProcessor.Emit(OpCodes.Ldloc_0);
			iLProcessor.Emit(OpCodes.Callvirt, method4);
			iLProcessor.Emit(OpCodes.Brfalse, instruction4);
			iLProcessor.Emit(OpCodes.Ldloc_1);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Ldloc_0);
			iLProcessor.Emit(OpCodes.Callvirt, method6);
			iLProcessor.Emit(OpCodes.Stloc_2);
			iLProcessor.Emit(OpCodes.Ldloca_S, containsMethod.Body.Variables[2]);
			iLProcessor.Emit(OpCodes.Call, method8);
			iLProcessor.Emit(OpCodes.Callvirt, method12);
			iLProcessor.Emit(OpCodes.Brfalse, instruction3);
			iLProcessor.Emit(OpCodes.Leave, instruction2);
			iLProcessor.Append(instruction4);
			iLProcessor.Emit(OpCodes.Leave, instruction);
			Instruction instruction5 = iLProcessor.Create(OpCodes.Ldloc_0);
			iLProcessor.Append(instruction5);
			iLProcessor.Emit(OpCodes.Callvirt, method13);
			iLProcessor.Emit(OpCodes.Endfinally);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Ldc_I4_0);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Append(instruction2);
			iLProcessor.Emit(OpCodes.Ldc_I4_1);
			iLProcessor.Emit(OpCodes.Ret);
			ExceptionHandler exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Finally);
			exceptionHandler.TryStart = instruction3;
			exceptionHandler.TryEnd = instruction5;
			exceptionHandler.HandlerStart = instruction5;
			exceptionHandler.HandlerEnd = instruction;
			exceptionHandler.CatchType = _context.Global.Services.TypeProvider.SystemException;
			containsMethod.Body.ExceptionHandlers.Add(exceptionHandler);
			containsMethod.Body.OptimizeMacros();
		}

		private void EmitCopyToMethodBody(MethodDefinition copyToMethod, FieldReference dictionaryFieldInstance)
		{
			copyToMethod.Attributes &= ~MethodAttributes.Abstract;
			ILProcessor iLProcessor = copyToMethod.Body.GetILProcessor();
			GenericInstanceType obj = (GenericInstanceType)dictionaryFieldInstance.DeclaringType;
			TypeReference item = obj.GenericArguments[0];
			TypeReference item2 = obj.GenericArguments[1];
			GenericInstanceType genericInstanceType = new GenericInstanceType(_keyValuePairTypeDef);
			genericInstanceType.GenericArguments.Add(item);
			genericInstanceType.GenericArguments.Add(item2);
			MethodDefinition method = _keyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Key" && m.Parameters.Count == 0);
			MethodReference getKeyMethod = TypeResolver.For(genericInstanceType).Resolve(method);
			MethodDefinition method2 = _keyValuePairTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Value" && m.Parameters.Count == 0);
			MethodReference getValueMethod = TypeResolver.For(genericInstanceType).Resolve(method2);
			VariableDefinition keyValuePairLocalVariable = new VariableDefinition(genericInstanceType);
			copyToMethod.Body.Variables.Add(keyValuePairLocalVariable);
			ICollectionProjectedMethodBodyWriter.EmitCopyToLoop(_context, iLProcessor, genericInstanceType, delegate(ILProcessor processor)
			{
				processor.Emit(OpCodes.Ldarg_0);
				processor.Emit(OpCodes.Ldfld, dictionaryFieldInstance);
			}, delegate(ILProcessor processor)
			{
				processor.Emit(OpCodes.Stloc, keyValuePairLocalVariable);
				processor.Emit(OpCodes.Ldloca_S, keyValuePairLocalVariable);
				if (_collectionKind == CollectionKind.Key)
				{
					processor.Emit(OpCodes.Call, getKeyMethod);
				}
				else
				{
					processor.Emit(OpCodes.Call, getValueMethod);
				}
			});
			copyToMethod.Body.OptimizeMacros();
		}

		private void EmitThrowInvalidMutationExceptionMethodBody(MethodDefinition method)
		{
			method.Attributes &= ~MethodAttributes.Abstract;
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldstr, GetInvalidMutationExceptionMessage());
			iLProcessor.Emit(OpCodes.Newobj, _notSupportedExceptionCtor);
			iLProcessor.Emit(OpCodes.Throw);
			method.Body.OptimizeMacros();
		}

		private TypeDefinition EmitEnumeratorType(ModuleDefinition module)
		{
			TypeAttributes attributes = TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;
			string name = $"{_iDictionaryTypeDef.Name.Substring(1, _iDictionaryTypeDef.Name.Length - 3)}{_collectionKind}Enumerator`2";
			TypeDefinition typeDefinition = new TypeDefinition("System.Runtime.InteropServices.WindowsRuntime", name, attributes, _context.Global.Services.TypeProvider.SystemObject);
			module.Types.Add(typeDefinition);
			typeDefinition.GenericParameters.Add(new GenericParameter("K", typeDefinition));
			typeDefinition.GenericParameters.Add(new GenericParameter("V", typeDefinition));
			GenericInstanceType genericInstanceType = new GenericInstanceType(_iEnumeratorTypeDef);
			genericInstanceType.GenericArguments.Add(typeDefinition.GenericParameters[(int)_collectionKind]);
			InterfaceUtilities.MakeImplementInterface(typeDefinition, genericInstanceType);
			FieldDefinition field = AddDictionaryField(typeDefinition);
			FieldDefinition field2 = AddEnumeratorField(typeDefinition, _iEnumeratorTypeDef);
			TypeResolver typeResolver = TypeResolver.For(new GenericInstanceType(typeDefinition)
			{
				GenericArguments = 
				{
					(TypeReference)typeDefinition.GenericParameters[0],
					(TypeReference)typeDefinition.GenericParameters[1]
				}
			});
			FieldReference fieldReference = typeResolver.Resolve(field);
			FieldReference fieldReference2 = typeResolver.Resolve(field2);
			MethodDefinition method = _iEnumerableTypeDef.Methods.Single((MethodDefinition m) => m.Name == "GetEnumerator");
			MethodReference getEnumeratorMethod = TypeResolver.For(fieldReference2.FieldType).Resolve(method);
			EmitEnumeratorConstructor(typeDefinition, fieldReference, fieldReference2, getEnumeratorMethod);
			EmitEnumeratorDisposeBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.IDisposable.Dispose"), fieldReference2);
			EmitEnumeratorMoveNextMethodBody(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.IEnumerator.MoveNext"), fieldReference2);
			MethodDefinition methodDefinition = typeDefinition.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "System.Collections.IEnumerator.Reset");
			if (methodDefinition != null)
			{
				EmitEnumeratorResetMethodBody(methodDefinition, fieldReference, fieldReference2, getEnumeratorMethod);
			}
			MethodDefinition methodDefinition2 = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.Generic.IEnumerator`1.get_Current");
			EmitEnumeratorOfTGetCurrentMethod(methodDefinition2, fieldReference2);
			EmitEnumeratorGetCurrentMethod(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "System.Collections.IEnumerator.get_Current"), typeResolver.Resolve(methodDefinition2));
			return typeDefinition;
		}

		private void EmitEnumeratorConstructor(TypeDefinition typeDefinition, FieldReference dictionaryField, FieldReference enumeratorField, MethodReference getEnumeratorMethod)
		{
			MethodAttributes attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
			MethodDefinition methodDefinition = new MethodDefinition(".ctor", attributes, typeDefinition.Module.ImportReference(_context.Global.Services.TypeProvider.SystemVoid));
			methodDefinition.Parameters.Add(new ParameterDefinition("dictionary", ParameterAttributes.None, dictionaryField.FieldType));
			typeDefinition.Methods.Add(methodDefinition);
			ILProcessor iLProcessor = methodDefinition.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Call, GetObjectConstructor());
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Stfld, dictionaryField);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Callvirt, getEnumeratorMethod);
			iLProcessor.Emit(OpCodes.Stfld, enumeratorField);
			iLProcessor.Emit(OpCodes.Ret);
			methodDefinition.Body.OptimizeMacros();
		}

		private void EmitEnumeratorDisposeBody(MethodDefinition method, FieldReference enumeratorFieldInstance)
		{
			MethodDefinition methodToForwardTo = _iDisposableTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Dispose" && m.Parameters.Count == 0);
			EmitMethodForwardingToFieldMethodBody(method, enumeratorFieldInstance, methodToForwardTo);
		}

		private void EmitEnumeratorMoveNextMethodBody(MethodDefinition method, FieldReference enumeratorFieldInstance)
		{
			MethodDefinition methodToForwardTo = _nonGenericIEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "MoveNext" && m.Parameters.Count == 0);
			EmitMethodForwardingToFieldMethodBody(method, enumeratorFieldInstance, methodToForwardTo);
		}

		private void EmitEnumeratorResetMethodBody(MethodDefinition resetMethod, FieldReference dictionaryFieldInstance, FieldReference enumeratorFieldInstance, MethodReference getEnumeratorMethod)
		{
			resetMethod.Attributes &= ~MethodAttributes.Abstract;
			ILProcessor iLProcessor = resetMethod.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, dictionaryFieldInstance);
			iLProcessor.Emit(OpCodes.Callvirt, getEnumeratorMethod);
			iLProcessor.Emit(OpCodes.Stfld, enumeratorFieldInstance);
			iLProcessor.Emit(OpCodes.Ret);
			resetMethod.Body.OptimizeMacros();
		}

		private void EmitEnumeratorOfTGetCurrentMethod(MethodDefinition getCurrentMethod, FieldReference enumeratorFieldInstance)
		{
			MethodDefinition method = _iEnumeratorTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Current");
			MethodReference method2 = TypeResolver.For(enumeratorFieldInstance.FieldType).Resolve(method);
			GenericInstanceType genericInstanceType = (GenericInstanceType)((GenericInstanceType)enumeratorFieldInstance.FieldType).GenericArguments[0];
			string keyValuePairGetItemMethodName = ((_collectionKind == CollectionKind.Key) ? "get_Key" : "get_Value");
			MethodDefinition method3 = genericInstanceType.Resolve().Methods.Single((MethodDefinition m) => m.HasThis && m.Name == keyValuePairGetItemMethodName && m.Parameters.Count == 0);
			MethodReference method4 = TypeResolver.For(genericInstanceType).Resolve(method3);
			getCurrentMethod.Attributes &= ~MethodAttributes.Abstract;
			MethodBody body = getCurrentMethod.Body;
			ILProcessor iLProcessor = body.GetILProcessor();
			body.Variables.Add(new VariableDefinition(genericInstanceType));
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, enumeratorFieldInstance);
			iLProcessor.Emit(OpCodes.Callvirt, method2);
			iLProcessor.Emit(OpCodes.Stloc_0);
			iLProcessor.Emit(OpCodes.Ldloca_S, body.Variables[0]);
			iLProcessor.Emit(OpCodes.Call, method4);
			iLProcessor.Emit(OpCodes.Ret);
			getCurrentMethod.Body.OptimizeMacros();
		}

		private void EmitEnumeratorGetCurrentMethod(MethodDefinition getCurrentMethod, MethodReference iEnumeratorOfTCurrentMethod)
		{
			getCurrentMethod.Attributes &= ~MethodAttributes.Abstract;
			ILProcessor iLProcessor = getCurrentMethod.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Call, iEnumeratorOfTCurrentMethod);
			iLProcessor.Emit(OpCodes.Box, getCurrentMethod.DeclaringType.GenericParameters[(int)_collectionKind]);
			iLProcessor.Emit(OpCodes.Ret);
			getCurrentMethod.Body.OptimizeMacros();
		}

		private void EmitMethodForwardingToFieldMethodBody(MethodDefinition method, FieldReference fieldInstance, MethodReference methodToForwardTo)
		{
			method.Attributes &= ~MethodAttributes.Abstract;
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, fieldInstance);
			for (int i = 0; i < method.Parameters.Count; i++)
			{
				iLProcessor.Emit(OpCodes.Ldarg, i + 1);
			}
			iLProcessor.Emit(OpCodes.Callvirt, methodToForwardTo);
			iLProcessor.Emit(OpCodes.Ret);
			method.Body.OptimizeMacros();
		}

		private FieldDefinition AddDictionaryField(TypeDefinition typeDefinition)
		{
			GenericInstanceType genericInstanceType = new GenericInstanceType(_iDictionaryTypeDef);
			genericInstanceType.GenericArguments.Add(typeDefinition.GenericParameters[0]);
			genericInstanceType.GenericArguments.Add(typeDefinition.GenericParameters[1]);
			FieldDefinition fieldDefinition = new FieldDefinition("dictionary", FieldAttributes.Private | FieldAttributes.InitOnly, genericInstanceType);
			typeDefinition.Fields.Add(fieldDefinition);
			return fieldDefinition;
		}

		private FieldDefinition AddEnumeratorField(TypeDefinition typeDefinition, TypeDefinition iEnumeratorTypeDef)
		{
			GenericInstanceType genericInstanceType = new GenericInstanceType(_keyValuePairTypeDef);
			genericInstanceType.GenericArguments.Add(typeDefinition.GenericParameters[0]);
			genericInstanceType.GenericArguments.Add(typeDefinition.GenericParameters[1]);
			GenericInstanceType genericInstanceType2 = new GenericInstanceType(iEnumeratorTypeDef);
			genericInstanceType2.GenericArguments.Add(genericInstanceType);
			FieldDefinition fieldDefinition = new FieldDefinition("enumerator", FieldAttributes.Private, genericInstanceType2);
			typeDefinition.Fields.Add(fieldDefinition);
			return fieldDefinition;
		}

		private MethodDefinition GetObjectConstructor()
		{
			return _context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.IsConstructor && m.HasThis && m.Parameters.Count == 0);
		}

		private string GetInvalidMutationExceptionMessage()
		{
			if (_collectionKind != 0)
			{
				return "Mutating a value collection derived from a dictionary is not allowed.";
			}
			return "Mutating a key collection derived from a dictionary is not allowed.";
		}
	}
}
