using System;
using System.Globalization;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Results;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public sealed class SharedRuntimeMetadataAccess : IRuntimeMetadataAccess
	{
		private readonly SourceWritingContext _context;

		private readonly MethodReference _enclosingMethod;

		private readonly TypeResolver _typeResolver;

		private readonly DefaultRuntimeMetadataAccess _default;

		private readonly GenericSharingAnalysisResults _genericSharingAnalysis;

		public SharedRuntimeMetadataAccess(SourceWritingContext context, MethodReference enclosingMethod, DefaultRuntimeMetadataAccess defaultRuntimeMetadataAccess)
		{
			_context = context;
			_enclosingMethod = enclosingMethod;
			_typeResolver = new TypeResolver(enclosingMethod.DeclaringType as GenericInstanceType, enclosingMethod as GenericInstanceMethod);
			_default = defaultRuntimeMetadataAccess;
			_genericSharingAnalysis = context.Global.Results.PrimaryCollection.GenericSharingAnalysis;
		}

		public string StaticData(TypeReference type)
		{
			return RetrieveType(type, _enclosingMethod, () => _default.StaticData(type), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", GetTypeRgctxDataExpression(), index.ToString(CultureInfo.InvariantCulture)), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", "method->rgctx_data", index.ToString(CultureInfo.InvariantCulture)), RuntimeGenericContextInfo.Static, _genericSharingAnalysis);
		}

		public string TypeInfoFor(TypeReference type)
		{
			return RetrieveType(type, _enclosingMethod, () => _default.TypeInfoFor(type), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", GetTypeRgctxDataExpression(), index.ToString(CultureInfo.InvariantCulture)), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", "method->rgctx_data", index.ToString(CultureInfo.InvariantCulture)), RuntimeGenericContextInfo.Class, _genericSharingAnalysis);
		}

		public string UnresolvedTypeInfoFor(TypeReference type)
		{
			return RetrieveType(type, _enclosingMethod, () => _default.UnresolvedTypeInfoFor(type), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", GetTypeRgctxDataExpression(), index.ToString(CultureInfo.InvariantCulture)), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", "method->rgctx_data", index.ToString(CultureInfo.InvariantCulture)), RuntimeGenericContextInfo.Class, _genericSharingAnalysis);
		}

		public string Il2CppTypeFor(TypeReference type)
		{
			return RetrieveType(type, _enclosingMethod, () => _default.Il2CppTypeFor(type), (int index) => Emit.Call("IL2CPP_RGCTX_TYPE", GetTypeRgctxDataExpression(), index.ToString(CultureInfo.InvariantCulture)), (int index) => Emit.Call("IL2CPP_RGCTX_TYPE", "method->rgctx_data", index.ToString(CultureInfo.InvariantCulture)), RuntimeGenericContextInfo.Type, _genericSharingAnalysis);
		}

		public string ArrayInfo(TypeReference elementType)
		{
			return RetrieveType(elementType, _enclosingMethod, () => _default.ArrayInfo(elementType), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", GetTypeRgctxDataExpression(), index.ToString(CultureInfo.InvariantCulture)), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", "method->rgctx_data", index.ToString(CultureInfo.InvariantCulture)), RuntimeGenericContextInfo.Array, _genericSharingAnalysis);
		}

		public string Newobj(MethodReference ctor)
		{
			return RetrieveType(ctor.DeclaringType, _enclosingMethod, () => _default.Newobj(ctor), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", GetTypeRgctxDataExpression(), index.ToString(CultureInfo.InvariantCulture)), (int index) => Emit.Call("IL2CPP_RGCTX_DATA", "method->rgctx_data", index.ToString(CultureInfo.InvariantCulture)), RuntimeGenericContextInfo.Class, _genericSharingAnalysis);
		}

		private string GetTypeRgctxDataExpression()
		{
			string text = "method->klass";
			if (!_enclosingMethod.HasThis || _enclosingMethod.DeclaringType.IsValueType())
			{
				text = Emit.InitializedTypeInfo(text);
			}
			return $"{text}->rgctx_data";
		}

		public string Method(MethodReference method)
		{
			MethodReference methodReference = _typeResolver.Resolve(method);
			return RetrieveMethod(method, _enclosingMethod, () => _default.Method(method), (int index) => "(" + Emit.Cast(MethodSignatureWriter.GetMethodPointerForVTable(_context, methodReference), Emit.Call("IL2CPP_RGCTX_METHOD_INFO", GetTypeRgctxDataExpression(), index.ToString(CultureInfo.InvariantCulture)) + "->methodPointer") + ")", (int index) => "(" + Emit.Cast(MethodSignatureWriter.GetMethodPointerForVTable(_context, methodReference), Emit.Call("IL2CPP_RGCTX_METHOD_INFO", "method->rgctx_data", index.ToString(CultureInfo.InvariantCulture)) + "->methodPointer") + ")", RuntimeGenericContextInfo.Method, _genericSharingAnalysis);
		}

		public bool NeedsBoxingForValueTypeThis(MethodReference method)
		{
			return RetrieveMethod(method, _enclosingMethod, () => false, (int index) => true, (int index) => true, RuntimeGenericContextInfo.Method, _genericSharingAnalysis);
		}

		public void StartInitMetadataInline()
		{
			_default.StartInitMetadataInline();
		}

		public void EndInitMetadataInline()
		{
			_default.EndInitMetadataInline();
		}

		public string FieldInfo(FieldReference field)
		{
			if (GetRGCTXAccess(field.DeclaringType, _enclosingMethod, _genericSharingAnalysis) == RuntimeGenericAccess.None)
			{
				return _default.FieldInfo(field);
			}
			string arg = TypeInfoFor(field.DeclaringType);
			return $"IL2CPP_RGCTX_FIELD_INFO({arg},{_context.Global.Services.Naming.GetFieldIndex(field)})";
		}

		public string MethodInfo(MethodReference method)
		{
			return RetrieveMethod(method, _enclosingMethod, () => _default.MethodInfo(method), (int index) => Emit.Call("IL2CPP_RGCTX_METHOD_INFO", GetTypeRgctxDataExpression(), index.ToString(CultureInfo.InvariantCulture)), (int index) => Emit.Call("IL2CPP_RGCTX_METHOD_INFO", "method->rgctx_data", index.ToString(CultureInfo.InvariantCulture)), RuntimeGenericContextInfo.Method, _genericSharingAnalysis);
		}

		public string HiddenMethodInfo(MethodReference method)
		{
			return RetrieveMethod(method, _enclosingMethod, () => _default.HiddenMethodInfo(method), (int index) => Emit.Call("IL2CPP_RGCTX_METHOD_INFO", GetTypeRgctxDataExpression(), index.ToString(CultureInfo.InvariantCulture)), (int index) => Emit.Call("IL2CPP_RGCTX_METHOD_INFO", "method->rgctx_data", index.ToString(CultureInfo.InvariantCulture)), RuntimeGenericContextInfo.Method, _genericSharingAnalysis);
		}

		public string StringLiteral(string literal)
		{
			return StringLiteral(literal, default(MetadataToken), _context.Global.Services.TypeProvider.Corlib);
		}

		public string StringLiteral(string literal, MetadataToken token, AssemblyDefinition assemblyDefinition)
		{
			return _default.StringLiteral(literal, token, assemblyDefinition);
		}

		public static T RetrieveMethod<T>(MethodReference method, MethodReference enclosingMethod, Func<T> defaultFunc, Func<int, T> retrieveTypeSharedAccess, Func<int, T> retrieveMethodSharedAccess, RuntimeGenericContextInfo info, GenericSharingAnalysisResults genericSharingAnalysisService)
		{
			switch (GetRGCTXAccess(method, enclosingMethod, genericSharingAnalysisService))
			{
			case RuntimeGenericAccess.None:
				return defaultFunc();
			case RuntimeGenericAccess.Method:
			{
				GenericSharingData rgctx2 = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.Resolve());
				int num2 = RetrieveMethodIndex(method, info, rgctx2);
				if (num2 == -1)
				{
					throw new InvalidOperationException(FormatGenericContextErrorMessage(method.FullName));
				}
				return retrieveMethodSharedAccess(num2);
			}
			case RuntimeGenericAccess.This:
			case RuntimeGenericAccess.Type:
			{
				GenericSharingData rgctx = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.DeclaringType.Resolve());
				int num = RetrieveMethodIndex(method, info, rgctx);
				if (num == -1)
				{
					throw new InvalidOperationException(FormatGenericContextErrorMessage(method.FullName));
				}
				return retrieveTypeSharedAccess(num);
			}
			default:
				throw new ArgumentOutOfRangeException("method");
			}
		}

		public static string RetrieveType(TypeReference type, MethodReference enclosingMethod, Func<string> defaultFunc, Func<int, string> retrieveTypeSharedAccess, Func<int, string> retrieveMethodSharedAccess, RuntimeGenericContextInfo info, GenericSharingAnalysisResults genericSharingAnalysisService)
		{
			switch (GetRGCTXAccess(type, enclosingMethod, genericSharingAnalysisService))
			{
			case RuntimeGenericAccess.None:
				return defaultFunc();
			case RuntimeGenericAccess.Method:
			{
				GenericSharingData rgctx2 = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.Resolve());
				int num2 = RetrieveTypeIndex(type, info, rgctx2);
				if (num2 == -1)
				{
					throw new InvalidOperationException(FormatGenericContextErrorMessage(type.FullName));
				}
				return retrieveMethodSharedAccess(num2);
			}
			case RuntimeGenericAccess.This:
			case RuntimeGenericAccess.Type:
			{
				GenericSharingData rgctx = genericSharingAnalysisService.RuntimeGenericContextFor(enclosingMethod.DeclaringType.Resolve());
				int num = RetrieveTypeIndex(type, info, rgctx);
				if (num == -1)
				{
					throw new InvalidOperationException(FormatGenericContextErrorMessage(type.FullName));
				}
				return retrieveTypeSharedAccess(num);
			}
			default:
				throw new ArgumentOutOfRangeException("type");
			}
		}

		public static RuntimeGenericAccess GetRGCTXAccess(TypeReference type, MethodReference enclosingMethod, GenericSharingAnalysisResults genericSharingAnalysisService)
		{
			switch (GenericSharingVisitor.GenericUsageFor(type))
			{
			case GenericContextUsage.None:
				return RuntimeGenericAccess.None;
			case GenericContextUsage.Type:
				if (GenericSharingAnalysis.NeedsTypeContextAsArgument(enclosingMethod))
				{
					return RuntimeGenericAccess.Type;
				}
				return RuntimeGenericAccess.This;
			case GenericContextUsage.Method:
			case GenericContextUsage.Both:
				return RuntimeGenericAccess.Method;
			default:
				throw new ArgumentOutOfRangeException("type");
			}
		}

		public static RuntimeGenericAccess GetRGCTXAccess(MethodReference method, MethodReference enclosingMethod, GenericSharingAnalysisResults genericSharingAnalysisService)
		{
			switch (GenericSharingVisitor.GenericUsageFor(method))
			{
			case GenericContextUsage.None:
				return RuntimeGenericAccess.None;
			case GenericContextUsage.Type:
				if (GenericSharingAnalysis.NeedsTypeContextAsArgument(enclosingMethod))
				{
					return RuntimeGenericAccess.Type;
				}
				return RuntimeGenericAccess.This;
			case GenericContextUsage.Method:
			case GenericContextUsage.Both:
				return RuntimeGenericAccess.Method;
			default:
				throw new ArgumentOutOfRangeException("method");
			}
		}

		public static int RetrieveTypeIndex(TypeReference type, RuntimeGenericContextInfo info, GenericSharingData rgctx)
		{
			int result = -1;
			for (int i = 0; i < rgctx.RuntimeGenericDatas.Count; i++)
			{
				RuntimeGenericData runtimeGenericData = rgctx.RuntimeGenericDatas[i];
				if (runtimeGenericData.InfoType == info)
				{
					RuntimeGenericTypeData runtimeGenericTypeData = (RuntimeGenericTypeData)runtimeGenericData;
					if (runtimeGenericTypeData.GenericType != null && TypeReferenceEqualityComparer.AreEqual(runtimeGenericTypeData.GenericType, type))
					{
						result = i;
						break;
					}
				}
			}
			return result;
		}

		public static int RetrieveMethodIndex(MethodReference method, RuntimeGenericContextInfo info, GenericSharingData rgctx)
		{
			int result = -1;
			for (int i = 0; i < rgctx.RuntimeGenericDatas.Count; i++)
			{
				RuntimeGenericData runtimeGenericData = rgctx.RuntimeGenericDatas[i];
				if (runtimeGenericData.InfoType == info)
				{
					RuntimeGenericMethodData runtimeGenericMethodData = (RuntimeGenericMethodData)runtimeGenericData;
					if (runtimeGenericMethodData.GenericMethod != null && MethodReferenceComparer.AreEqual(runtimeGenericMethodData.GenericMethod, method))
					{
						result = i;
						break;
					}
				}
			}
			return result;
		}

		private static string FormatGenericContextErrorMessage(string name)
		{
			return $"Unable to retrieve the runtime generic context for '{name}'.";
		}
	}
}
