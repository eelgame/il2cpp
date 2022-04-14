using System;
using System.Collections.Generic;

namespace Unity.IL2CPP.Building.ToolChains.Android
{
	internal class ARM64Settings : TargetArchitectureSettings
	{
		public override string ABI => "arm64-v8a";

		public override string Arch => "arm64";

		public override string TCPrefix => "aarch64-linux-android";

		public override string Triple => "aarch64-linux-android";

		public override string Platform => "aarch64-none-linux-android";

		public override string LlvmTarget => "aarch64-linux-android" + APILevel;

		public override int APILevel => 21;

		public override IEnumerable<string> CxxFlags
		{
			get
			{
				yield return "-march=armv8-a";
			}
		}

		public override bool CanUseGoldLinker(Version ndkVersion, BuildConfiguration configuration)
		{
			return false;
		}
	}
}
