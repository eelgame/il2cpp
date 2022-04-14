using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.Metadata.RuntimeTypes;

namespace Unity.IL2CPP.Attributes
{
	public struct ReadOnlyAttributeWriterOutput
	{
		public readonly AssemblyDefinition Assembly;

		public readonly ReadOnlyCollection<AttributeClassTypeRange> AttributeTypeRanges;

		public readonly ReadOnlyCollection<IIl2CppRuntimeType> AttributeTypes;

		public ReadOnlyAttributeWriterOutput(AssemblyDefinition assembly, ReadOnlyCollection<AttributeClassTypeRange> attributeTypeRanges, ReadOnlyCollection<IIl2CppRuntimeType> attributeTypes)
		{
			Assembly = assembly;
			AttributeTypeRanges = attributeTypeRanges;
			AttributeTypes = attributeTypes;
		}
	}
}
