using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Attributes
{
	public static class AttributesWriter
	{
		public static ReadOnlyAttributeWriterOutput Write(SourceWritingContext context, AssemblyDefinition assembly, ReadOnlyCollectedAttributeSupportData attributeData)
		{
			using (IGeneratedMethodCodeWriter generatedMethodCodeWriter = context.CreateProfiledManagedSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileNameForAssembly(assembly, "Attr.cpp")))
			{
				generatedMethodCodeWriter.AddStdInclude("limits");
				ReadOnlyAttributeWriterOutput result = AttributesSupport.WriteAttributes(context.Global.Services.Naming, generatedMethodCodeWriter, assembly, attributeData);
				MethodWriter.WriteInlineMethodDefinitions(context, generatedMethodCodeWriter.FileName.FileNameWithoutExtension, generatedMethodCodeWriter);
				return result;
			}
		}
	}
}
