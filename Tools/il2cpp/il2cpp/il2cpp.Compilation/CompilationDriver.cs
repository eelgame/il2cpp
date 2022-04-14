using System;
using System.Collections.ObjectModel;
using System.Linq;
using il2cpp.Conversion;
using NiceIO;
using Unity.IL2CPP;
using Unity.IL2CPP.Building;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.Platforms;
using Unity.IL2CPP.Building.Statistics;
using Unity.IL2CPP.Common;
using Unity.MiniProfiling;

namespace il2cpp.Compilation
{
	internal static class CompilationDriver
	{
		public static CompilationResults Run(RuntimePlatform platform, BuildingOptions buildingOptions, ReadOnlyCollection<NPath> matchedAssemblyMethodSourceFiles)
		{
			IBuildStatistics statistics = null;
			NPath dataFolder = IL2CPPOptions.DataFolder;
			bool generateCMakeEnabled = BuildingOptionsParser.GenerateCMakeEnabled;
			foreach (NPath item in buildingOptions.AdditionalCpp)
			{
				item.Copy(buildingOptions.SourceDirectory);
			}
			DebuggerBuildOptions debuggerBuildOptions = DebuggerBuildUtils.DetermineBuildOptions(CodeGenOptions.EnableDebugger, CodeGenOptions.DebuggerOff);
			if (debuggerBuildOptions != 0)
			{
				buildingOptions.EnableScriptDebugging = true;
				buildingOptions.DataFolder = dataFolder;
			}
			string[] specifiedCompilerFlags = (string.IsNullOrEmpty(buildingOptions.CompilerFlags) ? null : buildingOptions.CompilerFlags.Split(' '));
			string[] specifiedLinkerFlags = (string.IsNullOrEmpty(buildingOptions.LinkerFlags) ? null : buildingOptions.LinkerFlags.Split(' '));
			PlatformSupport platformSupport = PlatformSupport.For(platform);
			CppToolChain cppToolChain = platformSupport.MakeCppToolChain(buildingOptions);
			NPath mapFileParser = ((IL2CPPOptions.MapFileParser == null) ? null : IL2CPPOptions.MapFileParser);
			IL2CPPOutputBuildDescription programBuildDescription = new IL2CPPOutputBuildDescription(buildingOptions.SourceDirectory, buildingOptions.CacheDirectory, buildingOptions.OutputPath, CodeGenOptions.Dotnetprofile, platformSupport, cppToolChain, dataFolder, buildingOptions.RelativeDataPath, buildingOptions.ForceRebuild, buildingOptions.Runtime, buildingOptions.RuntimeGC, buildingOptions.Architecture, ContextDataFactory.CreateFeaturesFromOptions(), ContextDataFactory.CreateDiagnosticOptionsFromOptions(), ContextDataFactory.CreateTestingOptionsFromOptions(), buildingOptions.AdditionalDefines, buildingOptions.AdditionalIncludeDirectories, buildingOptions.AdditionalLibraries.Select((string lib) => new NPath(lib)), specifiedCompilerFlags, specifiedLinkerFlags, buildingOptions.LibIL2CPPCacheDirectory, buildingOptions.BaselibDirectory, mapFileParser, debuggerBuildOptions, buildingOptions.DisableRuntimeLumping, CodeGenOptions.WriteBarrierValidation, CodeGenOptions.IncrementalGCTimeSlice, IL2CPPOptions.DevelopmentMode);
			ProgramBuildDescription programBuildDescription2 = platformSupport.PostProcessProgramBuildDescription(programBuildDescription);
			if (generateCMakeEnabled)
			{
				CppProgramCMakeGenerator.AddBuild(programBuildDescription2, cppToolChain);
			}
			if (IL2CPPOptions.CompileCpp)
			{
				using (BuildShell.CreateCommandLogger(buildingOptions.SourceDirectory, buildingOptions.CommandLog))
				{
					CppProgramBuilder builder = new CppProgramBuilder(cppToolChain, programBuildDescription2, buildingOptions.Verbose, buildingOptions.ForceRebuild, buildingOptions.IncludeFileNamesInHashes, buildingOptions.AvoidDynamicLibraryCopy, matchedAssemblyMethodSourceFiles);
					using (MiniProfiler.Section("Build"))
					{
						builder.BuildAndLogStatsForTestRunner(out statistics);
					}
				}
			}
			if (generateCMakeEnabled)
			{
				try
				{
					new CppProgramCMakeGenerator(buildingOptions.SourceDirectory, buildingOptions.GenerateCmake, ContextDataFactory.GetRuntimeBackendFromOptions()).Generate();
				}
				catch (Exception ex)
				{
					Console.WriteLine("Failed to generate CMake project: " + ex.ToString());
				}
			}
			if (BuildingOptionsParser.BuildingOptionsArgs.AssemblyOutput && !string.IsNullOrEmpty(CodeGenOptions.AssemblyMethod) && matchedAssemblyMethodSourceFiles != null)
			{
				foreach (NPath matchedAssemblyMethodSourceFile in matchedAssemblyMethodSourceFiles)
				{
					OutputSourceCodeFor(CodeGenOptions.AssemblyMethod, matchedAssemblyMethodSourceFile, new GeneratedCppCodeMethodSeacher(), "Generated C++");
					SourceCodeSearcher sourceCodeSearcher = cppToolChain.SourceCodeSearcher();
					if (sourceCodeSearcher == null)
					{
						throw new InvalidOperationException($"{cppToolChain} does not implement IFindMethodInSourceCode");
					}
					OutputSourceCodeFor(CodeGenOptions.AssemblyMethod, matchedAssemblyMethodSourceFile.ChangeExtension("s"), sourceCodeSearcher, $"{cppToolChain} assembly");
				}
			}
			return new CompilationResults(statistics);
		}

		private static void OutputSourceCodeFor(string methodToFind, NPath sourceCodeFile, SourceCodeSearcher searcher, string nameOfCode)
		{
			SourceExtractor sourceExtractor = new SourceExtractor(sourceCodeFile.ReadAllText());
			Console.WriteLine();
			Console.WriteLine($"**{nameOfCode} code for `{methodToFind}` ({sourceCodeFile})**");
			Console.WriteLine("```");
			Console.WriteLine(sourceExtractor.FindMethodInSourceCode(methodToFind, searcher));
			Console.WriteLine("```");
		}
	}
}
