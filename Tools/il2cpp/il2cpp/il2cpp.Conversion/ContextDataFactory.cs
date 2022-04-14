using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using NiceIO;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Common.Profiles;
using Unity.IL2CPP.Options;
using Unity.Options;

namespace il2cpp.Conversion
{
	internal static class ContextDataFactory
	{
		public static AssemblyConversionInputData CreateConversionDataFromOptions()
		{
			return new AssemblyConversionInputData(IL2CPPOptions.Generatedcppdir, IL2CPPOptions.DataFolder, IL2CPPOptions.SymbolsFolder, IL2CPPOptions.DataFolder.Combine("Metadata"), IL2CPPOptions.ExecutableAssembliesFolderOnDevice, IL2CPPOptions.EntryAssemblyName, IL2CPPOptions.ExtraTypesFile, CodeGenOptions.Dotnetprofile, CombineAssembliesFrom(IL2CPPOptions.Directory, IL2CPPOptions.Assembly).AsReadOnly(), IL2CPPOptions.SearchDir.AsReadOnly(), CodeGenOptions.MaximumRecursiveGenericDepth, CodeGenOptions.AssemblyMethod, CodeGenOptions.IncrementalGCTimeSlice, (!CodeGenOptions.CodeGenerationOption.HasFlag(CodeGenerationOptions.EnableSerial)) ? ReadingMode.Immediate : ReadingMode.Deferred, IL2CPPOptions.Jobs, IL2CPPOptions.DebugAssemblyName);
		}

		public static AssemblyConversionParameters CreateConversionParametersFromOptions()
		{
			return new AssemblyConversionParameters(CreateCodeGenerationOptionsFromOptions(), CreateFileGenerationOptionsFromOptions(), CreateGenericsOptionsFromOptions(), CodeGenOptions.ProfilerOptions, GetRuntimeBackendFromOptions(), CodeGenOptions.Dotnetprofile, CreateDiagnosticOptionsFromOptions(), CreateFeaturesFromOptions(), CreateTestingOptionsFromOptions());
		}

		public static AssemblyConversionInputDataForTopLevelAccess CreateTopLevelDataFromOptions()
		{
			return new AssemblyConversionInputDataForTopLevelAccess(CodeGenOptions.ConversionMode, MasterToSlaveArgumentPassThrough.GetPassThroughArguments, CodeGenOptions.SlaveAssembly, CodeGenOptions.SlaveEnableAttachMessage, OptionsFormatter.FormatWithValue<CodeGenOptions>);
		}

		private static CodeGenerationOptions CreateCodeGenerationOptionsFromOptions()
		{
			CodeGenerationOptions codeGenerationOptions = CodeGenOptions.CodeGenerationOption;
			if (CodeGenOptions.EnableStacktrace)
			{
				codeGenerationOptions |= CodeGenerationOptions.EnableStacktrace;
			}
			if (CodeGenOptions.EnableArrayBoundsCheck)
			{
				codeGenerationOptions |= CodeGenerationOptions.EnableArrayBoundsCheck;
			}
			if (CodeGenOptions.EnableDivideByZeroCheck)
			{
				codeGenerationOptions |= CodeGenerationOptions.EnableDivideByZeroCheck;
			}
			if (CodeGenOptions.EmitNullChecks)
			{
				codeGenerationOptions |= CodeGenerationOptions.EnableNullChecks;
			}
			if (CodeGenOptions.Dotnetprofile != Profile.UnityTiny || !CodeGenOptions.UseTinyRuntimeBackend)
			{
				codeGenerationOptions |= CodeGenerationOptions.EnableLazyStaticConstructors;
			}
			if (CodeGenOptions.EmitComments)
			{
				codeGenerationOptions |= CodeGenerationOptions.EnableComments;
			}
			return codeGenerationOptions;
		}

		private static FileGenerationOptions CreateFileGenerationOptionsFromOptions()
		{
			FileGenerationOptions fileGenerationOptions = CodeGenOptions.FileGenerationOption;
			if (CodeGenOptions.EmitSourceMapping)
			{
				fileGenerationOptions |= FileGenerationOptions.EmitSourceMapping;
			}
			if (CodeGenOptions.EmitMethodMap)
			{
				fileGenerationOptions |= FileGenerationOptions.EmitMethodMap;
			}
			return fileGenerationOptions;
		}

		private static GenericsOptions CreateGenericsOptionsFromOptions()
		{
			GenericsOptions genericsOptions = CodeGenOptions.GenericsOption;
			if (!CodeGenOptions.DisableGenericSharing)
			{
				genericsOptions |= GenericsOptions.EnableSharing;
			}
			if (CodeGenOptions.EnablePrimitiveValueTypeGenericSharing)
			{
				genericsOptions |= GenericsOptions.EnablePrimitiveValueTypeGenericSharing;
			}
			if (CodeGenOptions.EnablePrimitiveValueTypeGenericSharing && CodeGenOptions.Dotnetprofile == Profile.UnityAot)
			{
				genericsOptions |= GenericsOptions.EnableEnumTypeSharing;
			}
			return genericsOptions;
		}

		internal static DiagnosticOptions CreateDiagnosticOptionsFromOptions()
		{
			DiagnosticOptions diagnosticOptions = CodeGenOptions.DiagnosticOption;
			if (CodeGenOptions.EnableStats)
			{
				diagnosticOptions |= DiagnosticOptions.EnableStats;
			}
			if (CodeGenOptions.NeverAttachDialog)
			{
				diagnosticOptions |= DiagnosticOptions.NeverAttachDialog;
			}
			if (CodeGenOptions.EmitAttachDialog)
			{
				diagnosticOptions |= DiagnosticOptions.EmitAttachDialog;
			}
			if (CodeGenOptions.EnableTinyDebugging)
			{
				diagnosticOptions |= DiagnosticOptions.EnableTinyDebugging;
			}
			if (CodeGenOptions.DebuggerOff)
			{
				diagnosticOptions |= DiagnosticOptions.DebuggerOff;
			}
			if (CodeGenOptions.EmitReversePInvokeWrapperDebuggingHelpers)
			{
				diagnosticOptions |= DiagnosticOptions.EmitReversePInvokeWrapperDebuggingHelpers;
			}
			return diagnosticOptions;
		}

		internal static Features CreateFeaturesFromOptions()
		{
			Features features = CodeGenOptions.Feature;
			if (CodeGenOptions.EnableReload)
			{
				features |= Features.EnableReload;
			}
			if (CodeGenOptions.EnableDebugger)
			{
				features |= Features.EnableDebugger;
			}
			if (CodeGenOptions.CodeConversionCache)
			{
				features |= Features.EnableCodeConversionCache;
			}
			if (CodeGenOptions.EnableDeepProfiler)
			{
				features |= Features.EnableDeepProfiler;
			}
			return features;
		}

		internal static TestingOptions CreateTestingOptionsFromOptions()
		{
			TestingOptions testingOptions = CodeGenOptions.TestingOption;
			if (CodeGenOptions.EnableErrorMessageTest)
			{
				testingOptions |= TestingOptions.EnableErrorMessageTest;
			}
			if (CodeGenOptions.GoogleBenchmark)
			{
				testingOptions |= TestingOptions.EnableGoogleBenchmark;
			}
			return testingOptions;
		}

		internal static RuntimeBackend GetRuntimeBackendFromOptions()
		{
			if (CodeGenOptions.UseTinyRuntimeBackend)
			{
				return RuntimeBackend.Tiny;
			}
			return RuntimeBackend.Big;
		}

		private static List<NPath> CombineAssembliesFrom(IEnumerable<NPath> assemblyDirectories, IEnumerable<NPath> explicitAssemblies)
		{
			List<NPath> list = new List<NPath>();
			if (assemblyDirectories != null)
			{
				list.AddRange(assemblyDirectories.SelectMany((NPath directory) => from f in directory.Files()
					where f.HasExtension("dll", "exe")
					select f));
			}
			if (explicitAssemblies != null)
			{
				list.AddRange(explicitAssemblies);
			}
			return list;
		}
	}
}
