using System.Collections.Generic;
using System.IO;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.ToolChains.Linux
{
	public class LinuxClangSdk
	{
		private readonly NPath _sdkPath;

		private readonly NPath _toolsPath;

		private List<NPath> _includePaths;

		private List<NPath> _libraryPaths;

		public List<NPath> IncludePaths => _includePaths;

		public List<NPath> LibraryPaths => _libraryPaths;

		public LinuxClangSdk(NPath sdkPath, NPath toolsPath)
		{
			_sdkPath = sdkPath;
			if (_sdkPath == null)
			{
				_sdkPath = new NPath("/");
			}
			_toolsPath = toolsPath;
			if (_toolsPath == null)
			{
				if (File.Exists(_sdkPath.Combine("usr/bin/clang")))
				{
					_toolsPath = _sdkPath;
				}
				else
				{
					_toolsPath = new NPath("/");
				}
			}
			_includePaths = new List<NPath>();
			_libraryPaths = new List<NPath>();
		}

		public NPath GetSysRoot()
		{
			return _sdkPath;
		}

		private NPath FindBinary(string binary)
		{
			if (PlatformUtils.IsWindows())
			{
				binary += ".exe";
			}
			if (File.Exists(_toolsPath.Combine("bin/" + binary)))
			{
				return _toolsPath.Combine("bin/" + binary);
			}
			if (File.Exists(_toolsPath.Combine("usr/bin/" + binary)))
			{
				return _toolsPath.Combine("usr/bin/" + binary);
			}
			return binary;
		}

		public NPath GetCCompilerPath()
		{
			return FindBinary("clang");
		}

		public NPath GetCppCompilerPath()
		{
			return FindBinary("clang++");
		}

		public NPath GetSysRootDirectory()
		{
			return _sdkPath;
		}

		private IEnumerable<string> CommonOptions()
		{
			if (_sdkPath != (NPath)"/")
			{
				yield return $"--sysroot={_sdkPath}";
			}
		}

		public IEnumerable<string> CompilerOptions()
		{
			foreach (string item in CommonOptions())
			{
				yield return item;
			}
		}

		public IEnumerable<string> LinkerOptions()
		{
			foreach (string item in CommonOptions())
			{
				yield return item;
			}
		}

		public bool CanBuildCode()
		{
			return GetReasonCannotBuildCode() == null;
		}

		public string GetReasonCannotBuildCode()
		{
			if (!_sdkPath.DirectoryExists())
			{
				return $"Linux Clang SDK could not be found at {_sdkPath}";
			}
			NPath cCompilerPath = GetCCompilerPath();
			if (!cCompilerPath.FileExists())
			{
				return "Could not find valid clang executable at " + cCompilerPath.ToString(SlashMode.Native);
			}
			NPath cppCompilerPath = GetCppCompilerPath();
			if (!cppCompilerPath.FileExists())
			{
				return "Could not find valid clang++ executable at " + cppCompilerPath.ToString(SlashMode.Native);
			}
			return null;
		}

		public void AddIncludeDirectory(NPath incDir)
		{
			_includePaths.Add(incDir);
		}

		public void AddLinkDirectory(NPath libDir)
		{
			_libraryPaths.Add(libDir);
		}

		public static LinuxClangSdk GetDependenciesInstallation()
		{
			LinuxClangSdk linuxClangSdk = new LinuxClangSdk(Unity.IL2CPP.Common.ToolChains.Linux.SysRoot, Unity.IL2CPP.Common.ToolChains.Linux.ToolPath);
			foreach (NPath item in Unity.IL2CPP.Common.ToolChains.Linux.IncludeDirectories())
			{
				linuxClangSdk.AddIncludeDirectory(item);
			}
			return linuxClangSdk;
		}

		public static LinuxClangSdk GetSystemInstallation()
		{
			return new LinuxClangSdk(null, null);
		}
	}
}
