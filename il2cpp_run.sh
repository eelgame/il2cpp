export CSHARP_DLL_PATH=`pwd`/CSharp/CSharp/bin/Debug/CSharp.dll
cd output/GameAssembly-CSharp-x64/
rundll32.exe GameAssembly-CSharp-x64.dll,il2cpp_test
read -s -n 1 -p "Press any key to continue . . ."
echo ""