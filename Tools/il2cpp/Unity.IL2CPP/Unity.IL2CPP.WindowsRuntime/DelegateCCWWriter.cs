using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal class DelegateCCWWriter : CCWWriterBase
	{
		private readonly TypeReference[] _implementedInterfaces = new TypeReference[0];

		private readonly string[] _queryableInterfaces;

		protected override IEnumerable<TypeReference> AllImplementedInterfaces => _implementedInterfaces;

		protected override IEnumerable<string> AllQueryableInterfaceNames => _queryableInterfaces;

		public DelegateCCWWriter(SourceWritingContext context, TypeReference type)
			: base(context, type)
		{
			_queryableInterfaces = new string[1] { context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(_type) };
		}

		public override void Write(IGeneratedMethodCodeWriter writer)
		{
			TypeResolver typeResolver = TypeResolver.For(_type);
			MethodReference invokeMethod = typeResolver.Resolve(_type.Resolve().Methods.Single((MethodDefinition m) => m.Name == "Invoke"));
			string text = MethodSignatureWriter.FormatComMethodParameterList(_context, invokeMethod, invokeMethod, typeResolver, MarshalType.WindowsRuntime, includeTypeNames: true, preserveSig: false);
			string text2 = _context.Global.Services.Naming.ForWindowsRuntimeDelegateComCallableWrapperInterface(_type);
			string text3 = _context.Global.Services.Naming.ForComCallableWrapperClass(_type);
			writer.AddInclude("vm/CachedCCWBase.h");
			AddIncludes(writer);
			writer.WriteLine();
			writer.WriteCommentedLine($"COM Callable Wrapper class definition for {_type.FullName}");
			writer.WriteLine("struct " + text3 + " IL2CPP_FINAL : il2cpp::vm::CachedCCWBase<" + text3 + ">, " + text2);
			using (new BlockWriter(writer, semicolon: true))
			{
				writer.WriteLine("inline {0}({1} obj) : ", text3, _context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.ObjectTypeReference));
				writer.Indent();
				writer.WriteLine("il2cpp::vm::CachedCCWBase<" + text3 + ">(obj)");
				writer.Dedent();
				using (new BlockWriter(writer))
				{
				}
				WriteCommonInterfaceMethods(writer);
				writer.WriteLine();
				writer.WriteCommentedLine($"COM Callable invoker for {_type.FullName}");
				string methodSignature = "virtual il2cpp_hresult_t STDCALL Invoke(" + text + ") override";
				writer.WriteMethodWithMetadataInitialization(methodSignature, invokeMethod.FullName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
				{
					new ComCallableWrapperMethodBodyWriter(_context, invokeMethod, invokeMethod, MarshalType.WindowsRuntime).WriteMethodBody(bodyWriter, metadataAccess);
				}, _context.Global.Services.Naming.ForMethod(invokeMethod) + "_WindowsRuntimeManagedInvoker", invokeMethod);
			}
		}
	}
}
