using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative
{
	internal class ManagedToNativeMarshaler : InteropMarshaler
	{
		public ManagedToNativeMarshaler(ReadOnlyContext context, TypeResolver typeResolver, MarshalType marshalType, bool useUnicodeCharset)
			: base(context, typeResolver, marshalType, useUnicodeCharset)
		{
		}

		public override bool CanMarshalAsInputParameter(MarshaledParameter parameter)
		{
			return MarshalInfoWriterFor(_context, parameter).CanMarshalTypeToNative();
		}

		public override bool CanMarshalAsOutputParameter(MarshaledParameter parameter)
		{
			return MarshalInfoWriterFor(_context, parameter).CanMarshalTypeFromNative();
		}

		public override bool CanMarshalAsOutputParameter(MethodReturnType methodReturnType)
		{
			return MarshalInfoWriterFor(_context, methodReturnType).CanMarshalTypeFromNative();
		}

		public override string GetPrettyCalleeName()
		{
			return "Native function";
		}

		public override string WriteMarshalEmptyInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
		{
			return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling of parameter '{0}' to native representation", parameter.NameInGeneratedCode);
			}, (IGeneratedMethodCodeWriter bodyWriter) => MarshalInfoWriterFor(_context, parameter).WriteMarshalEmptyVariableToNative(bodyWriter, new ManagedMarshalValue(_context, parameter.NameInGeneratedCode), parameters), delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}

		public override string WriteMarshalInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
		{
			return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling of parameter '{0}' to native representation", parameter.NameInGeneratedCode);
			}, (IGeneratedMethodCodeWriter bodyWriter) => MarshalInfoWriterFor(_context, parameter).WriteMarshalVariableToNative(bodyWriter, new ManagedMarshalValue(_context, parameter.NameInGeneratedCode), parameter.Name, metadataAccess), delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}

		public override void WriteMarshalOutputParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
		{
			if (valueName == parameter.NameInGeneratedCode)
			{
				return;
			}
			writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling of parameter '{0}' back from native representation", parameter.NameInGeneratedCode);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(_context, parameter);
				ManagedMarshalValue destinationVariable = new ManagedMarshalValue(_context, parameter.NameInGeneratedCode);
				if (parameter.IsOut)
				{
					defaultMarshalInfoWriter.WriteMarshalOutParameterFromNative(bodyWriter, valueName, destinationVariable, parameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, parameter.IsIn, metadataAccess);
				}
				else
				{
					defaultMarshalInfoWriter.WriteMarshalVariableFromNative(bodyWriter, valueName, destinationVariable, parameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, callConstructor: false, metadataAccess);
				}
				if (parameter.ParameterType is ByReferenceType)
				{
					bodyWriter.WriteWriteBarrierIfNeeded((parameter.ParameterType as ByReferenceType).ElementType, destinationVariable.GetNiceName(), valueName);
				}
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}

		public override string WriteMarshalReturnValue(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType, IList<MarshaledParameter> parameters, IRuntimeMetadataAccess metadataAccess)
		{
			return writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling of return value back from native representation");
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(writer.Context, methodReturnType);
				string variableName = defaultMarshalInfoWriter.UndecorateVariable(_context.Global.Services.Naming.ForInteropReturnValue());
				return defaultMarshalInfoWriter.WriteMarshalVariableFromNative(bodyWriter, variableName, parameters, safeHandleShouldEmitAddRef: false, forNativeWrapperOfManagedMethod: false, metadataAccess);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}

		public override void WriteMarshalCleanupEmptyParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling cleanup of parameter '{0}' native representation", parameter.NameInGeneratedCode);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				MarshalInfoWriterFor(_context, parameter).WriteMarshalCleanupOutVariable(bodyWriter, valueName, metadataAccess, parameter.NameInGeneratedCode);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}

		public override void WriteMarshalCleanupParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling cleanup of parameter '{0}' native representation", parameter.NameInGeneratedCode);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				MarshalInfoWriterFor(_context, parameter).WriteMarshalCleanupVariable(bodyWriter, valueName, metadataAccess, parameter.NameInGeneratedCode);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}

		public override void WriteMarshalCleanupReturnValue(IGeneratedMethodCodeWriter writer, MethodReturnType methodReturnType, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteIfNotEmpty(delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteCommentedLine("Marshaling cleanup of return value native representation");
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalInfoWriterFor(_context, methodReturnType);
				string variableName = defaultMarshalInfoWriter.UndecorateVariable(_context.Global.Services.Naming.ForInteropReturnValue());
				defaultMarshalInfoWriter.WriteMarshalCleanupOutVariable(bodyWriter, variableName, metadataAccess);
			}, delegate(IGeneratedMethodCodeWriter bodyWriter)
			{
				bodyWriter.WriteLine();
			});
		}
	}
}
