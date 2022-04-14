using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal class ProjectedClassCCWWriter : ICCWWriter
	{
		private readonly TypeReference _typeRef;

		public ProjectedClassCCWWriter(TypeReference type)
		{
			_typeRef = type;
		}

		public void Write(IGeneratedMethodCodeWriter writer)
		{
		}

		public void WriteCreateComCallableWrapperFunctionBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			DefaultMarshalInfoWriter defaultMarshalInfoWriter = MarshalDataCollector.MarshalInfoWriterFor(writer.Context, _typeRef, MarshalType.WindowsRuntime);
			defaultMarshalInfoWriter.WriteIncludesForMarshaling(writer);
			if (defaultMarshalInfoWriter.CanMarshalTypeToNative())
			{
				writer.WriteLine(writer.Context.Global.Services.Naming.ForVariable(_typeRef) + " _this = reinterpret_cast<" + writer.Context.Global.Services.Naming.ForVariable(_typeRef) + ">(obj);");
				string text = defaultMarshalInfoWriter.WriteMarshalVariableToNative(writer, new ManagedMarshalValue(writer.Context, "_this"), "_this", metadataAccess);
				writer.WriteLine("return " + text + ";");
			}
			else
			{
				writer.WriteStatement(Emit.RaiseManagedException(defaultMarshalInfoWriter.GetMarshalingException()));
			}
		}
	}
}
