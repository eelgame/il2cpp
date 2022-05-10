# 泛型扫描
## 使用方法
1. 拷贝huatuo-preprocess.exe、huatuo-preprocess.dll、huatuo-il2cpp.exe、huatuo-il2cpp.dll到2020.3.33f1c2\Editor\Data\il2cpp\build\deploy\netcoreapp3.1
2. 执行huatuo-preprocess.exe，执行一次就行，后面不需要再执行，最好管理员权限执行，否则可能导致写文件失败
3. .\huatuo-il2cpp.exe --assembly=C:\il2cpp\output\CSharp-stripped\CSharp.dll
## huatuo-preprocess.exe 通过cecil暴露il2cpp的私有接口
## huatuo-il2cpp.exe 借助il2cpp扫描泛型

![0J2)C4XSIQ 1NB5CWL_9V0L](https://user-images.githubusercontent.com/49626119/167367935-1c98f649-3b33-4b09-98ca-424e4396362d.png)
