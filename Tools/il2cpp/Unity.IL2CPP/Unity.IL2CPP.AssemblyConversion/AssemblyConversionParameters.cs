using Unity.IL2CPP.Common;
using Unity.IL2CPP.Common.Profiles;
using Unity.IL2CPP.Options;

namespace Unity.IL2CPP.AssemblyConversion
{
	public class AssemblyConversionParameters
	{
		public readonly bool UsingTinyClassLibraries;

		public readonly bool UsingTinyBackend;

		public readonly bool EmitSourceMapping;

		public readonly bool EmitMethodMap;

		public readonly bool NoLazyStaticConstructors;

		public readonly bool EmitNullChecks;

		public readonly bool EnableStacktrace;

		public readonly bool EnableArrayBoundsCheck;

		public readonly bool EnableDivideByZeroCheck;

		public readonly bool EmitComments;

		public readonly bool EnableSerialConversion;

		public readonly bool ReturnAsByRefParameter;

		public readonly bool CanShareEnumTypes;

		public readonly bool EnablePrimitiveValueTypeGenericSharing;

		public readonly bool DisableGenericSharing;

		public readonly ProfilerOptions ProfilerOptions;

		public readonly RuntimeBackend Backend;

		public readonly CodeGenerationOptions CodeGenerationOptions;

		public readonly DiagnosticOptions DiagnosticOptions;

		public readonly bool EnableStats;

		public readonly bool NeverAttachDialog;

		public readonly bool EmitAttachDialog;

		public readonly bool EnableTinyDebugging;

		public readonly bool DebuggerOff;

		public readonly bool EmitReversePInvokeWrapperDebuggingHelpers;

		public readonly bool EnableReload;

		public readonly bool CodeConversionCache;

		public readonly bool EnableDebugger;

		public readonly bool EnableDeepProfiler;

		public readonly bool EnableErrorMessageTest;

		public readonly bool GoogleBenchmark;

		public readonly bool EnableInlining;

		public AssemblyConversionParameters(CodeGenerationOptions codeGenerationOptions, FileGenerationOptions fileGenerationOptions, GenericsOptions genericsOptions, ProfilerOptions profilerOptions, RuntimeBackend runtimeBackend, RuntimeProfile profile, DiagnosticOptions diagnosticOptions, Features features, TestingOptions testingOptions)
		{
			CodeGenerationOptions = codeGenerationOptions;
			DiagnosticOptions = diagnosticOptions;
			Backend = runtimeBackend;
			ProfilerOptions = profilerOptions;
			UsingTinyClassLibraries = profile == Profile.UnityTiny;
			UsingTinyBackend = runtimeBackend == RuntimeBackend.Tiny;
			EmitSourceMapping = fileGenerationOptions.HasFlag(FileGenerationOptions.EmitSourceMapping);
			EmitMethodMap = fileGenerationOptions.HasFlag(FileGenerationOptions.EmitMethodMap);
			NoLazyStaticConstructors = !codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableLazyStaticConstructors);
			EmitNullChecks = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableNullChecks);
			EnableStacktrace = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableStacktrace);
			EnableArrayBoundsCheck = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableArrayBoundsCheck);
			EnableDivideByZeroCheck = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableDivideByZeroCheck);
			EmitComments = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableComments);
			EnableSerialConversion = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableSerial);
			ReturnAsByRefParameter = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableReturnAsByRefParameter);
			EnableInlining = codeGenerationOptions.HasFlag(CodeGenerationOptions.EnableInlining);
			CanShareEnumTypes = genericsOptions.HasFlag(GenericsOptions.EnableEnumTypeSharing);
			EnablePrimitiveValueTypeGenericSharing = genericsOptions.HasFlag(GenericsOptions.EnablePrimitiveValueTypeGenericSharing);
			DisableGenericSharing = !genericsOptions.HasFlag(GenericsOptions.EnableSharing);
			EnableStats = diagnosticOptions.HasFlag(DiagnosticOptions.EnableStats);
			NeverAttachDialog = diagnosticOptions.HasFlag(DiagnosticOptions.NeverAttachDialog);
			EmitAttachDialog = diagnosticOptions.HasFlag(DiagnosticOptions.EmitAttachDialog);
			EnableTinyDebugging = diagnosticOptions.HasFlag(DiagnosticOptions.EnableTinyDebugging);
			DebuggerOff = diagnosticOptions.HasFlag(DiagnosticOptions.DebuggerOff);
			EmitReversePInvokeWrapperDebuggingHelpers = diagnosticOptions.HasFlag(DiagnosticOptions.EmitReversePInvokeWrapperDebuggingHelpers);
			EnableReload = features.HasFlag(Features.EnableReload);
			EnableDebugger = features.HasFlag(Features.EnableDebugger);
			CodeConversionCache = features.HasFlag(Features.EnableCodeConversionCache);
			EnableDeepProfiler = features.HasFlag(Features.EnableDeepProfiler);
			EnableErrorMessageTest = testingOptions.HasFlag(TestingOptions.EnableErrorMessageTest);
			GoogleBenchmark = testingOptions.HasFlag(TestingOptions.EnableGoogleBenchmark);
		}
	}
}
