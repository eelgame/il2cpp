function pause(){
 read -s -n 1 -p "Press any key to continue . . ."
 echo ""
}

echo "UnityLinker"
./il2cpp/build/deploy/netcoreapp3.1/UnityLinker.exe --out=./output/CSharp-stripped --i18n=none --core-action=link --include-assembly=./dll/CSharp.dll,./dll/mscorlib.dll --dotnetruntime=il2cpp --dotnetprofile=unityaot --use-editor-options
echo "il2cpp convert-to-cpp"
./il2cpp/build/deploy/netcoreapp3.1/il2cpp.exe --convert-to-cpp -emit-null-checks --enable-array-bounds-check --dotnetprofile="unityaot" --generatedcppdir=./output/CSharp --assembly=./output/CSharp-stripped/CSharp.dll --copy-level=None
echo "il2cpp compile-cpp"
./il2cpp/build/deploy/netcoreapp3.1/il2cpp.exe --compile-cpp --libil2cpp-static --configuration=Debug --forcerebuild --generate-cmake=il2cpp --dotnetprofile="unityaot" --baselib-directory=`pwd`/windowsstandalonesupport/Variations/win64_development_il2cpp --platform=WindowsDesktop --architecture=x64 --outputpath=./output/GameAssembly-CSharp-x64/GameAssembly-CSharp-x64.dll --generatedcppdir=./output/CSharp --cachedirectory=./output/GameAssembly-CSharp-x64/cache

echo "patch CMakeLists"
sed -i -r 's/\/Yupch\-(c|cpp)\.(h|hpp) \/Fp\\\".*\\\\[a-zA-Z0-9]+\.pch\\\"//g' ./output/CSharp/CMakeLists.txt

pause