using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal class InterfaceNativeToManagedAdapterGenerator
	{
		public static Dictionary<TypeDefinition, TypeDefinition> Generate(MinimalContext context, IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> clrToWindowsRuntimeProjections, Dictionary<TypeDefinition, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>> adapterMethodBodyWriters)
		{
			Dictionary<TypeDefinition, TypeDefinition> dictionary = new Dictionary<TypeDefinition, TypeDefinition>();
			Dictionary<TypeDefinition, TypeDefinition> dictionary2 = new Dictionary<TypeDefinition, TypeDefinition>();
			CollectProjectedInterfaces(clrToWindowsRuntimeProjections, dictionary2);
			foreach (KeyValuePair<TypeDefinition, TypeDefinition> item in dictionary2)
			{
				if (!adapterMethodBodyWriters.TryGetValue(item.Key, out var value))
				{
					value = new Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>();
				}
				TypeDefinition value2 = CreateAdapterClass(context, item.Key, item.Value, value);
				dictionary.Add(item.Key, value2);
				context.Global.Collectors.Stats.RecordNativeToManagedInterfaceAdapter();
			}
			return dictionary;
		}

		private static void CollectProjectedInterfaces(IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> clrToWindowsRuntimeProjections, Dictionary<TypeDefinition, TypeDefinition> clrToWindowsRuntimeProjectedInterfaces)
		{
			foreach (KeyValuePair<TypeDefinition, TypeDefinition> clrToWindowsRuntimeProjection in clrToWindowsRuntimeProjections)
			{
				if (clrToWindowsRuntimeProjection.Key.IsInterface)
				{
					clrToWindowsRuntimeProjectedInterfaces.Add(clrToWindowsRuntimeProjection.Key, clrToWindowsRuntimeProjection.Value);
				}
			}
			KeyValuePair<TypeDefinition, TypeDefinition>[] array = clrToWindowsRuntimeProjectedInterfaces.ToArray();
			foreach (KeyValuePair<TypeDefinition, TypeDefinition> keyValuePair in array)
			{
				CollectProjectedInterfacesRecursively(keyValuePair.Key, clrToWindowsRuntimeProjectedInterfaces);
			}
		}

		private static void CollectProjectedInterfacesRecursively(TypeDefinition clrInterface, Dictionary<TypeDefinition, TypeDefinition> clrToWindowsRuntimeProjectedInterfaces)
		{
			foreach (InterfaceImplementation @interface in clrInterface.Interfaces)
			{
				TypeDefinition typeDefinition = @interface.InterfaceType.Resolve();
				if (!clrToWindowsRuntimeProjectedInterfaces.ContainsKey(typeDefinition))
				{
					if (typeDefinition.Name != "IEnumerable")
					{
						clrToWindowsRuntimeProjectedInterfaces.Add(typeDefinition, null);
					}
					CollectProjectedInterfacesRecursively(typeDefinition, clrToWindowsRuntimeProjectedInterfaces);
				}
			}
		}

		private static TypeDefinition CreateAdapterClass(MinimalContext context, TypeDefinition clrInterface, TypeDefinition windowsRuntimeInterface, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter> adapterMethodBodyWriters)
		{
			TypeDefinition typeDefinition = new TypeDefinition("System.Runtime.InteropServices.WindowsRuntime", context.Global.Services.Naming.ForWindowsRuntimeAdapterTypeName(windowsRuntimeInterface, clrInterface), TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit, context.Global.Services.TypeProvider.ObjectTypeReference);
			context.Global.Services.TypeProvider.Corlib.MainModule.Types.Add(typeDefinition);
			TypeReference typeReference;
			if (clrInterface.HasGenericParameters)
			{
				GenericInstanceType genericInstanceType = new GenericInstanceType(clrInterface);
				typeReference = genericInstanceType;
				foreach (GenericParameter genericParameter in clrInterface.GenericParameters)
				{
					GenericParameter item = new GenericParameter(genericParameter.Name, typeDefinition);
					typeDefinition.GenericParameters.Add(item);
					genericInstanceType.GenericArguments.Add(item);
				}
			}
			else
			{
				typeReference = clrInterface;
			}
			TypeResolver resolver = TypeResolver.For(typeReference);
			IEnumerable<TypeReference> enumerable = new TypeReference[1] { typeReference }.Union(adapterMethodBodyWriters.Select((KeyValuePair<MethodDefinition, InterfaceAdapterMethodBodyWriter> p) => resolver.Resolve(p.Key.DeclaringType))).Distinct(new TypeReferenceEqualityComparer());
			foreach (TypeReference item2 in enumerable)
			{
				InterfaceUtilities.MakeImplementInterface(typeDefinition, item2);
			}
			MethodDefinition[] array = typeDefinition.Methods.ToArray();
			foreach (MethodDefinition methodDefinition in array)
			{
				bool flag = false;
				MethodReference methodReference = methodDefinition.Overrides[0];
				foreach (TypeReference item3 in enumerable)
				{
					if (TypeReferenceEqualityComparer.AreEqual(item3, methodReference.DeclaringType))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					methodDefinition.Attributes &= ~MethodAttributes.Abstract;
					if (adapterMethodBodyWriters.TryGetValue(methodReference.Resolve(), out var value))
					{
						value(methodDefinition);
					}
					else
					{
						WriteThrowNotSupportedException(context, methodDefinition.Body.GetILProcessor());
					}
					methodDefinition.Body.OptimizeMacros();
				}
			}
			return typeDefinition;
		}

		private static void WriteThrowNotSupportedException(MinimalContext context, ILProcessor ilProcessor)
		{
			MethodDefinition method = context.Global.Services.TypeProvider.Corlib.MainModule.GetType("System", "NotSupportedException").Methods.Single((MethodDefinition m) => m.IsConstructor && m.Parameters.Count == 1 && m.Parameters[0].ParameterType.MetadataType == MetadataType.String);
			string value = "Cannot call method '" + ilProcessor.Body.Method.FullName + "'. IL2CPP does not yet support calling this projected method.";
			ilProcessor.Emit(OpCodes.Ldstr, value);
			ilProcessor.Emit(OpCodes.Newobj, method);
			ilProcessor.Emit(OpCodes.Throw);
		}
	}
}
