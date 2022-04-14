using System.Collections.Generic;

namespace Unity.IL2CPP.Building.ToolChains.Android
{
	internal class X86Settings : TargetArchitectureSettings
	{
		public override string ABI => "x86";

		public override string Arch => "x86";

		public override string TCPrefix => "x86";

		public override string Triple => "i686-linux-android";

		public override string Platform => "i686-none-linux-android";

		public override string LlvmTarget => "i686-linux-android" + APILevel;

		public override int APILevel => 19;

		public override IEnumerable<string> CxxFlags
		{
			get
			{
				yield return "-mtune=atom";
				yield return "-mssse3";
				yield return "-mfpmath=sse";
				yield return "-mstackrealign";
			}
		}
	}
}
