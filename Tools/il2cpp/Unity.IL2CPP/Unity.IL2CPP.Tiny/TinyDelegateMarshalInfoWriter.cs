using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Tiny
{
	internal class TinyDelegateMarshalInfoWriter : DelegateMarshalInfoWriter
	{
		private readonly TinyWriteContext _tinyContext;

		public TinyDelegateMarshalInfoWriter(TinyWriteContext tinyContext, ReadOnlyContext context, TypeReference type, bool forFieldMarshaling)
			: base(context, type, forFieldMarshaling)
		{
			_tinyContext = tinyContext;
		}

		public override void WriteMarshalVariableToNative(IGeneratedMethodCodeWriter writer, ManagedMarshalValue sourceVariable, string destinationVariable, string managedVariableName, IRuntimeMetadataAccess metadataAccess)
		{
			FieldDefinition field = _context.Global.Services.TypeProvider.SystemDelegate.Fields.Single((FieldDefinition f) => f.Name == "m_ReversePInvokeWrapperPtr");
			string text = _context.Global.Services.Naming.ForFieldGetter(field);
			writer.WriteLine("if (" + sourceVariable.Load() + " != NULL)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable + " = reinterpret_cast<Il2CppMethodPointer>(" + sourceVariable.Load() + "->" + text + "());");
			}
			writer.WriteLine("else");
			using (new BlockWriter(writer))
			{
				writer.WriteLine(destinationVariable + " = NULL;");
			}
		}

		public override void WriteMarshalVariableFromNative(IGeneratedMethodCodeWriter writer, string variableName, ManagedMarshalValue destinationVariable, IList<MarshaledParameter> methodParameters, bool safeHandleShouldEmitAddRef, bool forNativeWrapperOfManagedMethod, bool callConstructor, IRuntimeMetadataAccess metadataAccess)
		{
			TypeResolver typeResolver = TypeResolver.For(_typeRef);
			TypeDefinition typeDefinition = _typeRef.Resolve();
			MethodReference method = typeResolver.Resolve(typeDefinition.Methods.Single((MethodDefinition m) => m.IsConstructor));
			MethodDefinition methodDefinition = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "Invoke");
			_ = methodDefinition.Parameters.Count;
			writer.AddIncludeForMethodDeclaration(method);
			writer.WriteLine(destinationVariable.Store(Emit.NewObj(_context, _typeRef, metadataAccess)));
			string text = _context.Global.Services.Naming.ForDelegatePInvokeWrapper(_typeRef);
			TypeReference variableType = typeResolver.Resolve(methodDefinition.ReturnType);
			bool flag = MethodSignatureWriter.NeedsHiddenMethodInfo(_context, method, MethodCallType.Normal);
			string methodSignature = MethodSignatureWriter.GetMethodSignature(text, _context.Global.Services.Naming.ForVariable(variableType), MethodSignatureWriter.FormatParameters(_context, methodDefinition, ParameterFormat.WithTypeAndName, flag), "IL2CPP_EXTERN_C");
			writer.AddMethodForwardDeclaration(methodSignature);
			writer.AddIncludeForMethodDeclaration(methodDefinition);
			List<string> list = new List<string>();
			list.Add(destinationVariable.Load());
			list.Add("reinterpret_cast<Il2CppObject*>(" + variableName + ")");
			if (!writer.Context.Global.Parameters.UsingTinyBackend)
			{
				string text2 = "__dummyMethodInfo_" + variableName.Replace('.', '_').Replace('*', '_');
				writer.WriteStatement("RuntimeMethod " + text2);
				writer.WriteStatement(text2 + ".methodPointer = reinterpret_cast<Il2CppMethodPointer>(" + text + ")");
				list.Add("reinterpret_cast<intptr_t>(&" + text2 + ")");
			}
			else
			{
				list.Add("reinterpret_cast<intptr_t>(" + text + ")");
			}
			if (flag)
			{
				list.Add("NULL");
			}
			writer.WriteStatement(Emit.Call(_context.Global.Services.Naming.ForMethod(method), list));
			DelegateMethodsWriter.EmitTinyDelegateExtraFieldSetters(writer, destinationVariable.Load(), variableName, "false");
		}

		public override bool CanMarshalTypeToNative()
		{
			TypeResolver typeResolver = TypeResolver.For(_typeRef);
			TypeDefinition typeDefinition = _typeRef.Resolve();
			MethodReference interopMethod = typeResolver.Resolve(typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "Invoke"));
			return new DelegatePInvokeMethodBodyWriter(_context, interopMethod).IsDelegatePInvokeWrapperNecessary();
		}
	}
}
