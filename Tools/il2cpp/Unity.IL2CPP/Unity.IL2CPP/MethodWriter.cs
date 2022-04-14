using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Debugger;
using Unity.IL2CPP.GenericSharing;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public static class MethodWriter
	{
		public static void WriteMethodDefinition(AssemblyWriteContext context, IGeneratedMethodCodeWriter writer, MethodReference method, bool addToMethodCollection)
		{
			if (!MethodNeedsWritten(context, method))
			{
				return;
			}
			MethodDefinition methodDefinition = method.Resolve();
			context.Global.Services.ErrorInformation.CurrentMethod = methodDefinition;
			MethodWriteContext methodContext = new MethodWriteContext(context, method);
			if (methodDefinition.IsPInvokeImpl)
			{
				WriteExternMethodeDeclarationForInternalPInvokeImpl(context.SourceWritingContext, writer, methodDefinition);
			}
			writer.WriteCommentedLine(method.FullName);
			if (method.ShouldNotOptimize())
			{
				writer.WriteLine("IL2CPP_DISABLE_OPTIMIZATIONS");
			}
			if (method.IsGenericInstance || method.DeclaringType.IsGenericInstance)
			{
				context.Global.Collectors.GenericMethods.Add(context.SourceWritingContext, method);
			}
			string methodSignature;
			if (GenericSharingAnalysis.CanShareMethod(context, method))
			{
				context.Global.Collectors.Stats.RecordSharableMethod(method);
				context.Global.Collectors.SharedMethods.AddSharedMethod(GenericSharingAnalysis.GetSharedMethod(context, method), method);
				if (!GenericSharingAnalysis.IsSharedMethod(context, method))
				{
					return;
				}
				methodSignature = MethodSignatureWriter.GetSharedMethodSignature(methodContext, writer);
			}
			else
			{
				methodSignature = MethodSignatureWriter.GetMethodSignature(methodContext, writer, forMethodDefinition: true);
			}
			if (addToMethodCollection)
			{
				context.Global.Collectors.Methods.Add(method);
			}
			context.Global.Collectors.Stats.RecordMethod(method);
			writer.AddIncludeForTypeDefinition(methodContext.ResolvedReturnType);
			AddIncludesForParameterTypeDefinitions(methodContext, writer);
			writer.WriteMethodWithMetadataInitialization(methodSignature, method.FullName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				WritePrologue(method, bodyWriter, metadataAccess);
				WriteMethodBody(methodContext, bodyWriter, metadataAccess);
			}, context.Global.Services.Naming.ForMethod(method), method);
			if (method.ShouldNotOptimize())
			{
				writer.WriteLine("IL2CPP_ENABLE_OPTIMIZATIONS");
			}
			if (HasAdjustorThunk(method))
			{
				WriteAdjustorThunk(methodContext, writer);
			}
		}

		private static void AddIncludesForParameterTypeDefinitions(MethodWriteContext context, IGeneratedMethodCodeWriter writer)
		{
			MethodReference methodReference = context.MethodReference;
			TypeResolver typeResolver = context.TypeResolver;
			foreach (ParameterDefinition parameter in methodReference.Parameters)
			{
				TypeReference typeReference = GenericParameterResolver.ResolveParameterTypeIfNeeded(methodReference, parameter);
				if (ShouldWriteIncludeForParameter(typeReference))
				{
					writer.AddIncludeForTypeDefinition(typeResolver.Resolve(typeReference));
				}
			}
		}

		private static void WriteInlineMethodDefinition(MethodWriteContext context, IGeneratedMethodCodeWriter writer, MethodReference method, string usage)
		{
			context.Global.Services.ErrorInformation.CurrentMethod = method.Resolve();
			string methodSignature;
			if (GenericSharingAnalysis.CanShareMethod(context, method))
			{
				if (!GenericSharingAnalysis.IsSharedMethod(context, method))
				{
					return;
				}
				methodSignature = MethodSignatureWriter.GetSharedMethodSignatureInline(context, writer);
			}
			else
			{
				methodSignature = MethodSignatureWriter.GetInlineMethodSignature(context, writer);
			}
			writer.AddIncludeForTypeDefinition(context.ResolvedReturnType);
			AddIncludesForParameterTypeDefinitions(context, writer);
			writer.WriteMethodWithMetadataInitialization(methodSignature, method.FullName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				WriteMethodBody(context, bodyWriter, metadataAccess);
			}, context.Global.Services.Naming.ForMethod(method) + usage, method);
		}

		public static void WriteInlineMethodDefinitions(SourceWritingContext context, string usage, IGeneratedMethodCodeWriter writer)
		{
			HashSet<MethodReference> hashSet = new HashSet<MethodReference>(writer.Declarations.Methods, new MethodReferenceComparer());
			HashSet<MethodReference> hashSet2 = new HashSet<MethodReference>(writer.Declarations.SharedMethods, new MethodReferenceComparer());
			HashSet<MethodReference> hashSet3 = new HashSet<MethodReference>(writer.Declarations.Methods, new MethodReferenceComparer());
			HashSet<MethodReference> hashSet4 = new HashSet<MethodReference>(writer.Declarations.SharedMethods, new MethodReferenceComparer());
			string usage2 = context.Global.Services.Naming.Clean(usage);
			while (hashSet.Count > 0 || hashSet2.Count > 0)
			{
				foreach (MethodReference item in hashSet.Where((MethodReference m) => m.ShouldInline(context.Global.Parameters)))
				{
					if (!GenericSharingAnalysis.CanShareMethod(context, item))
					{
						WriteInlineMethodDefinition(context.CreateMethodWritingContext(item), writer, item, usage2);
					}
				}
				foreach (MethodReference item2 in hashSet2.Where((MethodReference m) => m.ShouldInline(context.Global.Parameters)))
				{
					WriteInlineMethodDefinition(context.CreateMethodWritingContext(item2), writer, item2, usage2);
				}
				hashSet = new HashSet<MethodReference>(writer.Declarations.Methods, new MethodReferenceComparer());
				hashSet.ExceptWith(hashSet3);
				hashSet3.UnionWith(hashSet);
				hashSet2 = new HashSet<MethodReference>(writer.Declarations.SharedMethods, new MethodReferenceComparer());
				hashSet2.ExceptWith(hashSet4);
				hashSet4.UnionWith(hashSet2);
			}
		}

		internal static bool HasAdjustorThunk(MethodReference method)
		{
			if (method.HasThis)
			{
				return method.DeclaringType.IsValueType();
			}
			return false;
		}

		internal static void CollectSequencePoints(PrimaryCollectionContext context, MethodDefinition method, SequencePointCollector sequencePointCollector)
		{
			if (!method.HasBody)
			{
				return;
			}
			try
			{
				context.Global.Services.ErrorInformation.CurrentMethod = method;
				if (!method.DebugInformation.HasSequencePoints)
				{
					sequencePointCollector.AddPausePoint(method, -1);
					{
						foreach (Instruction instruction2 in method.Body.Instructions)
						{
							if (instruction2.Operand is Instruction)
							{
								Instruction instruction = instruction2.Operand as Instruction;
								if (instruction.Offset < instruction2.Offset)
								{
									sequencePointCollector.AddPausePoint(method, instruction.Offset);
								}
							}
						}
						return;
					}
				}
				sequencePointCollector.AddSequencePoint(new SequencePointInfo(method, SequencePointKind.Normal, string.Empty, null, 0, 0, 0, 0, -1));
				sequencePointCollector.AddSequencePoint(new SequencePointInfo(method, SequencePointKind.Normal, string.Empty, null, 0, 0, 0, 0, 16777215));
				IDictionary<Instruction, SequencePoint> sequencePointMapping = method.DebugInformation.GetSequencePointMapping();
				foreach (SequencePoint value in sequencePointMapping.Values)
				{
					sequencePointCollector.AddSequencePoint(new SequencePointInfo(method, value));
				}
				foreach (Instruction instruction3 in method.Body.Instructions)
				{
					SequencePoint sequencePoint = TryGetSequencePoint(sequencePointMapping, instruction3);
					if (instruction3.IsCallInstruction())
					{
						Instruction previous = instruction3.Previous;
						while (sequencePoint == null && previous != null)
						{
							sequencePoint = TryGetSequencePoint(sequencePointMapping, previous);
							previous = previous.Previous;
						}
						if (sequencePoint != null)
						{
							sequencePointCollector.AddSequencePoint(new SequencePointInfo(method, SequencePointKind.StepOut, sequencePoint.Document.Url, sequencePoint.Document.Hash, sequencePoint.StartLine, sequencePoint.EndLine, sequencePoint.StartColumn, sequencePoint.EndColumn, instruction3.Offset));
						}
						else
						{
							sequencePointCollector.AddSequencePoint(SequencePointInfo.Empty(method, SequencePointKind.StepOut, instruction3.Offset));
						}
					}
				}
				sequencePointCollector.AddVariables(context, method);
			}
			catch (Exception innerException)
			{
				throw new InvalidOperationException("Error while processing debug information. This often indicates that debug information in a .pdb or .mdb file is not correct.\nCheck the debug information corresponding to the assembly '" + (context.Global.Services.ErrorInformation.CurrentMethod.DeclaringType.Module.FileName ?? context.Global.Services.ErrorInformation.CurrentMethod.DeclaringType.Module.Name) + "'.", innerException);
			}
		}

		private static SequencePoint TryGetSequencePoint(IDictionary<Instruction, SequencePoint> mapping, Instruction instruction)
		{
			if (mapping.TryGetValue(instruction, out var value))
			{
				return value;
			}
			return null;
		}

		private static bool MethodNeedsWritten(ReadOnlyContext context, MethodReference method)
		{
			if (IsGetOrSetGenericValueImplOnArray(method))
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
			if (method.IsStripped())
			{
				return false;
			}
			if (context.Global.Parameters.UsingTinyBackend)
			{
				if (IntrinsicRemap.ShouldRemap(context, method))
				{
					return false;
				}
				if (method.DeclaringType.MetadataType == MetadataType.String && method.Name == ".ctor")
				{
					return false;
				}
			}
			return MethodCanBeDirectlyCalled(context, method);
		}

		private static void WriteAdjustorThunk(MethodWriteContext context, IGeneratedMethodCodeWriter writer)
		{
			MethodReference method = context.MethodReference;
			string methodSignature = WriteAdjustorThunkMethodSignature(context, method, context.TypeResolver);
			writer.WriteMethodWithMetadataInitialization(methodSignature, context.Global.Services.Naming.ForMethodAdjustorThunk(method), delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				string text = context.Global.Services.Naming.ForVariable(method.DeclaringType);
				string item;
				if (method.DeclaringType.IsNullable())
				{
					bodyWriter.WriteLine("{0} _thisAdjusted;", text);
					bodyWriter.WriteLine("if (!il2cpp_codegen_is_fake_boxed_object({0}))", "__this");
					EmitCopyOfRealBoxedNullable(method, bodyWriter);
					bodyWriter.WriteLine("else");
					EmitCopyOfFakeBoxedNullable(method, bodyWriter);
					item = "&_thisAdjusted";
				}
				else
				{
					if (context.Global.Parameters.UsingTinyBackend)
					{
						bodyWriter.WriteStatement("int32_t _offset = ((sizeof(RuntimeObject) + IL2CPP_BOXED_OBJECT_ALIGNMENT - 1) & ~(IL2CPP_BOXED_OBJECT_ALIGNMENT - 1)) / sizeof(void*)");
					}
					else
					{
						bodyWriter.WriteStatement("int32_t _offset = 1");
					}
					bodyWriter.WriteLine("{0}* _thisAdjusted = reinterpret_cast<{0}*>({1} + _offset);", text, "__this");
					item = "_thisAdjusted";
				}
				List<string> list = new List<string> { item };
				for (int i = 0; i < method.Parameters.Count; i++)
				{
					list.Add(context.Global.Services.Naming.ForParameterName(method.Parameters[i]));
				}
				if (method.DeclaringType.IsNullable())
				{
					string text2 = "";
					if (method.ReturnType.IsNotVoid())
					{
						text2 = "_returnValue";
						bodyWriter.WriteLine(context.Global.Services.Naming.ForVariable(context.TypeResolver.Resolve(method.ReturnType)) + " _returnValue;");
					}
					MethodBodyWriter.WriteMethodCallExpression(text2, () => "method", bodyWriter, method, method, method, TypeResolver.Empty, MethodCallType.Normal, metadataAccess, null, list, useArrayBoundsCheck: true);
					bodyWriter.WriteLine("*reinterpret_cast<{1}*>({2} + 1) = _thisAdjusted.{0}();", context.Global.Services.Naming.ForFieldGetter(method.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "value")), context.Global.Services.Naming.ForVariable(((GenericInstanceType)method.DeclaringType).GenericArguments[0]), "__this");
					if (method.ReturnType.IsNotVoid())
					{
						bodyWriter.WriteManagedReturnStatement(text2);
					}
				}
				else
				{
					string text3 = "";
					if (method.ReturnType.IsNotVoid())
					{
						text3 = "_returnValue";
						bodyWriter.WriteLine(context.Global.Services.Naming.ForVariable(context.TypeResolver.Resolve(method.ReturnType)) + " " + text3 + ";");
					}
					MethodBodyWriter.WriteMethodCallExpression(text3, () => "method", bodyWriter, method, method, method, TypeResolver.Empty, MethodCallType.Normal, metadataAccess, null, list, useArrayBoundsCheck: true);
					if (!string.IsNullOrEmpty(text3))
					{
						bodyWriter.WriteManagedReturnStatement(text3 ?? "");
					}
				}
			}, context.Global.Services.Naming.ForMethodAdjustorThunk(method), method);
		}

		public static string WriteAdjustorThunkMethodSignature(ReadOnlyContext context, MethodReference method, TypeResolver typeResolver)
		{
			string parameters = MethodSignatureWriter.FormatParameters(context, method, ParameterFormat.WithTypeAndNameThisObject, !context.Global.Parameters.UsingTinyBackend);
			return MethodSignatureWriter.GetMethodSignature(context.Global.Services.Naming.ForMethodAdjustorThunk(method), MethodSignatureWriter.FormatReturnType(context, typeResolver.Resolve(GenericParameterResolver.ResolveReturnTypeIfNeeded(method))), parameters, "IL2CPP_EXTERN_C");
		}

		private static void EmitCopyOfRealBoxedNullable(MethodReference method, IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.BeginBlock();
			bodyWriter.WriteLine("_thisAdjusted.{0}(*reinterpret_cast<{1}*>({2} + 1));", bodyWriter.Context.Global.Services.Naming.ForFieldSetter(method.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "value")), bodyWriter.Context.Global.Services.Naming.ForVariable(((GenericInstanceType)method.DeclaringType).GenericArguments[0]), "__this");
			bodyWriter.WriteLine("_thisAdjusted.{0}(true);", bodyWriter.Context.Global.Services.Naming.ForFieldSetter(method.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "has_value")));
			bodyWriter.EndBlock();
		}

		private static void EmitCopyOfFakeBoxedNullable(MethodReference method, IGeneratedMethodCodeWriter bodyWriter)
		{
			bodyWriter.BeginBlock();
			bodyWriter.WriteLine(EmitCopyOfNullableFieldValueFromAdjustorThunk(bodyWriter.Context, "value", method.DeclaringType));
			bodyWriter.WriteLine(EmitCopyOfNullableFieldValueFromAdjustorThunk(bodyWriter.Context, "has_value", method.DeclaringType));
			bodyWriter.EndBlock();
		}

		private static string EmitCopyOfNullableFieldValueFromAdjustorThunk(ReadOnlyContext context, string fieldName, TypeReference nullableTypeReference)
		{
			return string.Format("_thisAdjusted.{0}((({1}*)({2} + 1))->{3}());", context.Global.Services.Naming.ForFieldSetter(nullableTypeReference.Resolve().Fields.Single((FieldDefinition f) => f.Name == fieldName)), context.Global.Services.Naming.ForVariable(nullableTypeReference), "__this", context.Global.Services.Naming.ForFieldGetter(nullableTypeReference.Resolve().Fields.Single((FieldDefinition f) => f.Name == fieldName)));
		}

		private static bool ShouldWriteIncludeForParameter(TypeReference resolvedParameterType)
		{
			resolvedParameterType = resolvedParameterType.WithoutModifiers();
			if (resolvedParameterType is ByReferenceType byReferenceType)
			{
				return ShouldWriteIncludeForParameter(byReferenceType.ElementType);
			}
			if (resolvedParameterType is PointerType pointerType)
			{
				return ShouldWriteIncludeForParameter(pointerType.ElementType);
			}
			if (!(resolvedParameterType is TypeSpecification) || resolvedParameterType is GenericInstanceType || resolvedParameterType is ArrayType)
			{
				return !resolvedParameterType.IsGenericParameter;
			}
			return false;
		}

		private static void WriteMethodBodyForComOrWindowsRuntimeMethod(SourceWritingContext context, MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			MethodDefinition methodDefinition = method.Resolve();
			if (methodDefinition.IsConstructor)
			{
				if (methodDefinition.DeclaringType.IsImport && !methodDefinition.DeclaringType.IsWindowsRuntimeProjection())
				{
					WriteMethodBodyForComObjectConstructor(method, writer);
				}
				else
				{
					WriteMethodBodyForWindowsRuntimeObjectConstructor(context, method, writer, metadataAccess);
				}
			}
			else if (methodDefinition.IsFinalizerMethod())
			{
				WriteMethodBodyForComOrWindowsRuntimeFinalizer(methodDefinition, writer, metadataAccess);
			}
			else if (method.DeclaringType.IsIl2CppComObject(context) && method.Name == "ToString")
			{
				WriteMethodBodyForIl2CppComObjectToString(context, methodDefinition, writer, metadataAccess);
			}
			else if (method.HasThis)
			{
				WriteMethodBodyForDirectComOrWindowsRuntimeCall(context, method, writer, metadataAccess);
			}
			else
			{
				new ComStaticMethodBodyWriter(context, method).WriteMethodBody(writer, metadataAccess);
			}
		}

		private static void WriteMethodBodyForComObjectConstructor(MethodReference method, IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine("il2cpp_codegen_com_create_instance({0}::CLSID, &{1}->{2});", writer.Context.Global.Services.Naming.ForTypeNameOnly(method.DeclaringType), "__this", writer.Context.Global.Services.Naming.ForIl2CppComObjectIdentityField());
			writer.WriteLine("il2cpp_codegen_com_register_rcw({0});", "__this");
		}

		private static void WriteMethodBodyForWindowsRuntimeObjectConstructor(MinimalContext context, MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			if (method.Resolve().HasGenericParameters)
			{
				throw new InvalidOperationException("Cannot construct generic Windows Runtime objects.");
			}
			if (IsUnconstructibleWindowsRuntimeClass(context, method.DeclaringType, out var errorMessage))
			{
				writer.WriteStatement(Emit.RaiseManagedException($"il2cpp_codegen_get_invalid_operation_exception(\"{errorMessage}\")"));
			}
			else
			{
				new WindowsRuntimeConstructorMethodBodyWriter(context, method).WriteMethodBody(writer, metadataAccess);
			}
		}

		private static bool IsUnconstructibleWindowsRuntimeClass(ReadOnlyContext context, TypeReference type, out string errorMessage)
		{
			if (type.IsAttribute())
			{
				errorMessage = $"Cannot construct type '{type.FullName}'. Windows Runtime attribute types are not constructable.";
				return true;
			}
			TypeReference typeReference = context.Global.Services.WindowsRuntime.ProjectToCLR(type);
			if (typeReference != type)
			{
				errorMessage = $"Cannot construct type '{type.FullName}'. It has no managed representation. Instead, use '{typeReference.FullName}'.";
				return true;
			}
			errorMessage = null;
			return false;
		}

		private static void WriteMethodBodyForComOrWindowsRuntimeFinalizer(MethodDefinition finalizer, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			if (finalizer.DeclaringType.IsIl2CppComObject(writer.Context))
			{
				ReleaseIl2CppObjectIdentity(writer);
			}
			CallBaseTypeFinalizer(finalizer, writer, metadataAccess);
		}

		private static void ReleaseIl2CppObjectIdentity(IGeneratedMethodCodeWriter writer)
		{
			string text = writer.Context.Global.Services.Naming.ForIl2CppComObjectIdentityField();
			writer.WriteLine("if (__this->" + text + " != NULL)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("il2cpp_codegen_il2cpp_com_object_cleanup(__this);");
				writer.WriteLine("__this->" + text + "->Release();");
				writer.WriteLine("__this->" + text + " = NULL;");
			}
			writer.WriteLine();
		}

		private static void CallBaseTypeFinalizer(MethodDefinition finalizer, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			MethodReference baseTypeFinalizer = null;
			TypeReference typeReference = finalizer.DeclaringType.BaseType;
			while (typeReference != null)
			{
				TypeDefinition typeDefinition = typeReference.Resolve();
				TypeResolver typeResolver = TypeResolver.For(typeReference);
				foreach (MethodDefinition method in typeDefinition.Methods)
				{
					if (method.IsFinalizerMethod())
					{
						baseTypeFinalizer = typeResolver.Resolve(method);
						goto end_IL_0087;
					}
				}
				typeReference = typeResolver.Resolve(typeDefinition.BaseType);
				continue;
				end_IL_0087:
				break;
			}
			if (baseTypeFinalizer != null)
			{
				List<string> argumentArray = new List<string>(2) { "__this" };
				TypeResolver typeResolver2 = TypeResolver.For(finalizer.DeclaringType);
				MethodBodyWriter.WriteMethodCallExpression("", () => metadataAccess.HiddenMethodInfo(baseTypeFinalizer), writer, finalizer, typeResolver2.Resolve(baseTypeFinalizer), baseTypeFinalizer, typeResolver2, MethodCallType.Normal, metadataAccess, new VTableBuilder(), argumentArray, useArrayBoundsCheck: false);
			}
		}

		private static void WriteMethodBodyForIl2CppComObjectToString(SourceWritingContext context, MethodDefinition methodDefinition, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			string text = context.Global.Services.Naming.ForInteropInterfaceVariable(context.Global.Services.TypeProvider.IStringableType);
			string text2 = context.Global.Services.Naming.ForTypeNameOnly(context.Global.Services.TypeProvider.IStringableType);
			MethodDefinition interfaceMethod = context.Global.Services.TypeProvider.IStringableType.Methods.Single((MethodDefinition m) => m.Name == "ToString");
			writer.AddIncludeForTypeDefinition(context.Global.Services.TypeProvider.IStringableType);
			writer.WriteLine(text2 + "* " + text + " = il2cpp_codegen_com_query_interface_no_throw<" + text2 + ">(__this);");
			writer.WriteLine("if (" + text + " != NULL)");
			using (new BlockWriter(writer))
			{
				new ComMethodWithPreOwnedInterfacePointerMethodBodyWriter(context, interfaceMethod).WriteMethodBody(writer, metadataAccess);
			}
			MethodDefinition objectToString = context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.Name == "ToString");
			List<string> argumentArray = new List<string> { "__this" };
			writer.WriteLine(context.Global.Services.Naming.ForVariable(objectToString.ReturnType) + " toStringRetVal;");
			MethodBodyWriter.WriteMethodCallExpression("toStringRetVal", () => metadataAccess.HiddenMethodInfo(objectToString), writer, methodDefinition, objectToString, objectToString, TypeResolver.Empty, MethodCallType.Normal, metadataAccess, new VTableBuilder(), argumentArray, useArrayBoundsCheck: true);
			writer.WriteManagedReturnStatement("toStringRetVal");
		}

		private static void WriteMethodBodyForDirectComOrWindowsRuntimeCall(MinimalContext context, MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			MethodDefinition methodDefinition = method.Resolve();
			if (!methodDefinition.IsComOrWindowsRuntimeMethod(context))
			{
				throw new InvalidOperationException("WriteMethodBodyForDirectComOrWindowsRuntimeCall called for non-COM and non-Windows Runtime method");
			}
			MethodReference methodReference = (methodDefinition.DeclaringType.IsInterface ? method : method.GetOverriddenInterfaceMethod(method.DeclaringType.GetInterfaces(context)));
			if (methodReference == null)
			{
				writer.WriteStatement(Emit.RaiseManagedException($"il2cpp_codegen_get_missing_method_exception(\"The method '{method.FullName}' has no implementation.\")"));
			}
			else if (!methodReference.DeclaringType.IsComOrWindowsRuntimeInterface(context))
			{
				WriteMethodBodyForProjectedInterfaceMethod(writer, method, methodReference, metadataAccess);
			}
			else
			{
				new ComInstanceMethodBodyWriter(context, method).WriteMethodBody(writer, metadataAccess);
			}
		}

		private static void WriteMethodBodyForProjectedInterfaceMethod(IGeneratedMethodCodeWriter writer, MethodReference method, MethodReference interfaceMethod, IRuntimeMetadataAccess metadataAccess)
		{
			MethodDefinition interfaceMethodDef = interfaceMethod.Resolve();
			TypeReference interfaceForAdapterType = GetInterfaceForAdapterType(writer.Context, method, interfaceMethod.DeclaringType);
			TypeResolver typeResolver = TypeResolver.For(interfaceForAdapterType);
			TypeDefinition nativeToManagedAdapterClassFor = writer.Context.Global.Services.WindowsRuntime.GetNativeToManagedAdapterClassFor(interfaceForAdapterType.Resolve());
			TypeReference typeReference = typeResolver.Resolve(nativeToManagedAdapterClassFor);
			MethodDefinition method2 = nativeToManagedAdapterClassFor.Methods.First((MethodDefinition m) => m.Overrides.Any((MethodReference o) => o.Resolve() == interfaceMethodDef));
			MethodReference method3 = typeResolver.Resolve(method2);
			writer.AddForwardDeclaration(typeReference);
			writer.AddIncludeForMethodDeclaration(method3);
			List<string> list = new List<string>();
			foreach (ParameterDefinition parameter in method.Parameters)
			{
				list.Add(writer.Context.Global.Services.Naming.ForParameterName(parameter));
			}
			TypeReference typeReference2 = TypeResolver.For(interfaceMethod.DeclaringType).Resolve(interfaceMethod.ReturnType);
			if (!typeReference2.IsVoid())
			{
				string text = "returnValue";
				writer.WriteStatement(writer.Context.Global.Services.Naming.ForVariable(typeReference2) + " " + text);
				writer.WriteMethodCallWithResultStatement(metadataAccess, "reinterpret_cast<" + writer.Context.Global.Services.Naming.ForVariable(typeReference) + ">(__this)", null, method3, MethodCallType.Normal, text, list.ToArray());
				writer.WriteManagedReturnStatement(text);
			}
			else
			{
				writer.WriteMethodCallStatement(metadataAccess, "reinterpret_cast<" + writer.Context.Global.Services.Naming.ForVariable(typeReference) + ">(__this)", null, method3, MethodCallType.Normal, list.ToArray());
			}
			metadataAccess.TypeInfoFor(typeReference);
		}

		private static TypeReference GetInterfaceForAdapterType(MinimalContext context, MethodReference method, TypeReference interfaceType)
		{
			List<TypeReference> list = new List<TypeReference>();
			foreach (TypeReference item in from i in method.DeclaringType.GetInterfaces(context)
				where i.IsComOrWindowsRuntimeInterface(context)
				select i)
			{
				TypeReference typeReference = context.Global.Services.WindowsRuntime.ProjectToCLR(item);
				if (item != typeReference)
				{
					list.Add(typeReference);
				}
			}
			foreach (TypeReference item2 in list)
			{
				if (TypeReferenceEqualityComparer.AreEqual(interfaceType, item2))
				{
					return item2;
				}
			}
			TypeDefinition typeDefinition = interfaceType.Resolve();
			if (typeDefinition.Module == context.Global.Services.TypeProvider.Corlib.MainModule && typeDefinition.Namespace == "System.Collections" && typeDefinition.Name == "IEnumerable")
			{
				foreach (TypeReference item3 in list)
				{
					TypeReference typeReference2 = FindInterfaceThatImplementsAnotherInterface(item3, interfaceType, "IEnumerable`1");
					if (typeReference2 != null)
					{
						return typeReference2;
					}
				}
				return interfaceType;
			}
			return interfaceType;
		}

		private static TypeReference FindInterfaceThatImplementsAnotherInterface(TypeReference potentialInterface, TypeReference interfaceType, string interfaceName)
		{
			TypeResolver typeResolver = TypeResolver.For(potentialInterface);
			foreach (TypeReference item in potentialInterface.Resolve().Interfaces.Select((InterfaceImplementation i) => typeResolver.Resolve(i.InterfaceType)))
			{
				if (potentialInterface.Name == interfaceName && TypeReferenceEqualityComparer.AreEqual(item, interfaceType))
				{
					return potentialInterface;
				}
				TypeReference typeReference = FindInterfaceThatImplementsAnotherInterface(item, interfaceType, interfaceName);
				if (typeReference != null)
				{
					return typeReference;
				}
			}
			return null;
		}

		private static void WriteMethodBodyForInternalCall(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess, IICallMappingService icallMapping)
		{
			MethodDefinition methodDefinition = method.Resolve();
			if (!methodDefinition.IsInternalCall)
			{
				throw new Exception();
			}
			if (IntrinsicRemap.ShouldRemap(writer.Context, methodDefinition))
			{
				string text = IntrinsicRemap.MappedNameFor(writer.Context, methodDefinition);
				IEnumerable<string> enumerable = MethodSignatureWriter.ParametersForICall(writer.Context, methodDefinition, ParameterFormat.WithName);
				enumerable = (IntrinsicRemap.HasCustomArguments(methodDefinition) ? IntrinsicRemap.GetCustomArguments(writer.Context, methodDefinition, methodDefinition, metadataAccess, enumerable) : enumerable);
				if (methodDefinition.ReturnType.MetadataType != MetadataType.Void)
				{
					writer.WriteManagedReturnStatement(text + "(" + enumerable.AggregateWithComma() + ")");
					return;
				}
				writer.WriteLine("{0}({1});", text, enumerable.AggregateWithComma());
			}
			else
			{
				if (methodDefinition.HasGenericParameters)
				{
					throw new NotSupportedException($"Internal calls cannot have generic parameters: {methodDefinition.FullName}");
				}
				string icall = method.FullName.Substring(method.FullName.IndexOf(" ") + 1);
				string text2 = icallMapping.ResolveICallFunction(icall);
				if (text2 != null)
				{
					EmitDirectICallInvocation(method, writer, text2, icallMapping.ResolveICallHeader(icall), methodDefinition);
				}
				else
				{
					EmitFunctionPointerICallInvocation(method, writer, methodDefinition, metadataAccess);
				}
			}
		}

		private static void EmitFunctionPointerICallInvocation(MethodReference method, IGeneratedMethodCodeWriter writer, MethodDefinition methodDefinition, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteInternalCallResolutionStatement(methodDefinition, metadataAccess);
			_ = writer.Context;
			string text = string.Empty;
			Action action = delegate
			{
			};
			if (!methodDefinition.ReturnType.IsVoid())
			{
				string text2 = writer.Context.Global.Services.Naming.ForVariable(method.ReturnType);
				action = delegate
				{
					writer.WriteManagedReturnStatement("icallRetVal");
				};
				text = text2 + " icallRetVal = ";
			}
			writer.WriteLine("{0}{1}({2});", text, "_il2cpp_icall_func", MethodSignatureWriter.FormatParametersForICall(writer.Context, method, ParameterFormat.WithName));
			action();
		}

		private static void EmitDirectICallInvocation(MethodReference method, IGeneratedMethodCodeWriter writer, string icall, string icallHeader, MethodDefinition methodDefinition)
		{
			if (icallHeader != null)
			{
				writer.AddInclude(icallHeader);
			}
			writer.WriteLine("typedef {0};", MethodSignatureWriter.GetICallMethodVariable(writer.Context, methodDefinition));
			if (writer.Context.Global.Parameters.UsingTinyBackend)
			{
				writer.WriteLine("using namespace tiny::icalls;");
			}
			else
			{
				writer.WriteLine("using namespace il2cpp::icalls;");
			}
			string text = $"(({writer.Context.Global.Services.Naming.ForMethodNameOnly(method)}_ftn){icall}) ({MethodSignatureWriter.FormatParametersForICall(writer.Context, method, ParameterFormat.WithName)})";
			if (method.ReturnType.IsVoid())
			{
				writer.WriteStatement(text);
			}
			else
			{
				writer.WriteManagedReturnStatement(text);
			}
		}

		private static void WriteMethodBodyForPInvokeImpl(MinimalContext context, IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			new PInvokeMethodBodyWriter(context, method).WriteMethodBody(writer, metadataAccess);
		}

		private static void WriteExternMethodeDeclarationForInternalPInvokeImpl(MinimalContext context, IGeneratedMethodCodeWriter writer, MethodReference method)
		{
			new PInvokeMethodBodyWriter(context, method).WriteExternMethodDeclarationForInternalPInvoke(writer);
		}

		internal static void WriteMethodForDelegatePInvokeIfNeeded(SourceWritingContext context, IGeneratedMethodCodeWriter writer, MethodReference method)
		{
			DelegatePInvokeMethodBodyWriter delegatePInvokeMethodBodyWriter = new DelegatePInvokeMethodBodyWriter(context, method);
			if (delegatePInvokeMethodBodyWriter.IsDelegatePInvokeWrapperNecessary())
			{
				TypeResolver typeResolver = TypeResolver.For(method.DeclaringType, method);
				string text = context.Global.Services.Naming.ForDelegatePInvokeWrapper(method.DeclaringType);
				bool includeHiddenMethodInfo = MethodSignatureWriter.NeedsHiddenMethodInfo(context, method, MethodCallType.Normal);
				string methodSignature = MethodSignatureWriter.GetMethodSignature(text, MethodSignatureWriter.FormatReturnType(context, typeResolver.Resolve(method.ReturnType)), MethodSignatureWriter.FormatParameters(context, method, ParameterFormat.WithTypeAndName, includeHiddenMethodInfo), "IL2CPP_EXTERN_C");
				writer.WriteMethodWithMetadataInitialization(methodSignature, method.FullName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
				{
					delegatePInvokeMethodBodyWriter.WriteMethodBody(bodyWriter, metadataAccess);
				}, text, method);
				context.Global.Collectors.WrappersForDelegateFromManagedToNative.Add(context, method);
			}
		}

		internal static void WriteReversePInvokeMethodDefinition(IGeneratedMethodCodeWriter writer, MethodReference method)
		{
			ReversePInvokeMethodBodyWriter.Create(writer.Context, method).WriteMethodDefinition(writer);
		}

		internal static bool MethodCanBeDirectlyCalled(ReadOnlyContext context, MethodReference method)
		{
			if (!TypeMethodsCanBeDirectlyCalled(context, method.DeclaringType))
			{
				return false;
			}
			if (method.HasGenericParameters)
			{
				return false;
			}
			MethodDefinition methodDefinition = method.Resolve();
			TypeDefinition declaringType = methodDefinition.DeclaringType;
			if (declaringType.IsWindowsRuntime && declaringType.IsInterface && !declaringType.IsPublic && context.Global.Services.WindowsRuntime.ProjectToCLR(declaringType) == declaringType)
			{
				return IsInternalInterfaceMethodCalledFromRuntime(method);
			}
			if (methodDefinition.IsAbstract)
			{
				return method.DeclaringType.IsComOrWindowsRuntimeInterface(context);
			}
			return true;
		}

		private static bool IsInternalInterfaceMethodCalledFromRuntime(MethodReference method)
		{
			TypeReference declaringType = method.DeclaringType;
			if (declaringType.Namespace == "Windows.Foundation" && declaringType.Name == "IUriRuntimeClass")
			{
				return method.Name == "get_RawUri";
			}
			return false;
		}

		internal static bool TypeMethodsCanBeDirectlyCalled(ReadOnlyContext context, TypeReference type)
		{
			if (type.HasGenericParameters)
			{
				return false;
			}
			TypeDefinition typeDefinition = type.Resolve();
			if (typeDefinition.IsInterface && !type.IsComOrWindowsRuntimeInterface(context))
			{
				return false;
			}
			if (typeDefinition.IsWindowsRuntimeProjection)
			{
				return typeDefinition.IsExposedToWindowsRuntime();
			}
			return true;
		}

		internal static bool IsGetOrSetGenericValueImplOnArray(MethodReference method)
		{
			if (method.DeclaringType.IsSystemArray())
			{
				if (!(method.Name == "GetGenericValueImpl"))
				{
					return method.Name == "SetGenericValueImpl";
				}
				return true;
			}
			return false;
		}

		private static void WriteMethodBody(MethodWriteContext context, IGeneratedMethodCodeWriter methodBodyWriter, IRuntimeMetadataAccess metadataAccess)
		{
			MethodReference methodReference = context.MethodReference;
			MethodDefinition methodDefinition = context.MethodDefinition;
			if (!ReplaceWithHardcodedAlternativeIfPresent(methodReference, methodBodyWriter, metadataAccess))
			{
				if (!methodDefinition.HasBody || !methodDefinition.Body.Instructions.Any())
				{
					WriteMethodBodyForMethodWithoutBody(methodBodyWriter, methodReference, metadataAccess);
					return;
				}
				AddRetInstructionAtTheEndIfNeeded(methodDefinition);
				new MethodBodyWriter(context, methodBodyWriter, metadataAccess).Generate();
			}
		}

		private static void WritePrologue(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			SourceWritingContext context = writer.Context;
			if (context.Global.Parameters.EnableStacktrace)
			{
				writer.WriteLine("StackTraceSentry _stackTraceSentry({0});", metadataAccess.MethodInfo(method));
			}
			if (context.Global.Parameters.EnableDeepProfiler)
			{
				writer.WriteLine("ProfilerMethodSentry _profilerMethodSentry({0});", metadataAccess.MethodInfo(method));
			}
		}

		private static void WriteMethodBodyForMethodWithoutBody(IGeneratedMethodCodeWriter writer, MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			if (!MethodCanBeDirectlyCalled(writer.Context, method))
			{
				throw new InvalidOperationException($"Trying to generate a body for method '{method.FullName}'");
			}
			MethodDefinition methodDefinition = method.Resolve();
			if (methodDefinition.IsRuntime && !methodDefinition.IsInternalCall && !methodDefinition.DeclaringType.IsInterface)
			{
				TypeDefinition typeDefinition = method.DeclaringType.Resolve();
				if (typeDefinition.BaseType.Namespace == "System" && typeDefinition.BaseType.Name == "MulticastDelegate")
				{
					new DelegateMethodsWriter(writer).WriteMethodBodyForIsRuntimeMethod(method, metadataAccess);
					return;
				}
			}
			if (writer.Context.Global.Results.Setup.RuntimeImplementedMethodWriters.TryGetWriter(methodDefinition, out var value))
			{
				value(writer, method, metadataAccess);
			}
			else if (methodDefinition.IsComOrWindowsRuntimeMethod(writer.Context))
			{
				WriteMethodBodyForComOrWindowsRuntimeMethod(writer.Context, method, writer, metadataAccess);
			}
			else if (methodDefinition.IsInternalCall)
			{
				WriteMethodBodyForInternalCall(method, writer, metadataAccess, writer.Context.Global.Services.ICallMapping);
			}
			else if (methodDefinition.IsPInvokeImpl)
			{
				WriteMethodBodyForPInvokeImpl(writer.Context, writer, methodDefinition, metadataAccess);
			}
			else
			{
				writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_missing_method_exception(\"The method '" + method.FullName + "' has no implementation.\")"));
			}
		}

		private static bool ReplaceWithHardcodedAlternativeIfPresent(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			string fullName = method.Resolve().FullName;
			if (fullName == "R System.Array::UnsafeMov(S)")
			{
				TypeReference variableType = TypeResolver.For(method.DeclaringType, method).Resolve(method.ReturnType);
				writer.WriteManagedReturnStatement("static_cast<" + writer.Context.Global.Services.Naming.ForVariable(variableType) + ">(" + writer.Context.Global.Services.Naming.ForParameterName(method.Parameters.First()) + ");");
				return true;
			}
			if (fullName == "System.Void Microsoft.FSharp.Core.CompilerMessageAttribute::.ctor(System.Object,System.Object)")
			{
				return true;
			}
			if (writer.Context.Global.Parameters.UsingTinyClassLibraries)
			{
				if (fullName == "System.Int64 Unity.Burst.BurstRuntime::GetHashCode64()")
				{
					if (!method.IsGenericInstance)
					{
						throw new ArgumentException("Method '" + method.FullName + "' is expected to be generic but is not");
					}
					writer.WriteManagedReturnStatement(HashStringWithFNV1A64((method as GenericInstanceMethod).GenericArguments[0].AssemblyQualifiedName()).ToString());
					return true;
				}
				if (fullName == "System.Int32 Unity.Burst.BurstRuntime::GetHashCode32()")
				{
					if (!method.IsGenericInstance)
					{
						throw new ArgumentException("Method '" + method.FullName + "' is expected to be generic but is not");
					}
					writer.WriteManagedReturnStatement(HashStringWithFNV1A32((method as GenericInstanceMethod).GenericArguments[0].AssemblyQualifiedName()).ToString());
					return true;
				}
			}
			return false;
		}

		public static void AddRetInstructionAtTheEndIfNeeded(MethodDefinition method)
		{
			if (!method.HasBody || !method.Body.HasExceptionHandlers || method.Body.Instructions[method.Body.Instructions.Count - 1].OpCode == OpCodes.Ret)
			{
				return;
			}
			ExceptionHandler exceptionHandler = method.Body.ExceptionHandlers[method.Body.ExceptionHandlers.Count - 1];
			if (exceptionHandler.HandlerEnd == null)
			{
				if (method.ReturnType.MetadataType != MetadataType.Void)
				{
					InjectEmptyVariableToTheStack(method.ReturnType, method.Body);
				}
				Instruction instruction = method.Body.Instructions[method.Body.Instructions.Count - 1];
				Instruction instruction2 = Instruction.Create(OpCodes.Ret);
				instruction2.Offset = instruction.Offset + instruction.GetSize();
				exceptionHandler.HandlerEnd = (method.ReturnType.IsVoid() ? instruction2 : instruction);
				method.Body.Instructions.Add(instruction2);
			}
		}

		private static void InjectEmptyVariableToTheStack(TypeReference type, MethodBody body)
		{
			Instruction instruction;
			if (!type.IsValueType())
			{
				instruction = Instruction.Create(OpCodes.Ldnull);
			}
			else if (type.IsPrimitive && type.MetadataType != MetadataType.UIntPtr && type.MetadataType != MetadataType.IntPtr)
			{
				switch (type.MetadataType)
				{
				case MetadataType.Boolean:
				case MetadataType.Char:
				case MetadataType.SByte:
				case MetadataType.Byte:
				case MetadataType.Int16:
				case MetadataType.UInt16:
				case MetadataType.Int32:
				case MetadataType.UInt32:
				case MetadataType.Int64:
				case MetadataType.UInt64:
					instruction = Instruction.Create(OpCodes.Ldc_I4_0);
					break;
				case MetadataType.Single:
					instruction = Instruction.Create(OpCodes.Ldc_R4, 0f);
					break;
				case MetadataType.Double:
					instruction = Instruction.Create(OpCodes.Ldc_R8, 0.0);
					break;
				default:
					throw new Exception();
				}
			}
			else
			{
				VariableDefinition variableDefinition = new VariableDefinition(type);
				body.Variables.Add(variableDefinition);
				instruction = Instruction.Create(OpCodes.Ldloc, variableDefinition);
			}
			body.Instructions.Add(instruction);
			instruction.Offset = instruction.Previous.Offset + instruction.Previous.GetSize();
		}

		private static int HashStringWithFNV1A32(string text)
		{
			uint num = 2166136261u;
			foreach (char c in text)
			{
				num = 16777619 * (num ^ (byte)(c & 0xFF));
				num = 16777619 * (num ^ (byte)((int)c >> 8));
			}
			return (int)num;
		}

		private static long HashStringWithFNV1A64(string text)
		{
			ulong num = 14695981039346656037uL;
			foreach (char c in text)
			{
				num = 1099511628211L * (num ^ (byte)(c & 0xFF));
				num = 1099511628211L * (num ^ (byte)((int)c >> 8));
			}
			return (long)num;
		}
	}
}
