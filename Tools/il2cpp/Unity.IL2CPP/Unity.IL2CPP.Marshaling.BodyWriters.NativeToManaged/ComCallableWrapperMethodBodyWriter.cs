using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	public class ComCallableWrapperMethodBodyWriter : NativeToManagedInteropMethodBodyWriter
	{
		protected virtual string ManagedObjectExpression { get; } = Emit.Call("GetManagedObjectInline");


		public ComCallableWrapperMethodBodyWriter(MinimalContext context, MethodReference managedMethod, MethodReference interfaceMethod, MarshalType marshalType)
			: base(context, managedMethod, interfaceMethod, marshalType, useUnicodeCharset: true)
		{
		}

		protected sealed override void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			MethodReturnType methodReturnType = GetMethodReturnType();
			bool isPreserveSig = InteropMethod.Resolve().IsPreserveSig;
			if (methodReturnType.ReturnType.MetadataType != MetadataType.Void && IsReturnValueMarshaled)
			{
				writer.WriteLine("{0} {1};", _context.Global.Services.Naming.ForVariable(_typeResolver.Resolve(methodReturnType.ReturnType)), _context.Global.Services.Naming.ForInteropReturnValue());
			}
			writer.WriteLine("try");
			using (new BlockWriter(writer))
			{
				WriteInteropCallStatementWithinTryBlock(writer, localVariableNames, metadataAccess);
			}
			writer.WriteLine("catch (const Il2CppExceptionWrapper& ex)");
			using (new BlockWriter(writer))
			{
				if (methodReturnType.ReturnType.MetadataType != MetadataType.Void && !isPreserveSig)
				{
					string text = _context.Global.Services.Naming.ForComInterfaceReturnParameterName();
					writer.WriteStatement(Emit.Memset(_context, text, 0, "sizeof(*" + text + ")"));
				}
				TypeResolver empty = TypeResolver.Empty;
				writer.AddIncludeForTypeDefinition(_context.Global.Services.TypeProvider.SystemString);
				writer.WriteLine(_context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.SystemString) + " exceptionStr = NULL;");
				writer.WriteLine("try");
				using (new BlockWriter(writer))
				{
					string[] argumentArray = new string[1] { "ex.ex" };
					MethodDefinition methodDefinition = _context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.Name == "ToString");
					MethodBodyWriter.WriteMethodCallExpression("exceptionStr", null, writer, _managedMethod, methodDefinition, methodDefinition, empty, MethodCallType.Virtual, metadataAccess, new VTableBuilder(), argumentArray, useArrayBoundsCheck: false);
				}
				writer.WriteLine("catch (const Il2CppExceptionWrapper&)");
				using (new BlockWriter(writer))
				{
					FieldDefinition fieldDefinition = _context.Global.Services.TypeProvider.SystemString.Fields.Single((FieldDefinition f) => f.Name == "Empty");
					string text2 = MethodBodyWriter.TypeStaticsExpressionFor(_context, fieldDefinition, empty, metadataAccess);
					writer.WriteLine("exceptionStr = " + text2 + _context.Global.Services.Naming.ForFieldGetter(fieldDefinition) + "();");
				}
				writer.WriteLine("il2cpp_codegen_store_exception_info(ex.ex, exceptionStr);");
				if (!isPreserveSig)
				{
					WriteExceptionReturnStatement(writer);
				}
				else
				{
					WritePreserveSigExceptionReturnStatement(writer, methodReturnType);
				}
			}
		}

		protected virtual void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			string text = "__thisValue";
			if (_managedMethod.DeclaringType.IsValueType())
			{
				writer.WriteLine("{0}* {1} = ({0}*)UnBox({3}, {2});", writer.Context.Global.Services.Naming.ForTypeNameOnly(_managedMethod.DeclaringType), text, metadataAccess.TypeInfoFor(_managedMethod.DeclaringType), ManagedObjectExpression);
			}
			else
			{
				writer.WriteLine("{0} {1} = ({0}){2};", writer.Context.Global.Services.Naming.ForVariable(_managedMethod.DeclaringType), text, ManagedObjectExpression);
			}
			if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void)
			{
				WriteMethodCallStatement(metadataAccess, text, localVariableNames, writer, writer.Context.Global.Services.Naming.ForInteropReturnValue());
			}
			else
			{
				WriteMethodCallStatement(metadataAccess, text, localVariableNames, writer);
			}
		}

		protected virtual void WriteExceptionReturnStatement(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteStatement("return ex.ex->hresult");
		}

		private void WritePreserveSigExceptionReturnStatement(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType)
		{
			if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
			{
				string text = "0";
				switch (methodReturnType.ReturnType.MetadataType)
				{
				case MetadataType.Int32:
				case MetadataType.UInt32:
					text = "ex.ex->hresult";
					break;
				case MetadataType.Single:
					text = "std::numeric_limits<float>::quiet_NaN()";
					break;
				case MetadataType.Double:
					text = "std::numeric_limits<double>::quiet_NaN()";
					break;
				case MetadataType.ValueType:
					text = _context.Global.Services.Naming.ForInteropReturnValue();
					writer.WriteStatement(Emit.Memset(_context, "&" + text, 0, "sizeof(" + text + ")"));
					break;
				}
				writer.WriteLine("return static_cast<{0}>({1});", MarshaledReturnType.DecoratedName, text);
			}
		}

		protected override void WriteReturnStatementEpilogue(IGeneratedMethodCodeWriter writer, string unmarshaledReturnValueVariableName)
		{
			if (InteropMethod.Resolve().IsPreserveSig)
			{
				if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void)
				{
					writer.WriteLine("return {0};", IsReturnValueMarshaled ? unmarshaledReturnValueVariableName : writer.Context.Global.Services.Naming.ForInteropReturnValue());
				}
				return;
			}
			if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void && IsReturnValueMarshaled)
			{
				writer.WriteLine("*{0} = {1};", writer.Context.Global.Services.Naming.ForComInterfaceReturnParameterName(), unmarshaledReturnValueVariableName);
			}
			writer.WriteLine("return IL2CPP_S_OK;");
		}

		protected void WriteRaiseManagedExceptionWithCustomHResult(IGeneratedMethodCodeWriter writer, MethodReference exceptionConstructor, int hresult, string hresultName, IRuntimeMetadataAccess metadataAccess, params string[] constructorArgs)
		{
			string text = "exception";
			PropertyDefinition propertyDefinition = writer.Context.Global.Services.TypeProvider.SystemException.Properties.Single((PropertyDefinition p) => p.Name == "HResult");
			TypeReference declaringType = exceptionConstructor.DeclaringType;
			writer.AddIncludeForTypeDefinition(declaringType);
			writer.AddIncludeForMethodDeclaration(exceptionConstructor);
			writer.AddIncludeForMethodDeclaration(propertyDefinition.SetMethod);
			writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(declaringType) + " " + text + " = " + Emit.NewObj(writer.Context, declaringType, metadataAccess) + ";");
			WriteMethodCallStatement(metadataAccess, text, exceptionConstructor, MethodCallType.Normal, writer, constructorArgs);
			writer.WriteLine("// " + hresultName);
			WriteMethodCallStatement(metadataAccess, text, propertyDefinition.SetMethod, MethodCallType.Normal, writer, hresult.ToString());
			writer.WriteStatement(Emit.RaiseManagedException(text));
		}
	}
}
