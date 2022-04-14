using System.Collections.Generic;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Collections.Generic;
using NiceIO;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Metadata;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	internal class DriverWriter
	{
		private readonly AssemblyDefinition _executable;

		private readonly MethodDefinition _entryPoint;

		private readonly SourceWritingContext _context;

		public DriverWriter(SourceWritingContext context, AssemblyDefinition executable)
		{
			_context = context;
			_executable = executable;
			_entryPoint = _executable.EntryPoint;
		}

		public void Write(NPath assemblyDirectory)
		{
			using (IGeneratedMethodCodeWriter writer = _context.CreateProfiledManagedSourceWriterInOutputDirectory("driver.cpp"))
			{
				WriteIncludes(writer);
				WriteMainInvoker(writer);
				WriteEntryPoint(writer, assemblyDirectory);
				WritePlatformSpecificEntryPoints(writer);
				MethodWriter.WriteInlineMethodDefinitions(_context, "driver", writer);
			}
		}

		private void WriteIncludes(IGeneratedCodeWriter writer)
		{
			writer.WriteLine("#include \"il2cpp-api.h\"");
			writer.WriteLine("#include \"utils/Exception.h\"");
			writer.WriteLine("#include \"utils/StringUtils.h\"");
			writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_DESKTOP");
			writer.WriteLine("#include \"Windows.h\"");
			writer.WriteLine("#include \"Shellapi.h\"");
			writer.WriteLine("#elif IL2CPP_TARGET_WINDOWS_GAMES");
			writer.WriteLine("#include \"Windows.h\"");
			writer.WriteLine("#endif");
			writer.WriteLine();
			writer.WriteLine("#if IL2CPP_TARGET_LUMIN");
			writer.WriteLine("#include \"ml_lifecycle.h\"");
			writer.WriteLine("#endif");
			writer.WriteLine("extern \"C\" char * platform_config_path();");
			writer.WriteLine("extern \"C\" char * platform_data_path();");
			writer.WriteLine();
			if (_context.Global.Parameters.GoogleBenchmark)
			{
				writer.AddInclude("il2cpp-benchmark-support.h");
			}
		}

		private void WriteMainInvoker(IGeneratedMethodCodeWriter writer)
		{
			if (_context.Global.Parameters.UsingTinyBackend)
			{
				writer.WriteMethodWithMetadataInitialization("int MainInvoker(int argc, const Il2CppNativeChar* const* argv)", "MainInvoker", WriteMainInvocation, "MainInvoker", null);
			}
			else
			{
				writer.WriteMethodWithMetadataInitialization("int MainInvoker(int argc, const Il2CppNativeChar* const* argv)", "MainInvoker", WriteMainInvokerBody, "MainInvoker", null);
			}
			writer.WriteLine();
		}

		private void WriteMainInvokerBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			writer.WriteLine("try");
			using (new BlockWriter(writer))
			{
				WriteMainInvocation(writer, metadataAccess);
			}
			writer.WriteLine("catch (const Il2CppExceptionWrapper& e)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("il2cpp_codegen_write_to_stderr(\"Unhandled Exception: \");");
				writer.WriteLine("auto method = il2cpp_class_get_method_from_name(il2cpp_object_get_class(e.ex), \"ToString\", 0);");
				writer.WriteLine("auto exceptionString = (Il2CppString*)il2cpp_runtime_invoke(method, e.ex, NULL, NULL);");
				writer.WriteLine("il2cpp_codegen_write_to_stderr(il2cpp::utils::StringUtils::Utf16ToUtf8(exceptionString->chars).c_str());");
				writer.WriteLine("#if IL2CPP_TARGET_IOS");
				writer.WriteLine("return 0;");
				writer.WriteLine("#else");
				writer.WriteLine("il2cpp_codegen_abort();");
				writer.WriteLine("il2cpp_codegen_no_return();");
				writer.WriteLine("#endif");
			}
		}

		private void WriteMainInvocation(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess)
		{
			if (!ValidateMainMethod(writer))
			{
				return;
			}
			List<string> list = new List<string>();
			if (_entryPoint.Parameters.Count > 0 && !_context.Global.Parameters.GoogleBenchmark)
			{
				ArrayType arrayType = (ArrayType)_entryPoint.Parameters[0].ParameterType;
				writer.AddIncludeForTypeDefinition(arrayType);
				writer.WriteLine("{0} args = {1};", writer.Context.Global.Services.Naming.ForVariable(arrayType), Emit.NewSZArray(writer.Context, arrayType, arrayType.ElementType, "argc - 1", metadataAccess));
				writer.WriteLine();
				writer.WriteLine("for (int i = 1; i < argc; i++)");
				using (new BlockWriter(writer))
				{
					writer.WriteLine("DECLARE_NATIVE_C_STRING_AS_STRING_VIEW_OF_IL2CPP_CHARS(argumentUtf16, argv[i]);");
					writer.WriteLine("{0} argument = il2cpp_codegen_string_new_utf16(argumentUtf16);", writer.Context.Global.Services.Naming.ForVariable(_context.Global.Services.TypeProvider.SystemString));
					writer.WriteStatement(Emit.StoreArrayElement("args", "i - 1", "argument", useArrayBoundsCheck: false));
				}
				writer.WriteLine();
				list.Add("args");
			}
			string text = "";
			if (_entryPoint.ReturnType.MetadataType == MetadataType.Int32)
			{
				text = "il2cppRetVal";
				writer.WriteLine("int32_t " + text + " = 0;");
			}
			else if (_entryPoint.ReturnType.MetadataType == MetadataType.UInt32)
			{
				text = "il2cppRetVal";
				writer.WriteLine("uint32_t " + text + " = 0;");
			}
			if (!_context.Global.Parameters.NoLazyStaticConstructors && (_entryPoint.DeclaringType.Attributes & TypeAttributes.BeforeFieldInit) == 0)
			{
				writer.WriteLine("IL2CPP_RUNTIME_CLASS_INIT({0});", metadataAccess.TypeInfoFor(_entryPoint.DeclaringType));
			}
			TypeResolver typeResolverForMethodToCall = TypeResolver.For(_entryPoint.DeclaringType, _entryPoint);
			MethodBodyWriter.WriteMethodCallExpression(text, () => metadataAccess.HiddenMethodInfo(_entryPoint), writer, null, _entryPoint, _entryPoint, typeResolverForMethodToCall, MethodCallType.Normal, metadataAccess, new VTableBuilder(), list, useArrayBoundsCheck: false);
			switch (_entryPoint.ReturnType.MetadataType)
			{
			case MetadataType.Void:
				writer.WriteLine("return 0;");
				break;
			case MetadataType.Int32:
				writer.WriteStatement("return il2cppRetVal");
				break;
			case MetadataType.UInt32:
				writer.WriteLine("return static_cast<int>(il2cppRetVal);");
				break;
			}
		}

		private bool ValidateMainMethod(IGeneratedMethodCodeWriter writer)
		{
			if (_entryPoint == null)
			{
				string arg = $"Entry point not found in assembly '{_executable.FullName}'.";
				writer.WriteStatement(Emit.RaiseManagedException($"il2cpp_codegen_get_missing_method_exception(\"{arg}\")"));
				return false;
			}
			if (_entryPoint.HasThis)
			{
				string arg2 = "Entry point must be static.";
				writer.WriteStatement(Emit.RaiseManagedException($"il2cpp_codegen_get_missing_method_exception(\"{arg2}\")"));
				return false;
			}
			TypeReference returnType = _entryPoint.ReturnType;
			if (returnType.MetadataType != MetadataType.Void && returnType.MetadataType != MetadataType.Int32 && returnType.MetadataType != MetadataType.UInt32)
			{
				string arg3 = "Entry point must have a return type of void, integer, or unsigned integer.";
				writer.WriteStatement(Emit.RaiseManagedException($"il2cpp_codegen_get_missing_method_exception(\"{arg3}\")"));
				return false;
			}
			Collection<ParameterDefinition> parameters = _entryPoint.Parameters;
			bool flag = parameters.Count < 2 && !_entryPoint.HasGenericParameters;
			if (flag && parameters.Count == 1)
			{
				if (!(parameters[0].ParameterType is ArrayType arrayType) || !arrayType.IsVector)
				{
					flag = false;
				}
				else if (arrayType.ElementType.MetadataType != MetadataType.String)
				{
					flag = false;
				}
			}
			if (!flag)
			{
				string arg4 = $"Entry point method for type '{_entryPoint.DeclaringType.FullName}' has invalid signature.";
				writer.WriteStatement(string.Format(Emit.RaiseManagedException($"il2cpp_codegen_get_missing_method_exception(\"{arg4}\")")));
				return false;
			}
			if (_entryPoint.DeclaringType.HasGenericParameters)
			{
				string arg5 = $"Entry point method is defined on a generic type '{_entryPoint.DeclaringType.FullName}'.";
				writer.WriteStatement(string.Format(Emit.RaiseManagedException($"il2cpp_codegen_get_missing_method_exception(\"{arg5}\")")));
				return false;
			}
			return true;
		}

		private void WriteEntryPoint(IGeneratedMethodCodeWriter writer, NPath assemblyDirectory)
		{
			writer.WriteLine("int EntryPoint(int argc, const Il2CppNativeChar* const* argv)");
			using (new BlockWriter(writer))
			{
				WriteWindowsMessageBoxHook(writer, _executable.Name.Name);
				WriteSetDebuggerOptions(writer);
				WriteSetCommandLineArgumentsAndInitIl2Cpp(writer);
				WriteSetConfiguration(writer);
				writer.WriteLine();
				writer.Dedent();
				writer.WriteLine("#if IL2CPP_TARGET_LUMIN");
				writer.Indent();
				writer.WriteLine("MLLifecycleSetReadyIndication();");
				writer.Dedent();
				writer.WriteLine("#endif");
				writer.WriteLine();
				writer.Indent();
				if (_context.Global.Parameters.GoogleBenchmark)
				{
					writer.WriteLine("il2cpp_benchmark_initialize(argc, argv);");
				}
				writer.WriteLine("int exitCode = MainInvoker(argc, argv);");
				writer.WriteLine();
				if (!writer.Context.Global.Parameters.UsingTinyClassLibraries)
				{
					writer.WriteLine("il2cpp_shutdown();");
				}
				else if (_context.Global.Parameters.UsingTinyClassLibraries && _context.Global.Parameters.EnableDebugger)
				{
					writer.WriteLine("#if IL2CPP_DEBUGGER_TESTS");
					writer.WriteLine("il2cpp_shutdown();");
					writer.WriteLine("#endif");
				}
				writer.WriteLine("return exitCode;");
			}
			writer.WriteLine();
		}

		private void WriteSetDebuggerOptions(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine("#if IL2CPP_MONO_DEBUGGER");
			writer.WriteLine("#define DEBUGGER_STRINGIFY(x) #x");
			writer.WriteLine("#define DEBUGGER_STRINGIFY2(x) DEBUGGER_STRINGIFY(x)");
			writer.WriteLine("#ifdef IL2CPP_MONO_DEBUGGER_LOGFILE");
			writer.WriteLine("#if IL2CPP_TARGET_JAVASCRIPT || IL2CPP_TARGET_IOS");
			writer.WriteLine("il2cpp_debugger_set_agent_options(\"--debugger-agent=transport=dt_socket,address=0.0.0.0:\" DEBUGGER_STRINGIFY2(IL2CPP_DEBUGGER_PORT) \",server=y,suspend=n,loglevel=9\");");
			writer.WriteLine("#else");
			writer.WriteLine("il2cpp_debugger_set_agent_options(\"--debugger-agent=transport=dt_socket,address=0.0.0.0:\" DEBUGGER_STRINGIFY2(IL2CPP_DEBUGGER_PORT) \",server=y,suspend=n,loglevel=9,logfile=\" DEBUGGER_STRINGIFY2(IL2CPP_MONO_DEBUGGER_LOGFILE) \"\");");
			writer.WriteLine("#endif");
			writer.WriteLine("#else");
			writer.WriteLine("il2cpp_debugger_set_agent_options(\"--debugger-agent=transport=dt_socket,address=0.0.0.0:\" DEBUGGER_STRINGIFY2(IL2CPP_DEBUGGER_PORT) \",server=y,suspend=n\");");
			writer.WriteLine("#endif");
			writer.WriteLine("#undef DEBUGGER_STRINGIFY");
			writer.WriteLine("#undef DEBUGGER_STRINGIFY2");
			writer.WriteLine("#endif");
			writer.WriteLine();
		}

		private void WriteSetConfiguration(ICodeWriter writer)
		{
			if (!_context.Global.Parameters.UsingTinyBackend)
			{
				writer.WriteLine();
				writer.WriteLine("#if IL2CPP_TARGET_WINDOWS");
				writer.WriteLine("il2cpp_set_config_utf16(argv[0]);");
				writer.WriteLine("#elif IL2CPP_TARGET_JAVASCRIPT");
				writer.WriteLine("il2cpp_set_config(\"/\");");
				writer.WriteLine("#else");
				writer.WriteLine("il2cpp_set_config(argv[0]);");
				writer.WriteLine("#endif");
			}
		}

		private static string EscapePath(string path)
		{
			return path.Replace("\\", "\\\\");
		}

		private void WriteSetCommandLineArgumentsAndInitIl2Cpp(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine("#if IL2CPP_DISABLE_GC");
			writer.WriteLine("il2cpp_gc_disable();");
			writer.WriteLine("#endif");
			writer.WriteLine();
			if (_context.Global.Parameters.UsingTinyBackend)
			{
				writer.WriteLine("#if IL2CPP_TARGET_WINDOWS");
				writer.Indent();
				writer.WriteLine("il2cpp_set_commandline_arguments_utf16(argc, argv, NULL);");
				writer.Dedent();
				writer.WriteLine("#else");
				writer.Indent();
				writer.WriteLine("il2cpp_set_commandline_arguments(argc, argv, NULL);");
				writer.WriteLine("#endif");
				writer.WriteLine("il2cpp_init();");
				return;
			}
			writer.Dedent();
			writer.WriteLine("#if IL2CPP_TARGET_LUMIN");
			writer.Indent();
			writer.WriteLine("il2cpp_set_data_dir(\"/package/Data\");");
			writer.WriteLine("il2cpp_set_config_dir(\"/package/Data/etc\");");
			writer.Dedent();
			writer.WriteLine("#endif");
			writer.WriteLine("#if IL2CPP_DRIVER_PLATFORM_CONFIG");
			writer.Indent();
			writer.WriteLine("il2cpp_set_data_dir(platform_data_path());");
			writer.Dedent();
			writer.WriteLine("#endif");
			writer.WriteLine();
			writer.WriteLine("#if IL2CPP_TARGET_WINDOWS");
			writer.Indent();
			writer.WriteLine("il2cpp_set_commandline_arguments_utf16(argc, argv, NULL);");
			writer.WriteLine("il2cpp_init_utf16(argv[0]);");
			writer.Dedent();
			writer.WriteLine("#else");
			writer.Indent();
			writer.WriteLine("il2cpp_set_commandline_arguments(argc, argv, NULL);");
			writer.WriteLine("il2cpp_init(argv[0]);");
			writer.Dedent();
			writer.WriteLine("#endif");
			writer.Indent();
		}

		private void WriteWindowsMessageBoxHook(ICodeWriter writer, string executableName)
		{
			if (!_context.Global.Parameters.NeverAttachDialog && (System.Diagnostics.Debugger.IsAttached || _context.Global.Parameters.EmitAttachDialog))
			{
				writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_DESKTOP");
				writer.WriteLine("MessageBoxW(NULL, L\"Attach\", L\"" + executableName + "\", MB_OK);");
				writer.WriteLine("#endif");
				writer.WriteLine();
			}
		}

		private void WriteEmscriptenTinyMetadataLoaderOnSuccess(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine("void OnSuccess(emscripten_fetch_t* pFetch)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("g_MetadataForWebTinyDebugger = (void*) pFetch->data;");
				writer.WriteLine("const char* argv[1];");
				writer.WriteLine("argv[0] = \"\";");
				writer.WriteLine("EntryPoint(1, argv);");
			}
		}

		private void WriteEmscriptenTinyMetadataLoaderOnError(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine("void OnError(emscripten_fetch_t* pFetch)");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("printf(\"Unable to load the file 'Data/Metadata/global-metadata.dat' from the server. This file is required for managed code debugging.\\n\");");
				writer.WriteLine("abort();");
			}
		}

		private void WritePlatformSpecificEntryPoints(IGeneratedMethodCodeWriter writer)
		{
			writer.WriteLine("#if IL2CPP_TARGET_WINDOWS");
			writer.WriteLine();
			writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_GAMES ");
			writer.WriteLine("#include <windef.h>");
			writer.WriteLine("#include <string>");
			writer.WriteLine("#include <locale>");
			writer.WriteLine("#include <codecvt>");
			writer.WriteLine("#elif !IL2CPP_TARGET_WINDOWS_DESKTOP ");
			writer.WriteLine("#include \"ActivateApp.h\"");
			writer.WriteLine("#endif");
			writer.WriteLine();
			writer.WriteLine("int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nShowCmd)");
			using (new BlockWriter(writer))
			{
				writer.Dedent();
				writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_DESKTOP || IL2CPP_TARGET_WINDOWS_GAMES");
				writer.WriteLine("#if IL2CPP_TARGET_WINDOWS_DESKTOP");
				writer.Indent();
				writer.WriteLine("int argc;");
				writer.WriteLine("wchar_t** argv = CommandLineToArgvW(GetCommandLineW(), &argc);");
				writer.WriteLine("int returnValue = EntryPoint(argc, argv);");
				writer.WriteLine("LocalFree(argv);");
				writer.WriteLine("return returnValue;");
				writer.Dedent();
				writer.WriteLine("#elif IL2CPP_TARGET_WINDOWS_GAMES");
				writer.Indent();
				writer.WriteLine("int result = EntryPoint(__argc, __wargv);");
				writer.WriteLine("return result;");
				writer.Dedent();
				writer.WriteLine("#endif");
				writer.WriteLine("#elif IL2CPP_WINRT_NO_ACTIVATE");
				writer.Indent();
				writer.WriteLine("wchar_t executableName[MAX_PATH + 2];");
				writer.WriteLine("GetModuleFileNameW(nullptr, executableName, MAX_PATH + 2);");
				writer.WriteLine();
				writer.WriteLine("int argc = 1;");
				writer.WriteLine("const wchar_t* argv[] = { executableName };");
				writer.WriteLine("return EntryPoint(argc, argv);");
				writer.Dedent();
				writer.WriteLine("#else");
				writer.Indent();
				writer.WriteLine("return WinRT::Activate(EntryPoint);");
				writer.Dedent();
				writer.WriteLine("#endif");
				writer.Indent();
			}
			writer.WriteLine();
			writer.WriteLine("#elif IL2CPP_TARGET_ANDROID && IL2CPP_TINY");
			writer.WriteLine();
			writer.WriteLine("#include <jni.h>");
			writer.WriteLine("int main(int argc, const char* argv[])");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("return EntryPoint(argc, argv);");
			}
			writer.WriteLine("extern \"C\"");
			writer.WriteLine("JNIEXPORT void start()");
			writer.WriteLine("{");
			writer.Indent();
			writer.WriteLine("const char* argv[1];");
			writer.WriteLine("argv[0] = \"\";");
			writer.WriteLine("main(1, argv);");
			writer.Dedent();
			writer.WriteLine("}");
			writer.WriteLine();
			writer.WriteLine("#elif IL2CPP_TARGET_IOS && IL2CPP_TINY");
			writer.WriteLine();
			writer.WriteLine("extern \"C\"");
			writer.WriteLine("void start()");
			writer.WriteLine("{");
			writer.Indent();
			writer.WriteLine("const char* argv[1];");
			writer.WriteLine("argv[0] = \"\";");
			writer.WriteLine("EntryPoint(1, argv);");
			writer.Dedent();
			writer.WriteLine("}");
			writer.WriteLine();
			writer.WriteLine("#elif IL2CPP_TARGET_JAVASCRIPT && IL2CPP_MONO_DEBUGGER && !IL2CPP_TINY_FROM_IL2CPP_BUILDER");
			writer.WriteLine("#include <emscripten.h>");
			writer.WriteLine("#include <emscripten/fetch.h>");
			writer.WriteLine("#include <emscripten/html5.h>");
			writer.WriteLine();
			writer.WriteLine("void* g_MetadataForWebTinyDebugger = NULL;");
			writer.WriteLine();
			WriteEmscriptenTinyMetadataLoaderOnSuccess(writer);
			writer.WriteLine();
			WriteEmscriptenTinyMetadataLoaderOnError(writer);
			writer.WriteLine();
			writer.WriteLine("int main(int argc, const char* argv[])");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("emscripten_fetch_attr_t attr;");
				writer.WriteLine("emscripten_fetch_attr_init(&attr);");
				writer.WriteLine("strcpy(attr.requestMethod, \"GET\");");
				writer.WriteLine("attr.attributes = EMSCRIPTEN_FETCH_LOAD_TO_MEMORY;");
				writer.WriteLine("attr.onsuccess = OnSuccess;");
				writer.WriteLine("attr.onerror = OnError;");
				writer.WriteLine("emscripten_fetch(&attr, \"Data/Metadata/global-metadata.dat\");");
				writer.WriteLine("#if (__EMSCRIPTEN_major__ >= 1) && (__EMSCRIPTEN_minor__ >= 39) && (__EMSCRIPTEN_tiny__ >= 5)");
				writer.WriteLine("emscripten_unwind_to_js_event_loop();");
				writer.WriteLine("#endif");
			}
			writer.WriteLine();
			writer.WriteLine("#else");
			writer.WriteLine();
			writer.WriteLine("int main(int argc, const char* argv[])");
			using (new BlockWriter(writer))
			{
				writer.WriteLine("return EntryPoint(argc, argv);");
			}
			writer.WriteLine();
			writer.WriteLine("#endif");
		}
	}
}
