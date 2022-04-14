using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	internal class TypeDefinitionMarshalInfoWriter : CustomMarshalInfoWriter
	{
		private readonly int _nativeSizeWithoutPointers;

		public override int NativeSizeWithoutPointers => _nativeSizeWithoutPointers;

		public TypeDefinitionMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType, bool forFieldMarshaling, bool forByReferenceType, bool forReturnValue, bool forNativeToManagedWrapper)
			: base(context, type, marshalType, forFieldMarshaling, forByReferenceType, forReturnValue, forNativeToManagedWrapper)
		{
			_nativeSizeWithoutPointers = CalculateNativeSizeWithoutPointers();
		}

		protected override void WriteMarshalToNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
		{
			string uniqueIdentifier = $"{_context.Global.Services.Naming.ForType(_type)}_{MarshalingUtils.MarshalTypeToString(_marshalType)}_ToNativeMethodDefinition";
			writer.WriteMethodWithMetadataInitialization(_marshalToNativeFunctionDeclaration, _marshalToNativeFunctionName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				for (int i = 0; i < base.Fields.Length; i++)
				{
					base.FieldMarshalInfoWriters[i].WriteMarshalVariableToNative(bodyWriter, new ManagedMarshalValue(_context, "unmarshaled", base.Fields[i]), base.FieldMarshalInfoWriters[i].UndecorateVariable($"marshaled.{_context.Global.Services.Naming.ForField(base.Fields[i])}"), null, metadataAccess);
				}
			}, uniqueIdentifier, null);
		}

		protected override void WriteMarshalFromNativeMethodDefinition(IGeneratedMethodCodeWriter writer)
		{
			string uniqueIdentifier = $"{_context.Global.Services.Naming.ForType(_type)}_{MarshalingUtils.MarshalTypeToString(_marshalType)}_FromNativeMethodDefinition";
			writer.WriteMethodWithMetadataInitialization(_marshalFromNativeFunctionDeclaration, _marshalFromNativeFunctionName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				for (int i = 0; i < base.Fields.Length; i++)
				{
					FieldDefinition fieldDefinition = base.Fields[i];
					ManagedMarshalValue destinationVariable = new ManagedMarshalValue(_context, "unmarshaled", fieldDefinition);
					if (!fieldDefinition.FieldType.IsValueType())
					{
						base.FieldMarshalInfoWriters[i].WriteMarshalVariableFromNative(bodyWriter, base.FieldMarshalInfoWriters[i].UndecorateVariable($"marshaled.{_context.Global.Services.Naming.ForField(fieldDefinition)}"), destinationVariable, null, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod: false, callConstructor: false, metadataAccess);
					}
					else
					{
						string text = destinationVariable.GetNiceName() + "_temp_" + i;
						bodyWriter.WriteVariable(fieldDefinition.FieldType, text);
						base.FieldMarshalInfoWriters[i].WriteMarshalVariableFromNative(bodyWriter, $"marshaled.{_context.Global.Services.Naming.ForField(fieldDefinition)}", new ManagedMarshalValue(_context, text), null, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod: false, callConstructor: false, metadataAccess);
						bodyWriter.WriteLine(destinationVariable.Store(text));
					}
				}
			}, uniqueIdentifier, null);
		}

		protected override void WriteMarshalCleanupFunction(IGeneratedMethodCodeWriter writer)
		{
			string uniqueIdentifier = $"{_context.Global.Services.Naming.ForType(_type)}_{MarshalingUtils.MarshalTypeToString(_marshalType)}_MarshalCleanupMethodDefinition";
			writer.WriteMethodWithMetadataInitialization(_marshalCleanupFunctionDeclaration, _marshalToNativeFunctionName, delegate(IGeneratedMethodCodeWriter bodyWriter, IRuntimeMetadataAccess metadataAccess)
			{
				for (int i = 0; i < base.Fields.Length; i++)
				{
					base.FieldMarshalInfoWriters[i].WriteMarshalCleanupVariable(bodyWriter, base.FieldMarshalInfoWriters[i].UndecorateVariable($"marshaled.{_context.Global.Services.Naming.ForField(base.Fields[i])}"), metadataAccess);
				}
			}, uniqueIdentifier, null);
		}

		internal int CalculateNativeSizeWithoutPointers()
		{
			int num = 0;
			DefaultMarshalInfoWriter[] fieldMarshalInfoWriters = base.FieldMarshalInfoWriters;
			foreach (DefaultMarshalInfoWriter defaultMarshalInfoWriter in fieldMarshalInfoWriters)
			{
				num += defaultMarshalInfoWriter.NativeSizeWithoutPointers;
			}
			return num;
		}
	}
}
