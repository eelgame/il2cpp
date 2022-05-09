## 使用方法
1. 拷贝huatuo-preprocess.exe、huatuo-preprocess.dll、huatuo-il2cpp.exe、huatuo-il2cpp.dll到2020.3.33f1c2\Editor\Data\il2cpp\build\deploy\netcoreapp3.1
2. 执行huatuo-preprocess.exe，执行一次就行，后面不需要再执行
3. .\huatuo-il2cpp.exe --assembly=C:\il2cpp\output\CSharp-stripped\CSharp.dll
## huatuo-preprocess.exe 通过cecil暴露il2pp的私有接口
## huatuo-il2cpp.exe 借助il2cpp扫描泛型
