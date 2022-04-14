using System;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal sealed class DictionaryCCWWriter : IProjectedComCallableWrapperMethodWriter
	{
		private class GetViewMethodBodyWriter : ProjectedMethodBodyWriter
		{
			public GetViewMethodBodyWriter(MinimalContext context, MethodReference getViewMethod)
				: base(context, getViewMethod, getViewMethod)
			{
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				TypeReference type = _typeResolver.Resolve(InteropMethod.ReturnType);
				TypeDefinition typeDefinition = _context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.ObjectModel", "ReadOnlyDictionary`2");
				TypeReference typeReference = _typeResolver.Resolve(typeDefinition);
				MethodReference method = _typeResolver.Resolve(typeDefinition.Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.Resolve().FullName == "System.Collections.Generic.IDictionary`2"));
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
					writer.WriteLine(_context.Global.Services.Naming.ForVariable(typeReference) + " readOnlyDictionary = " + Emit.NewObj(_context, _typeResolver.Resolve(typeReference), metadataAccess) + ";");
					WriteMethodCallStatement(metadataAccess, "readOnlyDictionary", method, MethodCallType.Normal, writer, text);
					writer.WriteLine(_context.Global.Services.Naming.ForInteropReturnValue() + " = readOnlyDictionary;");
				}
			}
		}

		private class InsertMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly MethodReference _setItemMethod;

			private readonly MethodReference _containsKeyMethod;

			public InsertMethodBodyWriter(MinimalContext context, MethodReference setItemMethod, MethodReference containsKeyMethod, MethodReference insertMethod)
				: base(context, insertMethod, insertMethod)
			{
				_setItemMethod = setItemMethod;
				_containsKeyMethod = containsKeyMethod;
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _containsKeyMethod, MethodCallType.Virtual, writer, _context.Global.Services.Naming.ForInteropReturnValue(), localVariableNames[0]);
				WriteMethodCallStatement(metadataAccess, ManagedObjectExpression, _setItemMethod, MethodCallType.Virtual, writer, localVariableNames[0], localVariableNames[1]);
			}
		}

		private class LookupMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly MethodReference _tryGetValueMethod;

			public LookupMethodBodyWriter(MinimalContext context, MethodReference tryGetValueMethod, MethodReference lookupMethod)
				: base(context, lookupMethod, lookupMethod)
			{
				_tryGetValueMethod = tryGetValueMethod;
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				writer.WriteLine("bool keyFound;");
				WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _tryGetValueMethod, MethodCallType.Virtual, writer, "keyFound", localVariableNames[0], Emit.AddressOf(_context.Global.Services.Naming.ForInteropReturnValue()));
				writer.WriteLine();
				writer.WriteLine("if (!keyFound)");
				using (new BlockWriter(writer))
				{
					WriteThrowKeyNotFoundExceptionWithEBoundsHResult(writer, _managedMethod, metadataAccess);
				}
			}
		}

		private class RemoveMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly MethodReference _removeMethod;

			public RemoveMethodBodyWriter(MinimalContext context, MethodReference removeMethod, MethodReference method)
				: base(context, method, method)
			{
				_removeMethod = removeMethod;
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				writer.WriteLine("bool removed;");
				WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, _removeMethod, MethodCallType.Virtual, writer, "removed", localVariableNames[0]);
				writer.WriteLine();
				writer.WriteLine("if (!removed)");
				using (new BlockWriter(writer))
				{
					WriteThrowKeyNotFoundExceptionWithEBoundsHResult(writer, _managedMethod, metadataAccess);
				}
			}
		}

		private class SplitMethodBodyWriter : ProjectedMethodBodyWriter
		{
			private readonly DictionaryCCWWriter _parent;

			private readonly TypeResolver _iDictionaryTypeResolver;

			public SplitMethodBodyWriter(MinimalContext context, DictionaryCCWWriter parent, TypeResolver iDictionaryTypeResolver, MethodReference method)
				: base(context, method, method)
			{
				_parent = parent;
				_iDictionaryTypeResolver = iDictionaryTypeResolver;
			}

			protected override void WriteInteropCallStatementWithinTryBlock(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
			{
				if (_context.Global.Services.TypeProvider.ConstantSplittableMapType == null)
				{
					string text = "Cannot call method '" + InteropMethod.FullName + "' from native code. It requires type System.Runtime.InteropServices.WindowsRuntime.ConstantSplittableMap`2<K, V> to be present. Was it incorrectly stripped?";
					writer.WriteStatement(Emit.RaiseManagedException("il2cpp_codegen_get_not_supported_exception(\"" + text + "\")"));
					return;
				}
				if (!_context.Global.Services.TypeProvider.ConstantSplittableMapType.IsSealed)
				{
					throw new InvalidProgramException("System.Runtime.InteropServices.WindowsRuntime.ConstantSplittableMap`2 was not sealed. Was System.Runtime.WindowsRuntime.dll modified unexpectedly?");
				}
				TypeReference typeReference = _iDictionaryTypeResolver.Resolve(_context.Global.Services.TypeProvider.ConstantSplittableMapType);
				TypeResolver typeResolver = TypeResolver.For(typeReference);
				MethodDefinition method = _context.Global.Services.TypeProvider.ConstantSplittableMapType.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1);
				MethodReference method2 = typeResolver.Resolve(method);
				MethodDefinition method3 = _context.Global.Services.TypeProvider.ConstantSplittableMapType.Methods.Single((MethodDefinition m) => m.HasThis && m.Name == "Split");
				MethodReference method4 = typeResolver.Resolve(method3);
				MethodReference method5 = TypeResolver.For(_iDictionaryTypeResolver.Resolve(_parent._iCollectionTypeRef)).Resolve(_parent._getCountMethodDef);
				writer.AddIncludeForTypeDefinition(typeReference);
				writer.AddIncludeForMethodDeclaration(method2);
				writer.AddIncludeForMethodDeclaration(method4);
				writer.WriteLine("int32_t itemsInCollection;");
				WriteMethodCallStatementWithResult(metadataAccess, ManagedObjectExpression, method5, MethodCallType.Virtual, writer, "itemsInCollection");
				writer.WriteLine("if (itemsInCollection > 1)");
				using (new BlockWriter(writer))
				{
					string text2 = _context.Global.Services.Naming.ForVariable(typeReference);
					string value = "IsInstSealed(" + ManagedObjectExpression + ", " + metadataAccess.TypeInfoFor(typeReference) + ")";
					writer.WriteLine(text2 + " splittableMap = " + Emit.Cast(text2, value) + ";");
					writer.WriteLine();
					writer.WriteLine("if (splittableMap == NULL)");
					using (new BlockWriter(writer))
					{
						writer.WriteLine("splittableMap = " + Emit.NewObj(_context, typeReference, metadataAccess) + ";");
						WriteMethodCallStatement(metadataAccess, "splittableMap", method2, MethodCallType.Normal, writer, ManagedObjectExpression);
					}
					writer.WriteLine();
					WriteMethodCallStatement(metadataAccess, "splittableMap", method4, MethodCallType.Normal, writer, localVariableNames[0], localVariableNames[1]);
				}
			}
		}

		private readonly TypeReference _iCollectionTypeRef;

		private readonly MethodDefinition _clearMethodDef;

		private readonly MethodDefinition _containsKeyMethodDef;

		private readonly MethodDefinition _getCountMethodDef;

		private readonly MethodDefinition _removeMethodDef;

		private readonly MethodDefinition _setItemMethodDef;

		private readonly MethodDefinition _tryGetValueMethodDef;

		public DictionaryCCWWriter(TypeDefinition iDictionary)
		{
			_iCollectionTypeRef = iDictionary.Interfaces.SingleOrDefault((InterfaceImplementation t) => t.InterfaceType.Name == "ICollection`1")?.InterfaceType;
			if (_iCollectionTypeRef == null)
			{
				_iCollectionTypeRef = iDictionary.Interfaces.Single((InterfaceImplementation t) => t.InterfaceType.Name == "IReadOnlyCollection`1").InterfaceType;
			}
			TypeDefinition typeDefinition = _iCollectionTypeRef.Resolve();
			_clearMethodDef = typeDefinition.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Clear");
			_containsKeyMethodDef = iDictionary.Methods.Single((MethodDefinition m) => m.Name == "ContainsKey");
			_getCountMethodDef = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "get_Count");
			_removeMethodDef = iDictionary.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Remove");
			_setItemMethodDef = iDictionary.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "set_Item");
			_tryGetValueMethodDef = iDictionary.Methods.Single((MethodDefinition m) => m.Name == "TryGetValue");
		}

		public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
		{
		}

		public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference method)
		{
			TypeReference declaringType = method.DeclaringType;
			TypeResolver typeResolver = TypeResolver.For(context.Global.Services.WindowsRuntime.ProjectToCLR(declaringType));
			TypeResolver typeResolver2 = TypeResolver.For(typeResolver.Resolve(_iCollectionTypeRef));
			switch (method.Name)
			{
			case "Clear":
			{
				MethodReference managedMethod3 = typeResolver2.Resolve(_clearMethodDef);
				return new ProjectedMethodBodyWriter(context, managedMethod3, method);
			}
			case "get_Size":
			{
				MethodReference managedMethod2 = typeResolver2.Resolve(_getCountMethodDef);
				return new ProjectedMethodBodyWriter(context, managedMethod2, method);
			}
			case "GetView":
				return new GetViewMethodBodyWriter(context, method);
			case "HasKey":
			{
				MethodReference managedMethod = typeResolver.Resolve(_containsKeyMethodDef);
				return new ProjectedMethodBodyWriter(context, managedMethod, method);
			}
			case "Insert":
			{
				MethodReference setItemMethod = typeResolver.Resolve(_setItemMethodDef);
				return new InsertMethodBodyWriter(context, setItemMethod, typeResolver.Resolve(_containsKeyMethodDef), method);
			}
			case "Lookup":
			{
				MethodReference tryGetValueMethod = typeResolver.Resolve(_tryGetValueMethodDef);
				return new LookupMethodBodyWriter(context, tryGetValueMethod, method);
			}
			case "Remove":
			{
				MethodReference removeMethod = typeResolver.Resolve(_removeMethodDef);
				return new RemoveMethodBodyWriter(context, removeMethod, method);
			}
			case "Split":
				return new SplitMethodBodyWriter(context, this, typeResolver, method);
			default:
				throw new NotSupportedException("DictionaryCCWWriter does not support writing method body for " + method.FullName + ".");
			}
		}

		private static void WriteThrowKeyNotFoundExceptionWithEBoundsHResult(IGeneratedMethodCodeWriter writer, MethodReference managedMethod, IRuntimeMetadataAccess metadataAccess)
		{
			TypeDefinition typeDefinition = writer.Context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "KeyNotFoundException");
			MethodDefinition method = typeDefinition.Methods.Single((MethodDefinition m) => m.HasThis && m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			string text = writer.Context.Global.Services.Naming.ForVariable(typeDefinition);
			PropertyDefinition propertyDefinition = writer.Context.Global.Services.TypeProvider.SystemException.Properties.Single((PropertyDefinition p) => p.Name == "HResult");
			string text2 = metadataAccess.StringLiteral("The given key was not present in the dictionary.", default(MetadataToken), writer.Context.Global.Services.TypeProvider.Corlib);
			writer.AddIncludeForTypeDefinition(typeDefinition);
			writer.AddIncludeForMethodDeclaration(method);
			writer.AddIncludeForMethodDeclaration(propertyDefinition.SetMethod);
			writer.WriteLine(text + " e = " + Emit.NewObj(writer.Context, typeDefinition, metadataAccess) + ";");
			writer.WriteMethodCallStatement(metadataAccess, "e", managedMethod, method, MethodCallType.Normal, text2);
			writer.WriteLine("// E_BOUNDS");
			writer.WriteMethodCallStatement(metadataAccess, "e", managedMethod, propertyDefinition.SetMethod, MethodCallType.Normal, (-2147483637).ToString());
			writer.WriteStatement(Emit.RaiseManagedException("e"));
		}
	}
}
