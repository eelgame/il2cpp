using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Attributes
{
	public class AttributeCollection
	{
		public ReadOnlyCollection<AttributeClassTypeRange> AttributeTypeRanges { get; }

		public ReadOnlyCollection<IIl2CppRuntimeType> AttributeTypes { get; }

		public ReadOnlyCollection<string> InitializerFunctionNames { get; }

		public AttributeCollection(ReadOnlyCollection<AttributeClassTypeRange> attributeTypeRanges, ReadOnlyCollection<IIl2CppRuntimeType> attributeTypes, ReadOnlyCollection<string> initializerFunctionNames)
		{
			AttributeTypeRanges = attributeTypeRanges;
			AttributeTypes = attributeTypes;
			InitializerFunctionNames = initializerFunctionNames;
		}

		public static AttributeCollection BuildAttributeCollection(GlobalPrimaryCollectionContext context, ReadOnlyCollection<AttributeData> attributeData)
		{
			List<AttributeClassTypeRange> list = new List<AttributeClassTypeRange>(attributeData.Count);
			List<IIl2CppRuntimeType> list2 = new List<IIl2CppRuntimeType>(attributeData.Count);
			List<string> list3 = new List<string>(attributeData.Count);
			foreach (AttributeData item in attributeData.OrderBy((AttributeData a) => a.MetadataToken))
			{
				int count = list2.Count;
				list2.AddRange(item.AttributeTypes.Select((CustomAttribute a) => context.Collectors.Types.Add(a.AttributeType)));
				list3.Add(item.FunctionName);
				list.Add(new AttributeClassTypeRange(item.MetadataToken, count, item.AttributeTypes.Length));
			}
			return new AttributeCollection(list.AsReadOnly(), list2.AsReadOnly(), list3.AsReadOnly());
		}
	}
}
