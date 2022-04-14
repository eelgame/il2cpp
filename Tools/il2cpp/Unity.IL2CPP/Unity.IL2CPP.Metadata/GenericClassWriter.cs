using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Metadata
{
	public class GenericClassWriter : MetadataWriter<IGeneratedCodeWriter>
	{
		public GenericClassWriter(IGeneratedCodeWriter writer)
			: base(writer)
		{
		}

		public void WriteDefinition(ReadOnlyContext context, Il2CppGenericInstanceRuntimeType type)
		{
			if (GenericsUtilities.CheckForMaximumRecursion(context, type.Type))
			{
				base.Writer.WriteLine("Il2CppGenericClass {0} = {{ NULL, {{ NULL, NULL }}, NULL }};", context.Global.Services.Naming.ForGenericClass(type.Type));
				return;
			}
			base.Writer.WriteExternForIl2CppGenericInst(type.GenericArguments);
			if (!context.Global.Services.ContextScope.IncludeTypeDefinitionInContext(type.GenericTypeDefinition.Type))
			{
				base.Writer.WriteExternForIl2CppType(type.GenericTypeDefinition);
			}
			WriteLine("Il2CppGenericClass {0} = {{ &{1}, {{ &{2}, {3} }}, {4} }};", context.Global.Services.Naming.ForGenericClass(type.Type), context.Global.Services.Naming.ForIl2CppType(type.GenericTypeDefinition), context.Global.Services.Naming.ForGenericInst(type.GenericArguments), "NULL", "NULL");
		}
	}
}
