using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters
{
	public abstract class InteropMethodBodyWriter : InteropMethodInfo
	{
		protected sealed override MethodReference InteropMethod => base.InteropMethod;

		protected virtual bool AreParametersMarshaled { get; } = true;


		protected virtual bool IsReturnValueMarshaled { get; } = true;


		protected InteropMethodBodyWriter(ReadOnlyContext context, MethodReference interopMethod, MethodReference methodForParameterNames, InteropMarshaler marshaler)
			: base(context, interopMethod, methodForParameterNames, marshaler)
		{
		}

		protected virtual void WriteScopedAllocationCheck(IGeneratedMethodCodeWriter writer)
		{
		}

		protected virtual void WriteMethodPrologue(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
		}

		protected abstract void WriteInteropCallStatement(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess);

		public virtual void WriteMethodBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = FirstOrDefaultUnmarshalableMarshalInfoWriter();
			if (defaultMarshalInfoWriter != null)
			{
				writer.WriteStatement(Emit.RaiseManagedException(defaultMarshalInfoWriter.GetMarshalingException()));
				return;
			}
			MarshaledParameter[] parameters = Parameters;
			foreach (MarshaledParameter parameter in parameters)
			{
				MarshalInfoWriterFor(parameter).WriteIncludesForMarshaling(writer);
			}
			MarshalInfoWriterFor(GetMethodReturnType()).WriteIncludesForMarshaling(writer);
			WriteMethodBodyImpl(writer, metadataAccess);
		}

		private void WriteMethodBodyImpl(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			WriteScopedAllocationCheck(writer);
			WriteMethodPrologue(writer, metadataAccess);
			string[] localVariableNames = (AreParametersMarshaled ? WriteMarshalInputParameters(writer, metadataAccess) : null);
			string unmarshaledReturnValueVariableName = null;
			writer.WriteLine("// {0} invocation", _marshaler.GetPrettyCalleeName());
			WriteInteropCallStatement(writer, localVariableNames, metadataAccess);
			writer.WriteLine();
			MethodReturnType methodReturnType = GetMethodReturnType();
			if (methodReturnType.ReturnType.MetadataType != MetadataType.Void)
			{
				if (IsReturnValueMarshaled)
				{
					unmarshaledReturnValueVariableName = _marshaler.WriteMarshalReturnValue(writer, methodReturnType, Parameters, metadataAccess);
					_marshaler.WriteMarshalCleanupReturnValue(writer, methodReturnType, metadataAccess);
				}
				else
				{
					unmarshaledReturnValueVariableName = writer.Context.Global.Services.Naming.ForInteropReturnValue();
				}
			}
			if (AreParametersMarshaled)
			{
				WriteMarshalOutputParameters(writer, localVariableNames, metadataAccess);
			}
			WriteReturnStatement(writer, unmarshaledReturnValueVariableName, metadataAccess);
		}

		protected DefaultMarshalInfoWriter FirstOrDefaultUnmarshalableMarshalInfoWriter()
		{
			MarshaledParameter[] parameters = Parameters;
			foreach (MarshaledParameter parameter in parameters)
			{
				if (!_marshaler.CanMarshalAsInputParameter(parameter))
				{
					return _marshaler.MarshalInfoWriterFor(_context, parameter);
				}
				if (IsOutParameter(parameter) && !_marshaler.CanMarshalAsOutputParameter(parameter))
				{
					return _marshaler.MarshalInfoWriterFor(_context, parameter);
				}
			}
			MethodReturnType methodReturnType = GetMethodReturnType();
			if (methodReturnType.ReturnType.MetadataType != MetadataType.Void && !_marshaler.CanMarshalAsOutputParameter(methodReturnType))
			{
				return _marshaler.MarshalInfoWriterFor(_context, methodReturnType);
			}
			return null;
		}

		private string[] WriteMarshalInputParameters(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			string[] array = new string[Parameters.Length];
			for (int i = 0; i < Parameters.Length; i++)
			{
				array[i] = WriteMarshalInputParameter(writer, Parameters[i], metadataAccess);
			}
			return array;
		}

		private string WriteMarshalInputParameter(IGeneratedMethodCodeWriter writer, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
		{
			if (IsInParameter(parameter))
			{
				return _marshaler.WriteMarshalInputParameter(writer, parameter, Parameters, metadataAccess);
			}
			return _marshaler.WriteMarshalEmptyInputParameter(writer, parameter, Parameters, metadataAccess);
		}

		private void WriteMarshalOutputParameters(IGeneratedMethodCodeWriter writer, string[] localVariableNames, IRuntimeMetadataAccess metadataAccess)
		{
			for (int i = 0; i < Parameters.Length; i++)
			{
				WriteMarshalOutputParameter(writer, localVariableNames[i], Parameters[i], metadataAccess);
				WriteCleanupParameter(writer, localVariableNames[i], Parameters[i], metadataAccess);
			}
		}

		private void WriteMarshalOutputParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
		{
			if (IsOutParameter(parameter))
			{
				_marshaler.WriteMarshalOutputParameter(writer, valueName, parameter, Parameters, metadataAccess);
			}
		}

		private void WriteCleanupParameter(IGeneratedMethodCodeWriter writer, string valueName, MarshaledParameter parameter, IRuntimeMetadataAccess metadataAccess)
		{
			if (ParameterRequiresCleanup(parameter))
			{
				if (IsInParameter(parameter))
				{
					_marshaler.WriteMarshalCleanupParameter(writer, valueName, parameter, metadataAccess);
				}
				else
				{
					_marshaler.WriteMarshalCleanupEmptyParameter(writer, valueName, parameter, metadataAccess);
				}
			}
		}

		protected virtual void WriteReturnStatement(IGeneratedMethodCodeWriter writer, string unmarshaledReturnValueVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			if (GetMethodReturnType().ReturnType.MetadataType != MetadataType.Void)
			{
				writer.WriteManagedReturnStatement(unmarshaledReturnValueVariableName);
			}
		}

		protected MethodReturnType GetMethodReturnType()
		{
			return InteropMethod.MethodReturnType;
		}

		protected string GetMethodName()
		{
			return InteropMethod.Name;
		}

		protected virtual string GetMethodNameInGeneratedCode()
		{
			return _context.Global.Services.Naming.ForMethodNameOnly(InteropMethod);
		}

		protected IList<CustomAttribute> GetCustomMethodAttributes()
		{
			return InteropMethod.Resolve().CustomAttributes;
		}

		protected DefaultMarshalInfoWriter MarshalInfoWriterFor(MarshaledParameter parameter)
		{
			return _marshaler.MarshalInfoWriterFor(_context, parameter);
		}

		protected DefaultMarshalInfoWriter MarshalInfoWriterFor(MethodReturnType methodReturnType)
		{
			return _marshaler.MarshalInfoWriterFor(_context, methodReturnType);
		}

		protected bool IsInParameter(MarshaledParameter parameter)
		{
			TypeReference parameterType = parameter.ParameterType;
			if (parameter.IsOut && !parameter.IsIn)
			{
				if (parameterType.IsValueType())
				{
					return !parameterType.IsByReference;
				}
				return false;
			}
			return true;
		}

		protected bool IsOutParameter(MarshaledParameter parameter)
		{
			TypeReference parameterType = parameter.ParameterType;
			if (parameter.IsOut && !parameterType.IsValueType())
			{
				return true;
			}
			if (parameter.IsIn && !parameter.IsOut)
			{
				return false;
			}
			if (parameter.ParameterType.IsByReference)
			{
				return true;
			}
			if (MarshalingUtils.IsStringBuilder(parameterType))
			{
				return true;
			}
			if (!parameter.ParameterType.IsValueType() && MarshalingUtils.IsBlittable(parameter.ParameterType, null, MarshalType.PInvoke, useUnicodeCharset: false))
			{
				return true;
			}
			return false;
		}

		protected static string GetDelegateCallingConvention(TypeDefinition delegateTypedef)
		{
			CustomAttribute customAttribute = delegateTypedef.CustomAttributes.FirstOrDefault((CustomAttribute attribute) => attribute.AttributeType.FullName == "System.Runtime.InteropServices.UnmanagedFunctionPointerAttribute");
			if (customAttribute == null || !customAttribute.HasConstructorArguments)
			{
				return "DEFAULT_CALL";
			}
			if (!(customAttribute.ConstructorArguments[0].Value is int))
			{
				return "DEFAULT_CALL";
			}
			switch ((int)customAttribute.ConstructorArguments[0].Value)
			{
			case 2:
				return "CDECL";
			case 3:
				return "STDCALL";
			default:
				return "DEFAULT_CALL";
			}
		}

		private bool ParameterRequiresCleanup(MarshaledParameter parameter)
		{
			if (!IsInParameter(parameter))
			{
				return parameter.IsOut;
			}
			return true;
		}
	}
}
