.\il2cpp\build\deploy\netcoreapp3.1\UnityLinker.exe --out=./output/CSharp-stripped --i18n=none --core-action=link --include-assembly=./dll/CSharp.dll,./dll/mscorlib.dll --dotnetruntime=il2cpp --dotnetprofile=unityaot --use-editor-options

.\il2cpp\build\deploy\netcoreapp3.1\il2cpp.exe --convert-to-cpp -emit-null-checks --enable-array-bounds-check --dotnetprofile="unityaot" --generatedcppdir=./output/CSharp --assembly=./output/CSharp-stripped/CSharp.dll --copy-level=None

.\il2cpp\build\deploy\netcoreapp3.1\il2cpp.exe --compile-cpp --libil2cpp-static --configuration=Release --forcerebuild --generate-cmake=il2cpp --dotnetprofile="unityaot" --baselib-directory=%~dp0\windowsstandalonesupport\Variations\win64_nondevelopment_il2cpp --platform=WindowsDesktop --architecture=x64 --outputpath=./output/GameAssembly-CSharp-x64/GameAssembly-CSharp-x64.dll --generatedcppdir=./output/CSharp --cachedirectory=./output/GameAssembly-CSharp-x64/cache
pause