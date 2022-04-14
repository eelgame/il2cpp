using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.IL2CPP
{
	internal static class InterfaceUtilities
	{
		public static void MakeImplementInterface(TypeDefinition type, TypeReference interfaceType)
		{
			ModuleDefinition module = type.Module;
			if (type.Interfaces.Any((InterfaceImplementation i) => TypeReferenceEqualityComparer.AreEqual(i.InterfaceType, interfaceType)))
			{
				return;
			}
			TypeResolver typeResolver = TypeResolver.For(interfaceType);
			foreach (InterfaceImplementation @interface in interfaceType.Resolve().Interfaces)
			{
				MakeImplementInterface(type, typeResolver.Resolve(@interface.InterfaceType));
			}
			type.Interfaces.Add(new InterfaceImplementation(module.ImportReference(interfaceType, type)));
			Dictionary<MethodDefinition, MethodDefinition> dictionary = new Dictionary<MethodDefinition, MethodDefinition>();
			TypeDefinition typeDefinition = interfaceType.Resolve();
			foreach (MethodDefinition item in typeDefinition.Methods.Where((MethodDefinition m) => !m.IsStripped()))
			{
				MethodReference methodReference = typeResolver.Resolve(item);
				TypeReference type2 = typeResolver.Resolve(methodReference.ReturnType);
				MethodDefinition methodDefinition = new MethodDefinition(typeDefinition.FullName + "." + methodReference.Name, MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.VtableLayoutMask | MethodAttributes.Abstract, module.ImportReference(type2, type));
				type.Methods.Add(methodDefinition);
				methodDefinition.Overrides.Add(methodReference);
				dictionary.Add(item, methodDefinition);
				foreach (ParameterDefinition parameter in methodReference.Parameters)
				{
					TypeReference type3 = typeResolver.Resolve(parameter.ParameterType);
					methodDefinition.Parameters.Add(new ParameterDefinition(parameter.Name, parameter.Attributes, module.ImportReference(type3, type)));
				}
			}
			foreach (PropertyDefinition property in typeDefinition.Properties)
			{
				TypeReference type4 = typeResolver.Resolve(property.PropertyType);
				PropertyDefinition propertyDefinition = new PropertyDefinition(interfaceType.FullName + "." + property.Name, property.Attributes, module.ImportReference(type4, type));
				type.Properties.Add(propertyDefinition);
				if (property.GetMethod != null)
				{
					propertyDefinition.GetMethod = dictionary[property.GetMethod];
				}
				if (property.SetMethod != null)
				{
					propertyDefinition.SetMethod = dictionary[property.SetMethod];
				}
				foreach (MethodDefinition otherMethod in property.OtherMethods)
				{
					propertyDefinition.OtherMethods.Add(dictionary[otherMethod]);
				}
			}
			foreach (EventDefinition @event in typeDefinition.Events)
			{
				TypeReference type5 = typeResolver.Resolve(@event.EventType);
				EventDefinition eventDefinition = new EventDefinition(interfaceType.FullName + "." + @event.Name, @event.Attributes, module.ImportReference(type5, type));
				type.Events.Add(eventDefinition);
				if (@event.AddMethod != null)
				{
					eventDefinition.AddMethod = dictionary[@event.AddMethod];
				}
				if (@event.RemoveMethod != null)
				{
					eventDefinition.RemoveMethod = dictionary[@event.RemoveMethod];
				}
				if (@event.InvokeMethod != null)
				{
					eventDefinition.InvokeMethod = dictionary[@event.InvokeMethod];
				}
				foreach (MethodDefinition otherMethod2 in @event.OtherMethods)
				{
					eventDefinition.OtherMethods.Add(dictionary[otherMethod2]);
				}
			}
		}
	}
}
