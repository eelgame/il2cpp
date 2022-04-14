using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public abstract class DefaultMarshalInfoWriter
	{
		protected readonly TypeReference _typeRef;

		protected readonly ReadOnlyContext _context;

		private static readonly ReadOnlyCollection<char> CharactersToReplaceWithUnderscore = new char[3] { '.', '[', ']' }.AsReadOnly();

		private static readonly char[] CharactersToRemove = new char[3] { '*', '(', ')' };

		public virtual int NativeSizeWithoutPointers => 0;

		public virtual string NativeSize => MarshaledTypes.Select((MarshaledType t) => $"sizeof({t.Name})").Aggregate((string x, string y) => x + " + " + y);

		public abstract MarshaledType[] MarshaledTypes { get; }

		public virtual string MarshalToNativeFunctionName => "NULL";

		public virtual string MarshalFromNativeFunctionName => "NULL";

		public virtual string MarshalCleanupFunctionName => "NULL";

		public virtual bool HasNativeStructDefinition => false;

		public DefaultMarshalInfoWriter(ReadOnlyContext context, TypeReference type)
		{
			_context = context;
			_typeRef = type;
		}

		public virtual void WriteNativeStructDefinition(IGeneratedCodeWriter writer)
		{
		}

		public virtual void WriteMarshalFunctionDeclarations(IGeneratedMethodCodeWriter writer)
		{
		}

		public virtual void WriteMarshalFunctionDefinitions(IGeneratedMethodCodeWriter writer)
		{
		}

		public virtual void WriteFieldDeclaration(IGeneratedCodeWriter writer, FieldReference field, string fieldNameSuffix = null)
		{
			MarshaledType[] marshaledTypes = MarshaledTypes;
			foreach (MarshaledType marshaledType in marshaledTypes)
			{
				string text = _context.Global.Services.Naming.ForField(field) + marshaledType.VariableName + fieldNameSuffix;
				writer.WriteLine("{0} {1};", marshaledType.DecoratedName, text);
			}
		}

		public virtual void WriteIncludesForFieldDeclaration(IGeneratedCodeWriter writer)
		{
			if (TreatAsValueType())
			{
				writer.AddIncludesForTypeReference(_typeRef, requiresCompleteType: true);
			}
			else
			{
				WriteMarshaledTypeForwardDeclaration(writer);
			}
		}

		public virtual void WriteMarshaledTypeForwardDeclaration(IGeneratedCodeWriter writer)
		{
			if (!_typeRef.IsEnum() && !_typeRef.IsSystemObject())
			{
				MarshaledType[] marshaledTypes = MarshaledTypes;
				foreach (MarshaledType marshaledType in marshaledTypes)
				{
					writer.AddForwardDeclaration($"struct {marshaledType.Name}");
				}
			}
		}

		public virtual void WriteIncludesForMarshaling(IGeneratedMethodCodeWriter writer)
		{
			writer.AddIncludeForTypeDefinition(_typeRef);
		}

		public virtual void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("{1} = {0};", sourceVariable.Load(), destinationVariable);
		}

		public virtual string WriteMarshalEmptyVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue variableName, IList<MarshaledParameter> methodParameters)
		{
			return variableName.Load();
		}

		public virtual void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine(destinationVariable.Store(variableName));
		}

		public virtual void WriteMarshalOutParameterFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool isIn, IRuntimeMetadataAccess metadataAccess)
		{
			WriteMarshalVariableFromNative(writer, variableName, destinationVariable, methodParameters, safeHandleShouldEmitAddRef, forNativeWrapperOfManagedMethod, callConstructor: true, metadataAccess);
		}

		public virtual string WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			return sourceVariable.Load();
		}

		public virtual string WriteMarshalEmptyVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
			string text = $"_{CleanVariableName(variableName)}_empty";
			writer.WriteVariable(_typeRef, text);
			return text;
		}

		public virtual void WriteMarshalOutParameterToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IList<MarshaledParameter> methodParameters, IRuntimeMetadataAccess metadataAccess)
		{
		}

		public virtual string WriteMarshalReturnValueToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, IRuntimeMetadataAccess metadataAccess)
		{
			return WriteMarshalVariableToNative(writer, sourceVariable, null, metadataAccess);
		}

		public virtual string WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, IRuntimeMetadataAccess metadataAccess)
		{
			return variableName;
		}

		public virtual void WriteMarshalCleanupVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
		}

		public virtual void WriteMarshalCleanupOutVariable(IGeneratedMethodCodeWriter writer, string variableName, IRuntimeMetadataAccess metadataAccess, string managedVariableName = null)
		{
			WriteMarshalCleanupVariable(writer, variableName, metadataAccess, managedVariableName);
		}

		public virtual void WriteDeclareAndAllocateObject(IGeneratedCodeWriter writer, string unmarshaledVariableName, string marshaledVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteVariable(_typeRef, unmarshaledVariableName);
		}

		public virtual string DecorateVariable(string unmarshaledParameterName, string marshaledVariableName)
		{
			return marshaledVariableName;
		}

		public virtual string UndecorateVariable(string variableName)
		{
			return variableName;
		}

		public virtual bool CanMarshalTypeToNative()
		{
			return true;
		}

		public virtual bool CanMarshalTypeFromNative()
		{
			return CanMarshalTypeToNative();
		}

		public virtual string GetMarshalingException()
		{
			throw new NotSupportedException($"Cannot retrieve marshaling exception for type ({_typeRef}) that can be marshaled.");
		}

		public virtual void WriteNativeVariableDeclarationOfType(IGeneratedMethodCodeWriter writer, string variableName)
		{
			MarshaledType[] marshaledTypes = MarshaledTypes;
			foreach (MarshaledType marshaledType in marshaledTypes)
			{
				writer.WriteStatement(Emit.Assign(right: (!marshaledType.Name.EndsWith("*") && !(marshaledType.Name == "Il2CppHString")) ? ((_typeRef.MetadataType == MetadataType.Class && !_typeRef.DerivesFromObject(writer.Context)) ? (marshaledType.Name + "()") : ((!_typeRef.MetadataType.IsPrimitiveType()) ? ((!marshaledType.Name.IsPrimitiveCppType()) ? "{}" : GeneratedCodeWriter.InitializerStringForPrimitiveCppType(marshaledType.Name)) : GeneratedCodeWriter.InitializerStringForPrimitiveType(_typeRef.MetadataType))) : "NULL", left: marshaledType.Name + " " + variableName + marshaledType.VariableName));
			}
		}

		public virtual bool TreatAsValueType()
		{
			return _typeRef.IsValueType();
		}

		protected string CleanVariableName(string variableName)
		{
			foreach (char item in CharactersToReplaceWithUnderscore)
			{
				variableName = variableName.Replace(item, '_');
			}
			variableName = string.Concat(variableName.Split(CharactersToRemove, StringSplitOptions.None));
			return _context.Global.Services.Naming.Clean(variableName);
		}
	}
}
