using System;
using System.IO;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Scheduling.Streams;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.CodeWriters
{
	public abstract class GeneratedCodeWriter : CppCodeWriter, IGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDisposable, IStream
	{
		private readonly SourceWritingContext _context;

		protected readonly CppDeclarations _cppDeclarations;

		public new ICppDeclarations Declarations => _cppDeclarations;

		public new SourceWritingContext Context => _context;

		protected GeneratedCodeWriter(SourceWritingContext context, StreamWriter stream)
			: this(context, stream, new CppDeclarations())
		{
		}

		private GeneratedCodeWriter(SourceWritingContext context, StreamWriter stream, CppDeclarations cppDeclarations)
			: base(context, stream, cppDeclarations)
		{
			_context = context;
			_cppDeclarations = cppDeclarations;
		}

		public void AddInclude(TypeReference type)
		{
			_cppDeclarations._typeIncludes.Add(type);
		}

		public void AddForwardDeclaration(TypeReference typeReference)
		{
			if (typeReference == null)
			{
				throw new ArgumentNullException("typeReference");
			}
			_cppDeclarations._forwardDeclarations.Add(GetForwardDeclarationType(typeReference));
		}

		private static TypeReference GetForwardDeclarationType(TypeReference typeReference)
		{
			typeReference = typeReference.WithoutModifiers();
			if (typeReference is PointerType pointerType)
			{
				return GetForwardDeclarationType(pointerType.ElementType);
			}
			if (typeReference is ByReferenceType byReferenceType)
			{
				return GetForwardDeclarationType(byReferenceType.ElementType);
			}
			return typeReference;
		}

		public void AddIncludesForTypeReference(TypeReference typeReference, bool requiresCompleteType = false)
		{
			TypeReference typeReference2 = typeReference;
			if (typeReference2.ContainsGenericParameters())
			{
				return;
			}
			if (typeReference2 is ArrayType typeReference3)
			{
				AddForwardDeclaration(typeReference3);
			}
			if (typeReference2 is GenericInstanceType genericInstanceType)
			{
				if (genericInstanceType.ElementType.IsValueType())
				{
					AddIncludeForType(genericInstanceType);
				}
				else
				{
					AddForwardDeclaration(genericInstanceType);
				}
			}
			if (typeReference2 is ByReferenceType byReferenceType)
			{
				typeReference2 = byReferenceType.ElementType;
			}
			if (typeReference2 is PointerType pointerType)
			{
				typeReference2 = pointerType.ElementType;
			}
			if (typeReference2.IsPrimitive)
			{
				if (typeReference2.MetadataType == MetadataType.IntPtr || typeReference2.MetadataType == MetadataType.UIntPtr)
				{
					AddIncludeForType(typeReference2);
				}
				return;
			}
			bool num = typeReference2.IsValueType();
			if (num || (requiresCompleteType && !(typeReference2 is TypeSpecification)))
			{
				AddIncludeForType(typeReference2);
			}
			if (!num)
			{
				AddForwardDeclaration(typeReference2);
			}
		}

		public void AddIncludeForTypeDefinition(TypeReference typeReference)
		{
			TypeReference typeReference2 = typeReference;
			if (typeReference2.ContainsGenericParameters())
			{
				if (typeReference2.IsGenericParameter)
				{
					return;
				}
				TypeDefinition typeDefinition = typeReference2.Resolve();
				if (typeDefinition == null || typeDefinition.IsEnum())
				{
					return;
				}
			}
			typeReference2 = typeReference2.WithoutModifiers();
			if (typeReference2 is ByReferenceType byReferenceType)
			{
				AddIncludeForTypeDefinition(byReferenceType.ElementType);
			}
			else if (typeReference2 is PointerType pointerType)
			{
				AddIncludeForTypeDefinition(pointerType.ElementType);
			}
			else if (typeReference2 is ArrayType arrayType)
			{
				AddIncludeForType(arrayType);
				AddIncludeForType(arrayType.ElementType);
			}
			else if (typeReference2 is GenericInstanceType type)
			{
				AddIncludeForType(type);
			}
			else
			{
				AddIncludeForType(typeReference2);
			}
		}

		public void AddIncludeOrExternForTypeDefinition(TypeReference type)
		{
			type = type.WithoutModifiers();
			if (type is ByReferenceType byReferenceType)
			{
				type = byReferenceType.ElementType;
			}
			if (type is PointerType pointerType)
			{
				type = pointerType.ElementType;
			}
			if (!type.IsValueType())
			{
				AddForwardDeclaration(type);
			}
			AddIncludeForType(type);
		}

		private void AddIncludeForType(TypeReference type)
		{
			type = type.WithoutModifiers();
			if (!type.HasGenericParameters && (!type.IsInterface() || type.IsComOrWindowsRuntimeInterface(_context)))
			{
				if (type.IsArray)
				{
					ArrayType arrayType = (ArrayType)type;
					AddIncludeOrExternForTypeDefinition(arrayType.ElementType);
					_cppDeclarations._arrayTypes.Add(arrayType);
				}
				else
				{
					AddInclude(type);
				}
			}
		}

		public void WriteExternForIl2CppType(IIl2CppRuntimeType type)
		{
			_cppDeclarations._typeExterns.Add(type);
		}

		public void WriteExternForIl2CppGenericInst(IIl2CppRuntimeType[] type)
		{
			_cppDeclarations._genericInstExterns.Add(type);
		}

		public void WriteExternForGenericClass(TypeReference type)
		{
			_cppDeclarations._genericClassExterns.Add(type);
		}

		public static string InitializerStringFor(TypeReference type)
		{
			if (type.FullName == "intptr_t" || type.FullName == "uintptr_t" || type.IsEnum())
			{
				return " = 0";
			}
			if (type.IsPrimitive)
			{
				string text = InitializerStringForPrimitiveType(type);
				if (text != null)
				{
					return $" = {text}";
				}
				return string.Empty;
			}
			if (!type.IsValueType())
			{
				return string.Format(" = {0}", "NULL");
			}
			return string.Empty;
		}

		public static string InitializerStringForPrimitiveType(TypeReference type)
		{
			return InitializerStringForPrimitiveType(type.MetadataType);
		}

		public static string InitializerStringForPrimitiveType(MetadataType type)
		{
			switch (type)
			{
			case MetadataType.Boolean:
				return "false";
			case MetadataType.Char:
			case MetadataType.SByte:
			case MetadataType.Byte:
				return "0x0";
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.Int64:
			case MetadataType.UInt64:
				return "0";
			case MetadataType.Double:
				return "0.0";
			case MetadataType.Single:
				return "0.0f";
			default:
				return null;
			}
		}

		public static string InitializerStringForPrimitiveCppType(string typeName)
		{
			switch (typeName)
			{
			case "bool":
				return InitializerStringForPrimitiveType(MetadataType.Boolean);
			case "char":
			case "wchar_t":
				return InitializerStringForPrimitiveType(MetadataType.Char);
			case "size_t":
			case "int8_t":
			case "int16_t":
			case "int32_t":
			case "int64_t":
			case "uint8_t":
			case "uint16_t":
			case "uint32_t":
			case "uint64_t":
				return InitializerStringForPrimitiveType(MetadataType.Int32);
			case "double":
				return InitializerStringForPrimitiveType(MetadataType.Double);
			case "float":
				return InitializerStringForPrimitiveType(MetadataType.Single);
			default:
				return null;
			}
		}

		public void WriteVariable(TypeReference type, string name)
		{
			if (type.IsGenericParameter())
			{
				throw new ArgumentException("Generic parameter encountered as variable type", "type");
			}
			string text = InitializerStringFor(type);
			string text2 = _context.Global.Services.Naming.ForVariable(type);
			if (!string.IsNullOrEmpty(text))
			{
				WriteLine("{0} {1}{2};", text2, name, text);
			}
			else
			{
				WriteLine("{0} {1};", text2, name);
				WriteStatement(Emit.Memset(_context, Emit.AddressOf(name), 0, "sizeof(" + name + ")"));
			}
		}

		public void WriteDefaultReturn(TypeReference type)
		{
			if (type.IsVoid())
			{
				WriteLine("return;");
				return;
			}
			WriteVariable(type, "ret");
			WriteLine("return ret;");
		}

		void IGeneratedCodeWriter.Write(IGeneratedCodeWriter other)
		{
			_cppDeclarations.Add(other.Declarations);
			base.Writer.Flush();
			other.Writer.Flush();
			Stream baseStream = other.Writer.BaseStream;
			long position = baseStream.Position;
			baseStream.Seek(0L, SeekOrigin.Begin);
			baseStream.CopyTo(base.Writer.BaseStream);
			baseStream.Seek(position, SeekOrigin.Begin);
			base.Writer.Flush();
		}
	}
}
