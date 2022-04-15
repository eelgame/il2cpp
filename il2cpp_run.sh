cd output/GameAssembly-CSharp-x64/
export CSHARP_DLL_PATH=$(dirname $0)/CSharp/CSharp/bin/Debug/CSharp.dll
rundll32.exe GameAssembly-CSharp-x64.dll,il2cpp_test
read -s -n 1 -p "Press any key to continue . . ."
echo ""