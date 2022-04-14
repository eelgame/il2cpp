using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.Building.ToolChains.Android
{
	internal abstract class TargetArchitectureSettings
	{
		public abstract string ABI { get; }

		public abstract string Arch { get; }

		public abstract string TCPrefix { get; }

		public abstract string Triple { get; }

		public abstract string Platform { get; }

		public abstract string LlvmTarget { get; }

		public abstract int APILevel { get; }

		public virtual IEnumerable<string> CxxFlags => new string[0];

		public virtual IEnumerable<string> StlLibraries => new string[0];

		public virtual bool CanUseGoldLinker(Version ndkVersion, BuildConfiguration configuration)
		{
			if (ndkVersion.Major == 18)
			{
				return configuration == BuildConfiguration.ReleasePlus;
			}
			return true;
		}
	}
}
