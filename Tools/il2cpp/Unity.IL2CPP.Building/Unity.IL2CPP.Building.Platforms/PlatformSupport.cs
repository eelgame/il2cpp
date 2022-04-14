using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.BuildDescriptions.Mono;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building.Platforms
{
	public abstract class PlatformSupport
	{
		public virtual BaselibBuildType BaselibBuildType => BaselibBuildType.StaticLibrary;

		public abstract string BaselibPlatformName { get; }

		public abstract bool Supports(RuntimePlatform platform);

		public virtual void RegisterRunner()
		{
		}

		public abstract CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors);

		public virtual CppToolChain MakeCppToolChain(Architecture architecture, BuildConfiguration buildConfiguration, bool treatWarningsAsErrors, bool assemblyOutput)
		{
			return MakeCppToolChain(architecture, buildConfiguration, treatWarningsAsErrors);
		}

		public virtual CppToolChain MakeCppToolChain(BuildingOptions buildingOptions)
		{
			return MakeCppToolChain(buildingOptions.Architecture, buildingOptions.Configuration, buildingOptions.TreatWarningsAsErrors, buildingOptions.AssemblyOutput);
		}

		public virtual ProgramBuildDescription PostProcessProgramBuildDescription(ProgramBuildDescription programBuildDescription)
		{
			return programBuildDescription;
		}

		public virtual Architecture GetSupportedArchitectureOfSameBitness(Architecture architecture)
		{
			return architecture;
		}

		public virtual MonoSourceFileList GetMonoSourceFileList()
		{
			return new UnityMonoSourceFileList();
		}

		public virtual MonoSourceFileList GetDebuggerMonoSourceFileList()
		{
			return new UnityMonoSourceFileList();
		}

		public static bool Available(RuntimePlatform runtimePlatform)
		{
			PlatformSupport support;
			return TryFor(runtimePlatform, out support);
		}

		public static bool TryFor(RuntimePlatform runtimePlatform, out PlatformSupport support)
		{
			foreach (Type item in from t in AllTypes()
				where typeof(PlatformSupport).IsAssignableFrom(t) && !t.IsAbstract && !t.IsGenericType
				select t)
			{
				PlatformSupport platformSupport = (PlatformSupport)Activator.CreateInstance(item);
				if (platformSupport.Supports(runtimePlatform))
				{
					support = platformSupport;
					return true;
				}
			}
			support = null;
			return false;
		}

		public static PlatformSupport For(RuntimePlatform runtimePlatform)
		{
			if (TryFor(runtimePlatform, out var support))
			{
				return support;
			}
			throw new InvalidOperationException($"Could not find platform support for {runtimePlatform.Name} runtime platform. Is platform plugin present?");
		}

		public static NPath ChooseExtension(RuntimePlatform runtimePlatform, Architecture architecture, NPath outputPath, string toolChainPath)
		{
			return ChooseExtension(runtimePlatform, architecture, outputPath, toolChainPath, null);
		}

		public static NPath ChooseExtension(RuntimePlatform runtimePlatform, Architecture architecture, NPath outputPath, string toolChainPath, string sysrootPath)
		{
			BuildingOptions buildingOptions = new BuildingOptions
			{
				Architecture = architecture,
				Configuration = BuildConfiguration.Debug,
				TreatWarningsAsErrors = true,
				UseDependenciesToolChain = false
			};
			if (!string.IsNullOrEmpty(toolChainPath))
			{
				buildingOptions.ToolChainPath = toolChainPath;
			}
			if (!string.IsNullOrEmpty(sysrootPath))
			{
				buildingOptions.SysrootPath = sysrootPath;
			}
			CppToolChain cppToolChain = For(runtimePlatform).MakeCppToolChain(buildingOptions);
			if (outputPath.ExtensionWithDot == ".dll")
			{
				return outputPath.ChangeExtension(cppToolChain.DynamicLibraryExtension);
			}
			return outputPath.ChangeExtension(cppToolChain.ExecutableExtension());
		}

		private static IEnumerable<Type> AllTypes()
		{
			List<Type> list = new List<Type>();
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				try
				{
					list.AddRange(assembly.GetTypes());
				}
				catch (ReflectionTypeLoadException ex)
				{
					if (ex.Types != null)
					{
						list.AddRange(ex.Types.Where((Type t) => t != null));
					}
				}
			}
			return list;
		}

		public virtual int GetDebuggerFixedPort()
		{
			return 56000;
		}

		public abstract string BaselibToolchainName(Architecture architecture);
	}
}
