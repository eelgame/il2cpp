using System;
using Mono.Cecil;
using Mono.Collections.Generic;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Naming
{
	public static class FieldNamingExtensions
	{
		public static string ForFieldPadding(this INamingService naming, FieldReference field)
		{
			return naming.ForField(field) + "_OffsetPadding";
		}

		public static int GetFieldIndex(this INamingService naming, FieldReference field, bool includeBase = false)
		{
			FieldDefinition fieldDefinition = field.Resolve();
			TypeDefinition typeDefinition = ((fieldDefinition.DeclaringType.BaseType != null) ? fieldDefinition.DeclaringType.BaseType.Resolve() : fieldDefinition.DeclaringType);
			int num = 0;
			while (includeBase && typeDefinition != null)
			{
				num += typeDefinition.Fields.Count;
				typeDefinition = ((typeDefinition.BaseType != null) ? typeDefinition.BaseType.Resolve() : null);
			}
			Collection<FieldDefinition> fields = fieldDefinition.DeclaringType.Fields;
			for (int i = 0; i < fields.Count; i++)
			{
				if (fieldDefinition == fields[i])
				{
					return num + i;
				}
			}
			throw new InvalidOperationException($"Field {field.Name} was not found on its definition {fieldDefinition.DeclaringType.FullName}!");
		}

		public static string ForField(this INamingService naming, FieldReference field)
		{
			return naming.Clean(NamingComponent.EscapeKeywords(field.Name)) + "_" + naming.GetFieldIndex(field, includeBase: true);
		}

		public static string ForFieldOffsetGetter(this INamingService naming, FieldReference field)
		{
			return $"get_offset_of_{naming.Clean(field.Name)}_" + naming.GetFieldIndex(field, includeBase: true);
		}

		public static string ForFieldGetter(this INamingService naming, FieldReference field)
		{
			return $"get_{naming.Clean(field.Name)}_" + naming.GetFieldIndex(field, includeBase: true);
		}

		public static string ForFieldAddressGetter(this INamingService naming, FieldReference field)
		{
			return $"get_address_of_{naming.Clean(field.Name)}_" + naming.GetFieldIndex(field, includeBase: true);
		}

		public static string ForFieldSetter(this INamingService naming, FieldReference field)
		{
			return $"set_{naming.Clean(field.Name)}_" + naming.GetFieldIndex(field, includeBase: true);
		}
	}
}
