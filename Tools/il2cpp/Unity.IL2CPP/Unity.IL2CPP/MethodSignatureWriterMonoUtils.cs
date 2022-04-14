using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP
{
	public class MethodSignatureWriterMonoUtils
	{
		private static readonly ReadOnlyCollection<string> MonoICallsUsingAHandle = new string[251]
		{
			"System.String Mono.Runtime::GetDisplayName()", "System.String Mono.Runtime::GetNativeStackTrace(System.Exception)", "System.Void System.AppDomain::DoUnhandledException(System.Exception)", "System.Int32 System.AppDomain::ExecuteAssembly(System.Reflection.Assembly,System.String[])", "System.Reflection.Assembly[] System.AppDomain::GetAssemblies(System.Boolean)", "System.Object System.AppDomain::GetData(System.String)", "System.Runtime.Remoting.Contexts.Context System.AppDomain::InternalGetContext()", "System.Runtime.Remoting.Contexts.Context System.AppDomain::InternalGetDefaultContext()", "System.String System.AppDomain::InternalGetProcessGuid(System.String)", "System.Boolean System.AppDomain::InternalIsFinalizingForUnload(System.Int32)",
			"System.Void System.AppDomain::InternalPopDomainRef()", "System.Void System.AppDomain::InternalPushDomainRef(System.AppDomain)", "System.Void System.AppDomain::InternalPushDomainRefByID(System.Int32)", "System.Runtime.Remoting.Contexts.Context System.AppDomain::InternalSetContext(System.Runtime.Remoting.Contexts.Context)", "System.AppDomain System.AppDomain::InternalSetDomain(System.AppDomain)", "System.AppDomain System.AppDomain::InternalSetDomainByID(System.Int32)", "System.Void System.AppDomain::InternalUnload(System.Int32)", "System.Reflection.Assembly System.AppDomain::LoadAssembly(System.String,System.Security.Policy.Evidence,System.Boolean)", "System.Reflection.Assembly System.AppDomain::LoadAssemblyRaw(System.Byte[],System.Byte[],System.Security.Policy.Evidence,System.Boolean)", "System.Void System.AppDomain::SetData(System.String,System.Object)",
			"System.AppDomain System.AppDomain::createDomain(System.String,System.AppDomainSetup)", "System.AppDomain System.AppDomain::getCurDomain()", "System.String System.AppDomain::getFriendlyName()", "System.AppDomain System.AppDomain::getRootDomain()", "System.AppDomainSetup System.AppDomain::getSetup()", "System.Void System.Array::SetValue(System.Object,System.Int32[])", "System.Void System.Array::SetValueImpl(System.Object,System.Int32)", "System.String System.Configuration.DefaultConfig::get_bundled_machine_config()", "System.String System.Configuration.DefaultConfig::get_machine_config_path()", "System.String System.Configuration.InternalConfigurationHost::get_bundled_app_config()",
			"System.String System.Configuration.InternalConfigurationHost::get_bundled_machine_config()", "System.MulticastDelegate System.Delegate::AllocDelegateLike_internal(System.Delegate)", "System.Delegate System.Delegate::CreateDelegate_internal(System.Type,System.Object,System.Reflection.MethodInfo,System.Boolean)", "System.Reflection.MethodInfo System.Delegate::GetVirtualMethod_internal()", "System.Boolean System.Enum::GetEnumValuesAndNames(System.RuntimeType,System.UInt64[]&,System.String[]&)", "System.String[] System.Environment::GetCommandLineArgs()", "System.String System.Environment::GetMachineConfigPath()", "System.String System.Environment::GetNewLine()", "System.String System.Environment::GetOSVersionString()", "System.String System.Environment::GetWindowsFolderPath(System.Int32)",
			"System.String System.Environment::get_MachineName()", "System.String System.Environment::get_UserName()", "System.String System.Environment::get_bundled_machine_config()", "System.Void System.Environment::internalBroadcastSettingChange()", "System.String System.Environment::internalGetEnvironmentVariable_native(System.IntPtr)", "System.String System.Environment::internalGetGacPath()", "System.String System.Environment::internalGetHome()", "System.String System.Globalization.CultureInfo::get_current_locale_name()", "System.IntPtr System.IO.MonoIO::FindFirstFile(System.Char*,System.String&,System.Int32&,System.Int32&)", "System.Boolean System.IO.MonoIO::FindNextFile(System.IntPtr,System.String&,System.Int32&,System.Int32&)",
			"System.String System.IO.MonoIO::GetCurrentDirectory(System.IO.MonoIOError&)", "System.Int32 System.IO.MonoIO::Read(System.IntPtr,System.Byte[],System.Int32,System.Int32,System.IO.MonoIOError&)", "System.Int32 System.IO.MonoIO::Write(System.IntPtr,System.Byte[],System.Int32,System.Int32,System.IO.MonoIOError&)", "System.String System.IO.Path::get_temp_path()", "System.Reflection.CustomAttributeData[] System.MonoCustomAttrs::GetCustomAttributesDataInternal(System.Reflection.ICustomAttributeProvider)", "System.Object[] System.MonoCustomAttrs::GetCustomAttributesInternal(System.Reflection.ICustomAttributeProvider,System.Type,System.Boolean)", "System.Boolean System.MonoCustomAttrs::IsDefinedInternal(System.Reflection.ICustomAttributeProvider,System.Type)", "System.Boolean System.Net.Dns::GetHostByAddr_internal(System.String,System.String&,System.String[]&,System.String[]&,System.Int32)", "System.Boolean System.Net.Dns::GetHostByName_internal(System.String,System.String&,System.String[]&,System.String[]&,System.Int32)", "System.Boolean System.Net.Dns::GetHostName_internal(System.String&)",
			"System.IntPtr System.Net.Sockets.Socket::Accept_internal(System.IntPtr,System.Int32&,System.Boolean)", "System.Int32 System.Net.Sockets.Socket::Available_internal(System.IntPtr,System.Int32&)", "System.Void System.Net.Sockets.Socket::Bind_internal(System.IntPtr,System.Net.SocketAddress,System.Int32&)", "System.Void System.Net.Sockets.Socket::Blocking_internal(System.IntPtr,System.Boolean,System.Int32&)", "System.Void System.Net.Sockets.Socket::Close_internal(System.IntPtr,System.Int32&)", "System.Void System.Net.Sockets.Socket::Connect_internal(System.IntPtr,System.Net.SocketAddress,System.Int32&,System.Boolean)", "System.Void System.Net.Sockets.Socket::Disconnect_internal(System.IntPtr,System.Boolean,System.Int32&)", "System.Boolean System.Net.Sockets.Socket::Duplicate_internal(System.IntPtr,System.Int32,System.IntPtr&,System.IO.MonoIOError&)", "System.Void System.Net.Sockets.Socket::GetSocketOption_arr_internal(System.IntPtr,System.Net.Sockets.SocketOptionLevel,System.Net.Sockets.SocketOptionName,System.Byte[]&,System.Int32&)", "System.Void System.Net.Sockets.Socket::GetSocketOption_obj_internal(System.IntPtr,System.Net.Sockets.SocketOptionLevel,System.Net.Sockets.SocketOptionName,System.Object&,System.Int32&)",
			"System.Int32 System.Net.Sockets.Socket::IOControl_internal(System.IntPtr,System.Int32,System.Byte[],System.Byte[],System.Int32&)", "System.Void System.Net.Sockets.Socket::Listen_internal(System.IntPtr,System.Int32,System.Int32&)", "System.Net.SocketAddress System.Net.Sockets.Socket::LocalEndPoint_internal(System.IntPtr,System.Int32,System.Int32&)", "System.Boolean System.Net.Sockets.Socket::Poll_internal(System.IntPtr,System.Net.Sockets.SelectMode,System.Int32,System.Int32&)", "System.Int32 System.Net.Sockets.Socket::ReceiveFrom_internal(System.IntPtr,System.Byte*,System.Int32,System.Net.Sockets.SocketFlags,System.Net.SocketAddress&,System.Int32&,System.Boolean)", "System.Int32 System.Net.Sockets.Socket::Receive_internal(System.IntPtr,System.Byte*,System.Int32,System.Net.Sockets.SocketFlags,System.Int32&,System.Boolean)", "System.Int32 System.Net.Sockets.Socket::Receive_internal(System.IntPtr,System.Net.Sockets.Socket/WSABUF*,System.Int32,System.Net.Sockets.SocketFlags,System.Int32&,System.Boolean)", "System.Int32 System.Net.Sockets.Socket::Receive_internal(System.IntPtr,System.Net.Sockets.Socket/WSABUF*,System.Int32,System.Net.Sockets.SocketFlags,System.Int32&,System.Boolean)", "System.Net.SocketAddress System.Net.Sockets.Socket::RemoteEndPoint_internal(System.IntPtr,System.Int32,System.Int32&)", "System.Void System.Net.Sockets.Socket::Select_internal(System.Net.Sockets.Socket[]&,System.Int32,System.Int32&)",
			"System.Boolean System.Net.Sockets.Socket::SendFile_internal(System.IntPtr,System.String,System.Byte[],System.Byte[],System.Net.Sockets.TransmitFileOptions,System.Int32&,System.Boolean)", "System.Int32 System.Net.Sockets.Socket::SendTo_internal(System.IntPtr,System.Byte*,System.Int32,System.Net.Sockets.SocketFlags,System.Net.SocketAddress,System.Int32&,System.Boolean)", "System.Int32 System.Net.Sockets.Socket::Send_internal(System.IntPtr,System.Byte*,System.Int32,System.Net.Sockets.SocketFlags,System.Int32&,System.Boolean)", "System.Int32 System.Net.Sockets.Socket::Send_internal(System.IntPtr,System.Net.Sockets.Socket/WSABUF*,System.Int32,System.Net.Sockets.SocketFlags,System.Int32&,System.Boolean)", "System.Int32 System.Net.Sockets.Socket::Send_internal(System.IntPtr,System.Net.Sockets.Socket/WSABUF*,System.Int32,System.Net.Sockets.SocketFlags,System.Int32&,System.Boolean)", "System.Void System.Net.Sockets.Socket::SetSocketOption_internal(System.IntPtr,System.Net.Sockets.SocketOptionLevel,System.Net.Sockets.SocketOptionName,System.Object,System.Byte[],System.Int32,System.Int32&)", "System.Void System.Net.Sockets.Socket::Shutdown_internal(System.IntPtr,System.Net.Sockets.SocketShutdown,System.Int32&)", "System.IntPtr System.Net.Sockets.Socket::Socket_internal(System.Net.Sockets.AddressFamily,System.Net.Sockets.SocketType,System.Net.Sockets.ProtocolType,System.Int32&)", "System.Boolean System.Net.Sockets.Socket::SupportsPortReuse(System.Net.Sockets.ProtocolType)", "System.Void System.Net.Sockets.Socket::cancel_blocking_socket_operation(System.Threading.Thread)",
			"System.Type System.Object::GetType()", "System.String System.Reflection.Assembly::GetAotId()", "System.Reflection.Assembly System.Reflection.Assembly::GetCallingAssembly()", "System.Reflection.Assembly System.Reflection.Assembly::GetEntryAssembly()", "System.Reflection.Assembly System.Reflection.Assembly::GetExecutingAssembly()", "System.Object System.Reflection.Assembly::GetFilesInternal(System.String,System.Boolean)", "System.Reflection.Module System.Reflection.Assembly::GetManifestModuleInternal()", "System.Boolean System.Reflection.Assembly::GetManifestResourceInfoInternal(System.String,System.Reflection.ManifestResourceInfo)", "System.IntPtr System.Reflection.Assembly::GetManifestResourceInternal(System.String,System.Int32&,System.Reflection.Module&)", "System.String[] System.Reflection.Assembly::GetManifestResourceNames()",
			"System.Reflection.Module[] System.Reflection.Assembly::GetModulesInternal()", "System.Type[] System.Reflection.Assembly::GetTypes(System.Boolean)", "System.Void System.Reflection.Assembly::InternalGetAssemblyName(System.String,Mono.MonoAssemblyName&,System.String&)", "System.IntPtr System.Reflection.Assembly::InternalGetReferencedAssemblies(System.Reflection.Assembly)", "System.Type System.Reflection.Assembly::InternalGetType(System.Reflection.Module,System.String,System.Boolean,System.Boolean)", "System.String System.Reflection.Assembly::InternalImageRuntimeVersion()", "System.Reflection.Assembly System.Reflection.Assembly::LoadFrom(System.String,System.Boolean)", "System.Boolean System.Reflection.Assembly::LoadPermissions(System.Reflection.Assembly,System.IntPtr&,System.Int32&,System.IntPtr&,System.Int32&,System.IntPtr&,System.Int32&)", "System.Reflection.MethodInfo System.Reflection.Assembly::get_EntryPoint()", "System.Boolean System.Reflection.Assembly::get_ReflectionOnly()",
			"System.String System.Reflection.Assembly::get_code_base(System.Boolean)", "System.String System.Reflection.Assembly::get_fullname()", "System.Boolean System.Reflection.Assembly::get_global_assembly_cache()", "System.String System.Reflection.Assembly::get_location()", "System.Reflection.Assembly System.Reflection.Assembly::load_with_partial_name(System.String,System.Security.Policy.Evidence)", "System.Void System.Reflection.Emit.AssemblyBuilder::UpdateNativeCustomAttributes(System.Reflection.Emit.AssemblyBuilder)", "System.Void System.Reflection.Emit.DynamicMethod::create_dynamic_method(System.Reflection.Emit.DynamicMethod)", "System.Void System.Reflection.Emit.EnumBuilder::setup_enum_type(System.Type)", "System.Object System.Reflection.Emit.ModuleBuilder::GetRegisteredToken(System.Int32)", "System.Void System.Reflection.Emit.ModuleBuilder::RegisterToken(System.Object,System.Int32)",
			"System.Void System.Reflection.Emit.ModuleBuilder::basic_init(System.Reflection.Emit.ModuleBuilder)", "System.Int32 System.Reflection.Emit.ModuleBuilder::getMethodToken(System.Reflection.Emit.ModuleBuilder,System.Reflection.MethodBase,System.Type[])", "System.Int32 System.Reflection.Emit.ModuleBuilder::getToken(System.Reflection.Emit.ModuleBuilder,System.Object,System.Boolean)", "System.Int32 System.Reflection.Emit.ModuleBuilder::getUSIndex(System.Reflection.Emit.ModuleBuilder,System.String)", "System.Void System.Reflection.Emit.ModuleBuilder::set_wrappers_type(System.Reflection.Emit.ModuleBuilder,System.Type)", "System.Byte[] System.Reflection.Emit.SignatureHelper::get_signature_field()", "System.Byte[] System.Reflection.Emit.SignatureHelper::get_signature_local()", "System.Reflection.TypeInfo System.Reflection.Emit.TypeBuilder::create_runtime_class()", "System.Reflection.EventInfo System.Reflection.EventInfo::internal_from_handle_type(System.IntPtr,System.IntPtr)", "System.Type[] System.Reflection.FieldInfo::GetTypeModifiers(System.Boolean)",
			"System.Runtime.InteropServices.MarshalAsAttribute System.Reflection.FieldInfo::get_marshal_info()", "System.Reflection.FieldInfo System.Reflection.FieldInfo::internal_from_handle_type(System.IntPtr,System.IntPtr)", "System.Int32 System.Reflection.MemberInfo::get_MetadataToken()", "System.Reflection.MethodBase System.Reflection.MethodBase::GetCurrentMethod()", "System.Reflection.MethodBody System.Reflection.MethodBase::GetMethodBodyInternal(System.IntPtr)", "System.Reflection.MethodBase System.Reflection.MethodBase::GetMethodFromHandleInternalType_native(System.IntPtr,System.IntPtr,System.Boolean)", "System.Type System.Reflection.Module::GetGlobalType()", "System.String System.Reflection.Module::GetGuidInternal()", "System.IntPtr System.Reflection.Module::GetHINSTANCE()", "System.Int32 System.Reflection.Module::GetMDStreamVersion(System.IntPtr)",
			"System.Void System.Reflection.Module::GetPEKind(System.IntPtr,System.Reflection.PortableExecutableKinds&,System.Reflection.ImageFileMachine&)", "System.Type[] System.Reflection.Module::InternalGetTypes()", "System.IntPtr System.Reflection.Module::ResolveFieldToken(System.IntPtr,System.Int32,System.IntPtr[],System.IntPtr[],System.Reflection.ResolveTokenError&)", "System.Reflection.MemberInfo System.Reflection.Module::ResolveMemberToken(System.IntPtr,System.Int32,System.IntPtr[],System.IntPtr[],System.Reflection.ResolveTokenError&)", "System.IntPtr System.Reflection.Module::ResolveMethodToken(System.IntPtr,System.Int32,System.IntPtr[],System.IntPtr[],System.Reflection.ResolveTokenError&)", "System.Byte[] System.Reflection.Module::ResolveSignature(System.IntPtr,System.Int32,System.Reflection.ResolveTokenError&)", "System.IntPtr System.Reflection.Module::ResolveTypeToken(System.IntPtr,System.Int32,System.IntPtr[],System.IntPtr[],System.Reflection.ResolveTokenError&)", "System.Int32 System.Reflection.Module::get_MetadataToken(System.Reflection.Module)", "System.Int32 System.Reflection.MonoCMethod::get_core_clr_security_level()", "System.Void System.Reflection.MonoEventInfo::get_event_info(System.Reflection.MonoEvent,System.Reflection.MonoEventInfo&)",
			"System.Type System.Reflection.MonoField::GetParentType(System.Boolean)", "System.Type System.Reflection.MonoField::ResolveType()", "System.Void System.Reflection.MonoField::SetValueInternal(System.Reflection.FieldInfo,System.Object,System.Object)", "System.Type[] System.Reflection.MonoMethod::GetGenericArguments()", "System.Reflection.MethodInfo System.Reflection.MonoMethod::GetGenericMethodDefinition_impl()", "System.Void System.Reflection.MonoMethod::GetPInvoke(System.Reflection.PInvokeAttributes&,System.String&,System.String&)", "System.Reflection.MethodInfo System.Reflection.MonoMethod::MakeGenericMethod_impl(System.Type[])", "System.Boolean System.Reflection.MonoMethod::get_IsGenericMethod()", "System.Boolean System.Reflection.MonoMethod::get_IsGenericMethodDefinition()", "System.Reflection.MonoMethod System.Reflection.MonoMethod::get_base_method(System.Reflection.MonoMethod,System.Boolean)",
			"System.Int32 System.Reflection.MonoMethod::get_core_clr_security_level()", "System.String System.Reflection.MonoMethod::get_name(System.Reflection.MethodBase)", "System.Void System.Reflection.MonoMethodInfo::get_method_info(System.IntPtr,System.Reflection.MonoMethodInfo&)", "System.Reflection.ParameterInfo[] System.Reflection.MonoMethodInfo::get_parameter_info(System.IntPtr,System.Reflection.MemberInfo)", "System.Runtime.InteropServices.MarshalAsAttribute System.Reflection.MonoMethodInfo::get_retval_marshal(System.IntPtr)", "System.Type[] System.Reflection.MonoPropertyInfo::GetTypeModifiers(System.Reflection.MonoProperty,System.Boolean)", "System.Void System.Reflection.MonoPropertyInfo::get_property_info(System.Reflection.MonoProperty,System.Reflection.MonoPropertyInfo&,System.Reflection.PInfo)", "System.Int32 System.Reflection.ParameterInfo::GetMetadataToken()", "System.Type[] System.Reflection.ParameterInfo::GetTypeModifiers(System.Boolean)", "System.Reflection.PropertyInfo System.Reflection.PropertyInfo::internal_from_handle_type(System.IntPtr,System.IntPtr)",
			"System.Void System.Runtime.CompilerServices.RuntimeHelpers::InitializeArray(System.Array,System.IntPtr)", "System.Delegate System.Runtime.InteropServices.Marshal::GetDelegateForFunctionPointerInternal(System.IntPtr,System.Type)", "System.IntPtr System.Runtime.InteropServices.Marshal::GetFunctionPointerForDelegateInternal(System.Delegate)", "System.IntPtr System.Runtime.InteropServices.Marshal::OffsetOf(System.Type,System.String)", "System.Void System.Runtime.InteropServices.Marshal::Prelink(System.Reflection.MethodInfo)", "System.Void System.Runtime.InteropServices.Marshal::PrelinkAll(System.Type)", "System.String System.Runtime.InteropServices.Marshal::PtrToStringAnsi(System.IntPtr)", "System.Int32 System.Runtime.InteropServices.Marshal::SizeOf(System.Type)", "System.Object System.Runtime.Remoting.Activation.ActivationServices::AllocateUninitializedClassInstance(System.Type)", "System.Void System.Runtime.Remoting.Activation.ActivationServices::EnableProxyActivation(System.Type,System.Boolean)",
			"System.Void System.Runtime.Remoting.Contexts.Context::RegisterContext(System.Runtime.Remoting.Contexts.Context)", "System.Void System.Runtime.Remoting.Contexts.Context::ReleaseContext(System.Runtime.Remoting.Contexts.Context)", "System.Object System.Runtime.Remoting.Proxies.RealProxy::InternalGetTransparentProxy(System.String)", "System.Reflection.MethodBase System.Runtime.Remoting.RemotingServices::GetVirtualMethod(System.Type,System.Reflection.MethodBase)", "System.Boolean System.Runtime.Remoting.RemotingServices::IsTransparentProxy(System.Object)", "System.Void System.RuntimeFieldHandle::SetValueInternal(System.Reflection.FieldInfo,System.Object,System.Object)", "System.IntPtr System.RuntimeMethodHandle::GetFunctionPointer(System.IntPtr)", "System.Object System.RuntimeType::CreateInstanceInternal(System.Type)", "System.IntPtr System.RuntimeType::GetConstructors_native(System.Reflection.BindingFlags)", "System.Reflection.ConstructorInfo System.RuntimeType::GetCorrespondingInflatedConstructor(System.Reflection.ConstructorInfo)",
			"System.Reflection.MethodInfo System.RuntimeType::GetCorrespondingInflatedMethod(System.Reflection.MethodInfo)", "System.IntPtr System.RuntimeType::GetEvents_native(System.IntPtr,System.Reflection.BindingFlags)", "System.IntPtr System.RuntimeType::GetFields_native(System.IntPtr,System.Reflection.BindingFlags)", "System.Type[] System.RuntimeType::GetGenericArgumentsInternal(System.Boolean)", "System.Int32 System.RuntimeType::GetGenericParameterPosition()", "System.Void System.RuntimeType::GetInterfaceMapData(System.Type,System.Type,System.Reflection.MethodInfo[]&,System.Reflection.MethodInfo[]&)", "System.Type[] System.RuntimeType::GetInterfaces()", "System.IntPtr System.RuntimeType::GetMethodsByName_native(System.IntPtr,System.Reflection.BindingFlags,System.Boolean)", "System.IntPtr System.RuntimeType::GetNestedTypes_native(System.IntPtr,System.Reflection.BindingFlags)", "System.Void System.RuntimeType::GetPacking(System.Int32&,System.Int32&)",
			"System.IntPtr System.RuntimeType::GetPropertiesByName_native(System.IntPtr,System.Reflection.BindingFlags,System.Boolean)", "System.TypeCode System.RuntimeType::GetTypeCodeImplInternal(System.Type)", "System.Boolean System.RuntimeType::IsTypeExportedToWindowsRuntime(System.RuntimeType)", "System.Boolean System.RuntimeType::IsWindowsRuntimeObjectType(System.RuntimeType)", "System.Type System.RuntimeType::MakeGenericType(System.Type,System.Type[])", "System.Type System.RuntimeType::MakePointerType(System.Type)", "System.String System.RuntimeType::getFullName(System.Boolean,System.Boolean)", "System.Reflection.MethodBase System.RuntimeType::get_DeclaringMethod()", "System.Type System.RuntimeType::get_DeclaringType()", "System.String System.RuntimeType::get_Name()",
			"System.String System.RuntimeType::get_Namespace()", "System.Int32 System.RuntimeType::get_core_clr_security_level()", "System.Type System.RuntimeType::make_array_type(System.Int32)", "System.Type System.RuntimeType::make_byref_type()", "System.Int32 System.RuntimeTypeHandle::GetArrayRank(System.RuntimeType)", "System.Reflection.RuntimeAssembly System.RuntimeTypeHandle::GetAssembly(System.RuntimeType)", "System.Reflection.TypeAttributes System.RuntimeTypeHandle::GetAttributes(System.RuntimeType)", "System.RuntimeType System.RuntimeTypeHandle::GetBaseType(System.RuntimeType)", "System.RuntimeType System.RuntimeTypeHandle::GetElementType(System.RuntimeType)", "System.IntPtr System.RuntimeTypeHandle::GetGenericParameterInfo(System.RuntimeType)",
			"System.Type System.RuntimeTypeHandle::GetGenericTypeDefinition_impl(System.RuntimeType)", "System.Int32 System.RuntimeTypeHandle::GetMetadataToken(System.RuntimeType)", "System.Reflection.RuntimeModule System.RuntimeTypeHandle::GetModule(System.RuntimeType)", "System.Boolean System.RuntimeTypeHandle::HasInstantiation(System.RuntimeType)", "System.Boolean System.RuntimeTypeHandle::HasReferences(System.RuntimeType)", "System.Boolean System.RuntimeTypeHandle::IsArray(System.RuntimeType)", "System.Boolean System.RuntimeTypeHandle::IsByRef(System.RuntimeType)", "System.Boolean System.RuntimeTypeHandle::IsComObject(System.RuntimeType)", "System.Boolean System.RuntimeTypeHandle::IsGenericTypeDefinition(System.RuntimeType)", "System.Boolean System.RuntimeTypeHandle::IsGenericVariable(System.RuntimeType)",
			"System.Boolean System.RuntimeTypeHandle::IsInstanceOfType(System.RuntimeType,System.Object)", "System.Boolean System.RuntimeTypeHandle::IsPointer(System.RuntimeType)", "System.Boolean System.RuntimeTypeHandle::IsPrimitive(System.RuntimeType)", "System.Boolean System.RuntimeTypeHandle::type_is_assignable_from(System.Type,System.Type)", "System.Boolean System.Security.Policy.Evidence::IsAuthenticodePresent(System.Reflection.Assembly)", "System.IntPtr System.Security.Principal.WindowsIdentity::GetCurrentToken()", "System.String System.Security.Principal.WindowsIdentity::GetTokenName(System.IntPtr)", "System.IntPtr System.Security.Principal.WindowsIdentity::GetUserToken(System.String)", "System.String System.Text.EncodingHelper::InternalCodePage(System.Int32&)", "System.Void System.Text.Normalization::load_normalization_resource(System.IntPtr&,System.IntPtr&,System.IntPtr&,System.IntPtr&,System.IntPtr&,System.IntPtr&)",
			"System.IntPtr System.Threading.Mutex::CreateMutex_internal(System.Boolean,System.String,System.Boolean&)", "System.IntPtr System.Threading.Mutex::OpenMutex_internal(System.String,System.Security.AccessControl.MutexRights,System.IO.MonoIOError&)", "System.IntPtr System.Threading.NativeEventCalls::CreateEvent_internal(System.Boolean,System.Boolean,System.String,System.Int32&)", "System.IntPtr System.Threading.NativeEventCalls::OpenEvent_internal(System.String,System.Security.AccessControl.EventWaitHandleRights,System.Int32&)", "System.Int32 System.Threading.WaitHandle::SignalAndWait_Internal(System.IntPtr,System.IntPtr,System.Int32)", "System.Int32 System.Threading.WaitHandle::Wait_internal(System.IntPtr*,System.Int32,System.Boolean,System.Int32)", "System.Type System.Type::internal_from_handle(System.IntPtr)", "System.Type System.Type::internal_from_name(System.String,System.Boolean,System.Boolean)", "System.String System.Web.Util.ICalls::GetMachineConfigPath()", "System.String System.Web.Util.ICalls::GetMachineInstallDirectory()",
			"System.Boolean System.Web.Util.ICalls::GetUnmanagedResourcesPtr(System.Reflection.Assembly,System.IntPtr&,System.Int32&)"
		}.AsReadOnly();
	}
}
