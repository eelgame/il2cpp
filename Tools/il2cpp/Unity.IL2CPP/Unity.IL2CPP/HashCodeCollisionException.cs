using System;
using System.Text;

namespace Unity.IL2CPP
{
	public class HashCodeCollisionException : Exception
	{
		public HashCodeCollisionException(string message)
			: base(message)
		{
		}

		public HashCodeCollisionException(string hashValue, string existingItem, string collidingItem)
			: this(FormatMessage(hashValue, existingItem, collidingItem))
		{
		}

		private static string FormatMessage(string hashValue, string existingItem, string collidingItem)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Hash code collision on value `" + hashValue + "`");
			stringBuilder.AppendLine("Existing Item was : `" + existingItem + "`");
			stringBuilder.AppendLine("Colliding Item was : `" + collidingItem + "`");
			return stringBuilder.ToString();
		}
	}
}
