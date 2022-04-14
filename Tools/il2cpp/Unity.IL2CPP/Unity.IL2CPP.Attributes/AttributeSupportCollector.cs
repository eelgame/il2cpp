using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Attributes
{
	internal class AttributeSupportCollector
	{
		private readonly MinimalContext _context;

		private List<AttributeData> _data;

		private AttributeSupportCollector(MinimalContext context)
		{
			_context = context;
			_data = new List<AttributeData>();
		}

		public static ReadOnlyCollection<AttributeData> Collect(MinimalContext context, AssemblyDefinition assembly)
		{
			AttributeSupportCollector attributeSupportCollector = new AttributeSupportCollector(context);
			attributeSupportCollector.Collect(assembly);
			return attributeSupportCollector._data.AsReadOnly();
		}

		private void Collect(AssemblyDefinition assembly)
		{
			Add(assembly);
			foreach (TypeDefinition allType in assembly.MainModule.GetAllTypes())
			{
				Collect(allType);
			}
		}

		private void Collect(TypeDefinition type)
		{
			Add(type);
			foreach (FieldDefinition field in type.Fields)
			{
				Add(field);
			}
			foreach (MethodDefinition method in type.Methods)
			{
				Add(method);
				foreach (ParameterDefinition parameter in method.Parameters)
				{
					Add(parameter, method);
				}
			}
			foreach (PropertyDefinition property in type.Properties)
			{
				Add(property);
			}
			foreach (EventDefinition @event in type.Events)
			{
				Add(@event);
			}
		}

		private void Add(TypeDefinition typeDefinition)
		{
			_context.Global.Services.ErrorInformation.CurrentType = typeDefinition;
			Add(_context.Global.Services.Naming.ForCustomAttributesCacheGenerator(typeDefinition), typeDefinition);
		}

		private void Add(FieldDefinition fieldDefinition)
		{
			_context.Global.Services.ErrorInformation.CurrentField = fieldDefinition;
			Add(_context.Global.Services.Naming.ForCustomAttributesCacheGenerator(fieldDefinition), fieldDefinition);
		}

		private void Add(MethodDefinition methodDefinition)
		{
			_context.Global.Services.ErrorInformation.CurrentMethod = methodDefinition;
			Add(_context.Global.Services.Naming.ForCustomAttributesCacheGenerator(methodDefinition), methodDefinition);
		}

		private void Add(PropertyDefinition propertyDefinition)
		{
			_context.Global.Services.ErrorInformation.CurrentProperty = propertyDefinition;
			Add(_context.Global.Services.Naming.ForCustomAttributesCacheGenerator(propertyDefinition), propertyDefinition);
		}

		private void Add(EventDefinition eventDefinition)
		{
			_context.Global.Services.ErrorInformation.CurrentEvent = eventDefinition;
			Add(_context.Global.Services.Naming.ForCustomAttributesCacheGenerator(eventDefinition), eventDefinition);
		}

		private void Add(ParameterDefinition parameterDefinition, MethodDefinition methodDefinition)
		{
			Add(_context.Global.Services.Naming.ForCustomAttributesCacheGenerator(parameterDefinition, methodDefinition), parameterDefinition);
		}

		private void Add(AssemblyDefinition assemblyDefinition)
		{
			Add(_context.Global.Services.Naming.ForCustomAttributesCacheGenerator(assemblyDefinition), assemblyDefinition);
		}

		private void Add(string name, ICustomAttributeProvider customAttributeProvider)
		{
			CustomAttribute[] array = customAttributeProvider.GetConstructibleCustomAttributes().ToArray();
			if (array.Length != 0)
			{
				_data.Add(new AttributeData(customAttributeProvider.MetadataToken.ToUInt32(), name, array));
			}
		}
	}
}
