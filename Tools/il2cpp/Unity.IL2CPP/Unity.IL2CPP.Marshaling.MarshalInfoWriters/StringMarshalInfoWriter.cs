using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class StringMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		public const NativeType kNativeTypeHString = (NativeType)47;

		private readonly string _marshaledTypeName;

		private readonly NativeType _nativeType;

		private readonly bool _isStringBuilder;

		private readonly MarshalInfo _marshalInfo;

		private readonly bool _useUnicodeCharSet;

		private readonly MarshaledType[] _marshaledTypes;

		private readonly bool _canReferenceOriginalManagedString;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public NativeType NativeType => _nativeType;

		public override int NativeSizeWithoutPointers
		{
			get
			{
				if (IsFixedSizeString)
				{
					return ((FixedSysStringMarshalInfo)_marshalInfo).Size * BytesPerCharacter;
				}
				return base.NativeSizeWithoutPointers;
			}
		}

		private bool IsFixedSizeString => _nativeType == NativeType.FixedSysString;

		private bool IsWideString
		{
			get
			{
				if (_nativeType != NativeType.LPWStr && _nativeType != NativeType.BStr && _nativeType != (NativeType)47)
				{
					if (IsFixedSizeString)
					{
						return _useUnicodeCharSet;
					}
					return false;
				}
				return true;
			}
		}

		private int BytesPerCharacter
		{
			get
			{
				if (!IsWideString)
				{
					return 1;
				}
				return 2;
			}
		}

		public static NativeType DetermineNativeTypeFor(MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharset, bool isStringBuilder)
		{
			NativeType nativeType = (NativeType)(((int?)marshalInfo?.NativeType) ?? ((marshalType != 0) ? 102 : (useUnicodeCharset ? 21 : 20)));
			bool flag = false;
			if ((uint)(nativeType - 19) <= 2u || nativeType == NativeType.FixedSysString || nativeType == (NativeType)47)
			{
				flag = true;
			}
			if (!flag || (isStringBuilder && nativeType != NativeType.LPStr && nativeType != NativeType.LPWStr))
			{
				switch (marshalType)
				{
				case MarshalType.PInvoke:
					nativeType = NativeType.LPStr;
					break;
				case MarshalType.COM:
					nativeType = NativeType.BStr;
					break;
				case MarshalType.WindowsRuntime:
					nativeType = (NativeType)47;
					break;
				}
			}
			return nativeType;
		}

		public StringMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling)
			: base(context, type)
		{
			_isStringBuilder = MarshalingUtils.IsStringBuilder(type);
			_useUnicodeCharSet = useUnicodeCharSet;
			_nativeType = DetermineNativeTypeFor(marshalType, marshalInfo, _useUnicodeCharSet, _isStringBuilder);
			if (_nativeType == (NativeType)47)
			{
				_marshaledTypeName = "Il2CppHString";
			}
			else if (IsWideString)
			{
				_marshaledTypeName = "Il2CppChar*";
			}
			else
			{
				_marshaledTypeName = "char*";
			}
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(_marshaledTypeName, _marshaledTypeName)
			};
			_marshalInfo = marshalInfo;
			_canReferenceOriginalManagedString = !_isStringBuilder && !forByReferenceType && !forFieldMarshaling && (_nativeType == NativeType.LPWStr || _nativeType == (NativeType)47);
		}

		public override void WriteFieldDeclaration(IGeneratedCodeWriter writer, FieldReference field, string fieldNameSuffix = null)
		{
			if (IsFixedSizeString)
			{
				string text = writer.Context.Global.Services.Naming.ForField(field) + fieldNameSuffix;
				writer.WriteLine("{0} {1}[{2}];", _marshaledTypeName.Replace("*", ""), text, ((FixedSysStringMarshalInfo)_marshalInfo).Size);
			}
			else
			{
				base.WriteFieldDeclaration(writer, field, fieldNameSuffix);
			}
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			WriteMarshalVariableToNative(writer, sourceVariable, destinationVariable, managedVariableName, metadataAccess, isMarshalingReturnValue: false);
		}

		public override string WriteMarshalReturnValueToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{sourceVariable.GetNiceName()}_marshaled";
			WriteNativeVariableDeclarationOfType(writer, text);
			WriteMarshalVariableToNative(writer, sourceVariable, text, null, metadataAccess, isMarshalingReturnValue: true);
			return text;
		}

		private void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess, bool isMarshalingReturnValue)
		{
			if (_nativeType == (NativeType)47)
			{
				writer.WriteLine("if ({0} == {1})", sourceVariable.Load(), "NULL");
				using (new BlockWriter(writer))
				{
					writer.WriteStatement(Emit.RaiseManagedException($"il2cpp_codegen_get_argument_null_exception(\"{(string.IsNullOrEmpty(managedVariableName) ? sourceVariable.GetNiceName() : managedVariableName)}\")"));
				}
			}
			if (IsFixedSizeString)
			{
				string text = (IsWideString ? "il2cpp_codegen_marshal_wstring_fixed" : "il2cpp_codegen_marshal_string_fixed");
				writer.WriteLine("{0}({1}, ({2})&{3}, {4});", text, sourceVariable.Load(), _marshaledTypeName, destinationVariable, ((FixedSysStringMarshalInfo)_marshalInfo).Size);
			}
			else if (_canReferenceOriginalManagedString && !isMarshalingReturnValue)
			{
				if (_nativeType == NativeType.LPWStr)
				{
					string text2 = sourceVariable.Load();
					writer.WriteLine("if ({0} != {1})", text2, "NULL");
					using (new BlockWriter(writer))
					{
						FieldDefinition field = _context.Global.Services.TypeProvider.SystemString.Fields.Single((FieldDefinition f) => !f.IsStatic && f.FieldType.MetadataType == MetadataType.Char);
						writer.WriteLine("{0} = {1}->{2}();", destinationVariable, sourceVariable.Load(), writer.Context.Global.Services.Naming.ForFieldAddressGetter(field));
						return;
					}
				}
				if (_nativeType != (NativeType)47)
				{
					throw new InvalidOperationException($"StringMarshalInfoWriter doesn't know how to marshal {_nativeType} while maintaining reference to original managed string.");
				}
				string niceName = sourceVariable.GetNiceName();
				string text3 = niceName + "NativeView";
				string text4 = niceName + "HStringReference";
				writer.WriteLine();
				writer.WriteLine("DECLARE_IL2CPP_STRING_AS_STRING_VIEW_OF_NATIVE_CHARS({0}, {1});", text3, sourceVariable.Load());
				writer.WriteLine("il2cpp::utils::Il2CppHStringReference {0}({1});", text4, text3);
				writer.WriteLine("{0} = {1};", destinationVariable, text4);
			}
			else
			{
				string text = (_isStringBuilder ? (IsWideString ? "il2cpp_codegen_marshal_wstring_builder" : "il2cpp_codegen_marshal_string_builder") : ((_nativeType == NativeType.BStr) ? "il2cpp_codegen_marshal_bstring" : ((_nativeType != (NativeType)47) ? (IsWideString ? "il2cpp_codegen_marshal_wstring" : "il2cpp_codegen_marshal_string") : "il2cpp_codegen_create_hstring")));
				writer.WriteLine("{0} = {1}({2});", destinationVariable, text, sourceVariable.Load());
			}
		}

		public override string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			return "NULL";
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			string text;
			if (_isStringBuilder)
			{
				text = (IsWideString ? "il2cpp_codegen_marshal_wstring_builder_result" : "il2cpp_codegen_marshal_string_builder_result");
				writer.WriteLine("{0}({1}, {2});", text, destinationVariable.Load(), variableName);
				return;
			}
			switch (_nativeType)
			{
			case NativeType.BStr:
				text = "il2cpp_codegen_marshal_bstring_result";
				break;
			case (NativeType)47:
				text = "il2cpp_codegen_marshal_hstring_result";
				break;
			default:
				text = (IsWideString ? "il2cpp_codegen_marshal_wstring_result" : "il2cpp_codegen_marshal_string_result");
				break;
			}
			writer.WriteLine(destinationVariable.Store("{0}({1})", text, variableName));
		}

		public override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			if (!_canReferenceOriginalManagedString)
			{
				FreeMarshaledString(writer, variableName);
			}
		}

		public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			FreeMarshaledString(writer, variableName);
		}

		public override string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
		{
			if (_isStringBuilder)
			{
				string text = $"_{variableName.GetNiceName()}_marshaled";
				string text2 = (IsWideString ? "il2cpp_codegen_marshal_empty_wstring_builder" : "il2cpp_codegen_marshal_empty_string_builder");
				writer.WriteLine("{0} {1} = {2}({3});", _marshaledTypeName, text, text2, variableName.Load());
				return text;
			}
			return base.WriteMarshalEmptyVariableToNative(writer, variableName, methodParameters);
		}

		private void FreeMarshaledString(IGeneratedCodeWriter writer, string variableName)
		{
			if (!IsFixedSizeString)
			{
				switch (_nativeType)
				{
				case NativeType.BStr:
					writer.WriteLine("il2cpp_codegen_marshal_free_bstring({0});", variableName);
					break;
				case (NativeType)47:
					writer.WriteLine("il2cpp_codegen_marshal_free_hstring({0});", variableName);
					break;
				default:
					writer.WriteLine("il2cpp_codegen_marshal_free({0});", variableName);
					break;
				}
				writer.WriteLine("{0} = {1};", variableName, "NULL");
			}
		}
	}
}
