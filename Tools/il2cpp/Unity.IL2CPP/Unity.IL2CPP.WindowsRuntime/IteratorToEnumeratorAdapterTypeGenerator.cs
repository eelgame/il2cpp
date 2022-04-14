using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal sealed class IteratorToEnumeratorAdapterTypeGenerator
	{
		private const TypeAttributes AdapterTypeAttributes = TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit;

		private readonly MinimalContext _context;

		private readonly ModuleDefinition _module;

		private readonly TypeDefinition _adapterType;

		private readonly TypeReference _iteratorType;

		private readonly TypeReference _ienumeratorType;

		private readonly TypeResolver _typeResolver;

		private readonly FieldDefinition _iteratorField;

		private readonly FieldDefinition _initializedField;

		private readonly FieldDefinition _hadCurrentField;

		private readonly FieldDefinition _currentField;

		private readonly MethodReference _getCurrentMethod;

		private readonly MethodReference _getHasCurrentMethod;

		private readonly MethodReference _moveNextMethod;

		private readonly MethodReference _invalidOperationExceptionConstructor;

		public IteratorToEnumeratorAdapterTypeGenerator(MinimalContext context, ModuleDefinition module, TypeDefinition iteratorType, TypeDefinition ienumeratorType)
		{
			_context = context;
			_module = module;
			TypeReference fieldType = _module.ImportReference(context.Global.Services.TypeProvider.ObjectTypeReference);
			_iteratorType = module.ImportReference(iteratorType);
			_ienumeratorType = module.ImportReference(ienumeratorType);
			string name = context.Global.Services.Naming.ForWindowsRuntimeAdapterTypeName(iteratorType, ienumeratorType);
			_adapterType = new TypeDefinition("System.Runtime.InteropServices.WindowsRuntime", name, TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, context.Global.Services.TypeProvider.ObjectTypeReference);
			if (ienumeratorType.HasGenericParameters)
			{
				GenericInstanceType genericInstanceType = new GenericInstanceType(_iteratorType);
				GenericInstanceType genericInstanceType2 = new GenericInstanceType(_ienumeratorType);
				foreach (GenericParameter genericParameter in ienumeratorType.GenericParameters)
				{
					GenericParameter item = new GenericParameter(genericParameter.Name, _adapterType);
					_adapterType.GenericParameters.Add(item);
					genericInstanceType.GenericArguments.Add(item);
					genericInstanceType2.GenericArguments.Add(item);
				}
				_iteratorType = _module.ImportReference(genericInstanceType, _adapterType);
				_ienumeratorType = _module.ImportReference(genericInstanceType2, _adapterType);
				fieldType = _adapterType.GenericParameters[0];
			}
			_iteratorField = new FieldDefinition("iterator", FieldAttributes.Private, _iteratorType);
			_initializedField = new FieldDefinition("initialized", FieldAttributes.Private, _module.ImportReference(context.Global.Services.TypeProvider.BoolTypeReference));
			_hadCurrentField = new FieldDefinition("hadCurrent", FieldAttributes.Private, _module.ImportReference(context.Global.Services.TypeProvider.BoolTypeReference));
			_currentField = new FieldDefinition("current", FieldAttributes.Private, fieldType);
			_typeResolver = TypeResolver.For(_iteratorType);
			_getCurrentMethod = _typeResolver.Resolve(iteratorType.Methods.First((MethodDefinition m) => m.Name == "get_Current"));
			_getHasCurrentMethod = _typeResolver.Resolve(iteratorType.Methods.First((MethodDefinition m) => m.Name == "get_HasCurrent"));
			_moveNextMethod = _typeResolver.Resolve(iteratorType.Methods.First((MethodDefinition m) => m.Name == "MoveNext"));
			TypeDefinition type = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System", "InvalidOperationException");
			_invalidOperationExceptionConstructor = type.Methods.First((MethodDefinition m) => m.IsConstructor && !m.IsStatic && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
		}

		public TypeDefinition Generate()
		{
			_module.Types.Add(_adapterType);
			InterfaceUtilities.MakeImplementInterface(_adapterType, _ienumeratorType);
			_adapterType.Fields.Add(_iteratorField);
			_adapterType.Fields.Add(_initializedField);
			_adapterType.Fields.Add(_hadCurrentField);
			_adapterType.Fields.Add(_currentField);
			foreach (MethodDefinition method in _adapterType.Methods)
			{
				method.Attributes &= ~MethodAttributes.Abstract;
				switch (method.Name)
				{
				case "System.Collections.IEnumerator.MoveNext":
					WriteMethodMoveNext(method);
					continue;
				case "System.Collections.IEnumerator.get_Current":
				case "System.Collections.Generic.IEnumerator`1.get_Current":
					WriteMethodGetCurrent(method);
					continue;
				case "System.Collections.IEnumerator.Reset":
					WriteMethodReset(method);
					continue;
				case "System.IDisposable.Dispose":
					WriteDisposeMethod(method);
					continue;
				}
				throw new NotSupportedException("Interface '" + _ienumeratorType.FullName + "' contains unsupported method '" + method.Name + "'.");
			}
			MethodDefinition methodDefinition = new MethodDefinition(".ctor", MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, _module.ImportReference(_context.Global.Services.TypeProvider.SystemVoid));
			_adapterType.Methods.Add(methodDefinition);
			methodDefinition.Parameters.Add(new ParameterDefinition("iterator", ParameterAttributes.None, _iteratorType));
			WriteConstructor(methodDefinition);
			return _adapterType;
		}

		private void WriteConstructor(MethodDefinition method)
		{
			MethodBody body = method.Body;
			ILProcessor iLProcessor = body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			MethodDefinition method2 = _context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.IsConstructor && !m.IsStatic && !m.HasParameters);
			iLProcessor.Emit(OpCodes.Call, _module.ImportReference(method2));
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Stfld, _typeResolver.Resolve(_iteratorField));
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldc_I4_1);
			iLProcessor.Emit(OpCodes.Stfld, _hadCurrentField);
			iLProcessor.Emit(OpCodes.Ret);
			body.OptimizeMacros();
		}

		private void WriteMethodMoveNext(MethodDefinition method)
		{
			MethodBody body = method.Body;
			ILProcessor iLProcessor = body.GetILProcessor();
			Instruction target = Instruction.Create(OpCodes.Nop);
			ExceptionHandler exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Catch)
			{
				CatchType = _module.ImportReference(_context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System", "Exception"))
			};
			method.Body.ExceptionHandlers.Add(exceptionHandler);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, _hadCurrentField);
			iLProcessor.Emit(OpCodes.Brtrue_S, target);
			Instruction instruction = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldc_I4_0);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			Instruction instruction3 = (Instruction)(instruction.Operand = (exceptionHandler.TryStart = body.Instructions.Last()));
			iLProcessor.Emit(OpCodes.Ldfld, _initializedField);
			iLProcessor.Emit(OpCodes.Brtrue_S, target);
			Instruction instruction4 = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, _typeResolver.Resolve(_iteratorField));
			iLProcessor.Emit(OpCodes.Callvirt, _module.ImportReference(_getHasCurrentMethod, _adapterType));
			iLProcessor.Emit(OpCodes.Stfld, _hadCurrentField);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldc_I4_1);
			iLProcessor.Emit(OpCodes.Stfld, _initializedField);
			iLProcessor.Emit(OpCodes.Br_S, target);
			Instruction instruction5 = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			instruction4.Operand = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, _typeResolver.Resolve(_iteratorField));
			iLProcessor.Emit(OpCodes.Callvirt, _module.ImportReference(_moveNextMethod, _adapterType));
			iLProcessor.Emit(OpCodes.Stfld, _hadCurrentField);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			instruction5.Operand = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldfld, _hadCurrentField);
			iLProcessor.Emit(OpCodes.Brfalse_S, target);
			Instruction instruction6 = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, _typeResolver.Resolve(_iteratorField));
			iLProcessor.Emit(OpCodes.Callvirt, _module.ImportReference(_getCurrentMethod, _adapterType));
			iLProcessor.Emit(OpCodes.Stfld, _typeResolver.Resolve(_currentField));
			iLProcessor.Emit(OpCodes.Leave_S, target);
			Instruction instruction8 = (Instruction)(instruction6.Operand = body.Instructions.Last());
			MethodDefinition method2 = _context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System.Runtime.InteropServices", "Marshal").Methods.Single((MethodDefinition m) => m.Name == "GetHRForException");
			iLProcessor.Emit(OpCodes.Call, _module.ImportReference(method2));
			instruction3 = (exceptionHandler.TryEnd = (exceptionHandler.HandlerStart = body.Instructions.Last()));
			iLProcessor.Emit(OpCodes.Ldc_I4, -2147483636);
			iLProcessor.Emit(OpCodes.Bne_Un_S, target);
			Instruction instruction11 = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldstr, "Collection was modified; enumeration operation may not execute.");
			iLProcessor.Emit(OpCodes.Newobj, _invalidOperationExceptionConstructor);
			iLProcessor.Emit(OpCodes.Throw);
			iLProcessor.Emit(OpCodes.Rethrow);
			instruction11.Operand = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			instruction3 = (Instruction)(instruction8.Operand = (exceptionHandler.HandlerEnd = body.Instructions.Last()));
			iLProcessor.Emit(OpCodes.Ldfld, _hadCurrentField);
			iLProcessor.Emit(OpCodes.Ret);
			body.OptimizeMacros();
		}

		private void WriteMethodGetCurrent(MethodDefinition method)
		{
			MethodBody body = method.Body;
			ILProcessor iLProcessor = body.GetILProcessor();
			Instruction target = Instruction.Create(OpCodes.Nop);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldfld, _initializedField);
			iLProcessor.Emit(OpCodes.Brtrue_S, target);
			Instruction instruction = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldstr, "Enumeration has not started. Call MoveNext.");
			iLProcessor.Emit(OpCodes.Newobj, _module.ImportReference(_invalidOperationExceptionConstructor));
			iLProcessor.Emit(OpCodes.Throw);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			instruction.Operand = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldfld, _hadCurrentField);
			iLProcessor.Emit(OpCodes.Brtrue_S, target);
			Instruction instruction2 = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldstr, "Enumeration already finished.");
			iLProcessor.Emit(OpCodes.Newobj, _module.ImportReference(_invalidOperationExceptionConstructor));
			iLProcessor.Emit(OpCodes.Throw);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			instruction2.Operand = body.Instructions.Last();
			iLProcessor.Emit(OpCodes.Ldfld, _typeResolver.Resolve(_currentField));
			if (_currentField.FieldType.IsGenericParameter && method.ReturnType.MetadataType == MetadataType.Object)
			{
				iLProcessor.Emit(OpCodes.Box, _currentField.FieldType);
			}
			iLProcessor.Emit(OpCodes.Ret);
			body.OptimizeMacros();
		}

		private void WriteMethodReset(MethodDefinition method)
		{
			MethodBody body = method.Body;
			ILProcessor iLProcessor = body.GetILProcessor();
			MethodDefinition method2 = _context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System", "NotSupportedException").Methods.First((MethodDefinition m) => m.IsConstructor && !m.IsStatic && !m.HasParameters);
			iLProcessor.Emit(OpCodes.Newobj, _module.ImportReference(method2));
			iLProcessor.Emit(OpCodes.Throw);
			body.OptimizeMacros();
		}

		private void WriteDisposeMethod(MethodDefinition method)
		{
			MethodBody body = method.Body;
			body.GetILProcessor().Emit(OpCodes.Ret);
			body.OptimizeMacros();
		}
	}
}
