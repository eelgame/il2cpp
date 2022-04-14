using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal abstract class ManagedToNativeInteropMethodBodyWriter : InteropMethodBodyWriter
	{
		public ManagedToNativeInteropMethodBodyWriter(ReadOnlyContext context, MethodReference interopMethod, MethodReference methodForParameterNames, MarshalType marshalType, bool useUnicodeCharset)
			: base(context, interopMethod, methodForParameterNames, new ManagedToNativeMarshaler(context, TypeResolver.For(interopMethod.DeclaringType, interopMethod), marshalType, useUnicodeCharset))
		{
		}

		protected override void WriteScopedAllocationCheck(IGeneratedMethodCodeWriter writer)
		{
		}

		protected string GetFunctionCallParametersExpression(string[] localVariableNames, bool includesRetVal)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < localVariableNames.Length; i++)
			{
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(Parameters[i]);
				MarshaledType[] marshaledTypes = defaultMarshalInfoWriter.MarshaledTypes;
				foreach (MarshaledType marshaledType in marshaledTypes)
				{
					string marshaledVariableName = localVariableNames[i] + marshaledType.VariableName;
					string item = defaultMarshalInfoWriter.DecorateVariable(Parameters[i].NameInGeneratedCode, marshaledVariableName);
					list.Add(item);
				}
			}
			MethodReturnType methodReturnType = GetMethodReturnType();
			DefaultMarshalInfoWriter defaultMarshalInfoWriter2 = MarshalInfoWriterFor(methodReturnType);
			MarshaledType[] marshaledTypes2 = defaultMarshalInfoWriter2.MarshaledTypes;
			for (int k = 0; k < marshaledTypes2.Length - 1; k++)
			{
				string marshaledVariableName2 = _context.Global.Services.Naming.ForInteropReturnValue() + marshaledTypes2[k].VariableName;
				string text = defaultMarshalInfoWriter2.DecorateVariable(null, marshaledVariableName2);
				list.Add("&" + text);
			}
			if (includesRetVal && methodReturnType.ReturnType.MetadataType != MetadataType.Void)
			{
				list.Add("&" + _context.Global.Services.Naming.ForInteropReturnValue());
			}
			return list.AggregateWithComma();
		}
	}
}
