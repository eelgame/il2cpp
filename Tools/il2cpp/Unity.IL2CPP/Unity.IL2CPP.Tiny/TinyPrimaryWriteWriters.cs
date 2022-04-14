using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Tiny
{
	internal static class TinyPrimaryWriteWriters
	{
		public static string WriteStaticConstructorInvokerForAssembly(SourceWritingContext context, AssemblyDefinition assembly)
		{
			List<MethodReference> list = new List<MethodReference>();
			foreach (TypeDefinition allType in assembly.MainModule.GetAllTypes())
			{
				if (!allType.HasGenericParameters)
				{
					MethodDefinition methodDefinition = allType.Methods.FirstOrDefault((MethodDefinition m) => m.IsStaticConstructor());
					if (methodDefinition != null)
					{
						list.Add(methodDefinition);
					}
				}
			}
			return WriteStaticConstructorInvokers(context, list, context.Global.Services.PathFactory.GetFileNameForAssembly(assembly, "StaticConstructors.cpp"), "Il2CppCallStaticConstructors_" + context.Global.Services.Naming.ForCleanAssemblyFileName(assembly));
		}

		public static string WriteStaticConstructorInvokerForGenerics(SourceWritingContext context, string name, ReadOnlyCollection<GenericInstanceType> genericTypes)
		{
			List<MethodReference> list = new List<MethodReference>();
			foreach (GenericInstanceType genericType in genericTypes)
			{
				MethodDefinition methodDefinition = genericType.Resolve().Methods.FirstOrDefault((MethodDefinition m) => m.IsStaticConstructor());
				if (methodDefinition != null)
				{
					list.Add(TypeResolver.For(genericType).Resolve(methodDefinition));
				}
			}
			return WriteStaticConstructorInvokers(context, list, name + "_StaticConstructors.cpp", "Il2CppCallStaticConstructors_" + name);
		}

		private static string WriteStaticConstructorInvokers(SourceWritingContext context, List<MethodReference> methods, string fileName, string methodName)
		{
			bool flag = methods.Count > 0;
			if (!flag)
			{
				return null;
			}
			using (IGeneratedMethodCodeWriter generatedMethodCodeWriter = context.CreateProfiledManagedSourceWriterInOutputDirectory(fileName))
			{
				if (flag)
				{
					generatedMethodCodeWriter.WriteLine("IL2CPP_EXTERN_C void " + methodName + "()");
					using (new BlockWriter(generatedMethodCodeWriter))
					{
						MethodUsage methodUsage = new MethodUsage();
						IRuntimeMetadataAccess defaultRuntimeMetadataAccess = context.Global.Services.Factory.GetDefaultRuntimeMetadataAccess(context, null, null, methodUsage);
						foreach (MethodReference method in methods)
						{
							generatedMethodCodeWriter.WriteMethodCallStatement(defaultRuntimeMetadataAccess, "NULL", null, method, MethodCallType.Normal);
						}
						foreach (MethodReference method2 in methodUsage.GetMethods())
						{
							generatedMethodCodeWriter.AddIncludeForMethodDeclaration(method2);
						}
					}
					MethodWriter.WriteInlineMethodDefinitions(context, "StaticConstructors", generatedMethodCodeWriter);
				}
			}
			if (!flag)
			{
				return null;
			}
			return methodName;
		}

		public static string WriteModuleInitializerInvokerForAssembly(SourceWritingContext context, AssemblyDefinition assembly)
		{
			MethodReference methodReference = assembly.ModuleInitializerMethod();
			bool flag = methodReference != null;
			if (!flag)
			{
				return null;
			}
			string text = (flag ? ("Il2CppCallModuleInitializers_" + context.Global.Services.Naming.ForCleanAssemblyFileName(assembly)) : null);
			using (IGeneratedMethodCodeWriter generatedMethodCodeWriter = context.CreateProfiledManagedSourceWriterInOutputDirectory(context.Global.Services.PathFactory.GetFileNameForAssembly(assembly, "ModuleInitializers.cpp")))
			{
				if (flag)
				{
					generatedMethodCodeWriter.WriteLine("IL2CPP_EXTERN_C void " + text + "()");
					using (new BlockWriter(generatedMethodCodeWriter))
					{
						generatedMethodCodeWriter.AddIncludeForMethodDeclaration(methodReference);
						generatedMethodCodeWriter.WriteLine(context.Global.Services.Naming.ForMethodNameOnly(methodReference) + "();");
						return text;
					}
				}
				return text;
			}
		}
	}
}
