using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;

namespace Unity.IL2CPP
{
	internal static class RuntimeImplementedMethods
	{
		public static void Register(PrimaryCollectionContext context)
		{
			if (context.Global.Parameters.UsingTinyClassLibraries)
			{
				RegisterMethod(context, "mscorlib", "System.Runtime.InteropServices", "Marshal", "SizeOf", GetSizeOfSharingData, WriteSizeOf);
				RegisterMethod(context, "mscorlib", "System.Runtime.InteropServices", "Marshal", "GetDelegateForFunctionPointer", GetSizeOfSharingData, WriteGetDelegateForFunctionPointer);
			}
		}

		private static void RegisterMethod(PrimaryCollectionContext context, string assemblyName, string @namespace, string typeName, string methodName, GetGenericSharingDataDelegate getSharingData, WriteRuntimeImplementedMethodBodyDelegate writeMethodBody)
		{
			TypeDefinition typeDefinition = new TypeReference(@namespace, typeName, context.Global.Services.TypeProvider.Corlib.MainModule, new AssemblyNameReference(assemblyName, new Version(0, 0))).Resolve();
			if (typeDefinition == null)
			{
				return;
			}
			foreach (MethodDefinition method in typeDefinition.Methods)
			{
				if (method.Name == methodName)
				{
					context.Global.Collectors.RuntimeImplementedMethodWriters.RegisterMethod(method, getSharingData, writeMethodBody);
				}
			}
		}

		private static IEnumerable<RuntimeGenericData> GetSizeOfSharingData()
		{
			return new RuntimeGenericTypeData[0];
		}

		private static void WriteSizeOf(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			TypeReference typeReference = ((GenericInstanceMethod)method).GenericArguments[0];
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, typeReference, MarshalType.PInvoke);
			string nativeSize = defaultMarshalInfoWriter.NativeSize;
			if (nativeSize != "-1")
			{
				defaultMarshalInfoWriter.WriteIncludesForMarshaling(writer);
				writer.WriteManagedReturnStatement(nativeSize);
			}
			else
			{
				string text = ((!typeReference.IsGenericInstance && !typeReference.HasGenericParameters) ? ("Type '" + typeReference.FullName + "' cannot be marshaled as an unmanaged structure; no meaningful size or offset can be computed.") : ("Type '" + typeReference.FullName + "' is a generic type. No meaningful size or offset can be computed."));
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_argument_exception(\"" + text + "\")"));
			}
		}

		private static void WriteGetDelegateForFunctionPointer(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			TypeReference type = ((GenericInstanceMethod)method).GenericArguments[0];
			string variableName = MethodSignatureWriter.ParametersForICall(writer.Context, method.Resolve(), ParameterFormat.WithName).Single();
			string text = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, type, MarshalType.PInvoke).WriteMarshalVariableFromNative(writer, variableName, null, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, metadataAccess);
			if (writer.Context.Global.Parameters.ReturnAsByRefParameter)
			{
				writer.WriteLine("*il2cppRetVal = " + text + ";");
			}
			else
			{
				writer.WriteLine("return " + text + ";");
			}
		}
	}
}
