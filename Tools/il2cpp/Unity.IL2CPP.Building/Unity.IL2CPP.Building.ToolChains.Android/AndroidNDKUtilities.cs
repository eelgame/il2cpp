using System;
using System.Collections.Generic;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains.Android
{
	public class AndroidNDKUtilities
	{
		private const string k_VersionString = "r19";

		private const string k_PackageDescription = "Android NDK";

		private readonly Version k_PackageRevision = new Version(19, 0, 5232133);

		private const string Toolchain = "llvm";

		private const string GccVersion = "4.9";

		private const string GnuStlVersion = "4.9";

		private readonly Version _version;

		private readonly TargetArchitectureSettings _architectureSettings;

		public NPath AndroidNdkRootDir { get; private set; }

		private NPath ToolchainDir => AndroidNdkRootDir.Combine("toolchains", "llvm", "prebuilt", HostPlatform, "bin");

		public NPath CppCompilerPath => ToolchainDir.Combine("clang++");

		public NPath CCompilerPath => ToolchainDir.Combine("clang");

		public NPath LinkerPath => CppCompilerPath;

		public NPath GdbPath => AndroidNdkRootDir.Combine("prebuilt", HostPlatform, "bin", "gdb");

		public NPath ObjCopyPath => GccToolchain.Combine("bin", _architectureSettings.Triple + "-objcopy");

		public NPath GccToolchain => AndroidNdkRootDir.Combine("toolchains", _architectureSettings.TCPrefix + "-4.9", "prebuilt", HostPlatform);

		public NPath CompilerSysRoot
		{
			get
			{
				if (!UnifiedHeaders)
				{
					return LinkerSysRoot;
				}
				return AndroidNdkRootDir.Combine("sysroot");
			}
		}

		public NPath LinkerSysRoot => AndroidNdkRootDir.Combine("platforms", $"android-{_architectureSettings.APILevel}", "arch-" + _architectureSettings.Arch);

		public NPath StlRoot
		{
			get
			{
				NPath nPath = AndroidNdkRootDir.Combine("sources", "cxx-stl");
				if (!LegacyStl)
				{
					return nPath.Combine("llvm-libc++");
				}
				return nPath.Combine("gnu-libstdc++", "4.9");
			}
		}

		public NPath GdbServer => AndroidNdkRootDir.Combine("prebuilt", "android-" + _architectureSettings.Arch, "gdbserver", "gdbserver");

		public IEnumerable<NPath> StlIncludePaths
		{
			get
			{
				if (GnuBinutils)
				{
					yield return StlRoot.Combine("include");
					if (LegacyStl)
					{
						yield return StlRoot.Combine("include", "backward");
						yield return StlRoot.Combine("libs", _architectureSettings.ABI, "include");
					}
					else
					{
						yield return AndroidNdkRootDir.Combine("sources", "cxx-stl", "llvm-libc++abi", "include");
						yield return AndroidNdkRootDir.Combine("sources", "android", "support", "include");
					}
				}
			}
		}

		public IEnumerable<NPath> StlLibraryPaths
		{
			get
			{
				if (GnuBinutils)
				{
					yield return StlRoot.Combine("libs", _architectureSettings.ABI);
				}
			}
		}

		public IEnumerable<string> StlLibraries
		{
			get
			{
				if (!GnuBinutils)
				{
					yield break;
				}
				if (LegacyStl)
				{
					yield return "gnustl_static";
					yield return "atomic";
					yield break;
				}
				yield return "c++_static";
				yield return "c++abi";
				if (_architectureSettings.APILevel < 21)
				{
					yield return "android_support";
				}
				foreach (string stlLibrary in _architectureSettings.StlLibraries)
				{
					yield return stlLibrary;
				}
			}
		}

		public string Platform
		{
			get
			{
				if (GnuBinutils)
				{
					return _architectureSettings.Platform;
				}
				return _architectureSettings.LlvmTarget;
			}
		}

		public IEnumerable<string> ArchitectureCompilerFlags
		{
			get
			{
				if (UnifiedHeaders)
				{
					if (GnuBinutils)
					{
						yield return "-isystem " + CompilerSysRoot.Combine("usr", "include", _architectureSettings.Triple).InQuotes();
					}
					yield return $"-D__ANDROID_API__={_architectureSettings.APILevel}";
				}
				foreach (string cxxFlag in _architectureSettings.CxxFlags)
				{
					yield return cxxFlag;
				}
			}
		}

		private bool UnifiedHeaders => _version.Major >= 16;

		internal bool LegacyStl => _version.Major < 18;

		internal bool GnuBinutils => _version.Major < 19;

		private static string HostPlatform
		{
			get
			{
				if (PlatformUtils.IsWindows())
				{
					if (!Environment.Is64BitOperatingSystem)
					{
						return "windows";
					}
					return "windows-x86_64";
				}
				if (PlatformUtils.IsLinux())
				{
					return "linux-x86_64";
				}
				return "darwin-x86_64";
			}
		}

		private static NPath GetNdkRootDir()
		{
			string environmentVariable = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT");
			if (string.IsNullOrEmpty(environmentVariable))
			{
				if (UnitySourceCode.Available || !Il2CppDependencies.Available)
				{
					throw new Exception("Android NDK not found. Make sure environment variable ANDROID_NDK_ROOT is not empty.");
				}
				if (PlatformUtils.IsOSX())
				{
					Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", CommonPaths.Il2CppDependencies.Combine("android-ndk-mac"));
				}
				else if (PlatformUtils.IsLinux())
				{
					Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", CommonPaths.Il2CppDependencies.Combine("android-ndk-linux"));
				}
				else
				{
					Environment.SetEnvironmentVariable("ANDROID_NDK_ROOT", CommonPaths.Il2CppDependencies.Combine("android-ndk-win"));
				}
				environmentVariable = Environment.GetEnvironmentVariable("ANDROID_NDK_ROOT");
				Console.WriteLine("ANDROID_NDK_ROOT is set to " + environmentVariable);
			}
			return new NPath(environmentVariable);
		}

		private static void SetSdkRootDir()
		{
			string environmentVariable = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
			if (string.IsNullOrEmpty(environmentVariable) && !UnitySourceCode.Available && Il2CppDependencies.Available)
			{
				if (PlatformUtils.IsOSX())
				{
					Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT", CommonPaths.Il2CppDependencies.Combine("android-sdk-darwin-x86_64"));
				}
				else if (PlatformUtils.IsLinux())
				{
					Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT", CommonPaths.Il2CppDependencies.Combine("android-sdk-linux-x86_64"));
				}
				else
				{
					Environment.SetEnvironmentVariable("ANDROID_SDK_ROOT", CommonPaths.Il2CppDependencies.Combine("android-sdk-windows-x86_64"));
				}
				environmentVariable = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
				Console.WriteLine("ANDROID_SDK_ROOT is set to " + environmentVariable);
			}
		}

		public AndroidNDKUtilities(NPath ndkRootPath, Architecture architecture, bool useDependenciesToolChain)
		{
			SetSdkRootDir();
			if (ndkRootPath == null || ndkRootPath.Depth == 0)
			{
				ndkRootPath = ((!useDependenciesToolChain) ? GetNdkRootDir() : Unity.IL2CPP.Common.ToolChains.Android.AndroidNDKDirectory);
			}
			AndroidComponentProperties androidComponentProperties = AndroidComponentProperties.Read(ndkRootPath);
			if (androidComponentProperties == null || androidComponentProperties.PackageDescription != "Android NDK")
			{
				throw new Exception(string.Format("Android NDK {0} or newer not detected at '{1}'.", "r19", ndkRootPath));
			}
			if (androidComponentProperties.PackageRevision < k_PackageRevision)
			{
				throw new Exception(string.Format("Android NDK {0} or newer required. Revision {1} detected at '{2}'.", "r19", androidComponentProperties.PackageRevision, ndkRootPath));
			}
			_version = androidComponentProperties.PackageRevision;
			AndroidNdkRootDir = ndkRootPath;
			if (architecture is ARMv7Architecture)
			{
				_architectureSettings = new ARMv7Settings();
				return;
			}
			if (architecture is ARM64Architecture)
			{
				_architectureSettings = new ARM64Settings();
				return;
			}
			if (architecture is x86Architecture)
			{
				_architectureSettings = new X86Settings();
				return;
			}
			throw new NotSupportedException("Unknown architecture: " + architecture);
		}

		public bool CanUseGoldLinker(BuildConfiguration configuration)
		{
			return _architectureSettings.CanUseGoldLinker(_version, configuration);
		}

		public IEnumerable<string> GetArchitectureLinkerFlags(BuildConfiguration configuration)
		{
			string text = ((Environment.GetEnvironmentVariable("UNITY_IL2CPP_ANDROID_USE_LLD_LINKER") == null) ? (CanUseGoldLinker(configuration) ? "gold" : "bfd") : "lld");
			string text2 = (PlatformUtils.IsWindows() ? ".exe" : string.Empty);
			yield return "-fuse-ld=" + text + text2;
		}
	}
}
