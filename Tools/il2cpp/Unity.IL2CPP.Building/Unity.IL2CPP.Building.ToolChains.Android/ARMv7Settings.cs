using System.Collections.Generic;

namespace Unity.IL2CPP.Building.ToolChains.Android
{
	internal class ARMv7Settings : TargetArchitectureSettings
	{
		public override string ABI => "armeabi-v7a";

		public override string Arch => "arm";

		public override string TCPrefix => "arm-linux-androideabi";

		public override string Triple => "arm-linux-androideabi";

		public override string Platform => "armv7-none-linux-androideabi";

		public override string LlvmTarget => "armv7-linux-androideabi" + APILevel;

		public override int APILevel => 19;

		public override IEnumerable<string> CxxFlags
		{
			get
			{
				yield return "-march=armv7-a";
				yield return "-mfloat-abi=softfp";
				yield return "-mfpu=neon-fp16";
			}
		}

		public override IEnumerable<string> StlLibraries
		{
			get
			{
				yield return "unwind";
				yield return "atomic";
			}
		}
	}
}
