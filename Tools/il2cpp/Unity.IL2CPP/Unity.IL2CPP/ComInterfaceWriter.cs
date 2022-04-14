using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;

namespace Unity.IL2CPP
{
	public class ComInterfaceWriter
	{
		private readonly SourceWritingContext _context;

		private readonly IGeneratedCodeWriter _writer;

		public ComInterfaceWriter(IGeneratedCodeWriter writer)
		{
			_context = writer.Context;
			_writer = writer;
		}

		public void WriteComInterfaceFor(TypeReference type)
		{
			if (type.IsIActivationFactory(_context))
			{
				return;
			}
			_writer.WriteCommentedLine(type.FullName);
			WriteForwardDeclarations(type);
			string text = (type.Resolve().IsExposedToWindowsRuntime() ? "Il2CppIInspectable" : "Il2CppIUnknown");
			_writer.WriteLine("struct NOVTABLE {0} : {1}", _context.Global.Services.Naming.ForTypeNameOnly(type), text);
			using (new BlockWriter(_writer, semicolon: true))
			{
				_writer.WriteStatement("static const Il2CppGuid IID");
				_writer.Context.Global.Collectors.InteropGuids.Add(_context, type);
				TypeReference typeReference = _context.Global.Services.WindowsRuntime.ProjectToCLR(type);
				if (typeReference != type && typeReference.IsInterface())
				{
					_writer.Context.Global.Collectors.InteropGuids.Add(_context, typeReference);
				}
				TypeResolver typeResolver = TypeResolver.For(type);
				foreach (MethodDefinition method in type.Resolve().Methods)
				{
					MethodReference methodReference = typeResolver.Resolve(method);
					_writer.Write(GetSignature(_writer.Context, methodReference, methodReference, typeResolver, null, isImplementation: false));
					_writer.WriteLine(" = 0;");
				}
			}
		}

		public static string GetSignature(MinimalContext context, MethodReference method, MethodReference interfaceMethod, TypeResolver typeResolver, string typeName = null, bool isImplementation = true)
		{
			StringBuilder stringBuilder = new StringBuilder();
			MarshalType marshalType = ((!interfaceMethod.DeclaringType.Resolve().IsExposedToWindowsRuntime()) ? MarshalType.COM : MarshalType.WindowsRuntime);
			bool isPreserveSig = interfaceMethod.Resolve().IsPreserveSig;
			string value = "il2cpp_hresult_t";
			if (isPreserveSig)
			{
				if (interfaceMethod.ReturnType.MetadataType == MetadataType.Void)
				{
					value = "void";
				}
				else
				{
					TypeReference type = typeResolver.Resolve(interfaceMethod.ReturnType);
					MarshalInfo marshalInfo = interfaceMethod.MethodReturnType.MarshalInfo;
					MarshaledType[] marshaledTypes = MarshalDataCollector.MarshalInfoWriterFor(context, type, marshalType, marshalInfo, useUnicodeCharSet: true).MarshaledTypes;
					value = marshaledTypes[marshaledTypes.Length - 1].DecoratedName;
				}
			}
			if (string.IsNullOrEmpty(typeName))
			{
				stringBuilder.Append("virtual ");
				stringBuilder.Append(value);
				stringBuilder.Append(" STDCALL ");
			}
			else
			{
				stringBuilder.Append(value);
				stringBuilder.Append(" ");
				stringBuilder.Append(typeName);
				stringBuilder.Append("::");
			}
			stringBuilder.Append(context.Global.Services.Naming.ForMethodNameOnly(interfaceMethod));
			stringBuilder.Append('(');
			stringBuilder.Append(MethodSignatureWriter.FormatComMethodParameterList(context, method, interfaceMethod, typeResolver, marshalType, includeTypeNames: true, isPreserveSig));
			stringBuilder.Append(')');
			if (string.IsNullOrEmpty(typeName) && isImplementation)
			{
				stringBuilder.Append(" IL2CPP_OVERRIDE");
			}
			return stringBuilder.ToString();
		}

		private void WriteForwardDeclarations(TypeReference type)
		{
			TypeResolver typeResolver = TypeResolver.For(type);
			MarshalType marshalType = ((!type.Resolve().IsExposedToWindowsRuntime()) ? MarshalType.COM : MarshalType.WindowsRuntime);
			foreach (MethodDefinition method in type.Resolve().Methods)
			{
				foreach (ParameterDefinition parameter in method.Parameters)
				{
					MarshalDataCollector.MarshalInfoWriterFor(_context, typeResolver.Resolve(parameter.ParameterType), marshalType, parameter.MarshalInfo, useUnicodeCharSet: true).WriteIncludesForFieldDeclaration(_writer);
				}
				if (method.ReturnType.MetadataType != MetadataType.Void)
				{
					MarshalDataCollector.MarshalInfoWriterFor(_context, typeResolver.Resolve(method.ReturnType), marshalType, method.MethodReturnType.MarshalInfo, useUnicodeCharSet: true).WriteIncludesForFieldDeclaration(_writer);
				}
			}
		}
	}
}
