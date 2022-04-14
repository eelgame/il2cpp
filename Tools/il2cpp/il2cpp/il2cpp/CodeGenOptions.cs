using System;
using System.Runtime.InteropServices;
using NiceIO;
using Unity.IL2CPP.Common.Profiles;
using Unity.IL2CPP.Options;
using Unity.Options;

namespace il2cpp
{
	[ProgramOptions]
	public class CodeGenOptions
	{
		public static bool UseTinyRuntimeBackend = false;

		[HideFromHelp]
		public static RuntimeProfile Dotnetprofile = Profile.UnityAot;

		[HelpDetails("Enables generation of null checks", null)]
		public static bool EmitNullChecks = false;

		[HelpDetails("Enables generation of stacktrace sentries in C++ code at the start of every managed method. This enables support for stacktraces for platforms that do not have system APIs to walk the stack (for example, one such platform is WebGL)", null)]
		public static bool EnableStacktrace = false;

		[HideFromHelp]
		public static bool EnableDeepProfiler = false;

		[HelpDetails("Enables conversion statistics", null)]
		public static bool EnableStats = false;

		[HelpDetails("Enables generation of array bounds checks", null)]
		public static bool EnableArrayBoundsCheck = false;

		[HelpDetails("Enables generation of divide by zero checks", null)]
		public static bool EnableDivideByZeroCheck = false;

		[HideFromHelp]
		public static bool WriteBarrierValidation = false;

		[HelpDetails("Enable incremental GC if n > 0, with a maximimum time slice of n ms.", null)]
		public static int IncrementalGCTimeSlice = 0;

		[HideFromHelp]
		public static bool EnableErrorMessageTest = false;

		[HideFromHelp]
		public static bool EnablePrimitiveValueTypeGenericSharing = true;

		[HideFromHelp]
		public static ProfilerOptions ProfilerOptions = ProfilerOptions.MethodEnterExit;

		[HideFromHelp]
		public static bool EmitSourceMapping = false;

		[HideFromHelp]
		public static bool EmitMethodMap = false;

		[HideFromHelp]
		public static bool NeverAttachDialog;

		[HideFromHelp]
		public static bool EmitAttachDialog;

		[HideFromHelp]
		public static bool CodeConversionCache;

		[HideFromHelp]
		public static bool EnableDebugger;

		[HideFromHelp]
		public static bool DebuggerOff;

		[HelpDetails("String to match the name of method(s) to show the assembly output for", null)]
		public static string AssemblyMethod;

		[HelpDetails("Disables generic sharing", null)]
		public static bool DisableGenericSharing = false;

		[HelpDetails("Adds features for helping to debug dots generated code, such as type IDs", null)]
		public static bool EnableTinyDebugging;

		public static bool EmitReversePInvokeWrapperDebuggingHelpers = false;

		[HideFromHelp]
		public static bool GoogleBenchmark = false;

		[HelpDetails("Set the maximum depth to implement recursive generic methods. The default value is 7.", null)]
		public static int MaximumRecursiveGenericDepth = 7;

		[HideFromHelp]
		public static ConversionMode ConversionMode;

		[HideFromHelp]
		public static NPath SlaveAssembly;

		[HideFromHelp]
		public static string SlaveEnableAttachMessage;

		[HelpDetails("Enable code to allow the runtime to be shutdown and reloaded (this has code size and runtime performance impact).", null)]
		public static bool EnableReload;

		[HelpDetails("Specify an option related to code generation", null)]
		public static CodeGenerationOptions CodeGenerationOption;

		[HelpDetails("Specify an option related to file output", null)]
		public static FileGenerationOptions FileGenerationOption;

		[HelpDetails("Specify an option related to generics", null)]
		public static GenericsOptions GenericsOption;

		[HelpDetails("Enable a feature of il2cpp", null)]
		public static Features Feature;

		[HelpDetails("Enable a diagnostic ability", null)]
		public static DiagnosticOptions DiagnosticOption;

		[HideFromHelp]
		public static TestingOptions TestingOption;

		public static bool EmitComments => true;

		public static void SetToDefaults()
		{
			UseTinyRuntimeBackend = false;
			EmitNullChecks = false;
			EnableStacktrace = false;
			EnableDeepProfiler = false;
			EnableStats = false;
			EnableArrayBoundsCheck = false;
			EnableErrorMessageTest = false;
			EnableDivideByZeroCheck = false;
			EnablePrimitiveValueTypeGenericSharing = true;
			Dotnetprofile = Profile.UnityAot;
			ProfilerOptions = ProfilerOptions.MethodEnterExit;
			EmitSourceMapping = false;
			EmitMethodMap = false;
			NeverAttachDialog = false;
			EnableDebugger = false;
			DebuggerOff = false;
			EmitAttachDialog = false;
			CodeConversionCache = false;
			AssemblyMethod = null;
			WriteBarrierValidation = false;
			IncrementalGCTimeSlice = 0;
			DisableGenericSharing = false;
			EnableTinyDebugging = false;
			GoogleBenchmark = false;
			MaximumRecursiveGenericDepth = 7;
			ConversionMode = ConversionMode.Classic;
			SlaveAssembly = null;
			SlaveEnableAttachMessage = null;
			EnableReload = false;
			CodeGenerationOption = CodeGenerationOptions.None;
			FileGenerationOption = FileGenerationOptions.None;
			GenericsOption = GenericsOptions.None;
			Feature = Features.None;
			DiagnosticOption = DiagnosticOptions.None;
			TestingOption = TestingOptions.None;
		}

		public static void Initialize()
		{
			Validate();
			SetOtherOptionsBasedOnOptions();
		}

		private static void SetOtherOptionsBasedOnOptions()
		{
			if (Dotnetprofile == Profile.UnityTiny)
			{
				DisableGenericSharing = true;
				UseTinyRuntimeBackend = !EnableDebugger;
				EmitReversePInvokeWrapperDebuggingHelpers = IL2CPPOptions.DevelopmentMode;
			}
			if (ConversionMode != ConversionMode.FullPerAssemblyInProcess && ConversionMode != ConversionMode.PartialPerAssemblyInProcess)
			{
				CodeGenerationOption |= CodeGenerationOptions.EnableInlining;
			}
		}

		private static void Validate()
		{
			if (!string.IsNullOrEmpty(SlaveEnableAttachMessage))
			{
				MessageBoxW(IntPtr.Zero, SlaveEnableAttachMessage, "il2cpp " + SlaveEnableAttachMessage, 0);
			}
		}

		[DllImport("user32.dll")]
		private static extern int MessageBoxW(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string message, [MarshalAs(UnmanagedType.LPWStr)] string title, int options);
	}
}
