using System;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal sealed class ListCCWWriter : IProjectedComCallableWrapperMethodWriter
	{
		private class ExceptionWithEBoundsHResultMethodBodyWriter : ProjectedMethodBodyWriter
		{
			public ExceptionWithEBoundsHResultMethodBodyWriter(MinimalContext context, MethodReference getItemMethod, MethodReference method)
				: base(context, getItemMethod, method)
			{
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				writer.WriteLine("try");
				using (new BlockWriter(writer))
				{
					if (_managedMethod.ReturnType.MetadataType != MetadataType.Void)
					{
						WriteMethodCallStatement(metadataAccess, ManagedObjectExpression, localVariableNames, writer, _context.Global.Services.Naming.ForInteropReturnValue());
					}
					else
					{
						WriteMethodCallStatement(metadataAccess, ManagedObjectExpression, localVariableNames, writer);
					}
				}
				writer.WriteLine("catch (const Il2CppExceptionWrapper& ex)");
				using (new BlockWriter(writer))
				{
					TypeReference type = new TypeReference("System", "ArgumentOutOfRangeException", _context.Global.Services.TypeProvider.Corlib.MainModule, _context.Global.Services.TypeProvider.Corlib.Name);
					writer.WriteLine("if (IsInst(ex.ex, " + metadataAccess.TypeInfoFor(type) + "))");
					using (new BlockWriter(writer))
					{
						writer.WriteLine($"ex.ex->hresult = {-2147483637}; // E_BOUNDS");
					}
					writer.WriteLine();
					writer.WriteLine("throw;");
				}
			}
		}

		private class GetManyMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly TypeReference _itemType;

			private readonly MethodReference _getCountMethod;

			private readonly MethodReference _getItemMethod;

			private readonly TypeDefinition _argumentOutOfRangeException;

			private readonly MethodDefinition _argumentOutOfRangeExceptionConstructor;

			protected override bool AreParametersMarshaled { get; }

			public GetManyMethodBodyWriter(MinimalContext context, MethodReference getCountMethod, MethodReference getItemMethod, MethodReference method)
				: base(context, method, method)
			{
				_itemType = ((GenericInstanceType)getCountMethod.DeclaringType).GenericArguments[0];
				_getCountMethod = getCountMethod;
				_getItemMethod = getItemMethod;
				_argumentOutOfRangeException = new TypeReference("System", "ArgumentOutOfRangeException", context.Global.Services.TypeProvider.Corlib.MainModule, context.Global.Services.TypeProvider.Corlib.Name).Resolve();
				_argumentOutOfRangeExceptionConstructor = _argumentOutOfRangeException.Methods.Single((MethodDefinition m) => m.IsConstructor && m.HasThis && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				string variableName = MarshaledParameterTypes[0].VariableName;
				string variableName2 = MarshaledParameterTypes[1].VariableName;
				string variableName3 = MarshaledParameterTypes[2].VariableName;
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, _itemType, MarshalType.WindowsRuntime, null, useUnicodeCharSet: false, forByReferenceType: false, forFieldMarshaling: true);
				string text = "elementsInCollection";
				string text2 = "signedElementsInCollection";
				writer.WriteLine("uint32_t " + text + ";");
				writer.WriteLine("int32_t " + text2 + ";");
				WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _getCountMethod, MethodCallType.Virtual, writer, text2);
				writer.WriteStatement(text + " = " + text2);
				writer.WriteLine("if (" + variableName + " != " + text + " && " + variableName3 + " != NULL)");
				using (new BlockWriter(writer))
				{
					writer.WriteLine($"if ({variableName} > {text} || {variableName} > {int.MaxValue})");
					using (new BlockWriter(writer))
					{
						WriteRaiseManagedExceptionWithCustomHResult(writer, _argumentOutOfRangeExceptionConstructor, -2147483637, "E_BOUNDS", metadataAccess, metadataAccess.StringLiteral("index"));
					}
					writer.AddStdInclude("algorithm");
					writer.WriteLine(_context.Global.Services.Naming.ForInteropReturnValue() + " = std::min(" + variableName2 + ", " + text + " - " + variableName + ");");
					writer.WriteLine("for (uint32_t i = 0; i < " + _context.Global.Services.Naming.ForInteropReturnValue() + "; i++)");
					using (new BlockWriter(writer))
					{
						writer.WriteLine(_context.Global.Services.Naming.ForVariable(_itemType) + " itemManaged;");
						WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _getItemMethod, MethodCallType.Virtual, writer, "itemManaged", "i + " + variableName);
						defaultMarshalInfoWriter.WriteMarshalVariableToNative(writer, new ManagedMarshalValue(_context, "itemManaged"), variableName3 + "[i]", null, metadataAccess);
					}
				}
				writer.WriteLine("else");
				using (new BlockWriter(writer))
				{
					writer.WriteLine(_context.Global.Services.Naming.ForInteropReturnValue() + " = 0;");
				}
			}
		}

		private class GetViewMethodBodyWriter : ProjectedMethodBodyWriter
		{
			public GetViewMethodBodyWriter(MinimalContext context, MethodReference method)
				: base(context, method, method)
			{
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				TypeReference type = _typeResolver.Resolve(InteropMethod.ReturnType);
				TypeDefinition typeDefinition = writer.Context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.ObjectModel", "ReadOnlyCollection`1");
				TypeReference typeReference = _typeResolver.Resolve(typeDefinition);
				MethodReference method = _typeResolver.Resolve(typeDefinition.Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Resolve().FullName == "System.Collections.Generic.IList`1"));
				writer.AddIncludeForTypeDefinition(typeReference);
				string text = "__thisValue";
				writer.WriteLine(_context.Global.Services.Naming.ForVariable(InteropMethod.DeclaringType) + " " + text + " = " + ManagedObjectExpression + ";");
				writer.WriteLine("if (IsInst(" + text + ", " + metadataAccess.TypeInfoFor(type) + "))");
				using (new BlockWriter(writer))
				{
					writer.WriteLine(_context.Global.Services.Naming.ForInteropReturnValue() + " = " + text + ";");
				}
				writer.WriteLine("else");
				using (new BlockWriter(writer))
				{
					writer.WriteLine(_context.Global.Services.Naming.ForVariable(typeReference) + " readOnlyCollection = " + Emit.NewObj(writer.Context, _typeResolver.Resolve(typeReference), metadataAccess) + ";");
					WriteMethodCallStatement(metadataAccess, "readOnlyCollection", method, MethodCallType.Normal, writer, text);
					writer.WriteLine(_context.Global.Services.Naming.ForInteropReturnValue() + " = readOnlyCollection;");
				}
			}
		}

		private class NonGenericGetViewMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly TypeDefinition _adapterType;

			private readonly MethodDefinition _adapterCtor;

			public NonGenericGetViewMethodBodyWriter(MinimalContext context, MethodReference method)
				: base(context, method, method)
			{
				AssemblyNameReference assembly = new AssemblyNameReference("System.Runtime.WindowsRuntime.UI.Xaml", new Version(4, 0, 0, 0));
				_adapterType = context.Global.Services.TypeProvider.OptionalResolve("System.Runtime.InteropServices.WindowsRuntime.Xaml", "ListToBindableVectorViewAdapter", assembly);
				if (_adapterType == null)
				{
					throw new InvalidOperationException("Failed to resolve ListToBindableVectorViewAdapter, which is required for generating marshaling code for IBindableVector.GetView(). Is linker broken?");
				}
				_adapterCtor = _adapterType.Methods.SingleOrDefault((MethodDefinition m) => m.IsConstructor);
				if (_adapterCtor == null)
				{
					throw new InvalidOperationException("Failed to find ListToBindableVectorViewAdapter constructor, which is required for generating marshaling code for IBindableVector.GetView(). Is linker broken?");
				}
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				string text = "viewAdapter";
				writer.AddIncludeForTypeDefinition(_adapterType);
				writer.AddIncludeForMethodDeclaration(_adapterCtor);
				writer.WriteStatement(Emit.Assign(_context.Global.Services.Naming.ForVariable(_adapterType) + " " + text, Emit.NewObj(writer.Context, _adapterType, metadataAccess)));
				WriteMethodCallStatement(metadataAccess, text, _adapterCtor, MethodCallType.Normal, writer, ManagedObjectExpression);
				writer.WriteStatement(Emit.Assign(_context.Global.Services.Naming.ForInteropReturnValue(), text));
			}
		}

		private class IndexOfMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly TypeReference _itemType;

			private readonly MethodReference _getCountMethod;

			private readonly MethodReference _getItemMethod;

			public IndexOfMethodBodyWriter(MinimalContext context, MethodReference getCountMethod, MethodReference getItemMethod, MethodReference method)
				: base(context, method, method)
			{
				GenericInstanceType genericInstanceType = getCountMethod.DeclaringType as GenericInstanceType;
				_itemType = ((genericInstanceType != null) ? genericInstanceType.GenericArguments[0] : context.Global.Services.TypeProvider.ObjectTypeReference);
				_getCountMethod = getCountMethod;
				_getItemMethod = getItemMethod;
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				string value = localVariableNames[0];
				string value2 = localVariableNames[1];
				writer.WriteLine(Emit.Dereference(value2) + " = 0;");
				writer.WriteLine(writer.Context.Global.Services.Naming.ForInteropReturnValue() + " = false;");
				writer.WriteLine();
				string text = "elementsInCollection";
				writer.WriteLine("int " + text + ";");
				WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _getCountMethod, MethodCallType.Virtual, writer, text);
				writer.WriteLine("for (int i = 0; i < " + text + "; i++)");
				using (new BlockWriter(writer))
				{
					writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(_itemType) + " item;");
					WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _getItemMethod, MethodCallType.Virtual, writer, "item", "i");
					string text2 = "compareResult";
					writer.WriteLine("bool " + text2 + ";");
					WriteComparisonExpression(writer, "item", value, metadataAccess, text2);
					writer.WriteLine("if (" + text2 + ")");
					using (new BlockWriter(writer))
					{
						writer.WriteLine(Emit.Dereference(value2) + " = static_cast<uint32_t>(i);");
						writer.WriteLine(writer.Context.Global.Services.Naming.ForInteropReturnValue() + " = true;");
						writer.WriteLine("break;");
					}
				}
			}

			private void WriteComparisonExpression(IGeneratedMethodCodeWriter writer, string value1, string value2, IRuntimeMetadataAccess metadataAccess, string resultVariable)
			{
				switch (_itemType.MetadataType)
				{
				case MetadataType.SByte:
				case MetadataType.Byte:
				case MetadataType.Int16:
				case MetadataType.UInt16:
				case MetadataType.Int32:
				case MetadataType.UInt32:
				case MetadataType.Int64:
				case MetadataType.UInt64:
				case MetadataType.Single:
				case MetadataType.Double:
				case MetadataType.IntPtr:
				case MetadataType.UIntPtr:
					writer.WriteStatement(resultVariable + " = " + value1 + " == " + value2);
					return;
				case MetadataType.String:
				{
					MethodDefinition method = writer.Context.Global.Services.TypeProvider.SystemString.Methods.Single((MethodDefinition m) => !m.HasThis && m.Name == "Equals" && m.Parameters.Count == 2 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String && m.Parameters[1].ParameterType.MetadataType == MetadataType.String);
					writer.AddIncludeForMethodDeclaration(method);
					WriteMethodCallStatementWithResult(metadataAccess, "NULL", method, MethodCallType.Normal, writer, resultVariable, value1, value2);
					return;
				}
				}
				MethodDefinition method2 = writer.Context.Global.Services.TypeProvider.SystemObject.Methods.Single((MethodDefinition m) => m.HasThis && m.IsVirtual && m.Name == "Equals" && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.Object);
				if (_itemType.IsValueType())
				{
					MethodReference virtualMethodTargetMethodForConstrainedCallOnValueType = new VTableBuilder().GetVirtualMethodTargetMethodForConstrainedCallOnValueType(_context, _itemType, method2);
					if (virtualMethodTargetMethodForConstrainedCallOnValueType != null && TypeReferenceEqualityComparer.AreEqual(virtualMethodTargetMethodForConstrainedCallOnValueType.DeclaringType, _itemType))
					{
						writer.AddIncludeForMethodDeclaration(virtualMethodTargetMethodForConstrainedCallOnValueType);
						WriteMethodCallStatementWithResult(metadataAccess, Emit.AddressOf(value1), virtualMethodTargetMethodForConstrainedCallOnValueType, MethodCallType.Normal, writer, resultVariable, Emit.Box(writer.Context, _itemType, value2, metadataAccess));
						return;
					}
					value1 = Emit.Box(writer.Context, _itemType, value1, metadataAccess);
					value2 = Emit.Box(writer.Context, _itemType, value2, metadataAccess);
				}
				WriteMethodCallStatementWithResult(metadataAccess, value1, method2, MethodCallType.Virtual, writer, resultVariable, value2);
			}
		}

		private class RemoveAtEndMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly MethodReference _getCountMethod;

			private readonly MethodReference _removeAtMethod;

			public RemoveAtEndMethodBodyWriter(MinimalContext context, MethodReference getCountMethod, MethodReference removeAtMethod, MethodReference method)
				: base(context, method, method)
			{
				_getCountMethod = getCountMethod;
				_removeAtMethod = removeAtMethod;
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				string text = writer.Context.Global.Services.Naming.ForVariable(_managedMethod.DeclaringType);
				string text2 = "__thisValue";
				writer.WriteLine(text + " " + text2 + " = (" + text + ")" + ManagedObjectExpression + ";");
				writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(_getCountMethod.ReturnType) + " itemsInCollection;");
				WriteMethodCallStatementWithResult(metadataAccess, text2, _getCountMethod, MethodCallType.Virtual, writer, "itemsInCollection");
				writer.WriteLine("if (itemsInCollection == 0)");
				using (new BlockWriter(writer))
				{
					MethodDefinition exceptionConstructor = new TypeReference("System", "InvalidOperationException", writer.Context.Global.Services.TypeProvider.Corlib.MainModule, writer.Context.Global.Services.TypeProvider.Corlib.Name).Resolve().Methods.Single((MethodDefinition m) => m.IsConstructor && m.HasThis && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
					string literal = "Cannot remove the last element from an empty collection.";
					WriteRaiseManagedExceptionWithCustomHResult(writer, exceptionConstructor, -2147483637, "E_BOUNDS", metadataAccess, metadataAccess.StringLiteral(literal));
				}
				WriteMethodCallStatement(metadataAccess, text2, _removeAtMethod, MethodCallType.Virtual, writer, "itemsInCollection - 1");
			}
		}

		private class ReplaceAllMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly MethodReference _clearMethod;

			private readonly MethodReference _addMethod;

			public ReplaceAllMethodBodyWriter(MinimalContext context, MethodReference clearMethod, MethodReference addMethod, MethodReference replaceAllMethod)
				: base(context, replaceAllMethod, replaceAllMethod)
			{
				_clearMethod = clearMethod;
				_addMethod = addMethod;
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				string text = writer.Context.Global.Services.Naming.ForVariable(_managedMethod.DeclaringType);
				string text2 = "__thisValue";
				writer.WriteLine(text + " " + text2 + " = (" + text + ")" + ManagedObjectExpression + ";");
				WriteMethodCallStatement(metadataAccess, text2, _clearMethod, MethodCallType.Virtual, writer);
				writer.WriteLine("if (" + localVariableNames[0] + " != NULL)");
				using (new BlockWriter(writer))
				{
					writer.WriteLine("il2cpp_array_size_t itemsInCollection = " + localVariableNames[0] + "->max_length;");
					writer.WriteLine("for (il2cpp_array_size_t i = 0; i < itemsInCollection; i++)");
					using (new BlockWriter(writer))
					{
						string text3 = Emit.LoadArrayElement(localVariableNames[0], "i", useArrayBoundsCheck: false);
						WriteMethodCallStatement(metadataAccess, text2, _addMethod, MethodCallType.Virtual, writer, text3);
					}
				}
			}
		}

		private readonly TypeDefinition _iListTypeDef;

		private readonly TypeDefinition _iCollectionTypeDef;

		private readonly TypeReference _iCollectionTypeRef;

		private readonly MethodDefinition _addMethodDef;

		private readonly MethodDefinition _clearMethodDef;

		private readonly MethodDefinition _getCountMethodDef;

		private readonly MethodDefinition _getItemMethodDef;

		private readonly MethodDefinition _insertMethodDef;

		private readonly MethodDefinition _removeAtMethodDef;

		private readonly MethodDefinition _setItemMethodDef;

		public ListCCWWriter(TypeDefinition iList)
		{
			bool flag = true;
			_iListTypeDef = iList;
			_iCollectionTypeRef = iList.Interfaces.SingleOrDefault((InterfaceImplementation t) => t.InterfaceType.Name == "IReadOnlyCollection`1")?.InterfaceType;
			if (_iCollectionTypeRef == null)
			{
				_iCollectionTypeRef = iList.Interfaces.SingleOrDefault((InterfaceImplementation t) => t.InterfaceType.Name == "ICollection`1")?.InterfaceType;
				if (_iCollectionTypeRef == null)
				{
					_iCollectionTypeRef = iList.Interfaces.SingleOrDefault((InterfaceImplementation t) => t.InterfaceType.Name == "ICollection").InterfaceType;
					flag = false;
				}
			}
			_iCollectionTypeDef = _iCollectionTypeRef.Resolve();
			if (flag)
			{
				_addMethodDef = _iCollectionTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Add");
				_clearMethodDef = _iCollectionTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Clear");
			}
			else
			{
				_addMethodDef = iList.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Add");
				_clearMethodDef = iList.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Clear");
			}
			_getCountMethodDef = _iCollectionTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Count");
			_getItemMethodDef = _iListTypeDef.Methods.Single((MethodDefinition m) => m.Name == "get_Item");
			_insertMethodDef = _iListTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Insert");
			_removeAtMethodDef = _iListTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "RemoveAt");
			_setItemMethodDef = _iListTypeDef.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "set_Item");
		}

		public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
		{
		}

		public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference method)
		{
			TypeReference declaringType = method.DeclaringType;
			TypeResolver typeResolver = TypeResolver.For(context.Global.Services.WindowsRuntime.ProjectToCLR(declaringType));
			TypeResolver typeResolver2 = TypeResolver.For(typeResolver.Resolve(_iCollectionTypeRef));
			MethodReference methodReference = typeResolver2.Resolve(_getCountMethodDef);
			MethodReference getItemMethod = typeResolver.Resolve(_getItemMethodDef);
			switch (method.Name)
			{
			case "Append":
				return new ProjectedMethodBodyWriter(context, typeResolver2.Resolve(_addMethodDef), method);
			case "Clear":
				return new ProjectedMethodBodyWriter(context, typeResolver2.Resolve(_clearMethodDef), method);
			case "get_Size":
				return new ProjectedMethodBodyWriter(context, methodReference, method);
			case "GetAt":
				return new ExceptionWithEBoundsHResultMethodBodyWriter(context, getItemMethod, method);
			case "GetMany":
				return new GetManyMethodBodyWriter(context, methodReference, getItemMethod, method);
			case "GetView":
				if (!_iListTypeDef.HasGenericParameters)
				{
					return new NonGenericGetViewMethodBodyWriter(context, method);
				}
				return new GetViewMethodBodyWriter(context, method);
			case "IndexOf":
				return new IndexOfMethodBodyWriter(context, methodReference, getItemMethod, method);
			case "InsertAt":
			{
				MethodReference getItemMethod3 = typeResolver.Resolve(_insertMethodDef);
				return new ExceptionWithEBoundsHResultMethodBodyWriter(context, getItemMethod3, method);
			}
			case "RemoveAt":
				return new ExceptionWithEBoundsHResultMethodBodyWriter(context, typeResolver.Resolve(_removeAtMethodDef), method);
			case "RemoveAtEnd":
				return new RemoveAtEndMethodBodyWriter(context, methodReference, typeResolver.Resolve(_removeAtMethodDef), method);
			case "ReplaceAll":
				return new ReplaceAllMethodBodyWriter(context, typeResolver2.Resolve(_clearMethodDef), typeResolver2.Resolve(_addMethodDef), method);
			case "SetAt":
			{
				MethodReference getItemMethod2 = typeResolver.Resolve(_setItemMethodDef);
				return new ExceptionWithEBoundsHResultMethodBodyWriter(context, getItemMethod2, method);
			}
			default:
				throw new NotSupportedException("ListCCWWriter does not support writing method body for " + method.FullName + ".");
			}
		}
	}
}
