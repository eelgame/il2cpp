using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using il2cpp.Compilation;
using il2cpp.Conversion;
using NiceIO;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.Platforms;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;
using Unity.Options;

namespace il2cpp
{
	public static class Il2CppOptionParser
	{
		public static void ParseArguments(string[] args, out bool continueToRun, out ExitCode exitCode, out RuntimePlatform platform, out BuildingOptions buildingOptions, out string profilerOutput)
		{
			CodeGenOptions.SetToDefaults();
			IL2CPPOptions.SetToDefaults();
			BuildingOptionsParser.SetToDefaults();
			StatisticsOptions.SetToDefaults();
			EmscriptenToolChain.EmscriptenBuildingOptions.SetToDefaults();
			platform = null;
			buildingOptions = null;
			profilerOutput = null;
			exitCode = ExitCode.Success;
			Type[] array = CollectAllOptionTypes();
			if (OptionsParser.HelpRequested(args) || args.Length == 0)
			{
				OptionsParser.DisplayHelp(array);
				continueToRun = false;
				return;
			}
			string[] deprecatedOptions = new string[1] { "copy-level=" };
			List<string> remainingArguments = (from r in OptionsParser.Prepare(args, array, OptionsHelpers.CommonCustomOptionParser)
				where !deprecatedOptions.Any((string o) => r.Contains(o))
				select r).ToList();
			remainingArguments = CollectFoundAssembliesFromRemainingArguments(Environment.CurrentDirectory.ToNPath(), remainingArguments, out var foundAssemblies).ToList();
			if (remainingArguments.Count > 0)
			{
				Console.WriteLine("Either unknown arguments were used or one or more assemblies could not be found : ");
				foreach (string item in remainingArguments)
				{
					Console.WriteLine("\t {0}", item);
				}
				continueToRun = false;
				exitCode = ExitCode.UnknownArgument;
			}
			else
			{
				CodeGenOptions.Initialize();
				SetupOtherArguments(foundAssemblies, out profilerOutput);
				if ((IL2CPPOptions.CompileCpp || BuildingOptionsParser.GenerateCMakeEnabled) && !ParseBuildingArguments(out exitCode, out platform, out buildingOptions))
				{
					continueToRun = false;
				}
				else
				{
					continueToRun = true;
				}
			}
		}

		private static bool ParseBuildingArguments(out ExitCode exitCode, out RuntimePlatform platform, out BuildingOptions buildingOptions)
		{
			BuildingOptionsParser.Parse(IL2CPPOptions.Generatedcppdir, IL2CPPOptions.Assembly, CodeGenOptions.Dotnetprofile, ContextDataFactory.GetRuntimeBackendFromOptions(), out platform, out buildingOptions, IL2CPPOptions.DevelopmentMode, CodeGenOptions.EnableDebugger, CodeGenOptions.DebuggerOff);
			buildingOptions.Validate();
			if (buildingOptions.AssemblyOutput)
			{
				CppToolChain cppToolChain = PlatformSupport.For(platform).MakeCppToolChain(buildingOptions);
				if (!cppToolChain.CanGenerateAssemblyCode())
				{
					Console.WriteLine($"The {cppToolChain} toolchain does not support assembly output.");
					exitCode = ExitCode.UnknownArgument;
					return false;
				}
			}
			exitCode = ExitCode.Success;
			return true;
		}

		private static void SetupOtherArguments(List<NPath> foundAssemblies, out string profilerOutput)
		{
			IL2CPPOptions.Assembly = (from p in IL2CPPOptions.Assembly.Concat(foundAssemblies)
				select p.MakeAbsolute()).ToArray();
			if (!IL2CPPOptions.CompileCpp && !IL2CPPOptions.ConvertToCpp)
			{
				IL2CPPOptions.CompileCpp = true;
				IL2CPPOptions.ConvertToCpp = true;
			}
			if (IL2CPPOptions.Plugin != null)
			{
				NPath[] plugin = IL2CPPOptions.Plugin;
				for (int i = 0; i < plugin.Length; i++)
				{
					Assembly.LoadFrom(plugin[i].ToString());
				}
			}
			if (IL2CPPOptions.Generatedcppdir == null)
			{
				IL2CPPOptions.Generatedcppdir = NPath.CreateTempDirectory("il2cpp_generatedcpp");
			}
			else if (IL2CPPOptions.Generatedcppdir.IsRelative)
			{
				IL2CPPOptions.Generatedcppdir = NPath.CurrentDirectory.Combine(IL2CPPOptions.Generatedcppdir);
			}
			if (IL2CPPOptions.DataFolder == null)
			{
				IL2CPPOptions.DataFolder = IL2CPPOptions.Generatedcppdir.Combine("Data");
			}
			if (IL2CPPOptions.SymbolsFolder == null)
			{
				IL2CPPOptions.SymbolsFolder = IL2CPPOptions.Generatedcppdir.Combine("Symbols");
			}
			if (IL2CPPOptions.ExtraTypesFile == null)
			{
				IL2CPPOptions.ExtraTypesFile = new NPath[0];
			}
			if (StatisticsOptions.StatsOutputDir == null)
			{
				StatisticsOptions.StatsOutputDir = IL2CPPOptions.Generatedcppdir;
			}
			else
			{
				StatisticsGenerator.DetermineAndSetupOutputDirectory();
			}
			profilerOutput = (IL2CPPOptions.ProfilerReport ? StatisticsOptions.StatsOutputDir.Combine("profile.json") : null);
		}

		private static Type[] CollectAllOptionTypes()
		{
			return OptionsParser.LoadOptionTypesFromAssembly(typeof(Program).Assembly, includeReferencedAssemblies: true, (AssemblyName asm) => (!(asm.Name == "Unity.IL2CPP.RuntimeServices")) ? true : false, delegate(Assembly asm)
			{
				try
				{
					return asm.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					if (PlatformUtils.IsWindows() || !(asm.GetName().Name == "Unity.IL2CPP.Building"))
					{
						throw;
					}
					return ex.Types.Where((Type t) => t != null).ToArray();
				}
			});
		}

		public static IEnumerable<string> CollectFoundAssembliesFromRemainingArguments(NPath currentDirectory, IEnumerable<string> remainingArguments, out List<NPath> foundAssemblies)
		{
			List<NPath> list = new List<NPath>();
			List<string> list2 = new List<string>();
			foreach (string remainingArgument in remainingArguments)
			{
				try
				{
					if (Path.IsPathRooted(remainingArgument))
					{
						if (File.Exists(remainingArgument))
						{
							list.Add(remainingArgument.ToNPath());
						}
						else
						{
							list2.Add(remainingArgument);
						}
						continue;
					}
					NPath nPath = currentDirectory.Combine(remainingArgument);
					if (nPath.Exists())
					{
						list.Add(nPath);
					}
					else
					{
						list2.Add(remainingArgument);
					}
				}
				catch (ArgumentException)
				{
					list2.Add(remainingArgument);
				}
			}
			foundAssemblies = list;
			return list2;
		}
	}
}
