using System;
using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.StackAnalysis;

namespace Unity.IL2CPP
{
	internal static class ArithmeticOpCodes
	{
		internal static void Add(ReadOnlyContext context, Stack<StackInfo> valueStack)
		{
			StackInfo right = valueStack.Pop();
			StackInfo left = valueStack.Pop();
			TypeReference leftType = left.Type.WithoutModifiers();
			TypeReference rightType = right.Type.WithoutModifiers();
			TypeReference resultType = StackAnalysisUtils.ResultTypeForAdd(context, leftType, rightType);
			valueStack.Push(WriteWarningProtectedOperation(context, "add", left, right, resultType));
		}

		internal static void Add(ReadOnlyContext context, IGeneratedMethodCodeWriter writer, OverflowCheck check, Stack<StackInfo> valueStack, Func<string> getRaiseOverflowExceptionExpression)
		{
			StackInfo right = valueStack.Pop();
			StackInfo left = valueStack.Pop();
			if (check != 0)
			{
				TypeReference typeReference = StackTypeConverter.StackTypeFor(context, left.Type);
				TypeReference typeReference2 = StackTypeConverter.StackTypeFor(context, right.Type);
				if (RequiresPointerOverflowCheck(context, typeReference, typeReference2))
				{
					WritePointerOverflowCheckUsing64Bits(writer, "+", check, left, right, getRaiseOverflowExceptionExpression);
				}
				else if (Requires64BitOverflowCheck(typeReference.MetadataType, typeReference2.MetadataType))
				{
					if (check == OverflowCheck.Signed)
					{
						writer.WriteLine("if (il2cpp_codegen_check_add_overflow((int64_t){0}, (int64_t){1}))", left.Expression, right.Expression);
						writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
					}
					else
					{
						writer.WriteLine("if ((uint64_t){0} > kIl2CppUInt64Max - (uint64_t){1})", left.Expression, right.Expression);
						writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
					}
				}
				else
				{
					WriteNarrowOverflowCheckUsing64Bits(writer, "+", check, left.Expression, right.Expression, getRaiseOverflowExceptionExpression);
				}
			}
			TypeReference leftType = left.Type.WithoutModifiers();
			TypeReference rightType = right.Type.WithoutModifiers();
			TypeReference resultType = StackAnalysisUtils.ResultTypeForAdd(context, leftType, rightType);
			valueStack.Push(WriteWarningProtectedOperation(context, "add", left, right, resultType));
		}

		internal static void Sub(ReadOnlyContext context, Stack<StackInfo> valueStack)
		{
			StackInfo right = valueStack.Pop();
			StackInfo left = valueStack.Pop();
			TypeReference leftType = left.Type.WithoutModifiers();
			TypeReference rightType = right.Type.WithoutModifiers();
			TypeReference resultType = StackAnalysisUtils.ResultTypeForSub(context, leftType, rightType);
			valueStack.Push(WriteWarningProtectedOperation(context, "subtract", left, right, resultType));
		}

		internal static void Sub(ReadOnlyContext context, IGeneratedMethodCodeWriter writer, OverflowCheck check, Stack<StackInfo> valueStack, Func<string> getRaiseOverflowExceptionExpression)
		{
			StackInfo right = valueStack.Pop();
			StackInfo left = valueStack.Pop();
			if (check != 0)
			{
				TypeReference typeReference = StackTypeConverter.StackTypeFor(context, left.Type);
				TypeReference typeReference2 = StackTypeConverter.StackTypeFor(context, right.Type);
				if (RequiresPointerOverflowCheck(context, typeReference, typeReference2))
				{
					WritePointerOverflowCheckUsing64Bits(writer, "-", check, left, right, getRaiseOverflowExceptionExpression);
				}
				else if (Requires64BitOverflowCheck(typeReference.MetadataType, typeReference2.MetadataType))
				{
					if (check == OverflowCheck.Signed)
					{
						writer.WriteLine("if (il2cpp_codegen_check_sub_overflow((int64_t){0}, (int64_t){1}))", left.Expression, right.Expression);
						writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
					}
					else
					{
						writer.WriteLine("if ((uint64_t){0} < (uint64_t){1})", left.Expression, right.Expression);
						writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
					}
				}
				else
				{
					WriteNarrowOverflowCheckUsing64Bits(writer, "-", check, left.Expression, right.Expression, getRaiseOverflowExceptionExpression);
				}
			}
			TypeReference leftType = left.Type.WithoutModifiers();
			TypeReference rightType = right.Type.WithoutModifiers();
			TypeReference resultType = StackAnalysisUtils.ResultTypeForSub(context, leftType, rightType);
			valueStack.Push(WriteWarningProtectedOperation(context, "subtract", left, right, resultType));
		}

		internal static void Mul(ReadOnlyContext context, Stack<StackInfo> valueStack)
		{
			StackInfo right = valueStack.Pop();
			StackInfo left = valueStack.Pop();
			TypeReference leftType = left.Type.WithoutModifiers();
			TypeReference rightType = right.Type.WithoutModifiers();
			TypeReference resultType = StackAnalysisUtils.ResultTypeForMul(context, leftType, rightType);
			valueStack.Push(WriteWarningProtectedOperation(context, "multiply", left, right, resultType));
		}

		internal static void Mul(ReadOnlyContext context, IGeneratedMethodCodeWriter writer, OverflowCheck check, Stack<StackInfo> valueStack, Func<string> getRaiseOverflowExceptionExpression)
		{
			StackInfo right = valueStack.Pop();
			StackInfo left = valueStack.Pop();
			if (check != 0)
			{
				TypeReference typeReference = StackTypeConverter.StackTypeFor(context, left.Type);
				TypeReference typeReference2 = StackTypeConverter.StackTypeFor(context, right.Type);
				if (RequiresPointerOverflowCheck(context, typeReference, typeReference2))
				{
					WritePointerOverflowCheckUsing64Bits(writer, "*", check, left, right, getRaiseOverflowExceptionExpression);
				}
				else if (Requires64BitOverflowCheck(typeReference.MetadataType, typeReference2.MetadataType))
				{
					if (check == OverflowCheck.Signed)
					{
						writer.WriteLine("if (il2cpp_codegen_check_mul_overflow_i64((int64_t){0}, (int64_t){1}, kIl2CppInt64Min, kIl2CppInt64Max))", left.Expression, right.Expression);
						writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
					}
					else
					{
						writer.WriteLine("if (il2cpp_codegen_check_mul_oveflow_u64({0}, {1}))", left.Expression, right.Expression);
						writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
					}
				}
				else
				{
					WriteNarrowOverflowCheckUsing64Bits(writer, "*", check, left.Expression, right.Expression, getRaiseOverflowExceptionExpression);
				}
			}
			TypeReference leftType = left.Type.WithoutModifiers();
			TypeReference rightType = right.Type.WithoutModifiers();
			TypeReference resultType = StackAnalysisUtils.ResultTypeForMul(context, leftType, rightType);
			valueStack.Push(WriteWarningProtectedOperation(context, "multiply", left, right, resultType));
		}

		private static bool RequiresPointerOverflowCheck(ReadOnlyContext context, TypeReference leftStackType, TypeReference rightStackType)
		{
			if (!RequiresPointerOverflowCheck(context, leftStackType))
			{
				return RequiresPointerOverflowCheck(context, rightStackType);
			}
			return true;
		}

		private static bool RequiresPointerOverflowCheck(ReadOnlyContext context, TypeReference type)
		{
			if (!type.IsSameType(context.Global.Services.TypeProvider.SystemIntPtr))
			{
				return type.IsSameType(context.Global.Services.TypeProvider.SystemUIntPtr);
			}
			return true;
		}

		private static bool Requires64BitOverflowCheck(MetadataType leftStackType, MetadataType rightStackType)
		{
			if (!Requires64BitOverflowCheck(leftStackType))
			{
				return Requires64BitOverflowCheck(rightStackType);
			}
			return true;
		}

		private static bool Requires64BitOverflowCheck(MetadataType metadataType)
		{
			if (metadataType != MetadataType.UInt64)
			{
				return metadataType == MetadataType.Int64;
			}
			return true;
		}

		private static void WriteNarrowOverflowCheckUsing64Bits(IGeneratedMethodCodeWriter writer, string op, OverflowCheck check, string leftExpression, string rightExpression, Func<string> getRaiseOverflowExceptionExpression)
		{
			if (check == OverflowCheck.Signed)
			{
				writer.WriteLine("if (((int64_t){1} {0} (int64_t){2} < (int64_t)kIl2CppInt32Min) || ((int64_t){1} {0} (int64_t){2} > (int64_t)kIl2CppInt32Max))", op, leftExpression, rightExpression);
				writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
			}
			else
			{
				writer.WriteLine("if ((uint64_t)(uint32_t){1} {0} (uint64_t)(uint32_t){2} > (uint64_t)(uint32_t)kIl2CppUInt32Max)", op, leftExpression, rightExpression);
				writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
			}
		}

		private static StackInfo WriteWarningProtectedOperation(ReadOnlyContext context, string name, StackInfo left, StackInfo right, TypeReference resultType)
		{
			string rcast = CastExpressionForBinaryOperator(context, right);
			string lcast = CastExpressionForBinaryOperator(context, left);
			if (!resultType.IsPointer)
			{
				try
				{
					resultType = StackTypeConverter.StackTypeFor(context, resultType);
				}
				catch (ArgumentException)
				{
				}
			}
			return WriteWarningProtectedOperation(context, resultType, lcast, left.Expression, rcast, right.Expression, name);
		}

		private static string CastExpressionForBinaryOperator(ReadOnlyContext context, StackInfo right)
		{
			if (right.Type.IsPointer)
			{
				return "(" + context.Global.Services.Naming.ForVariable(StackTypeConverter.StackTypeForBinaryOperation(context, right.Type)) + ")";
			}
			try
			{
				return "(" + StackTypeConverter.CppStackTypeFor(context, right.Type) + ")";
			}
			catch (ArgumentException)
			{
				return "";
			}
		}

		private static StackInfo WriteWarningProtectedOperation(ReadOnlyContext context, TypeReference destType, string lcast, string left, string rcast, string right, string name)
		{
			return new StackInfo($"(({context.Global.Services.Naming.ForVariable(destType)})il2cpp_codegen_{name}({lcast}{left}, {rcast}{right}))", destType);
		}

		private static void WritePointerOverflowCheckUsing64Bits(IGeneratedMethodCodeWriter writer, string op, OverflowCheck check, StackInfo left, StackInfo right, Func<string> getRaiseOverflowExceptionExpression)
		{
			WritePointerOverflowCheckUsing64Bits(writer, op, check, left.Expression, right.Expression, getRaiseOverflowExceptionExpression);
		}

		private static void WritePointerOverflowCheckUsing64Bits(IGeneratedMethodCodeWriter writer, string op, OverflowCheck check, string leftExpression, string rightExpression, Func<string> getRaiseOverflowExceptionExpression)
		{
			if (check == OverflowCheck.Signed)
			{
				writer.WriteLine("if (((intptr_t){1} {0} (intptr_t){2} < (intptr_t)kIl2CppIntPtrMin) || ((intptr_t){1} {0} (intptr_t){2} > (intptr_t)kIl2CppIntPtrMax))", op, leftExpression, rightExpression);
				writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
			}
			else
			{
				writer.WriteLine("if ((uintptr_t){1} {0} (uintptr_t){2} > (uintptr_t)kIl2CppUIntPtrMax)", op, leftExpression, rightExpression);
				writer.WriteLine("\t{0};", getRaiseOverflowExceptionExpression());
			}
		}
	}
}
