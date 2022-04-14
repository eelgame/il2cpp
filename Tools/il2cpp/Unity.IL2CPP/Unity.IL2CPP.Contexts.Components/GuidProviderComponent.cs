using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP.Contexts.Components
{
	public class GuidProviderComponent : ServiceComponentBase<IGuidProvider, GuidProviderComponent>, IGuidProvider
	{
		private static readonly ReadOnlyCollection<byte> kParameterizedNamespaceGuid = new byte[16]
		{
			17, 244, 122, 213, 123, 115, 66, 192, 171, 174,
			135, 139, 30, 22, 173, 238
		}.AsReadOnly();

		public Guid GuidFor(ReadOnlyContext context, TypeReference type)
		{
			if (type is GenericInstanceType type2)
			{
				return ParameterizedGuidFromTypeIdentifier(IdentifierFor(context, type2));
			}
			if (type is TypeSpecification || type is GenericParameter)
			{
				throw new InvalidOperationException($"Cannot retrieve GUID for {type.FullName}");
			}
			TypeDefinition typeDefinition = type.Resolve();
			CustomAttribute customAttribute = typeDefinition.CustomAttributes.SingleOrDefault((CustomAttribute a) => a.AttributeType.FullName == "System.Runtime.InteropServices.GuidAttribute");
			if (customAttribute != null)
			{
				return new Guid((string)customAttribute.ConstructorArguments[0].Value);
			}
			customAttribute = typeDefinition.CustomAttributes.SingleOrDefault((CustomAttribute a) => a.AttributeType.FullName == "Windows.Foundation.Metadata.GuidAttribute");
			if (customAttribute != null)
			{
				return new Guid((uint)customAttribute.ConstructorArguments[0].Value, (ushort)customAttribute.ConstructorArguments[1].Value, (ushort)customAttribute.ConstructorArguments[2].Value, (byte)customAttribute.ConstructorArguments[3].Value, (byte)customAttribute.ConstructorArguments[4].Value, (byte)customAttribute.ConstructorArguments[5].Value, (byte)customAttribute.ConstructorArguments[6].Value, (byte)customAttribute.ConstructorArguments[7].Value, (byte)customAttribute.ConstructorArguments[8].Value, (byte)customAttribute.ConstructorArguments[9].Value, (byte)customAttribute.ConstructorArguments[10].Value);
			}
			throw new InvalidOperationException($"'{type.FullName}' doesn't have a GUID.");
		}

		private static Guid ParameterizedGuidFromTypeIdentifier(string typeIdentifier)
		{
			List<byte> list = new List<byte>();
			list.AddRange(kParameterizedNamespaceGuid);
			list.AddRange(Encoding.UTF8.GetBytes(typeIdentifier));
			byte[] array;
			using (SHA1Managed sHA1Managed = new SHA1Managed())
			{
				array = sHA1Managed.ComputeHash(list.ToArray());
			}
			int a = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(array, 0));
			short b = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(array, 4));
			short num = IPAddress.NetworkToHostOrder(BitConverter.ToInt16(array, 6));
			byte[] array2 = array.Skip(8).Take(8).ToArray();
			num = (short)(num & 0xFFF);
			num = (short)(num | 0x5000);
			array2[0] &= 63;
			array2[0] |= 128;
			return new Guid(a, b, num, array2);
		}

		public string IdentifierFor(ReadOnlyContext context, IEnumerable<TypeReference> nameElements)
		{
			return nameElements.Select((TypeReference element) => IdentifierFor(context, element)).AggregateWith(";");
		}

		private string IdentifierFor(ReadOnlyContext context, TypeReference type)
		{
			switch (type.MetadataType)
			{
			case MetadataType.Boolean:
				return "b1";
			case MetadataType.Char:
				return "c2";
			case MetadataType.Byte:
				return "u1";
			case MetadataType.Int16:
				return "i2";
			case MetadataType.UInt16:
				return "u2";
			case MetadataType.Int32:
				return "i4";
			case MetadataType.UInt32:
				return "u4";
			case MetadataType.Int64:
				return "i8";
			case MetadataType.UInt64:
				return "u8";
			case MetadataType.Single:
				return "f4";
			case MetadataType.Double:
				return "f8";
			case MetadataType.String:
				return "string";
			case MetadataType.Object:
				return "cinterface(IInspectable)";
			case MetadataType.ValueType:
				if (type.FullName == "System.Guid")
				{
					return "g16";
				}
				break;
			}
			TypeDefinition typeDefinition = context.Global.Services.WindowsRuntime.ProjectToWindowsRuntime(type.Resolve());
			if (type.MetadataType != MetadataType.Class && type.MetadataType != MetadataType.ValueType && type.MetadataType != MetadataType.GenericInstance)
			{
				throw new InvalidOperationException($"Cannot compute type identifier for {type.FullName}, as its metadata type is not supported: {type.MetadataType}.");
			}
			if (!typeDefinition.IsExposedToWindowsRuntime())
			{
				throw new InvalidOperationException("Cannot compute type identifier for " + type.FullName + ", as it is not a Windows Runtime type.");
			}
			if (type is GenericInstanceType genericInstanceType)
			{
				return "pinterface({" + GuidFor(context, typeDefinition).ToString() + "};" + IdentifierFor(context, genericInstanceType.GenericArguments) + ")";
			}
			if (typeDefinition.MetadataType == MetadataType.ValueType)
			{
				if (typeDefinition.IsEnum())
				{
					return "enum(" + typeDefinition.FullName + ";" + IdentifierFor(context, typeDefinition.GetUnderlyingEnumType()) + ")";
				}
				IEnumerable<TypeReference> nameElements = from f in typeDefinition.Fields
					where !f.IsStatic
					select f.FieldType;
				return "struct(" + typeDefinition.FullName + ";" + IdentifierFor(context, nameElements) + ")";
			}
			if (typeDefinition.IsInterface)
			{
				return "{" + GuidFor(context, typeDefinition).ToString() + "}";
			}
			if (typeDefinition.IsDelegate())
			{
				return "delegate({" + GuidFor(context, typeDefinition).ToString() + "})";
			}
			TypeReference typeReference = typeDefinition.ExtractDefaultInterface();
			if (typeReference is GenericInstanceType type2)
			{
				return "rc(" + typeDefinition.FullName + ";" + IdentifierFor(context, type2) + ")";
			}
			return "rc(" + typeDefinition.FullName + ";{" + GuidFor(context, typeReference).ToString() + "})";
		}

		protected override GuidProviderComponent ThisAsFull()
		{
			return this;
		}

		protected override IGuidProvider ThisAsRead()
		{
			return this;
		}
	}
}
