using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Naming;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP
{
	public class MethodSignatureWriter
	{
		public static string GetICallMethodVariable(ReadOnlyContext context, MethodDefinition method)
		{
			return $"{ICallReturnTypeFor(context, method)} (*{context.Global.Services.Naming.ForMethodNameOnly(method)}_ftn) ({FormatParametersForICall(context, method, ParameterFormat.WithType)})";
		}

		public static string GetMethodPointerForVTable(ReadOnlyContext context, MethodReference method)
		{
			ParameterFormat parameterFormat = ((method.DeclaringType.IsValueType() && method.HasThis) ? ParameterFormat.WithTypeThisObject : ParameterFormat.WithType);
			return GetMethodPointer(context, method, parameterFormat);
		}

		public static string GetMethodPointer(ReadOnlyContext context, MethodReference method, ParameterFormat parameterFormat = ParameterFormat.WithType)
		{
			TypeResolver typeResolver = new TypeResolver(method.DeclaringType as GenericInstanceType, method as GenericInstanceMethod);
			return GetMethodSignature("(*)", FormatReturnType(context, typeResolver.ResolveReturnType(method)), FormatParameters(context, method, parameterFormat, includeHiddenMethodInfo: true), string.Empty);
		}

		internal static string GetMethodSignature(MethodWriteContext context, IGeneratedCodeWriter writer, bool forMethodDefinition)
		{
			MethodReference methodReference = context.MethodReference;
			RecordIncludes(writer, methodReference, context.TypeResolver);
			string attributes = BuildMethodAttributes(methodReference);
			bool includeHiddenMethodInfo = NeedsHiddenMethodInfo(writer.Context, methodReference, MethodCallType.Normal, forMethodDefinition);
			return GetMethodSignature(writer.Context.Global.Services.Naming.ForMethodNameOnly(context.MethodReference), FormatReturnType(writer.Context, context.ResolvedReturnType), FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo), "IL2CPP_EXTERN_C", attributes);
		}

		internal static string GetInlineMethodSignature(MethodWriteContext context, IGeneratedCodeWriter writer)
		{
			MethodReference methodReference = context.MethodReference;
			RecordIncludes(writer, methodReference, context.TypeResolver);
			string attributes = BuildMethodAttributes(methodReference);
			bool includeHiddenMethodInfo = NeedsHiddenMethodInfo(writer.Context, methodReference, MethodCallType.Normal);
			return GetMethodSignature(context.Global.Services.Naming.ForMethodNameOnly(methodReference) + "_inline", FormatReturnType(writer.Context, context.ResolvedReturnType), FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo), "IL2CPP_MANAGED_FORCE_INLINE", attributes);
		}

		internal static string GetMethodSignatureRaw(MethodWriteContext context)
		{
			MethodReference methodReference = context.MethodReference;
			string attributes = BuildMethodAttributes(methodReference);
			bool includeHiddenMethodInfo = NeedsHiddenMethodInfo(context, methodReference, MethodCallType.Normal);
			return GetMethodSignature(context.Global.Services.Naming.ForMethodNameOnly(methodReference), FormatReturnType(context, context.ResolvedReturnType), FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo), "IL2CPP_EXTERN_C", attributes);
		}

		internal static string GetMethodSignatureRawInline(MethodWriteContext context)
		{
			MethodReference methodReference = context.MethodReference;
			string attributes = BuildMethodAttributes(methodReference);
			bool includeHiddenMethodInfo = NeedsHiddenMethodInfo(context, methodReference, MethodCallType.Normal);
			return GetMethodSignature(context.Global.Services.Naming.ForMethodNameOnly(methodReference) + "_inline", FormatReturnType(context, context.ResolvedReturnType), FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo), "IL2CPP_MANAGED_FORCE_INLINE", attributes);
		}

		public static string GetSharedMethodSignature(MethodWriteContext context, IGeneratedCodeWriter writer)
		{
			MethodReference methodReference = context.MethodReference;
			RecordIncludes(writer, methodReference, context.TypeResolver);
			string attributes = BuildMethodAttributes(methodReference);
			return GetMethodSignature(writer.Context.Global.Services.Naming.ForMethodNameOnly(methodReference) + "_gshared", FormatReturnType(context, context.ResolvedReturnType), FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo: true), "IL2CPP_EXTERN_C", attributes);
		}

		public static string GetSharedMethodSignatureInline(MethodWriteContext context, IGeneratedCodeWriter writer)
		{
			MethodReference methodReference = context.MethodReference;
			RecordIncludes(writer, methodReference, context.TypeResolver);
			string attributes = BuildMethodAttributes(methodReference);
			return GetMethodSignature(context.Global.Services.Naming.ForMethodNameOnly(methodReference) + "_gshared_inline", FormatReturnType(context, context.ResolvedReturnType), FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo: true), "IL2CPP_MANAGED_FORCE_INLINE", attributes);
		}

		public static string GetSharedMethodSignatureRaw(MethodWriteContext context)
		{
			MethodReference methodReference = context.MethodReference;
			string attributes = BuildMethodAttributes(methodReference);
			return GetMethodSignature(context.Global.Services.Naming.ForMethodNameOnly(methodReference) + "_gshared", FormatReturnType(context, context.ResolvedReturnType), FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo: true), "IL2CPP_EXTERN_C", attributes);
		}

		public static string GetSharedMethodSignatureRawInline(MethodWriteContext context)
		{
			MethodReference methodReference = context.MethodReference;
			string attributes = BuildMethodAttributes(methodReference);
			return GetMethodSignature(context.Global.Services.Naming.ForMethodNameOnly(methodReference) + "_gshared_inline", FormatReturnType(context, context.ResolvedReturnType), FormatParameters(context, methodReference, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo: true), "IL2CPP_MANAGED_FORCE_INLINE", attributes);
		}

		public static string FormatReturnType(ReadOnlyContext context, TypeReference managedReturnType)
		{
			if (context.Global.Parameters.ReturnAsByRefParameter || managedReturnType.IsVoid())
			{
				return context.Global.Services.Naming.ForVariable(context.Global.Services.TypeProvider.SystemVoid);
			}
			return context.Global.Services.Naming.ForVariable(managedReturnType);
		}

		public static string FormatParametersForICall(ReadOnlyContext context, MethodReference method, ParameterFormat format = ParameterFormat.WithTypeAndName)
		{
			List<string> list = ParametersForInternal(context, method, format, includeHiddenMethodInfo: false, useVoidPointerForThis: false, returnAsByRefParam: false).ToList();
			if (list.Count != 0)
			{
				return list.AggregateWithComma();
			}
			return string.Empty;
		}

		public static string FormatParameters(ReadOnlyContext context, MethodReference method, ParameterFormat format = ParameterFormat.WithTypeAndName, bool includeHiddenMethodInfo = false)
		{
			List<string> list = ParametersForInternal(context, method, format, includeHiddenMethodInfo, useVoidPointerForThis: false, context.Global.Parameters.ReturnAsByRefParameter).ToList();
			if (list.Count != 0)
			{
				return list.AggregateWithComma();
			}
			return string.Empty;
		}

		public static IEnumerable<string> ParametersForICall(ReadOnlyContext context, MethodReference methodDefinition, ParameterFormat format = ParameterFormat.WithTypeAndName)
		{
			return ParametersForInternal(context, methodDefinition, format, includeHiddenMethodInfo: false, useVoidPointerForThis: false, returnAsByRefParam: false);
		}

		public static IEnumerable<string> ParametersFor(ReadOnlyContext context, MethodReference methodDefinition, ParameterFormat format = ParameterFormat.WithTypeAndName, bool includeHiddenMethodInfo = false, bool useVoidPointerForThis = false)
		{
			return ParametersForInternal(context, methodDefinition, format, includeHiddenMethodInfo, useVoidPointerForThis, context.Global.Parameters.ReturnAsByRefParameter);
		}

		private static IEnumerable<string> ParametersForInternal(ReadOnlyContext context, MethodReference methodDefinition, ParameterFormat format, bool includeHiddenMethodInfo, bool useVoidPointerForThis, bool returnAsByRefParam)
		{
			switch (format)
			{
			case ParameterFormat.WithTypeAndNameThisObject:
			{
				TypeReference thisType = ThisTypeFor(methodDefinition);
				if (useVoidPointerForThis)
				{
					yield return FormatThisParameterAsVoidPointer(thisType);
				}
				else
				{
					yield return FormatParameterName(context, methodDefinition.Module.TypeSystem.Object, "__this", format, IsJobStruct(thisType));
				}
				break;
			}
			case ParameterFormat.WithTypeThisObject:
				yield return useVoidPointerForThis ? "void*" : context.Global.Services.Naming.ForVariable(context.Global.Services.TypeProvider.ObjectTypeReference);
				break;
			default:
				if (methodDefinition.HasThis)
				{
					yield return FormatThis(context, format, ThisTypeFor(methodDefinition));
				}
				break;
			case ParameterFormat.WithTypeAndNameNoThis:
			case ParameterFormat.WithNameNoThis:
				break;
			}
			foreach (ParameterDefinition parameter in methodDefinition.Resolve().Parameters)
			{
				yield return ParameterStringFor(context, methodDefinition, format, parameter, !parameter.ParameterType.IsValueType() && !parameter.ParameterType.IsPointer);
			}
			if (returnAsByRefParam)
			{
				TypeReference typeReference = TypeResolver.For(methodDefinition.DeclaringType as GenericInstanceType, methodDefinition as GenericInstanceMethod).Resolve(GenericParameterResolver.ResolveReturnTypeIfNeeded(methodDefinition));
				if (typeReference.IsNotVoid())
				{
					yield return FormatParameterName(context, typeReference.MakePointerType(), "il2cppRetVal", format);
				}
			}
			if (includeHiddenMethodInfo)
			{
				yield return FormatHiddenMethodArgument(format);
			}
		}

		private static TypeReference ThisTypeFor(MethodReference methodDefinition)
		{
			TypeReference typeReference = methodDefinition.DeclaringType;
			if (typeReference.IsValueType())
			{
				typeReference = new PointerType(typeReference);
			}
			else if (typeReference.IsSpecialSystemBaseType())
			{
				typeReference = methodDefinition.Module.TypeSystem.Object;
			}
			return typeReference;
		}

		public static string ICallReturnTypeFor(ReadOnlyContext context, MethodDefinition method)
		{
			return context.Global.Services.Naming.ForVariable(method.ReturnType);
		}

		private static string FormatMonoErrorForICall(ParameterFormat format)
		{
			switch (format)
			{
			case ParameterFormat.WithTypeAndName:
			case ParameterFormat.WithTypeAndNameNoThis:
			case ParameterFormat.WithTypeAndNameThisObject:
				return "MonoError* error_icall";
			case ParameterFormat.WithType:
			case ParameterFormat.WithTypeThisObject:
				return "MonoError*";
			case ParameterFormat.WithName:
				return "&error_icall";
			default:
				throw new ArgumentOutOfRangeException("format");
			}
		}

		private static string FormatHiddenMethodArgument(ParameterFormat format)
		{
			switch (format)
			{
			case ParameterFormat.WithTypeAndName:
			case ParameterFormat.WithTypeAndNameNoThis:
			case ParameterFormat.WithTypeAndNameThisObject:
				return "const RuntimeMethod* method";
			case ParameterFormat.WithType:
			case ParameterFormat.WithTypeThisObject:
				return "const RuntimeMethod*";
			case ParameterFormat.WithName:
			case ParameterFormat.WithNameNoThis:
				return "method";
			default:
				throw new ArgumentOutOfRangeException("format");
			}
		}

		public static string ParameterStringFor(ReadOnlyContext context, MethodReference methodDefinition, ParameterFormat format, ParameterDefinition parameterDefinition, bool useHandles = false)
		{
			TypeResolver typeResolver = TypeResolver.For(methodDefinition.DeclaringType);
			return FormatParameterName(context, typeResolver.Resolve(GenericParameterResolver.ResolveParameterTypeIfNeeded(methodDefinition, parameterDefinition)), context.Global.Services.Naming.ForParameterName(parameterDefinition), format);
		}

		public static string BuildMethodAttributes(MethodReference method)
		{
			List<string> list = new List<string>();
			MethodDefinition methodDefinition = method.Resolve();
			if (methodDefinition.NoInlining || IsJobStruct(ThisTypeFor(methodDefinition)))
			{
				list.Add("IL2CPP_NO_INLINE");
			}
			list.Add("IL2CPP_METHOD_ATTR");
			return list.AggregateWithSpace();
		}

		internal static string GetMethodSignature(string name, string returnType, string parameters, string specifiers = "", string attributes = "")
		{
			return specifiers + " " + attributes + " " + returnType + " " + name + " (" + parameters + ")";
		}

		internal static void RecordIncludes(IGeneratedCodeWriter writer, MethodReference method)
		{
			TypeResolver typeResolver = new TypeResolver(method.DeclaringType as GenericInstanceType, method as GenericInstanceMethod);
			if (method.HasThis)
			{
				writer.AddIncludesForTypeReference(method.DeclaringType.IsComOrWindowsRuntimeInterface(writer.Context) ? writer.Context.Global.Services.TypeProvider.SystemObject : method.DeclaringType);
			}
			if (method.ReturnType.IsNotVoid())
			{
				writer.AddIncludesForTypeReference(typeResolver.ResolveReturnType(method));
			}
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				writer.AddIncludesForTypeReference(typeResolver.ResolveParameterType(method, parameter), requiresCompleteType: true);
			}
		}

		internal static void RecordIncludes(IGeneratedCodeWriter writer, MethodReference method, TypeResolver typeResolver)
		{
			if (method.HasThis)
			{
				writer.AddIncludesForTypeReference(method.DeclaringType.IsComOrWindowsRuntimeInterface(writer.Context) ? writer.Context.Global.Services.TypeProvider.SystemObject : method.DeclaringType);
			}
			if (method.ReturnType.IsNotVoid())
			{
				writer.AddIncludesForTypeReference(typeResolver.ResolveReturnType(method));
			}
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				writer.AddIncludesForTypeReference(typeResolver.ResolveParameterType(method, parameter), requiresCompleteType: true);
			}
			if (writer.Context.Global.Parameters.UsingTinyBackend)
			{
				writer.AddInclude("StringLiteralsOffsets.h");
			}
		}

		private static string FormatThis(ReadOnlyContext context, ParameterFormat format, TypeReference thisType)
		{
			return FormatParameterName(context, thisType, "__this", format, IsJobStruct(thisType));
		}

		private static string FormatThisParameterAsVoidPointer(TypeReference thisType)
		{
			string text = (IsJobStruct(thisType) ? "IL2CPP_PARAMETER_RESTRICT " : string.Empty);
			return "void* " + text + "__this";
		}

		private static string FormatParameterName(ReadOnlyContext context, TypeReference parameterType, string parameterName, ParameterFormat format, bool addRestrictModifier = false)
		{
			string text = string.Empty;
			if (format == ParameterFormat.WithTypeAndName || format == ParameterFormat.WithTypeAndNameNoThis || format == ParameterFormat.WithType || format == ParameterFormat.WithTypeAndNameThisObject || format == ParameterFormat.WithTypeThisObject)
			{
				text += context.Global.Services.Naming.ForVariable(parameterType);
				if (addRestrictModifier)
				{
					text += " IL2CPP_PARAMETER_RESTRICT";
				}
			}
			if (format == ParameterFormat.WithTypeAndName || format == ParameterFormat.WithTypeAndNameNoThis || format == ParameterFormat.WithTypeAndNameThisObject)
			{
				text += " ";
			}
			if (format == ParameterFormat.WithTypeAndName || format == ParameterFormat.WithTypeAndNameNoThis || format == ParameterFormat.WithName || format == ParameterFormat.WithTypeAndNameThisObject || format == ParameterFormat.WithNameNoThis)
			{
				text += parameterName;
			}
			return text;
		}

		public static bool CanDevirtualizeMethodCall(MethodDefinition method)
		{
			if (method.IsVirtual && !method.DeclaringType.IsSealed)
			{
				return method.IsFinal;
			}
			return true;
		}

		public static bool NeedsHiddenMethodInfo(ReadOnlyContext context, MethodReference method, MethodCallType callType, bool forMethodDefinition = false)
		{
			if (context.Global.Parameters.UsingTinyBackend)
			{
				return false;
			}
			if (!forMethodDefinition && IntrinsicRemap.ShouldRemap(context, method) && !IntrinsicRemap.StillNeedsHiddenMethodInfo(context, method))
			{
				return false;
			}
			if (method.DeclaringType.IsArray && (method.Name == ".ctor" || method.Name == "Set" || method.Name == "Get" || method.Name == "Address"))
			{
				return false;
			}
			if (method.DeclaringType.IsSystemArray() && (method.Name == "GetGenericValueImpl" || method.Name == "SetGenericValueImpl"))
			{
				return false;
			}
			if (GenericsUtilities.IsGenericInstanceOfCompareExchange(method))
			{
				return false;
			}
			if (GenericsUtilities.IsGenericInstanceOfExchange(method))
			{
				return false;
			}
			if (callType == MethodCallType.Virtual && !CanDevirtualizeMethodCall(method.Resolve()))
			{
				return false;
			}
			return true;
		}

		internal static string FormatProjectedComCallableWrapperMethodDeclaration(SourceWritingContext context, MethodReference interfaceMethod, TypeResolver typeResolver, MarshalType marshalType)
		{
			string text = FormatComMethodParameterList(context, interfaceMethod, interfaceMethod, typeResolver, marshalType, includeTypeNames: true, preserveSig: false);
			string text2 = context.Global.Services.Naming.ForComCallableWrapperProjectedMethod(interfaceMethod);
			string text3 = context.Global.Services.Naming.ForVariable(interfaceMethod.DeclaringType);
			if (string.IsNullOrEmpty(text))
			{
				return "il2cpp_hresult_t " + text2 + "(" + text3 + " __this)";
			}
			return "il2cpp_hresult_t " + text2 + "(" + text3 + " __this, " + text + ")";
		}

		internal static string FormatComMethodParameterList(ReadOnlyContext context, MethodReference interopMethod, MethodReference interfaceMethod, TypeResolver typeResolver, MarshalType marshalType, bool includeTypeNames, bool preserveSig)
		{
			List<string> list = new List<string>();
			int num = 0;
			foreach (ParameterDefinition parameter in interopMethod.Parameters)
			{
				MarshalInfo marshalInfo = interfaceMethod.Parameters[num].MarshalInfo;
				TypeReference type = typeResolver.Resolve(parameter.ParameterType);
				MarshaledType[] marshaledTypes = MarshalDataCollector.MarshalInfoWriterFor(context, type, marshalType, marshalInfo, useUnicodeCharSet: true).MarshaledTypes;
				foreach (MarshaledType marshaledType in marshaledTypes)
				{
					list.Add(string.Format(includeTypeNames ? "{0} {1}" : "{1}", marshaledType.DecoratedName, context.Global.Services.Naming.ForParameterName(parameter) + marshaledType.VariableName));
				}
				num++;
			}
			TypeReference type2 = typeResolver.Resolve(interopMethod.ReturnType);
			if (type2.IsNotVoid())
			{
				MarshalInfo marshalInfo2 = interfaceMethod.MethodReturnType.MarshalInfo;
				MarshaledType[] marshaledTypes2 = MarshalDataCollector.MarshalInfoWriterFor(context, type2, marshalType, marshalInfo2, useUnicodeCharSet: true).MarshaledTypes;
				for (int j = 0; j < marshaledTypes2.Length - 1; j++)
				{
					list.Add(string.Format(includeTypeNames ? "{0}* {1}" : "{1}", marshaledTypes2[j].DecoratedName, context.Global.Services.Naming.ForComInterfaceReturnParameterName() + marshaledTypes2[j].VariableName));
				}
				if (!preserveSig)
				{
					list.Add(string.Format(includeTypeNames ? "{0}* {1}" : "{1}", marshaledTypes2[marshaledTypes2.Length - 1].DecoratedName, context.Global.Services.Naming.ForComInterfaceReturnParameterName()));
				}
			}
			return list.AggregateWithComma();
		}

		private static bool IsJobStruct(TypeReference thisType)
		{
			TypeDefinition typeDefinition = thisType.Resolve();
			if (typeDefinition.HasAttribute("Unity.Jobs.LowLevel.Unsafe.JobProducerTypeAttribute"))
			{
				return true;
			}
			foreach (InterfaceImplementation @interface in typeDefinition.Interfaces)
			{
				if (@interface.InterfaceType.HasAttribute("Unity.Jobs.LowLevel.Unsafe.JobProducerTypeAttribute"))
				{
					return true;
				}
			}
			return false;
		}
	}
}
