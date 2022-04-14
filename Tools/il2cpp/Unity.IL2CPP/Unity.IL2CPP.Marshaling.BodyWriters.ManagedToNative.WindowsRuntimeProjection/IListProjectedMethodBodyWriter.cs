using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection
{
	internal sealed class IListProjectedMethodBodyWriter
	{
		private readonly MinimalContext _context;

		private readonly TypeDefinition _iVectorType;

		public IListProjectedMethodBodyWriter(MinimalContext context, TypeDefinition iVectorType)
		{
			_context = context;
			_iVectorType = iVectorType;
		}

		public void WriteGetItem(MethodDefinition method)
		{
			MethodDefinition vectorMethod = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "GetAt");
			WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(method, method.Parameters[0], vectorMethod);
		}

		public void WriteIndexOf(MethodDefinition method)
		{
			MethodReference method2 = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "IndexOf");
			if (_iVectorType.HasGenericParameters)
			{
				method2 = TypeResolver.For(new GenericInstanceType(_iVectorType)
				{
					GenericArguments = { (TypeReference)method.DeclaringType.GenericParameters[0] }
				}).Resolve(method2);
			}
			MethodDefinition method3 = _context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "InvalidOperationException").Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			VariableDefinition variableDefinition = new VariableDefinition(_context.Global.Services.TypeProvider.UInt32TypeReference);
			method.Body.Variables.Add(variableDefinition);
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			Instruction instruction = iLProcessor.Create(OpCodes.Nop);
			Instruction instruction2 = iLProcessor.Create(OpCodes.Nop);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Ldloca, variableDefinition);
			iLProcessor.Emit(OpCodes.Callvirt, method2);
			iLProcessor.Emit(OpCodes.Brtrue, instruction);
			iLProcessor.Emit(OpCodes.Ldc_I4_M1);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Ldloc, variableDefinition);
			iLProcessor.Emit(OpCodes.Ldc_I4, int.MaxValue);
			iLProcessor.Emit(OpCodes.Bgt_Un, instruction2);
			iLProcessor.Emit(OpCodes.Ldloc, variableDefinition);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Append(instruction2);
			iLProcessor.Emit(OpCodes.Ldstr, "The backing collection is too large.");
			iLProcessor.Emit(OpCodes.Newobj, method3);
			iLProcessor.Emit(OpCodes.Throw);
		}

		public void WriteInsert(MethodDefinition method)
		{
			MethodDefinition vectorMethod = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "InsertAt");
			WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(method, method.Parameters[0], vectorMethod);
		}

		public void WriteRemoveAt(MethodDefinition method)
		{
			MethodDefinition vectorMethod = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "RemoveAt");
			WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(method, method.Parameters[0], vectorMethod);
		}

		public void WriteSetItem(MethodDefinition method)
		{
			MethodDefinition vectorMethod = _iVectorType.Methods.Single((MethodDefinition m) => m.Name == "SetAt");
			WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(method, method.Parameters[0], vectorMethod);
		}

		private void WriteCallVectorMethodWithIndexCheckAndExceptionTranslation(MethodDefinition currentMethod, ParameterDefinition indexParameter, MethodDefinition vectorMethod)
		{
			MethodReference method = ((!_iVectorType.HasGenericParameters) ? vectorMethod : TypeResolver.For(new GenericInstanceType(_iVectorType)
			{
				GenericArguments = { (TypeReference)currentMethod.DeclaringType.GenericParameters[0] }
			}).Resolve(vectorMethod));
			PropertyDefinition propertyDefinition = _context.Global.Services.TypeProvider.SystemException.Properties.Single((PropertyDefinition p) => p.Name == "HResult");
			MethodDefinition method2 = _context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "ArgumentOutOfRangeException").Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			ILProcessor iLProcessor = currentMethod.Body.GetILProcessor();
			Instruction instruction = iLProcessor.Create(OpCodes.Nop);
			iLProcessor.Emit(OpCodes.Ldarg, indexParameter.Index + 1);
			iLProcessor.Emit(OpCodes.Ldc_I4_0);
			iLProcessor.Emit(OpCodes.Blt, instruction);
			Instruction instruction2 = iLProcessor.Create(OpCodes.Ldarg_0);
			iLProcessor.Append(instruction2);
			for (int i = 0; i < vectorMethod.Parameters.Count; i++)
			{
				iLProcessor.Emit(OpCodes.Ldarg, i + 1);
			}
			iLProcessor.Emit(OpCodes.Callvirt, method);
			iLProcessor.Emit(OpCodes.Ret);
			Instruction instruction3 = iLProcessor.Create(OpCodes.Call, propertyDefinition.GetMethod);
			iLProcessor.Append(instruction3);
			iLProcessor.Emit(OpCodes.Ldc_I4, -2147483637);
			iLProcessor.Emit(OpCodes.Beq, instruction);
			iLProcessor.Emit(OpCodes.Rethrow);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Ldstr, "index");
			iLProcessor.Emit(OpCodes.Newobj, method2);
			iLProcessor.Emit(OpCodes.Throw);
			ExceptionHandler exceptionHandler = new ExceptionHandler(ExceptionHandlerType.Catch);
			exceptionHandler.TryStart = instruction2;
			exceptionHandler.TryEnd = instruction3;
			exceptionHandler.HandlerStart = instruction3;
			exceptionHandler.HandlerEnd = instruction;
			exceptionHandler.CatchType = _context.Global.Services.TypeProvider.SystemException;
			currentMethod.Body.ExceptionHandlers.Add(exceptionHandler);
		}
	}
}
