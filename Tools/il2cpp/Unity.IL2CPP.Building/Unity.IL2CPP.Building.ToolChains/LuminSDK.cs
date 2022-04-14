using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public static class LuminSDK
	{
		public static NPath SDKRoot { get; private set; }

		private static IEnumerable<string> _SharedFlags => new string[4]
		{
			"-no-canonical-prefixes",
			"-target aarch64-linux-android",
			"--sysroot=" + SDKRoot.Combine("lumin").InQuotes(),
			"--gcc-toolchain=" + SDKRoot.Combine("tool/toolchains").InQuotes()
		};

		public static IEnumerable<string> CompilerFlags => _SharedFlags.Concat(new string[26]
		{
			"-c", "-fPIC", "-fno-limit-debug-info", "-fno-omit-frame-pointer", "-fno-short-enums", "-fno-strict-aliasing", "-fdata-sections", "-ffunction-sections", "-fstack-protector-strong", "-ftemplate-backtrace-limit=0",
			"-funsigned-char", "-funwind-tables", "-fvisibility=hidden", "-fwrapv", "-march=armv8-a", "-mcpu=cortex-a57+crypto", "-Wformat", "-Wno-invalid-command-line-argument", "-Wno-unused-command-line-argument", "-Wno-inconsistent-missing-override",
			"-Wno-comment", "-Wno-format", "-Wno-unknown-attributes", "-Wno-tautological-pointer-compare", "-stdlib=libc++", "-nostdinc++"
		});

		public static NPath CompilerPath => SDKRoot.Combine("tools/toolchains/bin/clang++" + (PlatformUtils.IsWindows() ? ".exe" : ""));

		public static IEnumerable<string> Defines => new string[7] { "PLATFORM_LUMIN=1", "LUMIN", "LUMIN_64", "ANDROID", "__ANDROID__", "__arm64__", "NO_GETCONTEXT" };

		public static IEnumerable<NPath> LibraryPaths => new NPath[4]
		{
			SDKRoot.Combine("lumin/stl/libc++/lib"),
			SDKRoot.Combine("tools/toolchains/lib/gcc/aarch64-linux-android/4.9.x"),
			SDKRoot.Combine("lumin/usr/lib"),
			SDKRoot.Combine("lib/lumin")
		};

		public static IEnumerable<string> LinkerFlags => new string[1] { "-nodefaultlibs" }.Concat(_SharedFlags).Concat(new string[12]
		{
			"-z noexecstack", "-z nocopyreloc", "-z relro", "-z now", "-z origin", "--gc-sections", "--warn-shared-textrel", "--fatal-warnings", "--build-id", "--no-undefined",
			"--enable-new-dtags", "--no-as-needed"
		}.Select((string f) => "-Wl," + f.Replace(" ", ",")));

		public static NPath LinkerPath => CompilerPath;

		public static IEnumerable<NPath> PlatformIncludes => new NPath[1] { SDKRoot?.Combine("include") };

		public static IEnumerable<string> SystemDynamicLibraries => new string[10] { "c++", "c", "dl", "m", "ml_ext_logging", "ml_graphics", "ml_lifecycle", "ml_perception_client", "EGL", "GLESv3" };

		public static IEnumerable<NPath> SystemIncludes => new NPath[2]
		{
			SDKRoot?.Combine("lumin/stl/libc++/include"),
			SDKRoot?.Combine("lumin/usr/include")
		};

		public static IEnumerable<string> SystemStaticLibraries => new string[1] { "gcc" };

		public static void EnsureExists()
		{
			if (SDKRoot == null)
			{
				throw new Exception("Unable to find MLSDK");
			}
			if (!SDKRoot.Exists())
			{
				throw new DirectoryNotFoundException($"No MLSDK found at : {SDKRoot}");
			}
		}

		public static NPath InitializeSDK(NPath sdkRoot = null)
		{
			if (FindFirstValidSdkPath(sdkRoot) != null)
			{
				return SDKRoot = sdkRoot;
			}
			if (SDKRoot != null)
			{
				return SDKRoot;
			}
			return SDKRoot = FindFirstValidSdkPath(GetEnvironmentVariableAsNPath("LUMINSDK_UNITY"), GetEnvironmentVariableAsNPath("MLSDK_UNITY"));
		}

		private static NPath FindFirstValidSdkPath(params NPath[] path)
		{
			return path.FirstOrDefault((NPath p) => p != null && p.DirectoryExists(".metadata") && p.FileExists("mabu"));
		}

		private static NPath GetEnvironmentVariableAsNPath(string varName)
		{
			string environmentVariable = Environment.GetEnvironmentVariable(varName);
			if (!string.IsNullOrEmpty(environmentVariable))
			{
				return new NPath(environmentVariable);
			}
			return null;
		}
	}
}
