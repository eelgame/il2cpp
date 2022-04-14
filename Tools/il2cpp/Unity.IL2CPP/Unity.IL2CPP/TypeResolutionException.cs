using System;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP
{
	public class TypeResolutionException : Exception
	{
		public TypeNameParseInfo TypeNameInfo { get; }

		public TypeResolutionException(TypeNameParseInfo typeNameInfo)
		{
			TypeNameInfo = typeNameInfo;
		}
	}
}
