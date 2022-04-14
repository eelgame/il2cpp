using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP.Marshaling.MarshalInfoWriters
{
	public class BlittableStructMarshalInfoWriter : DefaultMarshalInfoWriter
	{
		private readonly TypeDefinition _type;

		private readonly MarshalType _marshalType;

		private readonly MarshaledType[] _marshaledTypes;

		public override MarshaledType[] MarshaledTypes => _marshaledTypes;

		public override int NativeSizeWithoutPointers => (from f in MarshalingUtils.GetFieldMarshalInfoWriters(_context, _type, _marshalType)
			select f.NativeSizeWithoutPointers).Sum();

		public BlittableStructMarshalInfoWriter(ReadOnlyContext context, TypeDefinition type, MarshalType marshalType)
			: base(context, type)
		{
			_type = type;
			_marshalType = marshalType;
			string text = context.Global.Services.Naming.ForVariable(_type);
			_marshaledTypes = new MarshaledType[1]
			{
				new MarshaledType(text, text)
			};
		}
	}
}
