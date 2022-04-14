using System.Collections.Generic;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal sealed class ComObjectMarshalInfoWriter : MarshalableMarshalInfoWriter
	{
		public const NativeType kNativeTypeIInspectable = (NativeType)46;

		private readonly bool _marshalAsInspectable;

		private readonly TypeReference _windowsRuntimeType;

		private readonly bool _isSealedNativeClass;

		private readonly bool _isClass;

		private readonly bool _isManagedWinRTClass;

		private readonly string _managedTypeName;

		private readonly TypeReference _defaultInterface;

		private readonly string _interfaceTypeName;

		private readonly MarshaledType[] _marshaledTypes;

		private readonly bool _forNativeToManagedWrapper;

		public sealed override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public ComObjectMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool forNativeToManagedWrapper)
			: base(context, type)
		{
			_windowsRuntimeType = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type);
			TypeDefinition typeDefinition = _windowsRuntimeType.Resolve();
			_forNativeToManagedWrapper = forNativeToManagedWrapper;
			_marshalAsInspectable = marshalType == MarshalType.WindowsRuntime || typeDefinition.IsExposedToWindowsRuntime() || (marshalInfo != null && marshalInfo.NativeType == (NativeType)46);
			_isClass = !typeDefinition.IsInterface() && !type.IsSystemObject();
			_isManagedWinRTClass = typeDefinition.IsWindowsRuntimeProjection && typeDefinition.Module != null && typeDefinition.Module.MetadataKind == MetadataKind.ManagedWindowsMetadata;
			_isSealedNativeClass = typeDefinition.IsSealed && !_isManagedWinRTClass;
			_defaultInterface = (_isClass ? typeDefinition.ExtractDefaultInterface() : _windowsRuntimeType);
			_managedTypeName = (_isClass ? context.Global.Services.Naming.ForTypeNameOnly(_windowsRuntimeType) : context.Global.Services.Naming.ForTypeNameOnly(context.Global.Services.TypeProvider.SystemObject));
			if (type.IsSystemObject())
			{
				_interfaceTypeName = (_marshalAsInspectable ? "Il2CppIInspectable" : "Il2CppIUnknown");
			}
			else
			{
				_interfaceTypeName = context.Global.Services.Naming.ForTypeNameOnly(_defaultInterface);
			}
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(_interfaceTypeName + "*", _interfaceTypeName + "*")
			};
		}

		public override void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
			WriteMarshaledTypeForwardDeclaration(writer);
		}

		public override void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			if (!_typeRef.IsSystemObject())
			{
				writer.AddForwardDeclaration($"struct {_interfaceTypeName}");
			}
		}

		public override void WriteNativeStructDefinition(IGeneratedCodeWriter writer)
		{
			WriteMarshaledTypeForwardDeclaration(writer);
		}

		public override void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			if (!_typeRef.IsSystemObject())
			{
				if (_isClass)
				{
					writer.AddIncludeForTypeDefinition(_windowsRuntimeType);
				}
				writer.AddIncludeForTypeDefinition(_defaultInterface);
			}
		}

		public sealed override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", sourceVariable.Load(), "NULL");
			using (new BlockWriter(writer))
			{
				if (_isSealedNativeClass)
				{
					writer.WriteLine(destinationVariable + " = il2cpp_codegen_com_query_interface<" + _interfaceTypeName + ">(static_cast<Il2CppComObject*>(" + sourceVariable.Load() + "));");
					if (_forNativeToManagedWrapper)
					{
						writer.WriteLine("(" + destinationVariable + ")->AddRef();");
					}
				}
				else if (_isManagedWinRTClass)
				{
					writer.WriteLine(destinationVariable + " = il2cpp_codegen_com_get_or_create_ccw<" + _interfaceTypeName + ">(" + sourceVariable.Load() + ");");
				}
				else
				{
					WriteMarshalToNativeForNonSealedType(writer, sourceVariable, destinationVariable);
				}
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("{0} = {1};", destinationVariable, "NULL");
			}
		}

		private void WriteMarshalToNativeForNonSealedType(IGeneratedCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable)
		{
			writer.WriteLine("if (il2cpp_codegen_is_import_or_windows_runtime({0}))", sourceVariable.Load());
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable + " = il2cpp_codegen_com_query_interface<" + _interfaceTypeName + ">(static_cast<Il2CppComObject*>(" + sourceVariable.Load() + "));");
				writer.WriteLine("(" + destinationVariable + ")->AddRef();");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable + " = il2cpp_codegen_com_get_or_create_ccw<" + _interfaceTypeName + ">(" + sourceVariable.Load() + ");");
			}
		}

		public sealed override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				if (_isManagedWinRTClass)
				{
					writer.WriteLine(destinationVariable.Store(Emit.Cast(_managedTypeName + "*", "CastclassSealed(il2cpp_codegen_com_unpack_ccw(" + variableName + "), " + metadataAccess.TypeInfoFor(_typeRef) + ")")));
					return;
				}
				TypeReference type = ((_typeRef.IsInterface() || !_typeRef.Resolve().IsComOrWindowsRuntimeType(_context)) ? _context.Global.Services.TypeProvider.Il2CppComObjectTypeReference : _typeRef);
				if (_isSealedNativeClass)
				{
					writer.WriteLine(destinationVariable.Store("il2cpp_codegen_com_get_or_create_rcw_for_sealed_class<{0}>({1}, {2})", _managedTypeName, variableName, metadataAccess.TypeInfoFor(_typeRef)));
				}
				else if (_marshalAsInspectable)
				{
					writer.WriteLine(destinationVariable.Store("il2cpp_codegen_com_get_or_create_rcw_from_iinspectable<{0}>({1}, {2})", _managedTypeName, variableName, metadataAccess.TypeInfoFor(type)));
				}
				else
				{
					writer.WriteLine(destinationVariable.Store("il2cpp_codegen_com_get_or_create_rcw_from_iunknown<{0}>({1}, {2})", _managedTypeName, variableName, metadataAccess.TypeInfoFor(type)));
				}
				writer.WriteLine();
				writer.WriteLine("if (il2cpp_codegen_is_import_or_windows_runtime(" + destinationVariable.Load() + "))");
				using (new BlockWriter(writer))
				{
					writer.WriteLine("il2cpp_codegen_com_cache_queried_interface(static_cast<Il2CppComObject*>(" + destinationVariable.Load() + "), " + _interfaceTypeName + "::IID, " + variableName + ");");
				}
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable.Store("NULL"));
			}
		}

		public sealed override void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName)
		{
			if (!_isSealedNativeClass)
			{
				WriteMarshalCleanupOutVariable(writer, variableName, metadataAccess, managedVariableName);
			}
		}

		public override void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			writer.WriteLine("if ({0} != {1})", variableName, "NULL");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("({0})->Release();", variableName);
				writer.WriteLine("{0} = {1};", variableName, "NULL");
			}
		}
	}
}
