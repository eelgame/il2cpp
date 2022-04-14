using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.ILPreProcessor
{
	internal class InjectBaseTypesAndFinalizersIntoComAndWindowsRuntimeTypesVisitor
	{
		public void Process(ReadOnlyContext context, AssemblyDefinition assembly)
		{
			foreach (TypeDefinition allType in assembly.MainModule.GetAllTypes())
			{
				if (allType.IsInterface || allType.IsValueType || allType.IsDelegate())
				{
					continue;
				}
				if (allType.IsIl2CppComObject(context))
				{
					if (context.Global.Services.TypeProvider.IStringableType != null && context.Global.Services.TypeProvider.IStringableType.Methods.Any((MethodDefinition m) => m.Name == "ToString"))
					{
						InjectToStringMethod(allType);
					}
					InjectFinalizer(allType);
				}
				else if (allType.IsIl2CppComDelegate(context) || allType.IsImport)
				{
					InjectBaseType(context, allType);
					InjectFinalizer(allType);
				}
			}
		}

		private static void InjectBaseType(ReadOnlyContext context, TypeDefinition type)
		{
			if (type.BaseType == null)
			{
				throw new InvalidOperationException($"COM import type '{type.FullName}' has no base type.");
			}
			if (type.BaseType.IsSystemObject())
			{
				type.BaseType = type.Module.ImportReference(context.Global.Services.TypeProvider.Il2CppComObjectTypeReference);
			}
		}

		private void InjectToStringMethod(TypeDefinition type)
		{
			MethodDefinition methodDefinition = new MethodDefinition("ToString", MethodAttributes.Public | MethodAttributes.Virtual, type.Module.TypeSystem.String);
			methodDefinition.HasThis = true;
			methodDefinition.ImplAttributes = MethodImplAttributes.CodeTypeMask;
			type.Methods.Add(methodDefinition);
		}

		private static void InjectFinalizer(TypeDefinition type)
		{
			if (!type.IsAttribute())
			{
				MethodDefinition methodDefinition = new MethodDefinition("Finalize", MethodAttributes.Family | MethodAttributes.Virtual | MethodAttributes.HideBySig, type.Module.TypeSystem.Void);
				methodDefinition.HasThis = true;
				methodDefinition.ImplAttributes = MethodImplAttributes.CodeTypeMask;
				type.Methods.Add(methodDefinition);
			}
		}
	}
}
