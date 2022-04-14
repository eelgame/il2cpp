using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	internal class NativeToManagedMarshaler : InteropMarshaler
	{
		public NativeToManagedMarshaler(ReadOnlyContext context, TypeResolver typeResolver, MarshalType marshalType, bool useUnicodeCharset)
			: base(context, typeResolver, marshalType, useUnicodeCharset)
		{
		}

		public override bool CanMarshalAsInputParameter(MarshaledParameter parameter)
		{
			return MarshalInfoWriterFor(_context, parameter).CanMarshalTypeFromNative();
		}

		public override bool CanMarshalAsOutputParameter(MarshaledParameter parameter)
		{
			return MarshalInfoWriterFor(_context, parameter).CanMarshalTypeToNative();
		}

		public override bool CanMarshalAsOutputParameter(MethodReturnType methodReturnType)
		{
			return MarshalInfoWriterFor(_context, methodReturnType).CanMarshalTypeToNative();
		}

		public override string GetPrettyCalleeName()
		{
			return "Managed method";
		}

		public override string WriteMarshalEmptyInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
		{
			return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling of parameter '{0}' to managed representation", parameter.NameInGeneratedCode);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(_context, parameter);
				return defaultMarshalInfoWriter.WriteMarshalEmptyVariableFromNative(bodyWriter, defaultMarshalInfoWriter.UndecorateVariable(parameter.NameInGeneratedCode), parameters, metadataAccess);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}

		public override string WriteMarshalInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
		{
			return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling of parameter '{0}' to managed representation", parameter.NameInGeneratedCode);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(_context, parameter);
				return defaultMarshalInfoWriter.WriteMarshalVariableFromNative(bodyWriter, defaultMarshalInfoWriter.UndecorateVariable(parameter.NameInGeneratedCode), parameters, safeHandleShouldEmitAddRef: true, forNativeWrapperOfManagedMethod: true, metadataAccess);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}

		public override void WriteMarshalOutputParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
		{
			if (!(valueName == parameter.NameInGeneratedCode))
			{
				writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
				{
					bodyWriter.WriteCommentedLine("Marshaling of parameter '{0}' back from managed representation", parameter.NameInGeneratedCode);
				}, delegate(IGeneratedMethodCodeWriter bodyWriter)
				{
					MarshalInfoWriterFor(_context, parameter).WriteMarshalOutParameterToNative(bodyWriter, new ManagedMarshalValue(_context, valueName), parameter.NameInGeneratedCode, parameter.Name, parameters, metadataAccess);
				}, delegate(IGeneratedMethodCodeWriter bodyWriter)
				{
					bodyWriter.WriteLine();
				});
			}
		}

		public override string WriteMarshalReturnValue(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
		{
			return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling of return value back from managed representation");
			}, (IGeneratedMethodCodeWriter bodyWriter) => MarshalInfoWriterFor(_context, methodReturnType).WriteMarshalReturnValueToNative(bodyWriter, new ManagedMarshalValue(objectVariableName: _context.Global.Services.Naming.ForInteropReturnValue(), context: _context), metadataAccess), delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}

		public override void WriteMarshalCleanupEmptyParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
		{
		}

		public override void WriteMarshalCleanupParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
		{
		}

		public override void WriteMarshalCleanupReturnValue(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType, IRuntimeMetadataAccess metadataAccess)
		{
		}

		public override DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, MarshaledParameter parameter)
		{
			return MarshalDataCollector.MarshalInfoWriterFor(context, parameter.ParameterType, _marshalType, parameter.MarshalInfo, _useUnicodeCharset, forByReferenceType: false, forFieldMarshaling: false, forReturnValue: false, forNativeToManagedWrapper: true);
		}

		public override DefaultMarshalInfoWriter MarshalInfoWriterFor(ReadOnlyContext context, MethodReturnType methodReturnType)
		{
			return MarshalDataCollector.MarshalInfoWriterFor(context, _typeResolver.Resolve(methodReturnType.ReturnType), _marshalType, methodReturnType.MarshalInfo, _useUnicodeCharset, forByReferenceType: false, forFieldMarshaling: false, forReturnValue: true, forNativeToManagedWrapper: true);
		}
	}
}
