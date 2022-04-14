using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public class Emit
	{
		public const string ClassMetadataType = "RuntimeClass";

		public const string MethodMetadataType = "RuntimeMethod";

		public const string FieldMetadataType = "RuntimeField";

		public const string TypeMetadataType = "RuntimeType";

		public static string Arrow(string left, string right)
		{
			return $"{left}->{right}";
		}

		public static string Assign(string left, string right)
		{
			return $"{left} = {right}";
		}

		public static string Box(ReadOnlyContext context, TypeReference type, string value, IRuntimeMetadataAccess metadataAccess)
		{
			if (!type.IsValueType())
			{
				return Cast(context, type, value);
			}
			return Call("Box", metadataAccess.TypeInfoFor(type), "&" + value);
		}

		public static string Call(string method)
		{
			return Call(method, Enumerable.Empty<string>());
		}

		public static string Call(string method, string argument)
		{
			return Call(method, new string[1] { argument });
		}

		public static string Call(string method, string argument1, string argument2)
		{
			return Call(method, new string[2] { argument1, argument2 });
		}

		public static string Call(string method, string argument1, string argument2, string argument3)
		{
			return Call(method, new string[3] { argument1, argument2, argument3 });
		}

		public static string Call(string method, IEnumerable<string> arguments)
		{
			return $"{method}({arguments.AggregateWithComma()})";
		}

		public static string Cast(ReadOnlyContext context, TypeReference type, string value)
		{
			return $"({context.Global.Services.Naming.ForVariable(type)}){value}";
		}

		public static IEnumerable<string> CastEach(string targetTypeName, IEnumerable<string> values)
		{
			List<string> list = new List<string>();
			foreach (string value in values)
			{
				list.Add($"({targetTypeName}){value}");
			}
			return list;
		}

		public static string Cast(string type, string value)
		{
			return $"({type}){value}";
		}

		public static string InitializedTypeInfo(string argument)
		{
			return $"InitializedTypeInfo({argument})";
		}

		public static string AddressOf(string value)
		{
			if (value.StartsWith("*"))
			{
				return value.Substring(1);
			}
			return $"(&{value})";
		}

		public static string Dereference(string value)
		{
			if (value.StartsWith("&"))
			{
				return value.Substring(1);
			}
			if (value.StartsWith("(&") && value.EndsWith(")"))
			{
				return value.Substring(2, value.Length - 3);
			}
			return $"*{value}";
		}

		public static string Dot(string left, string right)
		{
			return $"{left}.{right}";
		}

		public static string InParentheses(string expression)
		{
			return "(" + expression + ")";
		}

		public static string ArrayBoundsCheck(string array, string index)
		{
			return MultiDimensionalArrayBoundsCheck($"(uint32_t)({array})->max_length", index);
		}

		public static string MultiDimensionalArrayBoundsCheck(string length, string index)
		{
			return $"IL2CPP_ARRAY_BOUNDS_CHECK({index}, {length});";
		}

		public static string MultiDimensionalArrayBoundsCheck(ReadOnlyContext context, string array, string index, int rank)
		{
			if (!context.Global.Parameters.UsingTinyBackend)
			{
				return ArrayBoundsCheck(array, index);
			}
			string text = string.Empty;
			for (int i = 0; i < rank; i++)
			{
				if (text.Length > 0)
				{
					text += " * ";
				}
				text += $"{array}->bounds[{i}]";
			}
			return $"IL2CPP_ARRAY_BOUNDS_CHECK({index}, {text});";
		}

		public static string LoadArrayElement(string array, string index, bool useArrayBoundsCheck)
		{
			return $"({array})->{ArrayNaming.ForArrayItemGetter(useArrayBoundsCheck)}(static_cast<{ArrayNaming.ForArrayIndexType()}>({index}))";
		}

		public static string LoadArrayElementAddress(string array, string index, bool useArrayBoundsCheck)
		{
			return $"({array})->{ArrayNaming.ForArrayItemAddressGetter(useArrayBoundsCheck)}(static_cast<{ArrayNaming.ForArrayIndexType()}>({index}))";
		}

		public static string StoreArrayElement(string array, string index, string value, bool useArrayBoundsCheck)
		{
			return $"({array})->{ArrayNaming.ForArrayItemSetter(useArrayBoundsCheck)}(static_cast<{ArrayNaming.ForArrayIndexType()}>({index}), {value})";
		}

		public static string NewObj(ReadOnlyContext context, TypeReference type, IRuntimeMetadataAccess metadataAccess)
		{
			string text = (context.Global.Parameters.UsingTinyBackend ? Call("il2cpp_codegen_object_new", "sizeof(" + context.Global.Services.Naming.ForTypeNameOnly(type) + ")", metadataAccess.TypeInfoFor(type)) : Call("il2cpp_codegen_object_new", metadataAccess.TypeInfoFor(type)));
			if (type.IsValueType())
			{
				return text;
			}
			return Cast(context.Global.Services.Naming.ForTypeNameOnly(type) + "*", text);
		}

		public static string NewSZArray(ReadOnlyContext context, ArrayType arrayType, TypeReference unresolvedElementType, int length, IRuntimeMetadataAccess metadataAccess)
		{
			return NewSZArray(context, arrayType, unresolvedElementType, length.ToString(CultureInfo.InvariantCulture), metadataAccess);
		}

		public static string NewSZArray(ReadOnlyContext context, ArrayType arrayType, TypeReference unresolvedElementType, string length, IRuntimeMetadataAccess metadataAccess)
		{
			if (arrayType.Rank != 1)
			{
				throw new ArgumentException("Attempting for create a new sz array of invalid rank.", "arrayType");
			}
			if (!context.Global.Parameters.UsingTinyBackend)
			{
				return Cast(context, arrayType, Call("SZArrayNew", metadataAccess.ArrayInfo(unresolvedElementType), length));
			}
			return Cast(context, arrayType, Call("SZArrayNew<" + context.Global.Services.Naming.ForVariable(arrayType) + ">", metadataAccess.ArrayInfo(unresolvedElementType), "sizeof(" + context.Global.Services.Naming.ForVariable(arrayType.ElementType) + ")", length));
		}

		public static string Memset(ReadOnlyContext context, string address, int value, string size)
		{
			return Call("memset", address, value.ToString(), size);
		}

		public static string ArrayElementTypeCheck(string array, string value)
		{
			return $"ArrayElementTypeCheck ({array}, {value});";
		}

		public static string DivideByZeroCheck(TypeReference type, string denominator)
		{
			if (!type.IsIntegralType())
			{
				return string.Empty;
			}
			return $"DivideByZeroCheck({denominator})";
		}

		public static string RaiseManagedException(string exception, string throwingMethodInfo = null)
		{
			if (exception == "NULL")
			{
				return "il2cpp_codegen_raise_null_reference_exception()";
			}
			if (string.IsNullOrEmpty(throwingMethodInfo))
			{
				throwingMethodInfo = "NULL";
			}
			return "IL2CPP_RAISE_MANAGED_EXCEPTION(" + exception + ", " + throwingMethodInfo + ")";
		}

		public static string NullCheck(string name)
		{
			return "NullCheck(" + name + ")";
		}

		public static string MemoryBarrier()
		{
			return "il2cpp_codegen_memory_barrier()";
		}
	}
}
