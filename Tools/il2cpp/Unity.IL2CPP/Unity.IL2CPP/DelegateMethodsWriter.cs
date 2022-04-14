using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public class DelegateMethodsWriter
	{
		private readonly SourceWritingContext _context;

		private readonly IGeneratedMethodCodeWriter _writer;

		private readonly string _methodPtrGetterName;

		private readonly string _methodPtrSetterName;

		private readonly string _methodGetterName;

		private readonly string _methodSetterName;

		private readonly string _isDelegateOpenGetterName;

		private readonly string _targetGetterName;

		private readonly string _targetSetterName;

		private readonly string _prevGetterName;

		private readonly string _delegateCountGetterName;

		private const string InvokeReturnVariable = "result";

		private const string FunctionPointerType = "FunctionPointerType";

		public DelegateMethodsWriter(IGeneratedMethodCodeWriter writer)
		{
			_context = writer.Context;
			_writer = writer;
			FieldDefinition field = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "method_ptr");
			_methodPtrGetterName = _context.Global.Services.Naming.ForFieldGetter(field);
			_methodPtrSetterName = _context.Global.Services.Naming.ForFieldSetter(field);
			if (!_context.Global.Parameters.UsingTinyClassLibraries)
			{
				FieldDefinition field2 = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "method");
				_methodGetterName = _context.Global.Services.Naming.ForFieldGetter(field2);
				_methodSetterName = _context.Global.Services.Naming.ForFieldSetter(field2);
			}
			else
			{
				FieldDefinition field3 = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "m_IsDelegateOpen");
				_isDelegateOpenGetterName = _context.Global.Services.Naming.ForFieldGetter(field3);
			}
			FieldDefinition field4 = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "m_target");
			_targetGetterName = _context.Global.Services.Naming.ForFieldGetter(field4);
			_targetSetterName = _context.Global.Services.Naming.ForFieldSetter(field4);
			string expectedName = "delegates";
			FieldDefinition fieldDefinition = _context.Global.Services.TypeProvider.SystemMulticastDelegate.Fields.SingleOrDefault((FieldDefinition f) => f.Name == expectedName);
			if (fieldDefinition != null)
			{
				_prevGetterName = _context.Global.Services.Naming.ForFieldGetter(fieldDefinition);
			}
			FieldDefinition fieldDefinition2 = _context.Global.Services.TypeProvider.SystemMulticastDelegate.Fields.SingleOrDefault((FieldDefinition f) => f.Name == "delegateCount");
			if (fieldDefinition2 != null)
			{
				_delegateCountGetterName = _context.Global.Services.Naming.ForFieldGetter(fieldDefinition2);
			}
		}

		public void WriteMethodBodyForIsRuntimeMethod(MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			TypeDefinition typeDefinition = method.DeclaringType.Resolve();
			if (typeDefinition.BaseType.FullName != "System.MulticastDelegate")
			{
				throw new NotSupportedException("Cannot WriteMethodBodyForIsRuntimeMethod for non multicase delegate type: " + typeDefinition.FullName);
			}
			switch (method.Name)
			{
			case "Invoke":
				WriteMethodBodyForInvoke(method);
				break;
			case "BeginInvoke":
				WriteMethodBodyForBeginInvoke(method, metadataAccess);
				break;
			case "EndInvoke":
				WriteMethodBodyForDelegateEndInvoke(method);
				break;
			case ".ctor":
				WriteMethodBodyForDelegateConstructor(method);
				break;
			default:
				_writer.WriteDefaultReturn(TypeResolverFor(method).Resolve(GenericParameterResolver.ResolveReturnTypeIfNeeded(method)));
				break;
			}
		}

		public static void EmitTinyDelegateExtraFieldSetters(IGeneratedMethodCodeWriter writer, string delegateInstanceVariable, string reversePInvokeWrapper, string delegateOpenValue)
		{
			FieldDefinition field = writer.Context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "m_ReversePInvokeWrapperPtr");
			string text = writer.Context.Global.Services.Naming.ForFieldSetter(field);
			FieldDefinition field2 = writer.Context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "m_IsDelegateOpen");
			string text2 = writer.Context.Global.Services.Naming.ForFieldSetter(field2);
			writer.WriteStatement(Emit.Call(delegateInstanceVariable + "->" + text, "reinterpret_cast<void*>(" + reversePInvokeWrapper + ")"));
			writer.WriteStatement(Emit.Call(delegateInstanceVariable + "->" + text2, delegateOpenValue));
		}

		private static TypeResolver TypeResolverFor(MethodReference method)
		{
			return new TypeResolver(method.DeclaringType as GenericInstanceType, method as GenericInstanceMethod);
		}

		private void WriteMethodBodyForDelegateConstructor(MethodReference method)
		{
			string text = _context.Global.Services.Naming.ForParameterName(method.Parameters[0]);
			string text2 = _context.Global.Services.Naming.ForParameterName(method.Parameters[1]);
			if (!_context.Global.Parameters.UsingTinyBackend)
			{
				if (_context.Global.Parameters.UsingTinyClassLibraries)
				{
					WriteLine("{0}((intptr_t)il2cpp_codegen_get_method_pointer((RuntimeMethod*){1}));", ExpressionForFieldOfThis(_methodPtrSetterName), text2);
				}
				else
				{
					WriteLine("{0}(il2cpp_codegen_get_method_pointer((RuntimeMethod*){1}));", ExpressionForFieldOfThis(_methodPtrSetterName), text2);
					WriteLine("{0}({1});", ExpressionForFieldOfThis(_methodSetterName), text2);
				}
			}
			else
			{
				WriteLine("{0}({1});", ExpressionForFieldOfThis(_methodPtrSetterName), text2);
			}
			WriteLine("{0}({1});", ExpressionForFieldOfThis(_targetSetterName), text);
		}

		private void WriteMethodBodyForInvoke(MethodReference method)
		{
			if (method.ReturnType.MetadataType != MetadataType.Void)
			{
				_writer.WriteVariable(TypeResolverFor(method).ResolveReturnType(method), "result");
			}
			WriteInvocationsForDelegate45(method);
			if (method.ReturnType.MetadataType != MetadataType.Void && !_writer.Context.Global.Parameters.ReturnAsByRefParameter)
			{
				_writer.WriteStatement("return result");
			}
		}

		private void WriteInvocationsForDelegate45(MethodReference method)
		{
			if (_prevGetterName == null)
			{
				WriteInvocationsForDelegate("__this", method);
				return;
			}
			string text = "length";
			string text2 = "delegateArrayToInvoke";
			string text3 = "delegatesToInvoke";
			string text4 = "currentDelegate";
			string text5 = ExpressionForFieldOfThis(_prevGetterName);
			FieldDefinition fieldDefinition = _context.Global.Services.TypeProvider.SystemMulticastDelegate.Fields.Single((FieldDefinition f) => f.Name == "delegates");
			string text6 = _context.Global.Services.Naming.ForVariable(fieldDefinition.FieldType);
			_writer.AddIncludeForTypeDefinition(fieldDefinition.FieldType);
			_writer.WriteLine("{0} {1} = {2}();", text6, text2, text5);
			_writer.WriteLine("{0}** {1};", "Delegate_t", text3);
			_writer.WriteLine("{0} {1};", "il2cpp_array_size_t", text);
			_writer.WriteLine("if ({0} != NULL)", text2);
			using (new BlockWriter(_writer))
			{
				string text7 = ((_delegateCountGetterName != null) ? (ExpressionForFieldOfThis(_delegateCountGetterName) + "()") : (text2 + "->max_length"));
				_writer.WriteLine(text + " = " + text7 + ";");
				_writer.WriteLine("{0} = reinterpret_cast<{1}**>({2}->{3}(0));", text3, "Delegate_t", text2, ArrayNaming.ForArrayItemAddressGetter(useArrayBoundsCheck: false));
			}
			_writer.WriteLine("else");
			using (new BlockWriter(_writer))
			{
				_writer.WriteLine("{0} = 1;", text);
				_writer.WriteLine("{0} = reinterpret_cast<{1}**>(&{2});", text3, "Delegate_t", "__this");
			}
			_writer.WriteLine();
			_writer.WriteLine("for ({0} i = 0; i < {1}; i++)", "il2cpp_array_size_t", text);
			using (new BlockWriter(_writer))
			{
				_writer.WriteLine("{2}* {0} = {1}[i];", text4, text3, "Delegate_t");
				WriteInvocationsForDelegate(text4, method);
			}
		}

		private void WriteInvocationsForDelegate(string delegateVariableName, MethodReference method)
		{
			string text = ((!_context.Global.Parameters.UsingTinyClassLibraries) ? "Il2CppMethodPointer" : "intptr_t");
			string text2 = "targetMethodPointer";
			WriteLine(text + " " + text2 + " = " + ExpressionForFieldOf(delegateVariableName, _methodPtrGetterName) + "();");
			string text3 = "targetThis";
			WriteLine("RuntimeObject* " + text3 + " = " + ExpressionForFieldOf(delegateVariableName, _targetGetterName) + "();");
			if (!_context.Global.Parameters.UsingTinyClassLibraries)
			{
				string text4 = $"(RuntimeMethod*)({ExpressionForFieldOf(delegateVariableName, _methodGetterName)}())";
				WriteLine("RuntimeMethod* targetMethod = " + text4 + ";");
				WriteLine("if (!il2cpp_codegen_method_is_virtual(targetMethod))");
				_writer.BeginBlock();
				WriteLine("il2cpp_codegen_raise_execution_engine_exception_if_method_is_not_found({0});", "targetMethod");
				_writer.EndBlock();
				WriteLine("bool ___methodIsStatic = MethodIsStatic(targetMethod);");
				WriteLine("int ___parameterCount = il2cpp_codegen_method_parameter_count(targetMethod);");
				WriteLine("if (___methodIsStatic)");
				using (new BlockWriter(_writer))
				{
					WriteLine(string.Format("if ({0} == {1})", "___parameterCount", method.Parameters.Count));
					using (new BlockWriter(_writer))
					{
						_writer.WriteCommentedLine("open");
						EmitInvocation(text3, method, text2, "targetMethod", openDelegate: true, forStatic: true);
					}
					WriteLine("else");
					using (new BlockWriter(_writer))
					{
						_writer.WriteCommentedLine("closed");
						EmitInvocation(text3, method, text2, "targetMethod", openDelegate: false, forStatic: true);
					}
				}
				if (ShouldEmitOpenInstanceInvocation(method))
				{
					WriteLine(string.Format("else if ({0} != {1})", "___parameterCount", method.Parameters.Count));
					using (new BlockWriter(_writer))
					{
						_writer.WriteCommentedLine("open");
						EmitInvocation(text3, method, text2, "targetMethod", openDelegate: true);
					}
				}
				WriteLine("else");
				using (new BlockWriter(_writer))
				{
					_writer.WriteCommentedLine("closed");
					EmitInvocation(text3, method, text2, "targetMethod");
					return;
				}
			}
			WriteLine("if (" + ExpressionForFieldOf(delegateVariableName, _isDelegateOpenGetterName) + "())");
			using (new BlockWriter(_writer))
			{
				EmitInvocation(text3, method, text2, "targetMethod", openDelegate: true);
			}
			WriteLine("else");
			using (new BlockWriter(_writer))
			{
				EmitInvocation(text3, method, text2, "targetMethod");
			}
		}

		private void WriteInvokeChainedDelegates(MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			if (_prevGetterName == null)
			{
				return;
			}
			List<string> list = MethodSignatureWriter.ParametersFor(_context, method, ParameterFormat.WithNameNoThis).ToList();
			string text = ExpressionForFieldOfThis(_prevGetterName) + "()";
			list.Insert(0, "(" + _context.Global.Services.Naming.ForVariable(method.DeclaringType) + ")" + text);
			list.Add("method");
			WriteLine("if({0} != NULL)", text);
			using (new BlockWriter(_writer))
			{
				_writer.WriteStatement(Emit.Call(metadataAccess.Method(method), list));
			}
		}

		private static bool ShouldEmitOpenInstanceInvocation(MethodReference method)
		{
			if (!method.Parameters.Any())
			{
				return false;
			}
			ParameterDefinition parameterDefinition = method.Parameters[0];
			TypeReference typeReference = TypeResolverFor(method).ResolveParameterType(method, parameterDefinition);
			if (typeReference.IsValueType())
			{
				return false;
			}
			if (typeReference.IsPointer)
			{
				return false;
			}
			if (typeReference.IsByReference)
			{
				return false;
			}
			if (parameterDefinition.IsIn)
			{
				return false;
			}
			return true;
		}

		private string GetVirtualDelegateInvocationCondition(string methodInfoExpression, string targetVariableName, bool openDelegate)
		{
			if (openDelegate)
			{
				return "il2cpp_codegen_method_is_virtual(" + methodInfoExpression + ") && il2cpp_codegen_delegate_has_invoker((Il2CppDelegate*)__this)";
			}
			return targetVariableName + " != NULL && il2cpp_codegen_method_is_virtual(" + methodInfoExpression + ") && !il2cpp_codegen_object_is_of_sealed_type(" + targetVariableName + ") && il2cpp_codegen_delegate_has_invoker((Il2CppDelegate*)__this)";
		}

		private void EmitInvocation(string targetVariableName, MethodReference method, string methodPtrExpression, string methodInfoExpression, bool openDelegate = false, bool forStatic = false)
		{
			List<string> list = MethodSignatureWriter.ParametersFor(_context, method, ParameterFormat.WithTypeThisObject, !_context.Global.Parameters.UsingTinyClassLibraries, useVoidPointerForThis: true).ToList();
			List<string> list2 = MethodSignatureWriter.ParametersFor(_context, method, ParameterFormat.WithNameNoThis, includeHiddenMethodInfo: false, useVoidPointerForThis: true).ToList();
			if (openDelegate)
			{
				list.RemoveAt(0);
			}
			else
			{
				list2.Insert(0, targetVariableName);
			}
			if (!_context.Global.Parameters.UsingTinyClassLibraries)
			{
				list2.Add(methodInfoExpression);
				if (!forStatic)
				{
					WriteLine("if (" + GetVirtualDelegateInvocationCondition(methodInfoExpression, targetVariableName, openDelegate) + ")");
					using (new BlockWriter(_writer))
					{
						bool startWithElse = false;
						IEnumerable<string> parametersWithoutHiddenMethodInfo = list2.Take(list2.Count - 1);
						EmitInvokersForVirtualMethods(method, methodInfoExpression, openDelegate, parametersWithoutHiddenMethodInfo, startWithElse);
					}
					WriteLine("else");
					_writer.BeginBlock();
				}
			}
			bool flag = false;
			if (!_context.Global.Parameters.UsingTinyClassLibraries)
			{
				bool flag2 = (_context.Global.Parameters.ReturnAsByRefParameter ? (list2.Count > 3) : (list2.Count > 2));
				if (!forStatic && !openDelegate && flag2)
				{
					flag = EmitInvokerForInstanceMethodWithNullThis(method, methodPtrExpression, list, list2);
				}
			}
			if (flag)
			{
				WriteLine("else");
				_writer.BeginBlock();
			}
			if (_context.Global.Parameters.UsingTinyClassLibraries && !_context.Global.Parameters.UsingTinyBackend)
			{
				list.Add("const RuntimeMethod*");
				list2.Add("method");
			}
			WriteLine("typedef {0} (*{1}) ({2});", MethodSignatureWriter.FormatReturnType(_context, TypeResolverFor(method).ResolveReturnType(method)), "FunctionPointerType", list.AggregateWithComma());
			string text = Emit.Call("((FunctionPointerType)" + methodPtrExpression + ")", list2);
			_writer.WriteStatement((method.ReturnType.MetadataType != MetadataType.Void && !_writer.Context.Global.Parameters.ReturnAsByRefParameter) ? Emit.Assign("result", text) : text);
			if (flag)
			{
				_writer.EndBlock();
			}
			if (!forStatic && !_context.Global.Parameters.UsingTinyClassLibraries)
			{
				_writer.EndBlock();
			}
		}

		private bool EmitInvokerForInstanceMethodWithNullThis(MethodReference method, string methodPtrExpression, List<string> parametersForTypeDef, List<string> parametersForInvocation)
		{
			List<string> list = parametersForInvocation.Skip(1).ToList();
			TypeReference typeReference = GenericParameterResolver.ResolveParameterTypeIfNeeded(method, method.Parameters[0]);
			if (typeReference.GetNonPinnedAndNonByReferenceType().IsPrimitive)
			{
				return false;
			}
			bool flag = typeReference.GetNonPinnedAndNonByReferenceType().IsValueType();
			if (flag)
			{
				parametersForTypeDef = parametersForTypeDef.ToList();
				parametersForTypeDef[1] = "RuntimeObject*";
			}
			WriteLine("if (targetThis == NULL)");
			_writer.BeginBlock();
			WriteLine("typedef {0} (*{1}) ({2});", _context.Global.Services.Naming.ForVariable(TypeResolverFor(method).ResolveReturnType(method)), "FunctionPointerType", parametersForTypeDef.Skip(1).AggregateWithComma());
			if (flag)
			{
				string text = (typeReference.WithoutModifiers().IsByReference ? "" : "&");
				list[0] = "(" + parametersForTypeDef.Skip(1).First() + ")(reinterpret_cast<RuntimeObject*>(" + text + list[0] + ") - 1)";
			}
			string text2 = Emit.Call("((FunctionPointerType)" + methodPtrExpression + ")", list);
			_writer.WriteStatement((method.ReturnType.MetadataType != MetadataType.Void) ? Emit.Assign("result", text2) : text2);
			_writer.EndBlock();
			return true;
		}

		private void EmitInvokersForVirtualMethods(MethodReference method, string methodInfoExpression, bool openDelegate, IEnumerable<string> parametersWithoutHiddenMethodInfo, bool startWithElse)
		{
			WriteLine("{0}if (il2cpp_codegen_method_is_generic_instance({1}))", startWithElse ? "else " : string.Empty, methodInfoExpression);
			_writer.BeginBlock();
			EmitInvokersForGenericInstanceMethods(method, methodInfoExpression, openDelegate, parametersWithoutHiddenMethodInfo);
			_writer.EndBlock();
			WriteLine("else");
			_writer.BeginBlock();
			EmitInvokersForNonGenericMethods(method, methodInfoExpression, openDelegate, parametersWithoutHiddenMethodInfo);
			_writer.EndBlock();
		}

		private void EmitInvokersForGenericInstanceMethods(MethodReference method, string methodInfoExpression, bool openDelegate, IEnumerable<string> parametersWithoutHiddenMethodInfo)
		{
			List<string> list = new List<string> { methodInfoExpression };
			list.AddRange(parametersWithoutHiddenMethodInfo);
			WriteLine("if (il2cpp_codegen_method_is_interface_method({0}))", methodInfoExpression);
			EmitInvoker(method, openDelegate, list, isInterfaceMethod: true, isGenericInstance: true);
			WriteLine("else");
			EmitInvoker(method, openDelegate, list, isInterfaceMethod: false, isGenericInstance: true);
		}

		private void EmitInvokersForNonGenericMethods(MethodReference method, string methodInfoExpression, bool openDelegate, IEnumerable<string> parametersWithoutHiddenMethodInfo)
		{
			List<string> first = new List<string> { "il2cpp_codegen_method_get_slot(" + methodInfoExpression + ")" };
			List<string> second = new List<string> { "il2cpp_codegen_method_get_declaring_type(" + methodInfoExpression + ")" };
			IEnumerable<string> parameters = first.Concat(second).Concat(parametersWithoutHiddenMethodInfo);
			IEnumerable<string> parameters2 = first.Concat(parametersWithoutHiddenMethodInfo);
			WriteLine("if (il2cpp_codegen_method_is_interface_method({0}))", methodInfoExpression);
			EmitInvoker(method, openDelegate, parameters, isInterfaceMethod: true, isGenericInstance: false);
			WriteLine("else");
			EmitInvoker(method, openDelegate, parameters2, isInterfaceMethod: false, isGenericInstance: false);
		}

		private void EmitInvoker(MethodReference method, bool openDelegate, IEnumerable<string> parameters, bool isInterfaceMethod, bool isGenericInstance)
		{
			string text = Emit.Call(_writer.VirtualCallInvokeMethod(method, TypeResolverFor(method), openDelegate, isInterfaceMethod, isGenericInstance), parameters);
			_writer.Indent();
			_writer.WriteStatement((method.ReturnType.MetadataType != MetadataType.Void && !_writer.Context.Global.Parameters.ReturnAsByRefParameter) ? Emit.Assign("result", text) : text);
			_writer.Dedent();
		}

		private static string ExpressionForFieldOfThis(string targetFieldName)
		{
			return ExpressionForFieldOf("__this", targetFieldName);
		}

		private static string ExpressionForFieldOf(string variableName, string targetFieldName)
		{
			return $"{variableName}->{targetFieldName}";
		}

		private void WriteMethodBodyForBeginInvoke(MethodReference method, IRuntimeMetadataAccess metadataAccess)
		{
			WriteLine("void *__d_args[{0}] = {{0}};", method.Parameters.Count - 1);
			TypeResolver typeResolver = TypeResolverFor(method);
			if (BeginInvokeHasAdditionalParameters(method))
			{
				for (int i = 0; i < method.Parameters.Count - 2; i++)
				{
					ParameterDefinition parameterDefinition = method.Parameters[i];
					TypeReference typeReference = typeResolver.ResolveParameterType(method, parameterDefinition);
					string text = _context.Global.Services.Naming.ForParameterName(parameterDefinition);
					if (typeReference.IsByReference)
					{
						text = ValueStringForByReferenceType(metadataAccess, (ByReferenceType)typeReference, text);
					}
					else if (parameterDefinition.IsIn && typeReference is RequiredModifierType)
					{
						TypeReference elementType = ((RequiredModifierType)typeReference).ElementType;
						text = ValueStringForByReferenceType(metadataAccess, (ByReferenceType)elementType, text);
					}
					else if (typeReference.IsValueType())
					{
						text = Emit.Box(_context, typeReference, text, metadataAccess);
					}
					WriteLine("__d_args[{0}] = {1};", i, text);
				}
			}
			_writer.WriteManagedReturnStatement($"({_context.Global.Services.Naming.ForVariable(typeResolver.ResolveReturnType(method))})il2cpp_codegen_delegate_begin_invoke((RuntimeDelegate*)__this, __d_args, (RuntimeDelegate*){_context.Global.Services.Naming.ForParameterName(method.Parameters[method.Parameters.Count - 2])}, (RuntimeObject*){_context.Global.Services.Naming.ForParameterName(method.Parameters[method.Parameters.Count - 1])});");
		}

		private string ValueStringForByReferenceType(IRuntimeMetadataAccess metadataAccess, ByReferenceType parameterType, string valueString)
		{
			TypeReference elementType = parameterType.ElementType;
			valueString = (elementType.IsValueType() ? Emit.Box(_context, elementType, Emit.Dereference(valueString), metadataAccess) : Emit.Dereference(valueString));
			return valueString;
		}

		private static bool BeginInvokeHasAdditionalParameters(MethodReference method)
		{
			return method.Parameters.Count > 2;
		}

		private void WriteMethodBodyForDelegateEndInvoke(MethodReference method)
		{
			ParameterDefinition parameterReference = method.Parameters[method.Parameters.Count - 1];
			string text = "0";
			List<string> list = CollectOutArgsIfAny(method);
			if (list.Count > 0)
			{
				WriteLine("void* ___out_args[] = {");
				foreach (string item in list)
				{
					WriteLine("{0},", item);
				}
				WriteLine("};");
				text = "___out_args";
			}
			if (method.ReturnType.MetadataType == MetadataType.Void)
			{
				WriteLine("il2cpp_codegen_delegate_end_invoke((Il2CppAsyncResult*) {0}, {1});", _context.Global.Services.Naming.ForParameterName(parameterReference), text);
				return;
			}
			WriteLine("RuntimeObject *__result = il2cpp_codegen_delegate_end_invoke((Il2CppAsyncResult*) {0}, {1});", _context.Global.Services.Naming.ForParameterName(parameterReference), text);
			TypeReference typeReference = TypeResolverFor(method).ResolveReturnType(method);
			if (!typeReference.IsValueType())
			{
				_writer.WriteManagedReturnStatement("(" + _context.Global.Services.Naming.ForVariable(typeReference) + ")__result;");
			}
			else
			{
				_writer.WriteManagedReturnStatement("*" + Emit.Cast(_context, new PointerType(typeReference), "UnBox ((RuntimeObject*)__result)") + ";");
			}
		}

		private List<string> CollectOutArgsIfAny(MethodReference method)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < method.Parameters.Count - 1; i++)
			{
				if (method.Parameters[i].ParameterType.IsByReference)
				{
					list.Add(_context.Global.Services.Naming.ForParameterName(method.Parameters[i]));
				}
			}
			return list;
		}

		private void WriteLine(string format, params object[] args)
		{
			_writer.WriteLine(format, args);
		}
	}
}
