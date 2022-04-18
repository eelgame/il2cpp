# il2cpp
## 需要修改huatuo的地方
1. 增加il2cpp::vm::MetadataCache::LoadAssemblyFromFile函数
2. 增加il2cpp_test导出函数
```
// ==={{ huatuo
#ifdef IL2CPP_TARGET_WINDOWS
#include <iostream>

// CSHARP_DLL_PATH=CSharp.dll
// rundll32.exe GameAssembly-CSharp-x64.dll,il2cpp_test
int il2cpp_test()
{
    il2cpp_set_config_dir("Data/etc");
    il2cpp_set_data_dir("Data");
    il2cpp_init("IL2CPP Root Domain");
    char *tmp = getenv("CSHARP_DLL_PATH");
    if (tmp != nullptr)
    {
        std::string csharp_dll_path(tmp);

        if (!csharp_dll_path.empty())
            il2cpp::vm::MetadataCache::LoadAssemblyFromFile(csharp_dll_path.c_str());
    }

    const Il2CppAssembly *assembly = il2cpp_domain_assembly_open(il2cpp_domain_get(), "CSharp.dll");
    const Il2CppImage *image = il2cpp_assembly_get_image(assembly);

    Il2CppClass *clazz = il2cpp_class_from_name(image, "CSharp", "Main");
    const MethodInfo *method = il2cpp_class_get_method_from_name(clazz, "Entry", 0);
    Il2CppException* exception = nullptr;

    il2cpp_runtime_invoke(method, nullptr, nullptr, &exception);

    if (exception != nullptr) {
        std::cout << il2cpp::utils::Exception::FormatException(exception) << std::endl;
        exit(1);
    }
    return 0;
}

#endif
// ===}} huatuo
```

3. 优先热更dll
```
const Il2CppAssembly* il2cpp::vm::MetadataCache::GetOrLoadAssemblyByName(const char* assemblyNameOrPath, bool tryLoad)
{
    const char* assemblyName = huatuo::GetAssemblyNameFromPath(assemblyNameOrPath);

    il2cpp::utils::VmStringUtils::CaseInsensitiveComparer comparer;

    il2cpp::os::FastAutoLock lock(&il2cpp::vm::g_MetadataLock);

    for (auto assembly : s_cliAssemblies)
    {
        if (comparer(assembly->aname.name, assemblyName) || comparer(assembly->image->name, assemblyName))
            return assembly;
    }

    for (int i = 0; i < s_AssembliesCount; i++)
    {
        const Il2CppAssembly* assembly = s_AssembliesTable + i;

        if (comparer(assembly->aname.name, assemblyName) || comparer(assembly->image->name, assemblyName))
            return assembly;
    }

    if (tryLoad)
    {
        Il2CppAssembly* newAssembly = huatuo::metadata::Assembly::LoadFromFile(assemblyNameOrPath);
        if (newAssembly)
        {
            il2cpp::vm::Assembly::Register(newAssembly);
            s_cliAssemblies.push_back(newAssembly);
            return newAssembly;
        }
    }

    return nullptr;
}
```

```
git clone https://github.com/eelgame/il2cpp.git
cd il2cpp
cp -r $UNITY_PATH/2020.3.7f1/Editor/Data/il2cpp ./
```
```
热更dll路径：CSharp/CSharp/bin/Debug/CSharp.dll
```
```
run_test.sh #构建并运行。执行CSharp.Main.Entry方法
il2cpp_run_aot.sh #aot模式运行
il2cpp_run.sh #huatuo模式运行
```
