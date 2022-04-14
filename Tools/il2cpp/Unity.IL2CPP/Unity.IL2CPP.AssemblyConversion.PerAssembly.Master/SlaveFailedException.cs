using System;

namespace Unity.IL2CPP.AssemblyConversion.PerAssembly.Master
{
	public class SlaveFailedException : Exception
	{
		public SlaveFailedException(string message)
			: base(message)
		{
		}
	}
}
