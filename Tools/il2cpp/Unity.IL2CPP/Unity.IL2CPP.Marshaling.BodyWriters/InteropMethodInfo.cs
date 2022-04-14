using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters
{
	public class InteropMethodInfo
	{
		protected readonly ReadOnlyContext _context;

		protected readonly InteropMarshaler _marshaler;

		protected readonly TypeResolver _typeResolver;

		public readonly MarshaledParameter[] Parameters;

		public readonly MarshaledType[] MarshaledParameterTypes;

		public readonly MarshaledType MarshaledReturnType;

		protected virtual MethodReference InteropMethod { get; }

		public static InteropMethodInfo ForComCallableWrapper(ReadOnlyContext context, MethodReference managedMethod, MethodReference interfaceMethod, MarshalType marshalType)
		{
			return ForNativeToManaged(context, managedMethod, interfaceMethod, marshalType, useUnicodeCharset: true);
		}

		public static InteropMethodInfo ForNativeToManaged(ReadOnlyContext context, MethodReference managedMethod, MethodReference interopMethod, MarshalType marshalType, bool useUnicodeCharset)
		{
			return new InteropMethodInfo(context, interopMethod, managedMethod, new NativeToManagedMarshaler(context, TypeResolver.For(interopMethod.DeclaringType, interopMethod), marshalType, useUnicodeCharset));
		}

		protected InteropMethodInfo(ReadOnlyContext context, MethodReference interopMethod, MethodReference methodForParameterNames, InteropMarshaler marshaler)
		{
			_context = context;
			InteropMethod = interopMethod;
			_marshaler = marshaler;
			_typeResolver = TypeResolver.For(interopMethod.DeclaringType, interopMethod);
			MethodDefinition methodDefinition = interopMethod.Resolve();
			Parameters = new MarshaledParameter[methodDefinition.Parameters.Count];
			for (int i = 0; i < methodDefinition.Parameters.Count; i++)
			{
				ParameterDefinition parameterDefinition = methodForParameterNames.Parameters[i];
				ParameterDefinition parameterDefinition2 = methodDefinition.Parameters[i];
				TypeReference parameterType = _typeResolver.Resolve(parameterDefinition2.ParameterType);
				Parameters[i] = new MarshaledParameter(parameterDefinition.Name, context.Global.Services.Naming.ForParameterName(parameterDefinition), parameterType, parameterDefinition2.MarshalInfo, parameterDefinition2.IsIn, parameterDefinition2.IsOut);
			}
			List<MarshaledType> list = new List<MarshaledType>();
			MarshaledParameter[] parameters = Parameters;
			foreach (MarshaledParameter marshaledParameter in parameters)
			{
				MarshaledType[] marshaledTypes = marshaler.MarshalInfoWriterFor(context, marshaledParameter).MarshaledTypes;
				foreach (MarshaledType marshaledType in marshaledTypes)
				{
					list.Add(new MarshaledType(marshaledType.Name, marshaledType.DecoratedName, marshaledParameter.NameInGeneratedCode + marshaledType.VariableName));
				}
			}
			MarshaledType[] marshaledTypes2 = marshaler.MarshalInfoWriterFor(context, interopMethod.MethodReturnType).MarshaledTypes;
			for (int l = 0; l < marshaledTypes2.Length - 1; l++)
			{
				MarshaledType marshaledType2 = marshaledTypes2[l];
				list.Add(new MarshaledType(marshaledType2.Name + "*", marshaledType2.DecoratedName + "*", _context.Global.Services.Naming.ForComInterfaceReturnParameterName() + marshaledType2.VariableName));
			}
			MarshaledParameterTypes = list.ToArray();
			MarshaledReturnType = marshaledTypes2[marshaledTypes2.Length - 1];
		}
	}
}
