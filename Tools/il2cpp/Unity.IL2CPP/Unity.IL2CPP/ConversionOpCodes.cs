using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	internal static class ConversionOpCodes
	{
		internal static void WriteNumericConversionWithOverflow<TMaxValue>(ReadOnlyContext context, IGeneratedMethodCodeWriter writer, Stack<StackInfo> valueStack, TypeReference typeReference, bool treatInputAsUnsigned, TMaxValue maxValue, Func<string> getRaiseOverflowExceptionExpression)
		{
			WriteCheckForOverflow(context, writer, valueStack, treatInputAsUnsigned, maxValue, getRaiseOverflowExceptionExpression);
			WriteNumericConversion(context, valueStack, typeReference);
		}

		internal static void ConvertToNaturalIntWithOverflow<TMaxValueType>(ReadOnlyContext context, IGeneratedMethodCodeWriter writer, Stack<StackInfo> valueStack, TypeReference pointerType, bool treatInputAsUnsigned, TMaxValueType maxValue, Func<string> getRaiseOverflowExceptionExpression)
		{
			WriteCheckForOverflow(context, writer, valueStack, treatInputAsUnsigned, maxValue, getRaiseOverflowExceptionExpression);
			ConvertToNaturalInt(context, valueStack, pointerType);
		}

		internal static void WriteNumericConversionToFloatFromUnsigned(ReadOnlyContext context, Stack<StackInfo> valueStack)
		{
			StackInfo stackInfo = valueStack.Peek();
			if (stackInfo.Type.MetadataType == MetadataType.Single || stackInfo.Type.MetadataType == MetadataType.Double)
			{
				WriteNumericConversion(context, valueStack, stackInfo.Type, context.Global.Services.TypeProvider.DoubleTypeReference);
			}
			else if (stackInfo.Type.MetadataType == MetadataType.Int64 || stackInfo.Type.MetadataType == MetadataType.UInt64)
			{
				WriteNumericConversion(context, valueStack, context.Global.Services.TypeProvider.UInt64TypeReference, context.Global.Services.TypeProvider.DoubleTypeReference);
			}
			else
			{
				WriteNumericConversion(context, valueStack, context.Global.Services.TypeProvider.UInt32TypeReference, context.Global.Services.TypeProvider.DoubleTypeReference);
			}
		}

		internal static void WriteNumericConversionI8(ReadOnlyContext context, Stack<StackInfo> valueStack)
		{
			if (valueStack.Peek().Type.MetadataType == MetadataType.UInt32)
			{
				WriteNumericConversion(context, valueStack, context.Global.Services.TypeProvider.Int32TypeReference);
			}
			WriteNumericConversion(context, valueStack, context.Global.Services.TypeProvider.Int64TypeReference, context.Global.Services.TypeProvider.Int64TypeReference);
		}

		internal static void WriteNumericConversionU8(ReadOnlyContext context, Stack<StackInfo> valueStack)
		{
			if (valueStack.Peek().Type.IsSameType(context.Global.Services.TypeProvider.Int32TypeReference))
			{
				WriteNumericConversion(context, valueStack, context.Global.Services.TypeProvider.UInt32TypeReference);
			}
			if (valueStack.Peek().Type.IsSameType(context.Global.Services.TypeProvider.IntPtrTypeReference))
			{
				WriteNumericConversion(context, valueStack, context.Global.Services.TypeProvider.UIntPtrTypeReference);
			}
			WriteNumericConversion(context, valueStack, context.Global.Services.TypeProvider.UInt64TypeReference, context.Global.Services.TypeProvider.Int64TypeReference);
		}

		internal static void WriteNumericConversionFloat(ReadOnlyContext context, Stack<StackInfo> valueStack, TypeReference outputType)
		{
			if (valueStack.Peek().Type.MetadataType == MetadataType.UInt32)
			{
				WriteNumericConversion(context, valueStack, context.Global.Services.TypeProvider.Int32TypeReference, outputType);
			}
			else if (valueStack.Peek().Type.MetadataType == MetadataType.UInt64)
			{
				WriteNumericConversion(context, valueStack, context.Global.Services.TypeProvider.Int64TypeReference, outputType);
			}
			WriteNumericConversion(context, valueStack, outputType);
		}

		internal static void WriteCheckForOverflow<TMaxValue>(ReadOnlyContext context, IGeneratedMethodCodeWriter writer, Stack<StackInfo> valueStack, bool treatInputAsUnsigned, TMaxValue maxValue, Func<string> getRaiseOverflowExceptionExpression)
		{
			StackInfo stackInfo = valueStack.Peek();
			if (stackInfo.Type.IsSameType(context.Global.Services.TypeProvider.DoubleTypeReference) || stackInfo.Type.IsSameType(context.Global.Services.TypeProvider.SingleTypeReference))
			{
				writer.WriteLine("if ({0} > (double)({1})) {2};", stackInfo.Expression, maxValue, getRaiseOverflowExceptionExpression());
			}
			else if (treatInputAsUnsigned)
			{
				writer.WriteLine("if ((uint64_t)({0}) > {1}) {2};", stackInfo.Expression, maxValue, getRaiseOverflowExceptionExpression());
			}
			else
			{
				writer.WriteLine("if ((int64_t)({0}) > {1}) {2};", stackInfo.Expression, maxValue, getRaiseOverflowExceptionExpression());
			}
		}

		internal static void WriteNumericConversion(ReadOnlyContext context, Stack<StackInfo> valueStack, TypeReference typeReference)
		{
			WriteNumericConversion(context, valueStack, typeReference, typeReference);
		}

		internal static void WriteNumericConversion(ReadOnlyContext context, Stack<StackInfo> valueStack, TypeReference inputType, TypeReference outputType)
		{
			StackInfo stackInfo = valueStack.Pop();
			string text = string.Empty;
			if ((TypeReferenceEqualityComparer.AreEqual(stackInfo.Type, context.Global.Services.TypeProvider.SingleTypeReference) || TypeReferenceEqualityComparer.AreEqual(stackInfo.Type, context.Global.Services.TypeProvider.DoubleTypeReference)) && inputType.IsUnsignedIntegralType())
			{
				valueStack.Push(new StackInfo($"il2cpp_codegen_cast_floating_point<{context.Global.Services.Naming.ForVariable(inputType)}, {context.Global.Services.Naming.ForVariable(outputType)}, {context.Global.Services.Naming.ForVariable(stackInfo.Type)}>({stackInfo})", outputType));
				return;
			}
			if (stackInfo.Type.MetadataType == MetadataType.Pointer)
			{
				text = "(intptr_t)";
			}
			valueStack.Push(new StackInfo($"(({context.Global.Services.Naming.ForVariable(outputType)})(({context.Global.Services.Naming.ForVariable(inputType)}){text}{stackInfo}))", outputType));
		}

		internal static void ConvertToNaturalInt(ReadOnlyContext context, Stack<StackInfo> valueStack, TypeReference pointerType)
		{
			valueStack.Push(new StackInfo(string.Format(arg1: valueStack.Pop().Expression, format: "(({0}){1})", arg0: context.Global.Services.Naming.ForVariable(pointerType)), pointerType));
		}
	}
}
