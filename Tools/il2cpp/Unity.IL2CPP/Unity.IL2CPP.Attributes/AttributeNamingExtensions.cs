using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Attributes
{
	internal static class AttributeNamingExtensions
	{
		public static string ForCustomAttributesCacheGenerator(this INamingService naming, TypeDefinition typeDefinition)
		{
			return naming.ForTypeNameOnly(typeDefinition) + "_CustomAttributesCacheGenerator";
		}

		public static string ForCustomAttributesCacheGenerator(this INamingService naming, FieldDefinition fieldDefinition)
		{
			return naming.ForCustomAttributesCacheGenerator(fieldDefinition.DeclaringType) + "_" + naming.Clean(fieldDefinition.Name);
		}

		public static string ForCustomAttributesCacheGenerator(this INamingService naming, MethodDefinition methodDefinition)
		{
			return naming.ForCustomAttributesCacheGenerator(methodDefinition.DeclaringType) + "_" + naming.ForMethodNameOnly(methodDefinition);
		}

		public static string ForCustomAttributesCacheGenerator(this INamingService naming, PropertyDefinition propertyDefinition)
		{
			return naming.ForCustomAttributesCacheGenerator(propertyDefinition.DeclaringType) + "_" + ForPropertyInfo(naming, propertyDefinition);
		}

		public static string ForCustomAttributesCacheGenerator(this INamingService naming, EventDefinition eventDefinition)
		{
			return naming.ForCustomAttributesCacheGenerator(eventDefinition.DeclaringType) + "_" + ForEventInfo(naming, eventDefinition);
		}

		public static string ForCustomAttributesCacheGenerator(this INamingService naming, ParameterDefinition parameterDefinition, MethodDefinition method)
		{
			return naming.ForCustomAttributesCacheGenerator(method) + "_" + naming.ForParameterName(parameterDefinition);
		}

		public static string ForCustomAttributesCacheGenerator(this INamingService naming, AssemblyDefinition assemblyDefinition)
		{
			return naming.ForAssembly(assemblyDefinition) + "_CustomAttributesCacheGenerator";
		}

		private static string ForPropertyInfo(INamingService naming, PropertyDefinition property)
		{
			return ForPropertyInfo(naming, property, property.DeclaringType);
		}

		private static string ForPropertyInfo(INamingService naming, PropertyDefinition property, TypeReference declaringType)
		{
			string text = naming.Clean(NamingComponent.EscapeKeywords(property.Name));
			if (declaringType.Resolve().Properties.Count((PropertyDefinition p) => p.Name == property.Name) > 1)
			{
				text = text + "_" + property.Parameters.Select((ParameterDefinition param) => naming.ForTypeMangling(param.ParameterType)).Aggregate((string buff, string s) => buff + "_" + s);
			}
			return naming.TypeMember(declaringType, text + "_PropertyInfo");
		}

		private static string ForEventInfo(INamingService naming, EventDefinition ev)
		{
			return naming.TypeMember(ev.DeclaringType, naming.Clean(NamingComponent.EscapeKeywords(ev.Name)) + "_EventInfo");
		}
	}
}
