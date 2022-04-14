using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal sealed class GenericEnumeratorToIteratorAdapterWriter : CCWWriterBase
	{
		private sealed class GetCurrentMethodBodyWriter : ComCallableWrapperMethodBodyWriter
		{
			public GetCurrentMethodBodyWriter(MinimalContext context, MethodReference managedMethod, MethodReference interfaceMethod)
				: base(context, managedMethod, interfaceMethod, MarshalType.WindowsRuntime)
			{
			}

			protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
			{
			}

			protected override void WriteExceptionReturnStatement(IGeneratedMethodCodeWriter writer)
			{
				writer.WriteStatement("return (ex.ex->hresult == IL2CPP_COR_E_INVALIDOPERATION) ? IL2CPP_E_BOUNDS : ex.ex->hresult");
			}
		}

		private sealed class MoveNextMethodBodyWriter : ComCallableWrapperMethodBodyWriter
		{
			public MoveNextMethodBodyWriter(MinimalContext context, MethodReference managedMethod, MethodReference interfaceMethod)
				: base(context, managedMethod, interfaceMethod, MarshalType.WindowsRuntime)
			{
			}

			protected override void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
			{
			}
		}

		private const string InitializedFieldName = "_initialized";

		private const string HasCurrentFieldName = "_hasCurrent";

		private const string InitializeMethodName = "Initialize";

		private const string GetCurrentMethodName = "GetCurrent";

		private const string MoveNextMethodName = "MoveNext";

		private readonly GenericInstanceType _iIteratorType;

		private readonly TypeResolver _iIteratorTypeResolver;

		private readonly IEnumerable<MethodReference> _iIteratorMethods;

		private readonly string _typeName;

		private readonly MethodReference _iIteratorGetCurrentMethod;

		private readonly InteropMethodInfo _iIteratorGetCurrentMethodInfo;

		private readonly MethodReference _iEnumeratorGetCurrentMethod;

		private readonly MethodReference _iEnumeratorMoveNextMethod;

		private readonly string _returnValue;

		private readonly string _hresult;

		protected override bool ImplementsAnyIInspectableInterfaces => true;

		protected override IEnumerable<TypeReference> AllImplementedInterfaces => new TypeReference[1] { _type };

		public static void WriteDefinitions(SourceWritingContext context, IGeneratedMethodCodeWriter writer, GenericInstanceType type)
		{
			new GenericEnumeratorToIteratorAdapterWriter(context, type).Write(writer);
		}

		private GenericEnumeratorToIteratorAdapterWriter(SourceWritingContext context, GenericInstanceType iIterableType)
			: base(context, TypeResolver.For(iIterableType).Resolve(iIterableType.GetMethods().First((MethodReference m) => m.Name == "First").ReturnType))
		{
			_iIteratorType = (GenericInstanceType)_type;
			_iIteratorTypeResolver = TypeResolver.For(_iIteratorType);
			_iIteratorMethods = _iIteratorType.GetMethods();
			_typeName = _context.Global.Services.Naming.ForWindowsRuntimeAdapterClass(_iIteratorType);
			_iIteratorGetCurrentMethod = _iIteratorMethods.First((MethodReference m) => m.Name == "get_Current");
			GenericInstanceType obj = (GenericInstanceType)context.Global.Services.WindowsRuntime.ProjectToCLR(iIterableType);
			MethodReference methodReference = obj.GetMethods().First((MethodReference m) => m.Name == "GetEnumerator");
			GenericInstanceType type = (GenericInstanceType)TypeResolver.For(obj).Resolve(methodReference.ReturnType);
			_iEnumeratorGetCurrentMethod = type.GetMethods().First((MethodReference m) => m.Name == "get_Current");
			TypeReference type2 = type.GetInterfaces(context).First((TypeReference i) => i.FullName == "System.Collections.IEnumerator");
			_iEnumeratorMoveNextMethod = type2.GetMethods().First((MethodReference m) => m.Name == "MoveNext");
			_iIteratorGetCurrentMethodInfo = InteropMethodInfo.ForComCallableWrapper(_context, _iEnumeratorGetCurrentMethod, _iIteratorGetCurrentMethod, MarshalType.WindowsRuntime);
			_returnValue = _context.Global.Services.Naming.ForComInterfaceReturnParameterName();
			_hresult = _context.Global.Services.Naming.ForInteropHResultVariable();
		}

		public override void Write(IGeneratedMethodCodeWriter writer)
		{
			WriteTypeDefinition(writer);
			WriteMethodDefinitions(writer);
		}

		private void WriteTypeDefinition(IGeneratedMethodCodeWriter writer)
		{
			writer.AddInclude("vm/NonCachedCCWBase.h");
			AddIncludes(writer);
			writer.WriteCommentedLine(_iIteratorType.FullName + " adapter");
			writer.WriteLine("struct " + _typeName + " IL2CPP_FINAL : il2cpp::vm::NonCachedCCWBase<" + _typeName + ">, " + writer.Context.Global.Services.Naming.ForTypeNameOnly(_iIteratorType));
			using (new BlockWriter(writer, semicolon: true))
			{
				WriteConstructor(writer);
				WriteCommonInterfaceMethods(writer);
				foreach (MethodReference iIteratorMethod in _iIteratorMethods)
				{
					string signature = ComInterfaceWriter.GetSignature(_context, iIteratorMethod, iIteratorMethod, _iIteratorTypeResolver);
					writer.WriteLine(signature + ";");
				}
				using (new DedentWriter(writer))
				{
					writer.WriteLine("private:");
				}
				writer.WriteLine("bool _initialized;");
				writer.WriteLine("bool _hasCurrent;");
				writer.WriteLine("il2cpp_hresult_t Initialize();");
				writer.WriteLine("il2cpp_hresult_t GetCurrent(" + _iIteratorGetCurrentMethodInfo.MarshaledReturnType.DecoratedName + "* " + _returnValue + ");");
				writer.WriteLine("il2cpp_hresult_t MoveNext(bool* " + _returnValue + ");");
			}
		}

		private void WriteConstructor(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine("inline " + _typeName + "(RuntimeObject* obj) : il2cpp::vm::NonCachedCCWBase<" + _typeName + ">(obj), _initialized(false), _hasCurrent(false) {}");
		}

		private void WriteMethodDefinitions(IGeneratedMethodCodeWriter writer)
		{
			TypeReference type = _iIteratorTypeResolver.Resolve(_iIteratorMethods.First((MethodReference m) => m.Name == "get_Current").ReturnType);
			writer.AddIncludeForTypeDefinition(type);
			foreach (MethodReference method in _iIteratorMethods)
			{
				string signature = ComInterfaceWriter.GetSignature(writer.Context, method, method, _iIteratorTypeResolver, _typeName);
				writer.WriteMethodWithMetadataInitialization(signature, method.FullName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
				{
					switch (method.Name)
					{
					case "get_Current":
						WriteMethodGetCurrent(method, bodyWriter, metadataAccess);
						break;
					case "get_HasCurrent":
						WriteMethodGetHasCurrentCurrent(method, bodyWriter, metadataAccess);
						break;
					case "MoveNext":
						WriteMethodMoveNext(method, bodyWriter, metadataAccess);
						break;
					case "GetMany":
						WriteMethodGetMany(method, bodyWriter, metadataAccess);
						break;
					default:
						throw new NotSupportedException("Interface '" + _iIteratorType.FullName + "' contains unsupported method '" + method.Name + "'.");
					}
				}, _typeName + "_" + writer.Context.Global.Services.Naming.ForMethodNameOnly(method), method);
			}
			WriteMethodInitialize(writer);
			WriteMethodGetCurrent(writer);
			WriteMethodMoveNext(writer);
		}

		private void WriteMethodGetCurrent(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			WriteInitializationCode(writer);
			writer.WriteStatement("return " + Emit.Call("GetCurrent", _returnValue));
		}

		private void WriteMethodGetHasCurrentCurrent(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			WriteInitializationCode(writer);
			writer.WriteStatement(Emit.Assign("*" + _returnValue, "_hasCurrent"));
			writer.WriteStatement("return IL2CPP_S_OK");
		}

		private void WriteMethodMoveNext(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			WriteInitializationCode(writer);
			writer.WriteStatement(Emit.Assign(_hresult, Emit.Call("MoveNext", "&_hasCurrent")));
			writer.WriteStatement(Emit.Assign("*" + _returnValue, "_hasCurrent"));
			writer.WriteStatement("return " + _hresult);
		}

		private void WriteMethodGetMany(MethodReference method, IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			WriteInitializationCode(writer);
			writer.WriteVariable(_context.Global.Services.TypeProvider.UInt32TypeReference, "count");
			InteropMethodInfo interopMethodInfo = InteropMethodInfo.ForComCallableWrapper(_context, method, method, MarshalType.WindowsRuntime);
			string variableName = interopMethodInfo.MarshaledParameterTypes[0].VariableName;
			string variableName2 = interopMethodInfo.MarshaledParameterTypes[1].VariableName;
			writer.WriteLine("for (; _hasCurrent && (count < " + variableName + "); ++count)");
			using (new BlockWriter(writer))
			{
				writer.WriteStatement(Emit.Assign(_hresult, Emit.Call("GetCurrent", variableName2 + " + count")));
				writer.WriteLine("if (IL2CPP_HR_FAILED(" + _hresult + "))");
				using (new BlockWriter(writer))
				{
					writer.WriteStatement(Emit.Assign("*" + _returnValue, "count"));
					writer.WriteStatement("return " + _hresult);
				}
				writer.WriteStatement(Emit.Assign(_hresult, Emit.Call("MoveNext", "&_hasCurrent")));
				writer.WriteLine("if (IL2CPP_HR_FAILED(" + _hresult + "))");
				using (new BlockWriter(writer))
				{
					writer.WriteStatement(Emit.Assign("*" + _returnValue, "count + 1"));
					writer.WriteStatement("return " + _hresult);
				}
			}
			writer.WriteStatement(Emit.Assign("*" + _returnValue, "count"));
			writer.WriteStatement("return IL2CPP_S_OK");
		}

		private void WriteInitializationCode(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteStatement("il2cpp::vm::ScopedThreadAttacher _vmThreadHelper");
			writer.WriteStatement(Emit.Assign("il2cpp_hresult_t " + _hresult, Emit.Call("Initialize")));
			writer.WriteLine("if (IL2CPP_HR_FAILED(" + _hresult + "))");
			using (new BlockWriter(writer))
			{
				writer.WriteStatement(Emit.Memset(_context, _returnValue, 0, "sizeof(*" + _returnValue + ")"));
				writer.WriteStatement("return " + _hresult);
			}
		}

		private void WriteMethodInitialize(IGeneratedMethodCodeWriter writer)
		{
			string methodSignature = "il2cpp_hresult_t " + _typeName + "::Initialize()";
			writer.WriteMethodWithMetadataInitialization(methodSignature, "Initialize", delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				bodyWriter.WriteLine("if (_initialized)");
				using (new BlockWriter(bodyWriter))
				{
					bodyWriter.WriteStatement("return IL2CPP_S_OK");
				}
				bodyWriter.WriteStatement(Emit.Assign("il2cpp_hresult_t " + _hresult, Emit.Call("MoveNext", "&_hasCurrent")));
				bodyWriter.WriteStatement(Emit.Assign("_initialized", "IL2CPP_HR_SUCCEEDED(" + _hresult + ")"));
				bodyWriter.WriteStatement("return " + _hresult);
			}, _typeName + "_Initialize", null);
		}

		private void WriteMethodGetCurrent(IGeneratedMethodCodeWriter writer)
		{
			string methodSignature = "il2cpp_hresult_t " + _typeName + "::GetCurrent(" + _iIteratorGetCurrentMethodInfo.MarshaledReturnType.DecoratedName + "* " + _returnValue + ")";
			writer.WriteMethodWithMetadataInitialization(methodSignature, "GetCurrent", delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				new GetCurrentMethodBodyWriter(writer.Context, _iEnumeratorGetCurrentMethod, _iIteratorGetCurrentMethod).WriteMethodBody(bodyWriter, metadataAccess);
			}, _typeName + "_GetCurrent", _iEnumeratorGetCurrentMethod);
		}

		private void WriteMethodMoveNext(IGeneratedMethodCodeWriter writer)
		{
			string methodSignature = "il2cpp_hresult_t " + _typeName + "::MoveNext(bool* " + _returnValue + ")";
			writer.WriteMethodWithMetadataInitialization(methodSignature, "MoveNext", delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				MethodReference interfaceMethod = _iIteratorType.GetMethods().First((MethodReference m) => m.Name == "MoveNext");
				new MoveNextMethodBodyWriter(writer.Context, _iEnumeratorMoveNextMethod, interfaceMethod).WriteMethodBody(bodyWriter, metadataAccess);
			}, _typeName + "_MoveNext", _iEnumeratorMoveNextMethod);
		}
	}
}
