using System.Collections.Generic;

namespace Unity.IL2CPP
{
	public class StringMetadataTokenComparer : EqualityComparer<StringMetadataToken>, IComparer<StringMetadataToken>
	{
		public override bool Equals(StringMetadataToken x, StringMetadataToken y)
		{
			return AreEqual(x, y);
		}

		public override int GetHashCode(StringMetadataToken obj)
		{
			return obj.Literal.GetHashCode();
		}

		public static bool AreEqual(StringMetadataToken x, StringMetadataToken y)
		{
			if (x == y)
			{
				return true;
			}
			return x.Literal == y.Literal;
		}

		public int Compare(StringMetadataToken x, StringMetadataToken y)
		{
			if (x == y)
			{
				return 0;
			}
			if (x == null)
			{
				return -1;
			}
			if (y == null)
			{
				return 1;
			}
			return string.CompareOrdinal(x.Literal, y.Literal);
		}
	}
}
