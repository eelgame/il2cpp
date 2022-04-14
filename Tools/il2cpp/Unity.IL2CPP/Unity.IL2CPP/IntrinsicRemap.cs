using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public class IntrinsicRemap
	{
		private class RemappedMethods
		{
			private readonly ReadOnlyDictionary<string, string> _mapping;

			private readonly HashSet<(string, string)> _typesWithIntrinsics = new HashSet<(string, string)>();

			public RemappedMethods(ReadOnlyDictionary<string, string> mapping)
			{
				_mapping = mapping;
				foreach (KeyValuePair<string, string> item3 in _mapping)
				{
					string key = item3.Key;
					int num = key.IndexOf(' ') + 1;
					int num2 = key.IndexOf(':');
					string text = key.Substring(num, num2 - num);
					int num3 = text.LastIndexOf('.');
					string item = text.Substring(0, num3);
					string item2 = text.Substring(num3 + 1);
					_typesWithIntrinsics.Add((item, item2));
				}
			}

			public bool TryGetValue(MethodReference method, out string mappedName)
			{
				if (!_typesWithIntrinsics.Contains((method.DeclaringType.Namespace, method.DeclaringType.Name)))
				{
					mappedName = null;
					return false;
				}
				return _mapping.TryGetValue(method.FullName, out mappedName);
			}
		}

		private const string GetTypeStringSignature = "System.Type System.Type::GetType(System.String)";

		private const string GetTypeStringSignatureBoolean = "System.Type System.Type::GetType(System.String,System.Boolean)";

		private const string GetTypeStringSignatureBooleanBoolean = "System.Type System.Type::GetType(System.String,System.Boolean,System.Boolean)";

		private const string GetTypeTargetName = "il2cpp_codegen_get_type";

		private const string GetCurrentMethodSignature = "System.Reflection.MethodBase System.Reflection.MethodBase::GetCurrentMethod()";

		private const string GetIUnknownForObjectSignatureObject = "System.IntPtr System.Runtime.InteropServices.Marshal::GetIUnknownForObject(System.Object)";

		private const string GetExecutingAssemblySignature = "System.Reflection.Assembly System.Reflection.Assembly::GetExecutingAssembly()";

		private static readonly RemappedMethods _remappedMethods = new RemappedMethods(new Dictionary<string, string>
		{
			{ "System.Double System.Math::Asin(System.Double)", "asin" },
			{ "System.Double System.Math::Cosh(System.Double)", "cosh" },
			{ "System.Double System.Math::Abs(System.Double)", "fabs" },
			{ "System.Single System.Math::Abs(System.Single)", "fabsf" },
			{ "System.Double System.Math::Log(System.Double)", "log" },
			{ "System.Double System.Math::Tan(System.Double)", "tan" },
			{ "System.Double System.Math::Exp(System.Double)", "exp" },
			{ "System.Int64 System.Math::Abs(System.Int64)", "il2cpp_codegen_abs" },
			{ "System.Double System.Math::Ceiling(System.Double)", "ceil" },
			{ "System.Double System.Math::Atan(System.Double)", "atan" },
			{ "System.Double System.Math::Tanh(System.Double)", "tanh" },
			{ "System.Double System.Math::Sqrt(System.Double)", "sqrt" },
			{ "System.Double System.Math::Log10(System.Double)", "log10" },
			{ "System.Double System.Math::Sinh(System.Double)", "sinh" },
			{ "System.Double System.Math::Cos(System.Double)", "cos" },
			{ "System.Double System.Math::Atan2(System.Double,System.Double)", "atan2" },
			{ "System.Int32 System.Math::Abs(System.Int32)", "il2cpp_codegen_abs" },
			{ "System.Double System.Math::Sin(System.Double)", "sin" },
			{ "System.Double System.Math::Acos(System.Double)", "acos" },
			{ "System.Double System.Math::Floor(System.Double)", "floor" },
			{ "System.Double System.Math::Round(System.Double)", "bankers_round" },
			{ "System.Reflection.MethodBase System.Reflection.MethodBase::GetCurrentMethod()", "il2cpp_codegen_get_method_object" },
			{ "System.Type System.Type::GetType(System.String)", "il2cpp_codegen_get_type" },
			{ "System.Type System.Type::GetType(System.String,System.Boolean)", "il2cpp_codegen_get_type" },
			{ "System.Type System.Type::GetType(System.String,System.Boolean,System.Boolean)", "il2cpp_codegen_get_type" },
			{ "System.IntPtr System.Runtime.InteropServices.Marshal::GetIUnknownForObject(System.Object)", "il2cpp_codegen_com_get_iunknown_for_object" },
			{ "System.Reflection.Assembly System.Reflection.Assembly::GetExecutingAssembly()", "il2cpp_codegen_get_executing_assembly" },
			{ "System.Void System.Threading.Volatile::Write(System.Byte&,System.Byte)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.Boolean&,System.Boolean)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.Double&,System.Double)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.Int16&,System.Int16)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.Int32&,System.Int32)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.Int64&,System.Int64)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.IntPtr&,System.IntPtr)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.SByte&,System.SByte)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.Single&,System.Single)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.UInt16&,System.UInt16)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.UInt32&,System.UInt32)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.UInt64&,System.UInt64)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(System.UIntPtr&,System.UIntPtr)", "VolatileWrite" },
			{ "System.Void System.Threading.Volatile::Write(T&,T)", "VolatileWrite" },
			{ "System.Byte System.Threading.Volatile::Read(System.Byte&)", "VolatileRead" },
			{ "System.Boolean System.Threading.Volatile::Read(System.Boolean&)", "VolatileRead" },
			{ "System.Double System.Threading.Volatile::Read(System.Double&)", "VolatileRead" },
			{ "System.Int16 System.Threading.Volatile::Read(System.Int16&)", "VolatileRead" },
			{ "System.Int32 System.Threading.Volatile::Read(System.Int32&)", "VolatileRead" },
			{ "System.Int64 System.Threading.Volatile::Read(System.Int64&)", "VolatileRead" },
			{ "System.IntPtr System.Threading.Volatile::Read(System.IntPtr&)", "VolatileRead" },
			{ "System.SByte System.Threading.Volatile::Read(System.SByte&)", "VolatileRead" },
			{ "System.Single System.Threading.Volatile::Read(System.Single&)", "VolatileRead" },
			{ "System.UInt16 System.Threading.Volatile::Read(System.UInt16&)", "VolatileRead" },
			{ "System.UInt32 System.Threading.Volatile::Read(System.UInt32&)", "VolatileRead" },
			{ "System.UInt64 System.Threading.Volatile::Read(System.UInt64&)", "VolatileRead" },
			{ "System.UIntPtr System.Threading.Volatile::Read(System.UIntPtr&)", "VolatileRead" },
			{ "T System.Threading.Volatile::Read(T&)", "VolatileRead" },
			{ "System.Boolean System.Platform::get_IsMacOS()", "il2cpp_codegen_platform_is_osx_or_ios" },
			{ "System.Boolean System.Platform::get_IsFreeBSD()", "il2cpp_codegen_platform_is_freebsd" },
			{ "System.Boolean System.Net.NetworkInformation.IPGlobalProperties::get_PlatformNeedsLibCWorkaround()", "il2cpp_codegen_platform_disable_libc_pinvoke" },
			{ "System.Single UnityEngine.Mathf::Sin(System.Single)", "sinf" },
			{ "System.Single UnityEngine.Mathf::Cos(System.Single)", "cosf" },
			{ "System.Single UnityEngine.Mathf::Tan(System.Single)", "tanf" },
			{ "System.Single UnityEngine.Mathf::Asin(System.Single)", "asinf" },
			{ "System.Single UnityEngine.Mathf::Acos(System.Single)", "acosf" },
			{ "System.Single UnityEngine.Mathf::Atan(System.Single)", "atanf" },
			{ "System.Single UnityEngine.Mathf::Atan2(System.Single,System.Single)", "atan2f" },
			{ "System.Single UnityEngine.Mathf::Sqrt(System.Single)", "sqrtf" },
			{ "System.Single UnityEngine.Mathf::Abs(System.Single)", "fabsf" },
			{ "System.Single UnityEngine.Mathf::Pow(System.Single,System.Single)", "powf" },
			{ "System.Single UnityEngine.Mathf::Exp(System.Single)", "expf" },
			{ "System.Single UnityEngine.Mathf::Log(System.Single)", "logf" },
			{ "System.Single UnityEngine.Mathf::Log10(System.Single)", "log10f" },
			{ "System.Single UnityEngine.Mathf::Ceil(System.Single)", "ceilf" },
			{ "System.Single UnityEngine.Mathf::Floor(System.Single)", "floorf" },
			{ "System.Single UnityEngine.Mathf::Round(System.Single)", "bankers_roundf" },
			{ "System.Void System.Diagnostics.Debugger::Break()", "IL2CPP_DEBUG_BREAK" },
			{ "System.IntPtr System.Runtime.InteropServices.Marshal::GetComInterfaceForObject(System.Object,System.Type)", "il2cpp_codegen_get_com_interface_for_object" },
			{ "T Unity.Collections.NativeArray`1::get_Item(System.Int32)", "IL2CPP_NATIVEARRAY_GET_ITEM" },
			{ "System.Void Unity.Collections.NativeArray`1::set_Item(System.Int32,T)", "IL2CPP_NATIVEARRAY_SET_ITEM" },
			{ "System.Int32 Unity.Collections.NativeArray`1::get_Length()", "IL2CPP_NATIVEARRAY_GET_LENGTH" },
			{ "System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility::AddressOf(T&)", "il2cpp_codegen_unsafe_cast" },
			{ "System.Boolean System.Object::ReferenceEquals(System.Object,System.Object)", "il2cpp_codegen_object_reference_equals" },
			{ "T System.Array::UnsafeLoad(T[],System.Int32)", "IL2CPP_ARRAY_UNSAFE_LOAD" },
			{ "System.Void Unity.ThrowStub::ThrowNotSupportedException()", "il2cpp_codegen_raise_profile_exception" },
			{ "System.Void GBenchmarkApp.GBenchmark::RunSpecifiedBenchmarks()", "benchmark::RunSpecifiedBenchmarks" }
		}.AsReadOnly());

		private static readonly RemappedMethods _remappedMethodsDots = new RemappedMethods(new Dictionary<string, string>
		{
			{ "System.Int32 System.Runtime.CompilerServices.RuntimeHelpers::get_OffsetToStringData()", "il2cpp_codegen_get_offset_to_string_data" },
			{ "System.String System.String::CreateString(System.Int32)", "il2cpp_codegen_string_new_length" },
			{ "System.String System.String::CreateString(System.Char[],System.Int32,System.Int32)", "il2cpp_codegen_string_new_from_char_array" },
			{ "System.Void Unity.Collections.LowLevel.Unsafe.UnsafeUtility::MemCpy(System.Void*,System.Void*,System.Int64)", "memcpy" },
			{ "System.Void Unity.Collections.LowLevel.Unsafe.UnsafeUtility::MemSet(System.Void*,System.Byte,System.Int64)", "memset" },
			{ "System.Int32 Unity.Collections.LowLevel.Unsafe.UnsafeUtility::MemCmp(System.Void*,System.Void*,System.Int64)", "memcmp" },
			{ "System.Int32 Unity.Collections.LowLevel.Unsafe.UnsafeUtility::MemMove(System.Void*,System.Void*,System.Int64)", "memove" },
			{ "System.Void System.Runtime.CompilerServices.RuntimeHelpers::MemoryCopy(System.Void*,System.Void*,System.Int32)", "memcpy" },
			{ "System.Int32 System.Runtime.CompilerServices.RuntimeHelpers::MemoryCompare(System.Void*,System.Void*,System.Int32)", "memcmp" },
			{ "System.Void* Unity.Collections.LowLevel.Unsafe.UnsafeUtility::AddressOf(T&)", "il2cpp_codegen_unsafe_cast" },
			{ "System.Int32 System.Array::get_Length()", "il2cpp_codegen_get_array_length" },
			{ "System.Int32 System.Array::GetLength(System.Int32)", "il2cpp_codegen_get_array_length" },
			{ "System.MulticastDelegate System.MulticastDelegate::CreateCombinedDelegate(System.Type,System.MulticastDelegate[],System.Int32)", "il2cpp_codegen_create_combined_delegate" },
			{ "System.Type System.Object::GetType()", "il2cpp_codegen_get_type" },
			{ "System.Type System.Type::GetBaseType()", "il2cpp_codegen_get_base_type" },
			{ "System.Boolean System.Type::IsInterfaceImpl()", "il2cpp_codegen_type_is_interface" },
			{ "System.Boolean System.Type::IsAbstractImpl()", "il2cpp_codegen_type_is_abstract" },
			{ "System.Boolean System.Type::IsPointerImpl()", "il2cpp_codegen_type_is_pointer" },
			{ "System.Boolean System.Type::IsAssignableFrom(System.Type)", "il2cpp_codegen_is_assignable_from" },
			{ "System.Int32 System.Globalization.FormatProvider::DoubleToString(System.Double,System.Byte*,System.Byte*,System.Int32)", "il2cpp_codegen_double_to_string" },
			{ "System.String System.Runtime.InteropServices.Marshal::PtrToStringAnsi(System.IntPtr)", "il2cpp_codegen_marshal_ptr_to_string_ansi" },
			{ "System.IntPtr System.Runtime.InteropServices.Marshal::StringToCoTaskMemAnsi(System.String)", "il2cpp_codegen_marshal_string_to_co_task_mem_ansi" },
			{ "System.Void System.Runtime.InteropServices.Marshal::FreeCoTaskMem(System.IntPtr)", "il2cpp_codegen_marshal_string_free_co_task_mem" },
			{ "System.IntPtr System.Runtime.InteropServices.Marshal::GetFunctionPointerForDelegate(System.Delegate)", "il2cpp_codegen_marshal_get_function_pointer_for_delegate" },
			{ "System.Void System.Diagnostics.Debugger::Break()", "IL2CPP_DEBUG_BREAK" },
			{ "System.Int32 System.Runtime.CompilerServices.RuntimeHelpers::MemCmpRef(T&,T&)", "il2cpp::utils::MemoryUtils::MemCmpRef" },
			{ "System.Int32 System.Runtime.CompilerServices.RuntimeHelpers::MemHashRef(T&)", "il2cpp::utils::MemoryUtils::MemHashRef" },
			{ "System.Void GBenchmarkApp.GBenchmark::RunSpecifiedBenchmarks()", "benchmark::RunSpecifiedBenchmarks" },
			{ "T Unity.Collections.NativeArray`1::get_Item(System.Int32)", "IL2CPP_NATIVEARRAY_GET_ITEM" },
			{ "System.Void Unity.Collections.NativeArray`1::set_Item(System.Int32,T)", "IL2CPP_NATIVEARRAY_SET_ITEM" },
			{ "System.Int32 Unity.Collections.NativeArray`1::get_Length()", "IL2CPP_NATIVEARRAY_GET_LENGTH" }
		}.AsReadOnly());

		private static readonly ReadOnlyDictionary<string, Func<ReadOnlyContext, MethodReference, MethodReference, IRuntimeMetadataAccess, IEnumerable<string>, IEnumerable<string>>> MethodNameMappingCustomArguments = new Dictionary<string, Func<ReadOnlyContext, MethodReference, MethodReference, IRuntimeMetadataAccess, IEnumerable<string>, IEnumerable<string>>>
		{
			{ "System.Reflection.MethodBase System.Reflection.MethodBase::GetCurrentMethod()", GetCallingMethodMetadata },
			{ "System.Type System.Type::GetType(System.String)", GetTypeRemappingCustomArguments },
			{ "System.Type System.Type::GetType(System.String,System.Boolean)", GetTypeRemappingCustomArguments },
			{ "System.Type System.Type::GetType(System.String,System.Boolean,System.Boolean)", GetTypeRemappingCustomArguments },
			{ "System.Reflection.Assembly System.Reflection.Assembly::GetExecutingAssembly()", GetCallingMethodMetadata },
			{ "T Unity.Collections.NativeArray`1::get_Item(System.Int32)", NativeArrayGetItemRemappedArguments },
			{ "System.Void Unity.Collections.NativeArray`1::set_Item(System.Int32,T)", NativeArraySetItemRemappedArguments },
			{ "System.Int32 Unity.Collections.NativeArray`1::get_Length()", NativeArrayGetLengthRemappedArguments },
			{ "System.Void Unity.ThrowStub::ThrowNotSupportedException()", GetCallingMethodMetadata }
		}.AsReadOnly();

		private static RemappedMethods GetMethodNameMapping(ReadOnlyContext context)
		{
			if (context.Global.Parameters.UsingTinyClassLibraries)
			{
				return _remappedMethodsDots;
			}
			return _remappedMethods;
		}

		public static bool ShouldRemap(ReadOnlyContext context, MethodReference methodToCall)
		{
			MethodReference methodDefinition = GetMethodDefinition(methodToCall);
			string mappedName;
			return GetMethodNameMapping(context).TryGetValue(methodDefinition, out mappedName);
		}

		public static bool StillNeedsHiddenMethodInfo(ReadOnlyContext context, MethodReference methodToCall)
		{
			MethodReference methodDefinition = GetMethodDefinition(methodToCall);
			if (!context.Global.Parameters.UsingTinyClassLibraries && GetMethodNameMapping(context).TryGetValue(methodDefinition, out var mappedName) && mappedName == "il2cpp_codegen_get_type")
			{
				return true;
			}
			return false;
		}

		private static MethodReference GetMethodDefinition(MethodReference methodToCall)
		{
			return methodToCall.Resolve() ?? methodToCall;
		}

		public static string MappedNameFor(ReadOnlyContext context, MethodReference methodToCall)
		{
			MethodReference methodDefinition = GetMethodDefinition(methodToCall);
			if (GetMethodNameMapping(context).TryGetValue(methodDefinition, out var mappedName))
			{
				return mappedName;
			}
			throw new KeyNotFoundException($"Could not find an intrinsic method to remap for '{methodToCall}'");
		}

		public static bool HasCustomArguments(MethodReference methodToCall)
		{
			MethodReference methodDefinition = GetMethodDefinition(methodToCall);
			return MethodNameMappingCustomArguments.ContainsKey(methodDefinition.FullName);
		}

		public static IEnumerable<string> GetCustomArguments(ReadOnlyContext context, MethodReference methodToCall, MethodReference callingMethod, IRuntimeMetadataAccess runtimeMetadata, IEnumerable<string> arguments)
		{
			MethodReference methodDefinition = GetMethodDefinition(methodToCall);
			return MethodNameMappingCustomArguments[methodDefinition.FullName](context, callingMethod, methodToCall, runtimeMetadata, arguments);
		}

		private static IEnumerable<string> GetCallingMethodMetadata(ReadOnlyContext context, MethodReference callingMethod, MethodReference methodToCall, IRuntimeMetadataAccess runtimeMetadata, IEnumerable<string> arguments)
		{
			return new string[1] { runtimeMetadata.MethodInfo(callingMethod) };
		}

		private static IEnumerable<string> GetTypeRemappingCustomArguments(ReadOnlyContext context, MethodReference callingMethod, MethodReference methodToCall, IRuntimeMetadataAccess runtimeMetadata, IEnumerable<string> arguments)
		{
			List<string> list = new List<string>();
			list.Add(runtimeMetadata.MethodInfo(methodToCall));
			list.AddRange(arguments);
			list.Add(runtimeMetadata.MethodInfo(callingMethod));
			return list;
		}

		private static IEnumerable<string> NativeArrayGetItemRemappedArguments(ReadOnlyContext context, MethodReference callingMethod, MethodReference methodToCall, IRuntimeMetadataAccess runtimeMetadata, IEnumerable<string> arguments)
		{
			string[] source = (arguments as string[]) ?? arguments.ToArray();
			return new string[3]
			{
				context.Global.Services.Naming.ForVariable(((GenericInstanceType)methodToCall.DeclaringType).GenericArguments[0]),
				"(" + source.First() + ")->" + context.Global.Services.Naming.ForField(methodToCall.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "m_Buffer")),
				source.Last()
			};
		}

		private static IEnumerable<string> NativeArraySetItemRemappedArguments(ReadOnlyContext context, MethodReference callingMethod, MethodReference methodToCall, IRuntimeMetadataAccess runtimeMetadata, IEnumerable<string> arguments)
		{
			string[] array = (arguments as string[]) ?? arguments.ToArray();
			return new string[4]
			{
				context.Global.Services.Naming.ForVariable(((GenericInstanceType)methodToCall.DeclaringType).GenericArguments[0]),
				"(" + array[0] + ")->" + context.Global.Services.Naming.ForField(methodToCall.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "m_Buffer")),
				array[1],
				"(" + array[2] + ")"
			};
		}

		private static IEnumerable<string> NativeArrayGetLengthRemappedArguments(ReadOnlyContext context, MethodReference callingMethod, MethodReference methodToCall, IRuntimeMetadataAccess runtimeMetadata, IEnumerable<string> arguments)
		{
			return new string[1] { "(" + arguments.First() + ")->" + context.Global.Services.Naming.ForField(methodToCall.DeclaringType.Resolve().Fields.Single((FieldDefinition f) => f.Name == "m_Length")) };
		}
	}
}
