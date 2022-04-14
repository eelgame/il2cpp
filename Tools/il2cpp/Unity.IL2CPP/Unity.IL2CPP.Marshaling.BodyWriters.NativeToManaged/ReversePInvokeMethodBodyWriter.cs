using System;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	internal class ReversePInvokeMethodBodyWriter : NativeToManagedInteropMethodBodyWriter, IReversePInvokeMethodBodyWriter
	{
		protected ReversePInvokeMethodBodyWriter(MinimalContext context, MethodReference managedMethod, MethodReference interopMethod, bool useUnicodeCharset)
			: base(context, managedMethod, interopMethod, MarshalType.PInvoke, useUnicodeCharset)
		{
		}

		public static IReversePInvokeMethodBodyWriter Create(MinimalContext context, MethodReference managedMethod)
		{
			if (IsReversePInvokeWrapperNecessary(context, managedMethod))
			{
				MethodReference interopMethod = GetInteropMethod(managedMethod);
				bool useUnicodeCharset = MarshalingUtils.UseUnicodeAsDefaultMarshalingForStringParameters(interopMethod);
				return new ReversePInvokeMethodBodyWriter(context, managedMethod, interopMethod, useUnicodeCharset);
			}
			if (context.Global.Parameters.EmitReversePInvokeWrapperDebuggingHelpers)
			{
				return new ReversePInvokeNotImplementedMethodBodyWriter(managedMethod);
			}
			throw new InvalidOperationException("Attempting create a reverse p/invoke wrapper for method '" + managedMethod.FullName + "' when it cannot be implemented.");
		}

		public static bool IsReversePInvokeWrapperNecessary(ReadOnlyContext context, MethodReference method)
		{
			return WhyReversePInvokeWrapperCannotBeImplemented(context, method) == ReversePInvokeWrapperNotImplementedReason.None;
		}

		public static bool IsReversePInvokeMethodThatMustBeGenerated(MethodReference method)
		{
			if (!method.IsGenericInstance)
			{
				return !method.DeclaringType.IsGenericInstance;
			}
			return false;
		}

		internal static ReversePInvokeWrapperNotImplementedReason WhyReversePInvokeWrapperCannotBeImplemented(ReadOnlyContext context, MethodReference method)
		{
			if (method.HasThis)
			{
				return ReversePInvokeWrapperNotImplementedReason.IsInstanceMethod;
			}
			if (method.HasGenericParameters)
			{
				return ReversePInvokeWrapperNotImplementedReason.HasGenericParameters;
			}
			if (method.Parameters.Any(ParameterIsGenericInstance))
			{
				return ReversePInvokeWrapperNotImplementedReason.AtLeastOneParameterIsGenericInstance;
			}
			if (IntrinsicRemap.ShouldRemap(context, method))
			{
				return ReversePInvokeWrapperNotImplementedReason.IsIntrinsicRemap;
			}
			MethodDefinition methodDefinition = method.Resolve();
			if (GetMonoPInvokeCallbackAttribute(methodDefinition) != null)
			{
				return ReversePInvokeWrapperNotImplementedReason.None;
			}
			if (GetNativePInvokeCallbackAttribute(methodDefinition) != null && (methodDefinition.Attributes & MethodAttributes.PInvokeImpl) != 0)
			{
				return ReversePInvokeWrapperNotImplementedReason.None;
			}
			if (method.FullName == "System.Int32 System.IO.Compression.DeflateStream::UnmanagedWrite(System.IntPtr,System.Int32,System.IntPtr)")
			{
				return ReversePInvokeWrapperNotImplementedReason.None;
			}
			if (method.FullName == "System.Int32 System.IO.Compression.DeflateStream::UnmanagedRead(System.IntPtr,System.Int32,System.IntPtr)")
			{
				return ReversePInvokeWrapperNotImplementedReason.None;
			}
			return ReversePInvokeWrapperNotImplementedReason.MissingPInvokeCallbackAttribute;
		}

		private static bool ParameterIsGenericInstance(ParameterDefinition p)
		{
			if (p.ParameterType.IsGenericInstance)
			{
				return true;
			}
			for (TypeSpecification typeSpecification = p.ParameterType as TypeSpecification; typeSpecification != null; typeSpecification = typeSpecification.ElementType as TypeSpecification)
			{
				if (typeSpecification.ElementType.IsGenericInstance)
				{
					return true;
				}
			}
			return false;
		}

		public void WriteMethodDeclaration(IGeneratedCodeWriter writer)
		{
			MarshaledParameter[] parameters = Parameters;
			foreach (MarshaledParameter parameter in parameters)
			{
				MarshalInfoWriterFor(parameter).WriteIncludesForFieldDeclaration(writer);
			}
			MarshalInfoWriterFor(GetMethodReturnType()).WriteIncludesForFieldDeclaration(writer);
			writer.AddMethodForwardDeclaration(GetMethodSignature());
		}

		public void WriteMethodDefinition(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteMethodWithMetadataInitialization(GetMethodSignature(), _managedMethod.FullName, WriteMethodBody, _context.Global.Services.Naming.ForReversePInvokeWrapperMethod(_managedMethod), _managedMethod);
			writer.Context.Global.Collectors.ReversePInvokeWrappers.AddReversePInvokeWrapper(_managedMethod);
		}

		private string GetMethodSignature()
		{
			string text = _context.Global.Services.Naming.ForReversePInvokeWrapperMethod(_managedMethod);
			string decoratedName = MarshaledReturnType.DecoratedName;
			string callingConvention = GetCallingConvention(_managedMethod);
			string text2 = MarshaledParameterTypes.Select((MarshaledType parameterType) => $"{parameterType.DecoratedName} {parameterType.VariableName}").AggregateWithComma();
			return $"extern \"C\" {decoratedName} {callingConvention} {text}({text2})";
		}

		private static MethodReference GetInteropMethod(MethodReference method)
		{
			CustomAttribute monoPInvokeCallbackAttribute = GetMonoPInvokeCallbackAttribute(method.Resolve());
			if (monoPInvokeCallbackAttribute == null || !monoPInvokeCallbackAttribute.HasConstructorArguments)
			{
				return method;
			}
			if (!((from argument in monoPInvokeCallbackAttribute.ConstructorArguments
				where argument.Type.Name == "Type"
				select argument.Value).FirstOrDefault() is TypeReference typeReference))
			{
				return method;
			}
			TypeDefinition typeDefinition = typeReference.Resolve();
			if (typeDefinition == null || !typeDefinition.IsDelegate())
			{
				return method;
			}
			MethodDefinition methodDefinition = typeDefinition.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Invoke");
			if (methodDefinition == null)
			{
				return method;
			}
			if (!VirtualMethodResolution.MethodSignaturesMatchIgnoreStaticness(TypeResolver.For(TypeResolver.For(method.DeclaringType, method).Resolve(typeDefinition)).Resolve(methodDefinition), method))
			{
				return method;
			}
			return methodDefinition;
		}

		private static CustomAttribute GetMonoPInvokeCallbackAttribute(MethodDefinition methodDef)
		{
			return methodDef.CustomAttributes.FirstOrDefault((CustomAttribute attribute) => attribute.AttributeType.FullName.Contains("MonoPInvokeCallback"));
		}

		private static CustomAttribute GetNativePInvokeCallbackAttribute(MethodDefinition methodDef)
		{
			return methodDef.CustomAttributes.FirstOrDefault((CustomAttribute attribute) => attribute.AttributeType.FullName.Contains("NativePInvokeCallback"));
		}

		protected override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			MethodReturnType methodReturnType = GetMethodReturnType();
			if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
			{
				string text = _context.Global.Services.Naming.ForVariable(_typeResolver.Resolve(methodReturnType.ReturnType));
				writer.WriteLine("{0} {1};", text, _context.Global.Services.Naming.ForInteropReturnValue());
				WriteMethodCallStatement(metadataAccess, "NULL", localVariableNames, writer, _context.Global.Services.Naming.ForInteropReturnValue());
			}
			else
			{
				WriteMethodCallStatement(metadataAccess, "NULL", localVariableNames, writer);
			}
		}

		internal static string GetCallingConvention(MethodReference managedMethod)
		{
			CustomAttribute monoPInvokeCallbackAttribute = GetMonoPInvokeCallbackAttribute(managedMethod.Resolve());
			if (monoPInvokeCallbackAttribute == null || !monoPInvokeCallbackAttribute.HasConstructorArguments)
			{
				return "DEFAULT_CALL";
			}
			if (!((from argument in monoPInvokeCallbackAttribute.ConstructorArguments
				where argument.Type.Name == "Type"
				select argument.Value).FirstOrDefault() is TypeReference typeReference))
			{
				return "DEFAULT_CALL";
			}
			TypeDefinition typeDefinition = typeReference.Resolve();
			if (typeDefinition == null)
			{
				return "DEFAULT_CALL";
			}
			return InteropMethodBodyWriter.GetDelegateCallingConvention(typeDefinition);
		}

		protected override void WriteReturnStatementEpilogue(IGeneratedMethodCodeWriter writer, string unmarshaledReturnValueVariableName)
		{
			if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void)
			{
				writer.WriteLine("return {0};", unmarshaledReturnValueVariableName);
			}
		}

		protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			if (GetNativePInvokeCallbackAttribute(_managedMethod.Resolve()) == null)
			{
				base.WriteMethodPrologue(writer, metadataAccess);
			}
		}
	}
}
