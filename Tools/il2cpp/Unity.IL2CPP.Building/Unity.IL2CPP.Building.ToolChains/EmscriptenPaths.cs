using System;
using System.Collections.Generic;
using System.IO;
using NiceIO;
using Unity.IL2CPP.Building.Statistics;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains
{
	public static class EmscriptenPaths
	{
		private static bool _useDependenciesToolChain;

		private static bool _localCheckoutRunningInEditor;

		private static string _emscriptenTemp;

		private static string _emscriptenCache;

		public static bool UseDependenciesToolChain
		{
			get
			{
				if (!BuildingTestRunnerHelper.IsRunningUnderTestRunner && !_useDependenciesToolChain)
				{
					return _localCheckoutRunningInEditor;
				}
				return true;
			}
			set
			{
				_useDependenciesToolChain = value;
			}
		}

		public static Dictionary<string, string> EmscriptenEnvironmentVariables => new Dictionary<string, string>
		{
			{
				"EM_CONFIG",
				EmscriptenConfig.ToString()
			},
			{
				"LLVM",
				ExternalEmscriptenLlvmRoot.ToString()
			},
			{
				"NODE",
				NodeExecutable.ToString()
			},
			{
				"EMSCRIPTEN",
				EmscriptenToolsRoot.ToString()
			},
			{ "EMSCRIPTEN_TMP", _emscriptenTemp },
			{ "EM_CACHE", _emscriptenCache },
			{
				"EMSCRIPTEN_NATIVE_OPTIMIZER",
				OptimizerExecutable.ToString()
			},
			{
				"BINARYEN",
				BinaryenRoot.ToString()
			},
			{ "EMCC_WASM_BACKEND", "0" }
		};

		public static NPath ExternalEmscriptenLlvmRoot
		{
			get
			{
				string environmentVariable = Environment.GetEnvironmentVariable("LLVM_ROOT");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					return environmentVariable;
				}
				if (UseDependenciesToolChain)
				{
					return Unity.IL2CPP.Common.ToolChains.EmscriptenFastCompCurrent.Root;
				}
				string text = FastCompDirectoryForExecutingPlatform();
				if (UnitySourceCode.Available)
				{
					return WebGLExternalPathForCurrentUnityVersion(text).Combine("builds");
				}
				return WebGlRoot.Combine(text);
			}
		}

		public static NPath WebGlExternalRoot => WebGLExternalRootForCurrentUnityVersion;

		public static NPath MacEmscriptenSdkRoot
		{
			get
			{
				if (UseDependenciesToolChain)
				{
					return Unity.IL2CPP.Common.ToolChains.EmscriptenSdkMac.Root;
				}
				string environmentVariable = Environment.GetEnvironmentVariable("EMSDK");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					return environmentVariable;
				}
				if (UnitySourceCode.Available)
				{
					return WebGLExternalPathForCurrentUnityVersion("EmscriptenSdk_Mac").Combine("builds");
				}
				return WebGlRoot.Combine("Emscripten_Mac/");
			}
		}

		public static NPath WindowsEmscriptenSdkRoot
		{
			get
			{
				if (UseDependenciesToolChain)
				{
					return Unity.IL2CPP.Common.ToolChains.EmscriptenSdkWin.Root;
				}
				string environmentVariable = Environment.GetEnvironmentVariable("EMSDK");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					return environmentVariable;
				}
				if (UnitySourceCode.Available)
				{
					return WebGLExternalPathForCurrentUnityVersion("EmscriptenSdk_Win").Combine("builds");
				}
				return WebGlRoot.Combine("Emscripten_Win/");
			}
		}

		public static NPath LinuxEmscriptenSdkRoot => MacEmscriptenSdkRoot;

		public static NPath EmscriptenToolsRoot
		{
			get
			{
				if (UseDependenciesToolChain)
				{
					return Unity.IL2CPP.Common.ToolChains.Emscripten.Root;
				}
				string environmentVariable = Environment.GetEnvironmentVariable("EMSCRIPTEN");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					return environmentVariable;
				}
				if (UnitySourceCode.Available)
				{
					return WebGLExternalPathForCurrentUnityVersion("Emscripten").Combine("builds");
				}
				return WebGlRoot.Combine("Emscripten");
			}
		}

		public static NPath Emcc => EmscriptenToolsRoot.Combine("emcc");

		public static NPath Emcpp => EmscriptenToolsRoot.Combine("em++");

		public static NPath WebsocketToPosixProxy => EmscriptenToolsRoot.Combine("tools", "websocket_to_posix_proxy", "build", "Release", "websocket_to_posix_proxy.exe");

		public static string LlcExecutableName
		{
			get
			{
				if (PlatformUtils.IsWindows())
				{
					return "llc.exe";
				}
				return "llc";
			}
		}

		public static NPath BaseLocation => new NPath(new Uri(typeof(EmscriptenToolChain).Assembly.CodeBase).LocalPath);

		public static NPath Python
		{
			get
			{
				string environmentVariable = Environment.GetEnvironmentVariable("EMSDK_PYTHON");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					return environmentVariable;
				}
				if (PlatformUtils.IsWindows())
				{
					return WindowsEmscriptenSdkRoot.Combine("python/2.7.5.3_64bit/python.exe");
				}
				if (PlatformUtils.IsOSX())
				{
					return new NPath("/usr/bin/python");
				}
				if (PlatformUtils.IsLinux())
				{
					return new NPath(File.Exists("/usr/bin/python2") ? "/usr/bin/python2" : "/usr/bin/python");
				}
				throw new NotSupportedException("Don't know how to get python path on current platform!");
			}
		}

		public static NPath NodeExecutable
		{
			get
			{
				if (UseDependenciesToolChain)
				{
					return Unity.IL2CPP.Common.ToolChains.NodeJsCurrent.NodeExecutable;
				}
				string environmentVariable = Environment.GetEnvironmentVariable("EMSDK_NODE");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					return environmentVariable;
				}
				NPath nPath = BaseLocation.ParentContaining("UnityExtensions");
				if (nPath != null)
				{
					return nPath.Combine("Tools", "nodejs", "bin", NodeExecutableNameForCurrentPlatform());
				}
				if (UnitySourceCode.Available)
				{
					if (PlatformUtils.IsWindows())
					{
						return WindowsEmscriptenSdkRoot.Combine("node/node.exe");
					}
					if (PlatformUtils.IsOSX())
					{
						return MacEmscriptenSdkRoot.Combine("node/0.10.18_64bit/bin/node");
					}
					throw new NotSupportedException("Don't know how to get node path on current platform!");
				}
				throw new NotSupportedException("Need Unity to use node");
			}
		}

		public static NPath FirefoxExecutable
		{
			get
			{
				if (UseDependenciesToolChain)
				{
					return Unity.IL2CPP.Common.ToolChains.FirefoxCurrent.FirefoxExecutable;
				}
				throw new NotSupportedException("The dependencies toolchain is required to use Firefox");
			}
		}

		public static NPath OptimizerExecutable
		{
			get
			{
				string environmentVariable = Environment.GetEnvironmentVariable("EMSCRIPTEN_NATIVE_OPTIMIZER");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					return environmentVariable;
				}
				return ExternalEmscriptenLlvmRoot.Combine("optimizer.exe");
			}
		}

		public static NPath BinaryenRoot
		{
			get
			{
				string environmentVariable = Environment.GetEnvironmentVariable("BINARYEN_ROOT");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					return environmentVariable;
				}
				return ExternalEmscriptenLlvmRoot.Combine("binaryen");
			}
		}

		public static NPath EmscriptenConfig
		{
			get
			{
				if (UseDependenciesToolChain)
				{
					return Unity.IL2CPP.Common.ToolChains.Emscripten.Root.Parent.Combine("emscripten.config");
				}
				string environmentVariable = Environment.GetEnvironmentVariable("EM_CONFIG");
				if (!string.IsNullOrEmpty(environmentVariable))
				{
					return environmentVariable;
				}
				return WebGlRoot.Combine("emscripten.config");
			}
		}

		public static NPath WebGlRoot
		{
			get
			{
				if (UnitySourceCode.Available)
				{
					return UnitySourceCode.Paths.UnityRoot.Combine("PlatformDependent/WebGL");
				}
				string[] append = new string[3] { "PlaybackEngines", "WebGLSupport", "BuildTools" };
				NPath nPath = BaseLocation.ParentContaining("PlaybackEngines");
				NPath nPath2 = nPath.Combine(append);
				if (nPath2.Exists())
				{
					return nPath2;
				}
				return nPath.Parent.ParentContaining("PlaybackEngines").Combine(append);
			}
		}

		private static NPath WebGLExternalRootForCurrentUnityVersion
		{
			get
			{
				if (!UseEmscriptenLocation5_5)
				{
					NPath nPath = WebGlRoot.Combine("External/");
					if (nPath.Exists())
					{
						return nPath;
					}
				}
				return UnitySourceCode.Paths.UnityRoot.Combine("External/");
			}
		}

		private static bool UseEmscriptenLocation5_5 => UnitySourceCode.Paths.UnityRoot.Combine("External/Emscripten/Emscripten/builds.7z").Exists();

		static EmscriptenPaths()
		{
			_useDependenciesToolChain = false;
			_localCheckoutRunningInEditor = Environment.GetEnvironmentVariable("UNITY_IL2CPP_PATH") != null;
			if (UnitySourceCode.Available)
			{
				BuildsDirectoryWithUnpackIfNecessary(WebGLExternalPathForCurrentUnityVersion(FastCompDirectoryForExecutingPlatform()));
				BuildsDirectoryWithUnpackIfNecessary(WebGLExternalPathForCurrentUnityVersion(SdkDirectoryForExecutingPlatform()));
			}
			_emscriptenTemp = ((EmscriptenToolChain.EmscriptenBuildingOptions.EmscriptenTemp != null) ? EmscriptenToolChain.EmscriptenBuildingOptions.EmscriptenTemp : TempDir.Empty("emscriptenTemp").ToString());
			_emscriptenCache = ((EmscriptenToolChain.EmscriptenBuildingOptions.EmscriptenTemp != null) ? EmscriptenToolChain.EmscriptenBuildingOptions.EmscriptenTemp : TempDir.Empty("emscriptencache").ToString());
		}

		public static void ShowWindowsEnvironmentSettings()
		{
			foreach (KeyValuePair<string, string> emscriptenEnvironmentVariable in EmscriptenEnvironmentVariables)
			{
				ConsoleOutput.Info.WriteLine("SET {0}={1}", emscriptenEnvironmentVariable.Key, emscriptenEnvironmentVariable.Value);
			}
		}

		private static string FastCompDirectoryForExecutingPlatform()
		{
			if (PlatformUtils.IsWindows())
			{
				return "EmscriptenFastComp_Win/";
			}
			if (PlatformUtils.IsOSX())
			{
				return "EmscriptenFastComp_Mac/";
			}
			if (PlatformUtils.IsLinux())
			{
				return "EmscriptenFastComp_Linux/";
			}
			throw new NotSupportedException("Unknown or unsupported OS for Emscripten");
		}

		private static string SdkDirectoryForExecutingPlatform()
		{
			if (PlatformUtils.IsWindows())
			{
				return "EmscriptenSdk_Win/";
			}
			if (PlatformUtils.IsOSX())
			{
				return "EmscriptenSdk_Mac/";
			}
			if (PlatformUtils.IsLinux())
			{
				return "Emscripten/";
			}
			throw new NotSupportedException("Unknown or unsupported OS for Emscripten");
		}

		private static void BuildsDirectoryWithUnpackIfNecessary(NPath rootDir)
		{
			Shell.ExecuteArgs executeArgs = new Shell.ExecuteArgs();
			executeArgs.Executable = "perl";
			executeArgs.Arguments = "Tools/Build/PrepareBuildsZip.pl " + rootDir.Combine("builds.7z").InQuotes();
			executeArgs.WorkingDirectory = UnitySourceCode.Paths.UnityRoot;
			if (Shell.ExecuteWithLiveOutput(executeArgs).ExitCode != 0)
			{
				throw new InvalidOperationException($"Failed to unpack Emscripten SDK in {rootDir}");
			}
		}

		private static string NodeExecutableNameForCurrentPlatform()
		{
			if (PlatformUtils.IsWindows())
			{
				return "node.exe";
			}
			if (PlatformUtils.IsOSX() || PlatformUtils.IsLinux())
			{
				return "node";
			}
			throw new NotSupportedException("Don't know how to get node executable on current platform!");
		}

		private static NPath WebGLExternalPathForCurrentUnityVersion(string path)
		{
			if (!UseEmscriptenLocation5_5)
			{
				NPath nPath = WebGlExternalRoot.Combine(path);
				if (nPath.Exists())
				{
					return nPath;
				}
			}
			return WebGlExternalRoot.Combine("Emscripten/", path);
		}
	}
}
