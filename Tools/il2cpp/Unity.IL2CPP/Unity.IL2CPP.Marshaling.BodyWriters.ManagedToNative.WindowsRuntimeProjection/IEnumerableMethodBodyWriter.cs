using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection
{
	internal sealed class IEnumerableMethodBodyWriter
	{
		private readonly TypeDefinition _iteratorToEnumeratorAdapter;

		private readonly TypeDefinition _iiterableType;

		public IEnumerableMethodBodyWriter(TypeDefinition iteratorToEnumeratorAdapter, TypeDefinition iiterableType)
		{
			_iteratorToEnumeratorAdapter = iteratorToEnumeratorAdapter;
			_iiterableType = iiterableType;
		}

		public void WriteGetEnumerator(MethodDefinition method)
		{
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			TypeReference declaringType = method.Overrides.First().DeclaringType;
			TypeResolver typeResolver = ((!_iiterableType.HasGenericParameters || declaringType.Resolve().HasGenericParameters) ? TypeResolver.For(declaringType) : TypeResolver.For(new GenericInstanceType(_iiterableType)
			{
				GenericArguments = { (TypeReference)method.DeclaringType.GenericParameters[0] }
			}));
			MethodReference methodReference = typeResolver.Resolve(_iiterableType.Methods.First((MethodDefinition m) => m.Name == "First"));
			MethodReference method2 = typeResolver.Resolve(_iteratorToEnumeratorAdapter.Methods.First((MethodDefinition m) => m.IsConstructor));
			method.Body.Variables.Add(new VariableDefinition(typeResolver.Resolve(methodReference.ReturnType)));
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Callvirt, methodReference);
			iLProcessor.Emit(OpCodes.Dup);
			iLProcessor.Emit(OpCodes.Stloc_0);
			Instruction instruction = Instruction.Create(OpCodes.Ldloc_0);
			iLProcessor.Emit(OpCodes.Brtrue, instruction);
			iLProcessor.Emit(OpCodes.Ldnull);
			iLProcessor.Emit(OpCodes.Ret);
			iLProcessor.Append(instruction);
			iLProcessor.Emit(OpCodes.Newobj, method2);
			iLProcessor.Emit(OpCodes.Ret);
		}
	}
}
