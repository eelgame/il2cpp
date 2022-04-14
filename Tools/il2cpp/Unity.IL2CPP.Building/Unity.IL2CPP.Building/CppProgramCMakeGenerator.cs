using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Building.BuildDescriptions;
using Unity.IL2CPP.Building.ToolChains;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building
{
	public class CppProgramCMakeGenerator
	{
		private enum LinkerCommandOutputType
		{
			Executable,
			SharedLibrary
		}

		private class LinkerCommandData
		{
			private readonly List<NPath> _libPaths;

			private readonly List<string> _linkerFlags;

			private readonly List<string> _libraries;

			private readonly Dictionary<string, string> _variables;

			private LinkerCommandOutputType _outputType;

			public ReadOnlyCollection<NPath> LibPaths => _libPaths.AsReadOnly();

			public ReadOnlyCollection<string> LinkerFlags => _linkerFlags.AsReadOnly();

			public ReadOnlyCollection<string> Libraries => _libraries.AsReadOnly();

			public ReadOnlyDictionary<string, string> Variables => new ReadOnlyDictionary<string, string>(_variables);

			public LinkerCommandOutputType OutputType
			{
				get
				{
					return _outputType;
				}
				set
				{
					_outputType = value;
				}
			}

			public LinkerCommandData()
			{
				_libPaths = new List<NPath>();
				_linkerFlags = new List<string>();
				_libraries = new List<string>();
				_variables = new Dictionary<string, string>();
				_outputType = LinkerCommandOutputType.Executable;
			}

			public void AddPath(NPath path)
			{
				_libPaths.Add(path);
			}

			public void AddFlag(string flag)
			{
				_linkerFlags.Add(flag);
			}

			public void AddLibrary(string lib)
			{
				_libraries.Add(lib);
			}

			public void AddVariable(string name, string value)
			{
				_variables.Add(name, value);
			}
		}

		private class SourceGroupDir
		{
			private readonly Dictionary<string, SourceGroupDir> _subGroups = new Dictionary<string, SourceGroupDir>();

			private readonly List<NPath> _files = new List<NPath>();

			public const string RootName = "___root";

			public ReadOnlyCollection<NPath> Files => _files.AsReadOnly();

			public ReadOnlyCollection<SourceGroupDir> Subgroups => new ReadOnlyCollection<SourceGroupDir>(_subGroups.Values.ToArray());

			public string Name { get; }

			public SourceGroupDir(string name)
			{
				Name = name;
			}

			public void AddFile(NPath file)
			{
				_files.Add(file);
			}

			private bool HasSubGroup(string name)
			{
				return _subGroups.ContainsKey(name);
			}

			public SourceGroupDir AddSubgroup(string name)
			{
				SourceGroupDir sourceGroupDir;
				if (!HasSubGroup(name))
				{
					sourceGroupDir = new SourceGroupDir(name);
					_subGroups.Add(name, sourceGroupDir);
				}
				else
				{
					sourceGroupDir = _subGroups[name];
				}
				return sourceGroupDir;
			}
		}

		private class BuildData
		{
			private static readonly HashSet<string> s_reservedWords = new HashSet<string>(new string[1] { "test" });

			private readonly CppToolChainContext _toolChainContext;

			private readonly ProgramBuildDescription _desc;

			private readonly CppToolChain _toolChain;

			private readonly List<IntermediateObjectFileCompilationData> _compilationData;

			public LinkerCommandData LinkerCommand { get; }

			public IEnumerable<IntermediateObjectFileCompilationData> CompilationData => _compilationData;

			public IEnumerable<NPath> SourceFiles => _compilationData.Select((IntermediateObjectFileCompilationData x) => x.CompilationInvocation.SourceFile);

			public string ExecName { get; }

			public bool UsesMsvcToolChain => _toolChain is MsvcToolChain;

			public MsvcToolChain MsvcToolChain => _toolChain as MsvcToolChain;

			public bool CopyFilesFromSeparateBuildOutputDirectory { get; }

			public bool CanBuildInCurrentEnvironment => _toolChain.CanBuildInCurrentEnvironment();

			public string CannotBuildInCurrentEnvironmentErrorMessage
			{
				get
				{
					string text = _toolChain.GetCannotBuildInCurrentEnvironmentErrorMessage();
					if (string.IsNullOrEmpty(text))
					{
						text = $"Builder is unable to build using selected toolchain ({_toolChain.GetType().Name}) or architecture ({_toolChain.Architecture})!";
					}
					return text;
				}
			}

			public BuildData(ProgramBuildDescription desc, CppToolChain toolChain)
			{
				_desc = desc;
				_toolChain = toolChain;
				ExecName = _desc.OutputFile.FileNameWithoutExtension;
				if (s_reservedWords.Contains(ExecName))
				{
					ExecName += "app";
				}
				CopyFilesFromSeparateBuildOutputDirectory = _desc.OutputFile.Parent.FileName == "build";
				_toolChainContext = _toolChain.CreateToolChainContext();
				LinkerCommand = CreateLinkerCommandData();
				CppCompilationInstruction[] array = _desc.CppCompileInstructions.Concat(_toolChainContext.ExtraCompileInstructions).ToArray();
				CppCompilationInstruction[] array2 = array;
				foreach (CppCompilationInstruction cppCompilationInstruction in array2)
				{
					cppCompilationInstruction.Defines = cppCompilationInstruction.Defines.Concat(_toolChain.ToolChainDefines());
					if (!PlatformUtils.IsWindows())
					{
						cppCompilationInstruction.IncludePaths = cppCompilationInstruction.IncludePaths.Concat(_toolChain.ToolChainIncludePaths()).Concat(_toolChainContext.ExtraIncludeDirectories);
					}
				}
				_compilationData = new List<IntermediateObjectFileCompilationData>();
				array2 = array;
				foreach (CppCompilationInstruction cppCompilationInstruction2 in array2)
				{
					_compilationData.Add(BuildIntermediateObjectFileData(cppCompilationInstruction2));
				}
			}

			private static IEnumerable<string> SplitQuotedString(string input)
			{
				bool flag = false;
				List<string> list = new List<string>();
				StringBuilder stringBuilder = new StringBuilder();
				foreach (char c in input)
				{
					switch (c)
					{
					case '"':
						flag = !flag;
						stringBuilder.Append(c);
						continue;
					case ' ':
						if (!flag)
						{
							if (stringBuilder.Length > 0)
							{
								list.Add(stringBuilder.ToString());
								stringBuilder.Clear();
							}
							continue;
						}
						break;
					}
					stringBuilder.Append(c);
				}
				return list;
			}

			private static LinkerCommandData ProcessWindowsLinkArgs(string args)
			{
				IEnumerable<string> enumerable = SplitQuotedString(args);
				LinkerCommandData linkerCommandData = new LinkerCommandData();
				foreach (string item in enumerable)
				{
					if (!item.StartsWith("/out:") && !item.StartsWith("@") && !(item == "/DEBUG") && !(item == "/DEBUGTYPE"))
					{
						if (item.StartsWith("/LIBPATH:"))
						{
							linkerCommandData.AddPath(new NPath(item.Substring(10, item.Length - 11)));
						}
						else if (item.StartsWith("/"))
						{
							linkerCommandData.AddFlag(item);
						}
						else
						{
							linkerCommandData.AddLibrary(item);
						}
					}
				}
				return linkerCommandData;
			}

			private static LinkerCommandData ProcessMacUnixLinkArgs(string args)
			{
				IEnumerable<string> enumerable = SplitQuotedString(args);
				LinkerCommandData linkerCommandData = new LinkerCommandData();
				bool flag = false;
				StringBuilder stringBuilder = new StringBuilder();
				HashSet<string> hashSet = new HashSet<string> { "-arch", "-macosx_version_min", "-framework" };
				foreach (string item in enumerable)
				{
					if (flag)
					{
						flag = false;
						continue;
					}
					switch (item)
					{
					case "-o":
					case "-map":
					case "-isysroot":
						flag = true;
						continue;
					case "-Xlinker":
						continue;
					}
					if (hashSet.Contains(item))
					{
						stringBuilder.Append(item);
					}
					else if (stringBuilder.Length > 0)
					{
						string text = stringBuilder.ToString();
						stringBuilder.AppendFormat(" {0}", item);
						if (text == "-framework")
						{
							linkerCommandData.AddLibrary(stringBuilder.ToString().InQuotes());
						}
						else if (text == "-macosx_version_min")
						{
							linkerCommandData.AddVariable("CMAKE_OSX_DEPLOYMENT_TARGET", item);
						}
						else
						{
							linkerCommandData.AddFlag(stringBuilder.ToString());
						}
						if (text == "-arch")
						{
							linkerCommandData.AddVariable("CMAKE_OSX_ARCHITECTURES", item);
						}
						stringBuilder.Clear();
					}
					else if (item.StartsWith("-l"))
					{
						linkerCommandData.AddLibrary(item.Substring(2));
					}
					else if (item.StartsWith("-L"))
					{
						linkerCommandData.AddPath(new NPath(item.Substring(2)));
					}
					else if (item.StartsWith("-"))
					{
						linkerCommandData.AddFlag(item);
					}
					else
					{
						linkerCommandData.AddLibrary(item);
					}
				}
				return linkerCommandData;
			}

			private LinkerCommandData CreateLinkerCommandData()
			{
				List<NPath> list = new List<NPath>();
				list.Add(_desc.OutputFile.Parent.Combine("dummy.o"));
				CppProgramBuilder.LinkerInvocation linkerInvocation = _toolChain.MakeLinkerInvocation(list, _desc.OutputFile, _desc.GetStaticLibraries(_toolChain.BuildConfiguration), _desc.GetDynamicLibraries(_toolChain.BuildConfiguration), _desc.AdditionalLinkerFlags, _toolChainContext);
				LinkerCommandData linkerCommandData = ((!PlatformUtils.IsWindows()) ? ProcessMacUnixLinkArgs(linkerInvocation.ExecuteArgs.Arguments) : ProcessWindowsLinkArgs(linkerInvocation.ExecuteArgs.Arguments));
				foreach (string linkerFlag in linkerCommandData.LinkerFlags)
				{
					if (PlatformUtils.IsWindows())
					{
						if (linkerFlag == "/DLL")
						{
							linkerCommandData.OutputType = LinkerCommandOutputType.SharedLibrary;
							return linkerCommandData;
						}
					}
					else if (PlatformUtils.IsOSX() && linkerFlag == "-dylib")
					{
						linkerCommandData.OutputType = LinkerCommandOutputType.SharedLibrary;
						return linkerCommandData;
					}
				}
				return linkerCommandData;
			}

			private IntermediateObjectFileCompilationData BuildIntermediateObjectFileData(CppCompilationInstruction cppCompilationInstruction)
			{
				CompilationInvocation compilationInvocation = new CompilationInvocation
				{
					CompilerExecutable = _toolChain.CompilerExecutableFor(cppCompilationInstruction.SourceFile),
					SourceFile = cppCompilationInstruction.SourceFile,
					Arguments = _toolChain.CompilerFlagsFor(cppCompilationInstruction),
					EnvVars = _toolChain.EnvVars()
				};
				return new IntermediateObjectFileCompilationData
				{
					CppCompilationInstruction = cppCompilationInstruction,
					CompilationInvocation = compilationInvocation
				};
			}
		}

		private static List<BuildData> s_builds = new List<BuildData>();

		private readonly NPath _generatedCppDir;

		private readonly string _projectName;

		private readonly RuntimeBackend _runtimeBackend;

		private const string ExtraDefines = "GC_NOT_DLL";

		public static void AddBuild(ProgramBuildDescription desc, CppToolChain cppToolChain)
		{
			s_builds.Add(new BuildData(desc, cppToolChain));
		}

		public CppProgramCMakeGenerator(NPath generatedCppDir, string projectName, RuntimeBackend runtimeBackend)
		{
			_generatedCppDir = generatedCppDir;
			_projectName = projectName;
			_runtimeBackend = runtimeBackend;
		}

		private List<NPath> GetLibPaths()
		{
			HashSet<NPath> hashSet = new HashSet<NPath>();
			foreach (BuildData s_build in s_builds)
			{
				foreach (NPath libPath in s_build.LinkerCommand.LibPaths)
				{
					hashSet.Add(libPath);
				}
			}
			return new List<NPath>(hashSet);
		}

		private Dictionary<string, string> GetVariables()
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (BuildData s_build in s_builds)
			{
				foreach (KeyValuePair<string, string> variable in s_build.LinkerCommand.Variables)
				{
					if (!dictionary.ContainsKey(variable.Key))
					{
						dictionary.Add(variable.Key, variable.Value);
					}
				}
			}
			return dictionary;
		}

		private NPath ComputeRuntimeRootPath(IEnumerable<NPath> sourceFilesToCompile)
		{
			foreach (NPath item in sourceFilesToCompile)
			{
				if (!item.IsChildOf(_generatedCppDir))
				{
					NPath nPath = item.ParentContaining("libil2cpp");
					if (nPath != null)
					{
						return nPath;
					}
				}
			}
			return null;
		}

		private List<NPath> GenerateExecutable(StreamWriter cmakeListsWriter, BuildData build, ref NPath runtimeRootPath)
		{
			HashSet<NPath> hashSet = new HashSet<NPath>();
			foreach (IntermediateObjectFileCompilationData compilationDatum in build.CompilationData)
			{
				NPath sourceFile = compilationDatum.CompilationInvocation.SourceFile;
				bool flag = sourceFile.IsChildOf(_generatedCppDir);
				hashSet.Add(flag ? sourceFile.RelativeTo(_generatedCppDir) : sourceFile);
				NPath nPath = sourceFile.ChangeExtension(".h");
				if (nPath.Exists())
				{
					hashSet.Add(flag ? new NPath(nPath.FileName) : nPath);
				}
				string fileName = nPath.FileName;
				nPath = sourceFile.Parent.Combine(fileName);
				if (nPath.Exists())
				{
					hashSet.Add(nPath);
				}
			}
			if (runtimeRootPath == null)
			{
				runtimeRootPath = ComputeRuntimeRootPath(build.SourceFiles);
				if (runtimeRootPath != null)
				{
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/codegen/il2cpp-codegen.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/codegen/il2cpp-codegen-il2cpp.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/codegen/il2cpp-codegen-common-big.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/codegen/il2cpp-codegen-common-small.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-blob.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-api-functions.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-api-types.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-config.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-metadata.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-string-types.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-vm-support.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-runtime-metadata.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-normalization-tables.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-number-formatter.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-object-internals.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-tabledefs.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/il2cpp-class-internals.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/debugger/il2cpp-compat.h"));
					hashSet.Add(runtimeRootPath.Combine("libil2cpp/debugger/il2cpp-c-types.h"));
					hashSet.Add(runtimeRootPath.Combine("external/mono/mono/mini/debugger-agent.h"));
					hashSet.Add(runtimeRootPath.Combine("external/mono/mono/metadata/il2cpp-compat-metadata.h"));
					if (_runtimeBackend == RuntimeBackend.Tiny)
					{
						hashSet.Add(runtimeRootPath.Combine("libil2cpptiny/codegen/il2cpp-codegen.h"));
						hashSet.Add(runtimeRootPath.Combine("libil2cpptiny/il2cpp-class-internals.h"));
						hashSet.Add(runtimeRootPath.Combine("libil2cpptiny/il2cpp-object-internals.h"));
					}
				}
			}
			ReadOnlyCollection<NPath> readOnlyCollection = hashSet.ToSortedCollection();
			if (build.LinkerCommand.OutputType == LinkerCommandOutputType.Executable)
			{
				cmakeListsWriter.WriteLine("add_executable({0}", build.ExecName);
			}
			else
			{
				cmakeListsWriter.WriteLine("add_library({0} SHARED", build.ExecName);
			}
			foreach (NPath item in readOnlyCollection)
			{
				cmakeListsWriter.WriteLine("\t{0}", item.InQuotes(SlashMode.Forward));
			}
			cmakeListsWriter.WriteLine(")");
			cmakeListsWriter.WriteLine();
			return new List<NPath>(readOnlyCollection);
		}

		private static void RecursivelyAddSourceGroups(SourceGroupDir group, string[] elements, int index, NPath path)
		{
			if (index == elements.Length - 1)
			{
				group.AddFile(path);
			}
			else
			{
				RecursivelyAddSourceGroups(group.AddSubgroup(elements[index]), elements, index + 1, path);
			}
		}

		private SourceGroupDir ComputeSourceGroups(IEnumerable<NPath> sourceFilesToCompile, NPath runtimeRootPath)
		{
			NPath sourceDirectory = MonoInstall.BleedingEdge.SourceDirectory;
			SourceGroupDir sourceGroupDir = new SourceGroupDir("___root");
			foreach (NPath item in sourceFilesToCompile)
			{
				if (item.IsRelative)
				{
					RecursivelyAddSourceGroups(sourceGroupDir.AddSubgroup("generated"), item.Elements.ToArray(), 0, item);
				}
				else if (item.IsChildOf(runtimeRootPath))
				{
					NPath nPath = item.RelativeTo(runtimeRootPath);
					RecursivelyAddSourceGroups(sourceGroupDir, nPath.Elements.ToArray(), 0, item);
				}
				else if (item.IsChildOf(sourceDirectory))
				{
					NPath nPath = item.RelativeTo(sourceDirectory);
					RecursivelyAddSourceGroups(sourceGroupDir, nPath.Elements.ToArray(), 0, item);
				}
			}
			return sourceGroupDir;
		}

		private static void OutputSourceGroups(StreamWriter cmakeListsWriter, SourceGroupDir dir, string currentName)
		{
			bool flag = dir.Name == "___root";
			string text = (flag ? string.Empty : (currentName + dir.Name));
			if (dir.Files.Count > 0 && !flag)
			{
				cmakeListsWriter.WriteLine("source_group({0} FILES", text.InQuotes());
				foreach (NPath file in dir.Files)
				{
					cmakeListsWriter.WriteLine("\t{0}", file.InQuotes(SlashMode.Forward));
				}
				cmakeListsWriter.WriteLine(")");
				cmakeListsWriter.WriteLine();
			}
			if (!flag)
			{
				text += "\\\\";
			}
			foreach (SourceGroupDir subgroup in dir.Subgroups)
			{
				OutputSourceGroups(cmakeListsWriter, subgroup, text);
			}
		}

		private void GenerateDefineSets(StreamWriter cmakeListsWriter, BuildData build)
		{
			Dictionary<string, List<NPath>> dictionary = new Dictionary<string, List<NPath>>();
			foreach (IntermediateObjectFileCompilationData compilationDatum in build.CompilationData)
			{
				IEnumerable<string> arguments = compilationDatum.CompilationInvocation.Arguments;
				StringBuilder stringBuilder = new StringBuilder();
				foreach (string item in arguments)
				{
					if (item.StartsWith("/D") || item.StartsWith("-D"))
					{
						stringBuilder.Append(item.Substring(2));
						stringBuilder.Append(' ');
					}
				}
				string key = stringBuilder.ToString().Trim();
				NPath sourceFile = compilationDatum.CompilationInvocation.SourceFile;
				if (dictionary.TryGetValue(key, out var value))
				{
					value.Add(sourceFile);
					continue;
				}
				value = new List<NPath>();
				value.Add(sourceFile);
				dictionary.Add(key, value);
			}
			foreach (KeyValuePair<string, List<NPath>> item2 in dictionary)
			{
				cmakeListsWriter.WriteLine("set_property(SOURCE");
				foreach (NPath item3 in item2.Value)
				{
					if (item3.IsChildOf(_generatedCppDir))
					{
						cmakeListsWriter.WriteLine("\t{0}", item3.RelativeTo(_generatedCppDir).InQuotes(SlashMode.Forward));
					}
					else
					{
						cmakeListsWriter.WriteLine("\t{0}", item3.InQuotes(SlashMode.Forward));
					}
				}
				cmakeListsWriter.WriteLine("\tPROPERTY COMPILE_DEFINITIONS");
				cmakeListsWriter.WriteLine("\t{0}", item2.Key + " GC_NOT_DLL");
				cmakeListsWriter.WriteLine(")");
				cmakeListsWriter.WriteLine();
			}
		}

		private static string EscapeChars(string input)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (char c in input)
			{
				if (c == '"' || c == '\\')
				{
					stringBuilder.Append('\\');
				}
				stringBuilder.Append(c);
			}
			return stringBuilder.ToString();
		}

		private void GenerateCompileFlagSets(StreamWriter cmakeListsWriter, BuildData build)
		{
			Dictionary<string, List<NPath>> dictionary = new Dictionary<string, List<NPath>>();
			foreach (IntermediateObjectFileCompilationData compilationDatum in build.CompilationData)
			{
				string[] array = compilationDatum.CompilationInvocation.Arguments.ToArray();
				StringBuilder stringBuilder = new StringBuilder();
				NPath sourceFile = compilationDatum.CompilationInvocation.SourceFile;
				for (int i = 0; i < array.Length; i++)
				{
					string text = array[i];
					if ((!text.StartsWith("/") && !text.StartsWith("-")) || text.StartsWith("/D") || text.StartsWith("-D") || text == "-c")
					{
						continue;
					}
					if (text.StartsWith("/I"))
					{
						if (new NPath(text.Substring(3, text.Length - 4)) == _generatedCppDir)
						{
							text = "/I\"${CMAKE_SOURCE_DIR}\"";
						}
					}
					else if ((PlatformUtils.IsOSX() && text == "-arch") || text == "-include")
					{
						continue;
					}
					stringBuilder.Append(text);
					stringBuilder.Append(' ');
				}
				if (PlatformUtils.IsOSX())
				{
					stringBuilder.Append("${XCODE_DISABLE_EXTRA_WARNINGS}");
				}
				string key = stringBuilder.ToString().Trim();
				if (dictionary.TryGetValue(key, out var value))
				{
					value.Add(sourceFile);
					continue;
				}
				value = new List<NPath>();
				value.Add(sourceFile);
				dictionary.Add(key, value);
			}
			foreach (KeyValuePair<string, List<NPath>> item in dictionary)
			{
				if (string.IsNullOrEmpty(item.Key))
				{
					continue;
				}
				cmakeListsWriter.WriteLine("set_property(SOURCE");
				foreach (NPath item2 in item.Value)
				{
					if (item2.IsChildOf(_generatedCppDir))
					{
						cmakeListsWriter.WriteLine("\t{0}", item2.RelativeTo(_generatedCppDir).InQuotes(SlashMode.Forward));
					}
					else
					{
						cmakeListsWriter.WriteLine("\t{0}", item2.InQuotes(SlashMode.Forward));
					}
				}
				cmakeListsWriter.WriteLine("\tPROPERTY COMPILE_FLAGS");
				cmakeListsWriter.WriteLine("\t{0}", EscapeChars(item.Key).InQuotes());
				cmakeListsWriter.WriteLine(")");
				cmakeListsWriter.WriteLine();
			}
		}

		private static void GenerateLinkLibraries(StreamWriter cmakeListsWriter, BuildData build)
		{
			cmakeListsWriter.Write("target_link_libraries({0} ", build.ExecName);
			foreach (string library in build.LinkerCommand.Libraries)
			{
				NPath nPath = new NPath(library.Trim(new char[1] { '"' }));
				string arg = EscapeChars(nPath.ToString()).InQuotes();
				foreach (BuildData s_build in s_builds)
				{
					if (s_build.ExecName == nPath.FileNameWithoutExtension)
					{
						arg = s_build.ExecName;
						break;
					}
				}
				cmakeListsWriter.Write("{0} ", arg);
			}
			cmakeListsWriter.WriteLine(")");
			cmakeListsWriter.WriteLine();
		}

		private static IEnumerable<string> FilterLinkerFlags(IEnumerable<string> linkerFlags)
		{
			foreach (string linkerFlag in linkerFlags)
			{
				if (PlatformUtils.IsOSX())
				{
					string[] array = linkerFlag.Split(new char[1] { ' ' });
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.Append("-Wl");
					string[] array2 = array;
					foreach (string arg in array2)
					{
						stringBuilder.AppendFormat(",{0}", arg);
					}
					yield return stringBuilder.ToString();
				}
				else
				{
					yield return linkerFlag;
				}
			}
		}

		public void Generate()
		{
			if (s_builds.Count == 0)
			{
				throw new InvalidOperationException("No builds have been added to the CMake generator");
			}
			foreach (BuildData s_build in s_builds)
			{
				if (!s_build.CanBuildInCurrentEnvironment)
				{
					throw new UserMessageException(s_build.CannotBuildInCurrentEnvironmentErrorMessage);
				}
			}
			using (FileStream stream = new FileStream(_generatedCppDir.Combine("CMakeLists.txt").ToString(), FileMode.Create))
			{
				using (StreamWriter streamWriter = new StreamWriter(stream))
				{
					streamWriter.WriteLine("cmake_minimum_required(VERSION 3.11)");
					streamWriter.WriteLine("cmake_policy(VERSION 3.11)");
					streamWriter.WriteLine();
					streamWriter.WriteLine("project({0})", _projectName);
					streamWriter.WriteLine();
					streamWriter.WriteLine("set(CMAKE_CXX_FLAGS \"\")");
					streamWriter.WriteLine("set(CMAKE_CXX_FLAGS_DEBUG \"\")");
					streamWriter.WriteLine("set(CMAKE_C_FLAGS \"\")");
					streamWriter.WriteLine("set(CMAKE_C_FLAGS_DEBUG \"\")");
					streamWriter.WriteLine();
					if (PlatformUtils.IsWindows())
					{
						streamWriter.WriteLine("set(CMAKE_C_FLAGS \"${CMAKE_C_FLAGS} /MP\")");
						streamWriter.WriteLine("set(CMAKE_CXX_FLAGS \"${CMAKE_CXX_FLAGS} /MP\")");
						streamWriter.WriteLine();
					}
					List<NPath> libPaths = GetLibPaths();
					if (libPaths.Count > 0)
					{
						streamWriter.WriteLine("link_directories(");
						foreach (NPath item in libPaths)
						{
							streamWriter.WriteLine("\t{0}", item.InQuotes(SlashMode.Forward));
						}
						streamWriter.WriteLine(")");
						streamWriter.WriteLine();
					}
					Dictionary<string, string> variables = GetVariables();
					if (variables.Count > 0)
					{
						foreach (KeyValuePair<string, string> item2 in variables)
						{
							streamWriter.WriteLine("set({0} {1})", item2.Key, item2.Value.InQuotes());
						}
						streamWriter.WriteLine();
					}
					if (PlatformUtils.IsOSX())
					{
						streamWriter.WriteLine("set(XCODE_DISABLE_EXTRA_WARNINGS \"-Wno-reorder -Wno-unused-function -Wno-unused-variable -Wno-delete-non-virtual-dtor -Wno-missing-braces -Wno-unused-private-field -Wno-unused-label -Wno-integer-overflow -Wno-infinite-recursion -Wno-unused-command-line-argument\")");
						streamWriter.WriteLine();
					}
					NPath runtimeRootPath = null;
					foreach (BuildData s_build2 in s_builds)
					{
						List<NPath> sourceFilesToCompile = GenerateExecutable(streamWriter, s_build2, ref runtimeRootPath);
						SourceGroupDir dir = ComputeSourceGroups(sourceFilesToCompile, runtimeRootPath);
						OutputSourceGroups(streamWriter, dir, string.Empty);
						streamWriter.WriteLine();
						GenerateDefineSets(streamWriter, s_build2);
						GenerateCompileFlagSets(streamWriter, s_build2);
						GenerateLinkLibraries(streamWriter, s_build2);
						streamWriter.WriteLine("set_property(TARGET {0} PROPERTY LINK_FLAGS \"{1}\")", s_build2.ExecName, EscapeChars(FilterLinkerFlags(s_build2.LinkerCommand.LinkerFlags).AggregateWithSpace()));
						if (s_build2.LinkerCommand.OutputType == LinkerCommandOutputType.Executable)
						{
							streamWriter.WriteLine();
							streamWriter.WriteLine("add_custom_command(TARGET {0} PRE_BUILD COMMAND ${{CMAKE_COMMAND}} -E copy_directory ${{CMAKE_SOURCE_DIR}}/Data $<TARGET_FILE_DIR:{0}>/Data)", s_build2.ExecName);
							if (s_build2.CopyFilesFromSeparateBuildOutputDirectory)
							{
								streamWriter.WriteLine("file(GLOB PINVOKE_TARGETS \"${CMAKE_SOURCE_DIR}/../build/*il2cpp-pinvoke-test-target.*\")");
								streamWriter.WriteLine("foreach (PINVOKE_TARGET IN LISTS PINVOKE_TARGETS)");
								streamWriter.WriteLine("    add_custom_command(TARGET {0} PRE_BUILD COMMAND ${{CMAKE_COMMAND}} -E copy ${{PINVOKE_TARGET}} $<TARGET_FILE_DIR:{0}>)", s_build2.ExecName);
								streamWriter.WriteLine("endforeach(PINVOKE_TARGET)");
							}
						}
					}
				}
			}
		}
	}
}
