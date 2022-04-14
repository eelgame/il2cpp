using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	internal class ReversePInvokeNotImplementedMethodBodyWriter : IReversePInvokeMethodBodyWriter
	{
		private static readonly ReadOnlyDictionary<ReversePInvokeWrapperNotImplementedReason, string> Reasons = new Dictionary<ReversePInvokeWrapperNotImplementedReason, string>
		{
			{
				ReversePInvokeWrapperNotImplementedReason.HasGenericParameters,
				"it has generic parameters."
			},
			{
				ReversePInvokeWrapperNotImplementedReason.IsIntrinsicRemap,
				"it is remapped to an instrinsic."
			},
			{
				ReversePInvokeWrapperNotImplementedReason.MissingPInvokeCallbackAttribute,
				"it does not have the [MonoPInvokeCallback] attribute."
			},
			{
				ReversePInvokeWrapperNotImplementedReason.AtLeastOneParameterIsGenericInstance,
				"it has at least one parameter which is a generic instance type."
			},
			{
				ReversePInvokeWrapperNotImplementedReason.IsInstanceMethod,
				"it is an instance method. Only static methods can be called back from native code."
			}
		}.AsReadOnly();

		private MethodReference _managedMethod;

		public ReversePInvokeNotImplementedMethodBodyWriter(MethodReference managedMethod)
		{
			_managedMethod = managedMethod;
		}

		public void WriteMethodDeclaration(IGeneratedCodeWriter writer)
		{
			writer.AddMethodForwardDeclaration(GetMethodSignature(writer.Context));
		}

		public void WriteMethodDefinition(IGeneratedMethodCodeWriter writer)
		{
			ReversePInvokeWrapperNotImplementedReason key = ReversePInvokeMethodBodyWriter.WhyReversePInvokeWrapperCannotBeImplemented(writer.Context, _managedMethod);
			writer.WriteLine(GetMethodSignature(writer.Context));
			writer.BeginBlock();
			writer.WriteStatement(Emit.Call("il2cpp_codegen_no_reverse_pinvoke_wrapper", _managedMethod.FullName.InQuotes(), Reasons[key].InQuotes()));
			writer.EndBlock();
		}

		private string GetMethodSignature(ReadOnlyContext context)
		{
			string arg = context.Global.Services.Naming.ForReversePInvokeWrapperMethod(_managedMethod);
			string callingConvention = ReversePInvokeMethodBodyWriter.GetCallingConvention(_managedMethod);
			return $"extern \"C\" void {callingConvention} {arg}()";
		}
	}
}
