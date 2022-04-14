using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection
{
	internal sealed class IDictionaryProjectedMethodBodyWriter
	{
		private readonly MinimalContext _context;

		private readonly TypeDefinition _iDictionaryTypeDef;

		private readonly TypeDefinition _iMapTypeDef;

		private readonly TypeResolver _iDictionaryInstanceTypeResolver;

		private readonly TypeResolver _iMapInstanceTypeResolver;

		private readonly MethodDefinition _argumentNullExceptionConstructor;

		private readonly MethodDefinition _argumentExceptionConstructor;

		private readonly MethodDefinition _hresultGetter;

		public IDictionaryProjectedMethodBodyWriter(MinimalContext context, TypeDefinition iDictionaryTypeDef, TypeDefinition iMapTypeDef)
		{
			_context = context;
			_iDictionaryTypeDef = iDictionaryTypeDef;
			_iMapTypeDef = iMapTypeDef;
			_iDictionaryInstanceTypeResolver = TypeResolver.For(new GenericInstanceType(_iDictionaryTypeDef)
			{
				GenericArguments = 
				{
					(TypeReference)_iDictionaryTypeDef.GenericParameters[0],
					(TypeReference)_iDictionaryTypeDef.GenericParameters[1]
				}
			});
			_iMapInstanceTypeResolver = TypeResolver.For(new GenericInstanceType(_iMapTypeDef)
			{
				GenericArguments = 
				{
					(TypeReference)_iDictionaryTypeDef.GenericParameters[0],
					(TypeReference)_iDictionaryTypeDef.GenericParameters[1]
				}
			});
			TypeDefinition typeDefinition = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "ArgumentNullException");
			_argumentNullExceptionConstructor = typeDefinition.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			TypeDefinition typeDefinition2 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "ArgumentException");
			_argumentExceptionConstructor = typeDefinition2.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			PropertyDefinition propertyDefinition = context.Global.Services.TypeProvider.SystemException.Properties.Single((PropertyDefinition p) => p.Name == "HResult");
			_hresultGetter = propertyDefinition.GetMethod;
		}

		public void WriteGetKeys(MethodDefinition method)
		{
			WriteGetReadOnlyCollection(method, DictionaryCollectionTypesGenerator.CollectionKind.Key);
		}

		public void WriteGetValues(MethodDefinition method)
		{
			WriteGetReadOnlyCollection(method, DictionaryCollectionTypesGenerator.CollectionKind.Value);
		}

		public void WriteContainsKey(MethodDefinition method)
		{
			MethodDefinition method2 = _iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "HasKey");
			MethodReference method3 = _iMapInstanceTypeResolver.Resolve(method2);
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			Instruction instruction = iLProcessor.Create(OpCodes.Ldarg_0);
			WriteKeyNullCheck(iLProcessor, instruction, 0);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Callvirt, method3);
			iLProcessor.Emit(OpCodes.Ret);
		}

		public void WriteGetItem(MethodDefinition method)
		{
			MethodDefinition method2 = _iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Lookup");
			MethodReference method3 = _iMapInstanceTypeResolver.Resolve(method2);
			MethodDefinition method4 = _context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "KeyNotFoundException").Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			Instruction instruction = iLProcessor.Create(OpCodes.Ldarg_0);
			WriteKeyNullCheck(iLProcessor, instruction, 0);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Callvirt, method3);
			iLProcessor.Emit(OpCodes.Ret);
			Instruction instruction2 = iLProcessor.Create(OpCodes.Call, _hresultGetter);
			Instruction instruction3 = iLProcessor.Create(OpCodes.Ldstr, "The given key was not present in the dictionary.");
			iLProcessor.Append(instruction2);
			iLProcessor.Emit(OpCodes.Ldc_I4, -2147483637);
			iLProcessor.Emit(OpCodes.Beq, instruction3);
			iLProcessor.Emit(OpCodes.Rethrow);
			iLProcessor.Append(instruction3);
			iLProcessor.Emit(OpCodes.Newobj, method4);
			iLProcessor.Emit(OpCodes.Throw);
			ExceptionHandler exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Catch);
			exceptionHandler.CatchType = _context.Global.Services.TypeProvider.SystemException;
			exceptionHandler.TryStart = instruction;
			exceptionHandler.TryEnd = instruction2;
			exceptionHandler.HandlerStart = instruction2;
			exceptionHandler.HandlerEnd = instruction3;
			method.Body.ExceptionHandlers.Add(exceptionHandler);
		}

		public void WriteTryGetValue(MethodDefinition method)
		{
			MethodReference containsKeyMethod = GetContainsKeyMethod(method.DeclaringType);
			MethodDefinition method2 = _iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Lookup");
			MethodReference method3 = _iMapInstanceTypeResolver.Resolve(method2);
			GenericParameter type = _iDictionaryTypeDef.GenericParameters[1];
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Call, containsKeyMethod);
			Instruction instruction = iLProcessor.Create(OpCodes.Ldarg_2);
			iLProcessor.Emit(OpCodes.Brtrue, instruction);
			Instruction instruction2 = iLProcessor.Create(OpCodes.Ldarg_2);
			iLProcessor.Append(instruction2);
			iLProcessor.Emit(OpCodes.Initobj, type);
			iLProcessor.Emit(OpCodes.Ldc_I4_0);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Callvirt, method3);
			iLProcessor.Emit(OpCodes.Stobj, type);
			iLProcessor.Emit(OpCodes.Ldc_I4_1);
			iLProcessor.Emit(OpCodes.Ret);
			Instruction instruction3 = iLProcessor.Create(OpCodes.Call, _hresultGetter);
			iLProcessor.Append(instruction3);
			iLProcessor.Emit(OpCodes.Ldc_I4, -2147483637);
			iLProcessor.Emit(OpCodes.Beq, instruction2);
			iLProcessor.Emit(OpCodes.Rethrow);
			ExceptionHandler exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Catch);
			exceptionHandler.TryStart = instruction;
			exceptionHandler.TryEnd = instruction3;
			exceptionHandler.HandlerStart = instruction3;
			exceptionHandler.CatchType = _context.Global.Services.TypeProvider.SystemException;
			method.Body.ExceptionHandlers.Add(exceptionHandler);
		}

		public void WriteAdd(MethodDefinition method)
		{
			MethodReference containsKeyMethod = GetContainsKeyMethod(method.DeclaringType);
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			MethodDefinition method2 = _iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Insert");
			MethodReference method3 = _iMapInstanceTypeResolver.Resolve(method2);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Call, containsKeyMethod);
			Instruction instruction = iLProcessor.Create(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Brfalse, instruction);
			iLProcessor.Emit(OpCodes.Ldstr, "An item with the same key has already been added.");
			iLProcessor.Emit(OpCodes.Newobj, _argumentExceptionConstructor);
			iLProcessor.Emit(OpCodes.Throw);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Ldarg_2);
			iLProcessor.Emit(OpCodes.Callvirt, method3);
			iLProcessor.Emit(OpCodes.Pop);
			iLProcessor.Emit(OpCodes.Ret);
		}

		internal void WriteRemove(MethodDefinition method)
		{
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			MethodReference containsKeyMethod = GetContainsKeyMethod(method.DeclaringType);
			MethodReference method2 = _iMapInstanceTypeResolver.Resolve(_iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Remove"));
			Instruction instruction = iLProcessor.Create(OpCodes.Ldarg_0);
			Instruction instruction2 = iLProcessor.Create(OpCodes.Nop);
			Instruction instruction3 = iLProcessor.Create(OpCodes.Nop);
			Instruction instruction4 = iLProcessor.Create(OpCodes.Nop);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Call, containsKeyMethod);
			iLProcessor.Emit(OpCodes.Brtrue, instruction2);
			iLProcessor.Emit(OpCodes.Ldc_I4_0);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Append(instruction2);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Callvirt, method2);
			iLProcessor.Emit(OpCodes.Ldc_I4_1);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Append(instruction3);
			iLProcessor.Emit(OpCodes.Call, _hresultGetter);
			iLProcessor.Emit(OpCodes.Ldc_I4, -2147483637);
			iLProcessor.Emit(OpCodes.Bne_Un, instruction4);
			iLProcessor.Emit(OpCodes.Ldc_I4_0);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Append(instruction4);
			iLProcessor.Emit(OpCodes.Rethrow);
			ExceptionHandler exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Catch);
			exceptionHandler.CatchType = _context.Global.Services.TypeProvider.SystemException;
			exceptionHandler.TryStart = instruction2;
			exceptionHandler.TryEnd = instruction3;
			exceptionHandler.HandlerStart = instruction3;
			exceptionHandler.HandlerEnd = instruction4;
			method.Body.ExceptionHandlers.Add(exceptionHandler);
		}

		internal void WriteSetItem(MethodDefinition method)
		{
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			MethodReference method2 = _iMapInstanceTypeResolver.Resolve(_iMapTypeDef.Methods.Single((MethodDefinition m) => m.Name == "Insert"));
			Instruction instruction = iLProcessor.Create(OpCodes.Ldarg_0);
			WriteKeyNullCheck(iLProcessor, instruction, 0);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Ldarg_2);
			iLProcessor.Emit(OpCodes.Callvirt, method2);
			iLProcessor.Emit(OpCodes.Pop);
			iLProcessor.Emit(OpCodes.Ret);
		}

		private MethodReference GetContainsKeyMethod(TypeDefinition adapterType)
		{
			string containsKeyMethodName = _iDictionaryTypeDef.FullName + ".ContainsKey";
			MethodDefinition method = adapterType.Methods.Single((MethodDefinition m) => m.Name == containsKeyMethodName);
			return _iDictionaryInstanceTypeResolver.Resolve(method);
		}

		private void WriteGetReadOnlyCollection(MethodDefinition method, DictionaryCollectionTypesGenerator.CollectionKind collectionKind)
		{
			bool implementICollection = _iDictionaryTypeDef.FullName == "System.Collections.Generic.IDictionary`2";
			TypeDefinition typeDefinition = new DictionaryCollectionTypesGenerator(_context, _iDictionaryTypeDef, collectionKind).EmitDictionaryKeyCollection(method.DeclaringType.Module, implementICollection);
			GenericInstanceType typeReference = new GenericInstanceType(typeDefinition)
			{
				GenericArguments = 
				{
					(TypeReference)method.DeclaringType.GenericParameters[0],
					(TypeReference)method.DeclaringType.GenericParameters[1]
				}
			};
			MethodDefinition method2 = typeDefinition.Methods.Single((MethodDefinition m) => m.IsConstructor);
			MethodReference method3 = TypeResolver.For(typeReference).Resolve(method2);
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Newobj, method3);
			iLProcessor.Emit(OpCodes.Ret);
		}

		private void WriteKeyNullCheck(ILProcessor ilProcessor, Instruction labelAfterThrow, int parameterIndex)
		{
			MethodDefinition method = ilProcessor.Body.Method;
			ilProcessor.Emit(OpCodes.Ldarg, parameterIndex + (method.HasThis ? 1 : 0));
			ilProcessor.Emit(OpCodes.Box, method.Parameters[parameterIndex].ParameterType);
			ilProcessor.Emit(OpCodes.Brtrue, labelAfterThrow);
			ilProcessor.Emit(OpCodes.Ldstr, "key");
			ilProcessor.Emit(OpCodes.Newobj, _argumentNullExceptionConstructor);
			ilProcessor.Emit(OpCodes.Throw);
		}
	}
}
