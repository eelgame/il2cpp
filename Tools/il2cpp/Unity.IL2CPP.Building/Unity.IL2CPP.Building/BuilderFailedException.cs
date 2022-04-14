using System;

namespace Unity.IL2CPP.Building
{
	public class BuilderFailedException : Exception
	{
		public BuilderFailedException(string failureReason)
			: base(failureReason)
		{
		}
	}
}
