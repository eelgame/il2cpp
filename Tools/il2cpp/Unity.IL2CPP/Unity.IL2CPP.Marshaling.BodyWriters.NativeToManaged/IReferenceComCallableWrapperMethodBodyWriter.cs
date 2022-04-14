using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	internal class IReferenceComCallableWrapperMethodBodyWriter : ComCallableWrapperMethodBodyWriter
	{
		private enum PropertyType
		{
			Empty = 0,
			UInt8 = 1,
			Int16 = 2,
			UInt16 = 3,
			Int32 = 4,
			UInt32 = 5,
			Int64 = 6,
			UInt64 = 7,
			Single = 8,
			Double = 9,
			Char16 = 10,
			Boolean = 11,
			String = 12,
			Inspectable = 13,
			DateTime = 14,
			TimeSpan = 15,
			Guid = 16,
			Point = 17,
			Size = 18,
			Rect = 19,
			Other = 20,
			UInt8Array = 1025,
			Int16Array = 1026,
			UInt16Array = 1027,
			Int32Array = 1028,
			UInt32Array = 1029,
			Int64Array = 1030,
			UInt64Array = 1031,
			SingleArray = 1032,
			DoubleArray = 1033,
			Char16Array = 1034,
			BooleanArray = 1035,
			StringArray = 1036,
			InspectableArray = 1037,
			DateTimeArray = 1038,
			TimeSpanArray = 1039,
			GuidArray = 1040,
			PointArray = 1041,
			SizeArray = 1042,
			RectArray = 1043,
			OtherArray = 1044
		}

		private readonly TypeReference _boxedType;

		private readonly TypeReference _overflowException;

		private readonly bool _isIPropertyArrayMethod;

		private readonly TypeReference _desiredConvertedType;

		public IReferenceComCallableWrapperMethodBodyWriter(MinimalContext context, MethodReference interfaceMethod, TypeReference boxedType)
			: base(context, interfaceMethod, interfaceMethod, MarshalType.WindowsRuntime)
		{
			_boxedType = boxedType;
			_overflowException = new TypeReference("System", "OverflowException", context.Global.Services.TypeProvider.Corlib.MainModule, context.Global.Services.TypeProvider.Corlib.Name);
			_isIPropertyArrayMethod = IsIPropertyArrayMethod(_managedMethod);
			_desiredConvertedType = (_isIPropertyArrayMethod ? ((ByReferenceType)_managedMethod.Parameters[0].ParameterType).ElementType : _managedMethod.ReturnType);
		}

		public override void WriteMethodBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			if (WillCertainlyThrowException())
			{
				WriteReturnFailedConversion(writer);
			}
			else
			{
				base.WriteMethodBody(writer, metadataAccess);
			}
		}

		protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			switch (_managedMethod.Name)
			{
			case "get_Value":
				WriteGetValueMethod(writer, metadataAccess);
				break;
			case "get_Type":
				WriteGetTypeMethod(writer, metadataAccess);
				break;
			case "get_IsNumericScalar":
				WriteGetIsNumericScalar(writer, metadataAccess);
				break;
			default:
			{
				ManagedMarshalValue resultVariable = (_isIPropertyArrayMethod ? new ManagedMarshalValue(writer.Context, localVariableNames[0]).Dereferenced : new ManagedMarshalValue(writer.Context, writer.Context.Global.Services.Naming.ForInteropReturnValue()));
				WriteGetTypedValueMethod(writer, resultVariable, metadataAccess);
				break;
			}
			}
		}

		private static bool IsIPropertyArrayMethod(MethodReference method)
		{
			switch (method.Name)
			{
			case "get_Value":
			case "get_Type":
			case "get_IsNumericScalar":
			case "GetUInt8":
			case "GetInt16":
			case "GetUInt16":
			case "GetInt32":
			case "GetUInt32":
			case "GetInt64":
			case "GetUInt64":
			case "GetSingle":
			case "GetDouble":
			case "GetChar16":
			case "GetBoolean":
			case "GetString":
			case "GetGuid":
			case "GetDateTime":
			case "GetTimeSpan":
			case "GetPoint":
			case "GetSize":
			case "GetRect":
				return false;
			case "GetUInt8Array":
			case "GetInt16Array":
			case "GetUInt16Array":
			case "GetInt32Array":
			case "GetUInt32Array":
			case "GetInt64Array":
			case "GetUInt64Array":
			case "GetSingleArray":
			case "GetDoubleArray":
			case "GetChar16Array":
			case "GetBooleanArray":
			case "GetStringArray":
			case "GetInspectableArray":
			case "GetGuidArray":
			case "GetDateTimeArray":
			case "GetTimeSpanArray":
			case "GetPointArray":
			case "GetSizeArray":
			case "GetRectArray":
				return true;
			default:
				throw new NotSupportedException("IReferenceComCallableWrapperMethodBodyWriter does not support writing body for " + method.FullName + ".");
			}
		}

		private void WriteGetValueMethod(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine(_context.Global.Services.Naming.ForInteropReturnValue() + " = " + GetUnboxedValueExpression(metadataAccess) + ";");
		}

		private string GetUnboxedValueExpression(IRuntimeMetadataAccess metadataAccess)
		{
			string pointerToValueExpression = GetPointerToValueExpression(metadataAccess);
			if (_boxedType.IsValueType())
			{
				return "*" + pointerToValueExpression;
			}
			return pointerToValueExpression;
		}

		private string GetPointerToValueExpression(IRuntimeMetadataAccess metadataAccess)
		{
			string text = _context.Global.Services.Naming.ForVariable(_boxedType);
			if (_boxedType.IsValueType())
			{
				return "static_cast<" + text + "*>(UnBox(" + ManagedObjectExpression + ", " + metadataAccess.TypeInfoFor(_boxedType) + "))";
			}
			return "static_cast<" + text + ">(" + ManagedObjectExpression + ")";
		}

		private void WriteGetTypeMethod(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine($"{_context.Global.Services.Naming.ForInteropReturnValue()} = {(int)GetBoxedPropertyType(_boxedType)};");
		}

		private void WriteGetIsNumericScalar(ICodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			string text = (IsNumericScalar(_boxedType) ? "true" : "false");
			writer.WriteLine(_context.Global.Services.Naming.ForInteropReturnValue() + " = " + text + ";");
		}

		private static bool IsNumericScalar(TypeReference type)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Byte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.Int64:
			case MetadataType.UInt64:
			case MetadataType.Single:
			case MetadataType.Double:
				return true;
			case MetadataType.ValueType:
				return type.IsEnum();
			default:
				return false;
			}
		}

		private void GetFailedConversionTypeNamesForExceptionMessage(out string fromType, out string toType)
		{
			fromType = GetBoxedPropertyType(_boxedType).ToString();
			if (_desiredConvertedType is ArrayType arrayType)
			{
				TypeReference elementType = arrayType.ElementType;
				string text = (elementType.IsWindowsRuntimePrimitiveType() ? GetDesiredTypeNameInExceptionMessage(elementType) : elementType.Name);
				toType = text + "[]";
			}
			else
			{
				toType = GetDesiredTypeNameInExceptionMessage(_desiredConvertedType).ToString();
			}
		}

		private bool WillCertainlyThrowException()
		{
			switch (_managedMethod.Name)
			{
			case "get_Value":
			case "get_Type":
			case "get_IsNumericScalar":
				return false;
			default:
			{
				TypeReference typeReference = _boxedType;
				if (typeReference.IsEnum())
				{
					typeReference = typeReference.GetUnderlyingEnumType();
				}
				if (TypeReferenceEqualityComparer.AreEqual(_desiredConvertedType, typeReference))
				{
					return false;
				}
				if (_desiredConvertedType.MetadataType == MetadataType.String && _boxedType.FullName == "System.Guid")
				{
					return false;
				}
				if (_desiredConvertedType is ArrayType desiredType && _boxedType is ArrayType && CanConvertArray(desiredType))
				{
					return false;
				}
				if (CanConvertNumber(_desiredConvertedType, typeReference))
				{
					return false;
				}
				if (_desiredConvertedType.Namespace == "System" && _desiredConvertedType.Name == "Guid" && _boxedType.MetadataType == MetadataType.String)
				{
					return false;
				}
				return true;
			}
			}
		}

		private void WriteGetTypedValueMethod(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, IRuntimeMetadataAccess metadataAccess)
		{
			TypeReference typeReference = _boxedType;
			if (typeReference.IsEnum())
			{
				typeReference = typeReference.GetUnderlyingEnumType();
			}
			if (TypeReferenceEqualityComparer.AreEqual(_desiredConvertedType, typeReference))
			{
				writer.WriteLine(resultVariable.Store(GetUnboxedValueExpression(metadataAccess)));
			}
			else if (_desiredConvertedType.MetadataType == MetadataType.String && _boxedType.FullName == "System.Guid")
			{
				MethodDefinition methodDefinition = _boxedType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "ToString" && m.Parameters.Count == 0);
				writer.AddIncludeForMethodDeclaration(methodDefinition);
				WriteAssignMethodCallExpressionToReturnVariable(writer, resultVariable, metadataAccess, methodDefinition, MethodCallType.Normal, GetPointerToValueExpression(metadataAccess));
			}
			else if (_desiredConvertedType is ArrayType arrayType && _boxedType is ArrayType boxedArrayType && CanConvertArray(arrayType))
			{
				WriteConvertArray(writer, resultVariable, metadataAccess, arrayType, boxedArrayType);
			}
			else if (CanConvertNumber(_desiredConvertedType, typeReference))
			{
				WriteConvertNumber(writer, resultVariable, metadataAccess, typeReference);
			}
			else
			{
				if (!(_desiredConvertedType.Namespace == "System") || !(_desiredConvertedType.Name == "Guid") || _boxedType.MetadataType != MetadataType.String)
				{
					throw new InvalidOperationException($"Cannot write conversion from {_boxedType} to {_desiredConvertedType}!");
				}
				WriteGuidParse(writer, resultVariable, GetPointerToValueExpression(metadataAccess), metadataAccess, _desiredConvertedType, delegate(TypeReference exceptionType)
				{
					WriteReturnFailedConversion(writer, exceptionType);
				});
			}
		}

		private static bool CanConvertNumber(TypeReference desiredType, TypeReference boxedUnderlyingType)
		{
			if (!IsNumericScalar(desiredType))
			{
				return false;
			}
			if (!IsNumericScalar(boxedUnderlyingType) && boxedUnderlyingType.MetadataType != MetadataType.String)
			{
				return boxedUnderlyingType.MetadataType == MetadataType.Object;
			}
			return true;
		}

		private static bool CanConvertArray(ArrayType desiredType)
		{
			TypeReference elementType = desiredType.ElementType;
			if (!elementType.IsWindowsRuntimePrimitiveType())
			{
				return false;
			}
			MetadataType metadataType = elementType.MetadataType;
			if (metadataType - 2 <= MetadataType.Void || metadataType == MetadataType.Object)
			{
				return false;
			}
			return true;
		}

		private void WriteAssignMethodCallExpressionToReturnVariable(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, IRuntimeMetadataAccess metadataAccess, MethodReference methodToCall, MethodCallType methodCallType, params string[] args)
		{
			List<string> list = new List<string>();
			list.AddRange(args);
			writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(methodToCall.ReturnType) + " il2cppRetVal;");
			MethodBodyWriter.WriteMethodCallExpression("il2cppRetVal", () => metadataAccess.HiddenMethodInfo(methodToCall), writer, _managedMethod, methodToCall, methodToCall, TypeResolver.Empty, methodCallType, metadataAccess, new VTableBuilder(), list, useArrayBoundsCheck: false);
			writer.WriteLine(resultVariable.Store("il2cppRetVal"));
		}

		private void WriteGuidParse(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, string sourceVariable, IRuntimeMetadataAccess metadataAccess, TypeReference desiredType, Action<TypeReference> translateExceptionAction)
		{
			writer.WriteLine("try");
			using (new BlockWriter(writer))
			{
				MethodDefinition methodDefinition = desiredType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "Parse");
				writer.AddIncludeForMethodDeclaration(methodDefinition);
				WriteAssignMethodCallExpressionToReturnVariable(writer, resultVariable, metadataAccess, methodDefinition, MethodCallType.Normal, sourceVariable);
			}
			writer.WriteLine("catch (const Il2CppExceptionWrapper&)");
			using (new BlockWriter(writer))
			{
				translateExceptionAction(_context.Global.Services.TypeProvider.SystemException);
			}
		}

		private void WriteConvertArray(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, IRuntimeMetadataAccess metadataAccess, ArrayType desiredArrayType, ArrayType boxedArrayType)
		{
			TypeReference desiredElementType = desiredArrayType.ElementType;
			TypeReference boxedUnderlyingElementTypeType = boxedArrayType.ElementType;
			if (boxedUnderlyingElementTypeType.IsEnum())
			{
				boxedUnderlyingElementTypeType = boxedUnderlyingElementTypeType.GetUnderlyingEnumType();
			}
			bool flag = desiredElementType.MetadataType == MetadataType.String && boxedUnderlyingElementTypeType.Namespace == "System" && boxedUnderlyingElementTypeType.Name == "Guid";
			bool flag2 = desiredElementType.Namespace == "System" && desiredElementType.Name == "Guid" && boxedUnderlyingElementTypeType.MetadataType == MetadataType.String;
			writer.WriteLine(_context.Global.Services.Naming.ForVariable(_boxedType) + " managedArray = " + GetPointerToValueExpression(metadataAccess) + ";");
			writer.WriteLine("il2cpp_array_size_t arrayLength = managedArray->max_length;");
			if (!flag2 && !flag && !CanConvertNumber(desiredElementType, boxedUnderlyingElementTypeType))
			{
				writer.WriteLine("if (arrayLength > 0)");
				using (new BlockWriter(writer))
				{
					WriteThrowInvalidCastExceptionForArray(writer, "managedArray", desiredElementType, boxedArrayType.ElementType, "0", metadataAccess, _overflowException);
				}
				writer.WriteLine();
				writer.WriteLine(resultVariable.Store("NULL"));
				return;
			}
			writer.WriteLine(resultVariable.Store(Emit.NewSZArray(_context, desiredArrayType, desiredElementType, "static_cast<uint32_t>(arrayLength)", metadataAccess)));
			writer.WriteLine("for (il2cpp_array_size_t i = 0; i < arrayLength; i++)");
			using (new BlockWriter(writer))
			{
				ManagedMarshalValue resultVariable2 = new ManagedMarshalValue(_context, resultVariable, "i");
				writer.WriteLine(_context.Global.Services.Naming.ForVariable(boxedUnderlyingElementTypeType) + " item = " + Emit.LoadArrayElement("managedArray", "i", useArrayBoundsCheck: false) + ";");
				if (flag)
				{
					MethodDefinition methodDefinition = boxedUnderlyingElementTypeType.Resolve().Methods.Single((MethodDefinition m) => m.Name == "ToString" && m.Parameters.Count == 0);
					writer.AddIncludeForMethodDeclaration(methodDefinition);
					WriteAssignMethodCallExpressionToReturnVariable(writer, resultVariable2, metadataAccess, methodDefinition, MethodCallType.Normal, "&item");
				}
				else if (flag2)
				{
					WriteGuidParse(writer, resultVariable2, "item", metadataAccess, desiredElementType, delegate(TypeReference exceptionType)
					{
						WriteThrowInvalidCastExceptionForArray(writer, "managedArray", desiredElementType, boxedUnderlyingElementTypeType, "i", metadataAccess, exceptionType);
					});
				}
				else
				{
					WriteConvertNumber(writer, resultVariable2, "item", metadataAccess, desiredElementType, boxedUnderlyingElementTypeType, delegate(TypeReference exceptionType)
					{
						WriteThrowInvalidCastExceptionForArray(writer, "managedArray", desiredElementType, boxedUnderlyingElementTypeType, "i", metadataAccess, exceptionType);
					});
				}
			}
		}

		private void WriteConvertNumber(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, IRuntimeMetadataAccess metadataAccess, TypeReference boxedUnderlyingType)
		{
			writer.WriteLine(_context.Global.Services.Naming.ForVariable(boxedUnderlyingType) + " value = " + GetUnboxedValueExpression(metadataAccess) + ";");
			WriteConvertNumber(writer, resultVariable, "value", metadataAccess, _desiredConvertedType, boxedUnderlyingType, delegate(TypeReference exceptionType)
			{
				WriteReturnFailedConversion(writer, exceptionType);
			});
		}

		private void WriteConvertNumber(IGeneratedMethodCodeWriter writer, ManagedMarshalValue resultVariable, string sourceVariable, IRuntimeMetadataAccess metadataAccess, TypeReference desiredType, TypeReference boxedUnderlyingType, Action<TypeReference> translateExceptionAction)
		{
			if (desiredType.IsUnsignedIntegralType() && (boxedUnderlyingType.IsSignedIntegralType() || boxedUnderlyingType.MetadataType == MetadataType.Single || boxedUnderlyingType.MetadataType == MetadataType.Double))
			{
				writer.WriteLine("if (" + sourceVariable + " < 0)");
				using (new BlockWriter(writer))
				{
					translateExceptionAction(_overflowException);
				}
				writer.WriteLine();
			}
			writer.WriteLine("try");
			using (new BlockWriter(writer))
			{
				MethodDefinition methodDefinition = boxedUnderlyingType.Resolve().Methods.SingleOrDefault((MethodDefinition m) => m.Name == $"System.IConvertible.To{desiredType.MetadataType}");
				string text = (boxedUnderlyingType.IsValueType() ? Emit.AddressOf(sourceVariable) : sourceVariable);
				MethodCallType methodCallType;
				if (methodDefinition != null)
				{
					writer.AddIncludeForMethodDeclaration(methodDefinition);
					methodCallType = MethodCallType.Normal;
				}
				else
				{
					methodDefinition = new TypeReference("System", "IConvertible", _context.Global.Services.TypeProvider.Corlib.MainModule, _context.Global.Services.TypeProvider.Corlib.Name).Resolve().Methods.Single((MethodDefinition m) => m.Name == $"To{desiredType.MetadataType}");
					methodCallType = MethodCallType.Virtual;
				}
				WriteAssignMethodCallExpressionToReturnVariable(writer, resultVariable, metadataAccess, methodDefinition, methodCallType, text, "NULL");
			}
			writer.WriteLine("catch (const Il2CppExceptionWrapper& ex)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("if (IsInst((RuntimeObject*)ex.ex, " + metadataAccess.TypeInfoFor(_overflowException) + "))");
				using (new BlockWriter(writer))
				{
					translateExceptionAction(_overflowException);
				}
				writer.WriteLine();
				translateExceptionAction(_context.Global.Services.TypeProvider.SystemException);
			}
		}

		private void WriteReturnFailedConversion(IGeneratedMethodCodeWriter writer)
		{
			GetFailedConversionTypeNamesForExceptionMessage(out var fromType, out var toType);
			writer.WriteStatement("return il2cpp_codegen_com_handle_invalid_iproperty_conversion(" + fromType.InQuotes() + ", " + toType.InQuotes() + ")");
		}

		private void WriteReturnFailedConversion(IGeneratedMethodCodeWriter writer, TypeReference exceptionType)
		{
			if (CanConvertNumber(_desiredConvertedType, _boxedType) && exceptionType == _overflowException)
			{
				GetFailedConversionTypeNamesForExceptionMessage(out var fromType, out var toType);
				string text = Emit.Call("il2cpp_codegen_com_handle_invalid_iproperty_conversion", ManagedObjectExpression, fromType.InQuotes(), toType.InQuotes());
				writer.WriteLine("return " + text + ";");
			}
			WriteReturnFailedConversion(writer);
		}

		private void WriteThrowInvalidCastExceptionForArray(IGeneratedMethodCodeWriter writer, string arrayExpression, TypeReference desiredElementType, TypeReference boxedElementType, string index, IRuntimeMetadataAccess metadataAccess, TypeReference exceptionType)
		{
			string desiredTypeNameInExceptionMessage = GetDesiredTypeNameInExceptionMessage(desiredElementType);
			List<string> list = new List<string>
			{
				GetBoxedPropertyType(_boxedType).ToString().InQuotes(),
				GetBoxedPropertyType(boxedElementType).ToString().InQuotes(),
				desiredTypeNameInExceptionMessage.InQuotes(),
				index
			};
			if (CanConvertNumber(desiredElementType, boxedElementType) && exceptionType == _overflowException)
			{
				list.Insert(0, Emit.LoadArrayElement(arrayExpression, index, useArrayBoundsCheck: false));
				if (boxedElementType.IsValueType())
				{
					list[0] = Emit.Call("Box", metadataAccess.TypeInfoFor(boxedElementType), Emit.LoadArrayElementAddress(arrayExpression, index, useArrayBoundsCheck: false));
				}
			}
			string text = Emit.Call("il2cpp_codegen_com_handle_invalid_ipropertyarray_conversion", list);
			writer.WriteLine("return " + text + ";");
		}

		private static PropertyType GetBoxedPropertyType(TypeReference type)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Byte:
				return PropertyType.UInt8;
			case MetadataType.Int16:
				return PropertyType.Int16;
			case MetadataType.UInt16:
				return PropertyType.UInt16;
			case MetadataType.Int32:
				return PropertyType.Int32;
			case MetadataType.UInt32:
				return PropertyType.UInt32;
			case MetadataType.Int64:
				return PropertyType.Int64;
			case MetadataType.UInt64:
				return PropertyType.UInt64;
			case MetadataType.Single:
				return PropertyType.Single;
			case MetadataType.Double:
				return PropertyType.Double;
			case MetadataType.Char:
				return PropertyType.Char16;
			case MetadataType.Boolean:
				return PropertyType.Boolean;
			case MetadataType.String:
				return PropertyType.String;
			case MetadataType.Object:
				return PropertyType.Inspectable;
			case MetadataType.ValueType:
				switch (type.FullName)
				{
				case "System.Guid":
					return PropertyType.Guid;
				case "System.DateTimeOffset":
					return PropertyType.DateTime;
				case "System.TimeSpan":
					return PropertyType.TimeSpan;
				case "Windows.Foundation.Point":
					return PropertyType.Point;
				case "Windows.Foundation.Size":
					return PropertyType.Size;
				case "Windows.Foundation.Rect":
					return PropertyType.Rect;
				default:
					return PropertyType.Other;
				}
			case MetadataType.Array:
				return GetBoxedPropertyType(((ArrayType)type).ElementType) + 1024;
			default:
				return PropertyType.Other;
			}
		}

		private static string GetDesiredTypeNameInExceptionMessage(TypeReference desiredType)
		{
			if (desiredType.MetadataType == MetadataType.Byte)
			{
				return "Byte";
			}
			return GetBoxedPropertyType(desiredType).ToString();
		}
	}
}
