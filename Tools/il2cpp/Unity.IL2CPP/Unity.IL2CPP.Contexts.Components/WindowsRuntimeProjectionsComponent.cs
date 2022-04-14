using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection;
using Unity.IL2CPP.WindowsRuntime;

namespace Unity.IL2CPP.Contexts.Components
{
	public class WindowsRuntimeProjectionsComponent : ServiceComponentBase<IWindowsRuntimeProjections, WindowsRuntimeProjectionsComponent>, IWindowsRuntimeProjections
	{
		private readonly Dictionary<TypeDefinition, TypeDefinition> _clrTypeToWindowsRuntimeTypeMap;

		private readonly Dictionary<TypeDefinition, TypeDefinition> _windowsRuntimeTypeToCLRTypeMap;

		private readonly Dictionary<TypeDefinition, IProjectedComCallableWrapperMethodWriter> _projectedComCallableWrapperWriterMap;

		private Dictionary<TypeDefinition, TypeDefinition> _nativeToManagedInterfaceAdapterClasses;

		private readonly AssemblyNameReference _windowsAssemblyReference;

		private ModuleDefinition _mscorlib;

		public WindowsRuntimeProjectionsComponent()
		{
			_clrTypeToWindowsRuntimeTypeMap = new Dictionary<TypeDefinition, TypeDefinition>();
			_windowsRuntimeTypeToCLRTypeMap = new Dictionary<TypeDefinition, TypeDefinition>();
			_projectedComCallableWrapperWriterMap = new Dictionary<TypeDefinition, IProjectedComCallableWrapperMethodWriter>();
			_nativeToManagedInterfaceAdapterClasses = new Dictionary<TypeDefinition, TypeDefinition>();
			_windowsAssemblyReference = new AssemblyNameReference("Windows", new Version(255, 255, 255, 255))
			{
				IsWindowsRuntime = true
			};
		}

		public void Initialize(PrimaryCollectionContext context)
		{
			_mscorlib = context.Global.Services.TypeProvider.Corlib.MainModule;
			if (!context.Global.InputData.Profile.SupportsWindowsRuntime)
			{
				return;
			}
			Dictionary<TypeDefinition, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>> dictionary = new Dictionary<TypeDefinition, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>>();
			AddProjection("System.ObjectModel", "System.Collections.Specialized", "INotifyCollectionChanged", "Windows.UI.Xaml.Interop", "INotifyCollectionChanged", out var clrType, out var windowsRuntimeType);
			AddProjection("System.ObjectModel", "System.Collections.Specialized", "NotifyCollectionChangedAction", "Windows.UI.Xaml.Interop", "NotifyCollectionChangedAction", out clrType, out windowsRuntimeType);
			AddProjection("System.ObjectModel", "System.Collections.Specialized", "NotifyCollectionChangedEventArgs", "Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventArgs", out clrType, out windowsRuntimeType);
			AddProjection("System.ObjectModel", "System.Collections.Specialized", "NotifyCollectionChangedEventHandler", "Windows.UI.Xaml.Interop", "NotifyCollectionChangedEventHandler", out clrType, out windowsRuntimeType);
			AddProjection("System.ObjectModel", "System.ComponentModel", "INotifyPropertyChanged", "Windows.UI.Xaml.Data", "INotifyPropertyChanged", out clrType, out windowsRuntimeType);
			AddProjection("System.ObjectModel", "System.ComponentModel", "PropertyChangedEventArgs", "Windows.UI.Xaml.Data", "PropertyChangedEventArgs", out clrType, out windowsRuntimeType);
			AddProjection("System.ObjectModel", "System.ComponentModel", "PropertyChangedEventHandler", "Windows.UI.Xaml.Data", "PropertyChangedEventHandler", out clrType, out windowsRuntimeType);
			if (AddProjection("System.ObjectModel", "System.Windows.Input", "ICommand", "Windows.UI.Xaml.Input", "ICommand", out clrType, out windowsRuntimeType))
			{
				ICommandProjectedMethodBodyWriter @object = new ICommandProjectedMethodBodyWriter(context, windowsRuntimeType);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "ICommand", "add_CanExecuteChanged", @object.WriteAddCanExecuteChanged);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "ICommand", "remove_CanExecuteChanged", @object.WriteRemoveCanExecuteChanged);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "ICommand", "CanExecute", @object.WriteCanExecute);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "ICommand", "Execute", @object.WriteExecute);
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new CommandCCWWriter(context, clrType));
			}
			AddProjection("System.Runtime", "System", "AttributeTargets", "Windows.Foundation.Metadata", "AttributeTargets", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime", "System", "AttributeUsageAttribute", "Windows.Foundation.Metadata", "AttributeUsageAttribute", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime", "System", "DateTimeOffset", "Windows.Foundation", "DateTime", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime", "System", "EventHandler`1", "Windows.Foundation", "EventHandler`1", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime", "System", "Exception", "Windows.Foundation", "HResult", out clrType, out windowsRuntimeType);
			if (AddProjection("System.Runtime", "System", "IDisposable", "Windows.Foundation", "IClosable", out clrType, out windowsRuntimeType))
			{
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IDisposable", "Dispose", new IDisposableDisposeMethodBodyWriter(windowsRuntimeType.Methods.Single((MethodDefinition m) => m.Name == "Close")).WriteDispose);
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new DisposableCCWWriter());
			}
			AddProjection("System.Runtime", "System", "Nullable`1", "Windows.Foundation", "IReference`1", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime", "System", "TimeSpan", "Windows.Foundation", "TimeSpan", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime", "System", "Type", "Windows.UI.Xaml.Interop", "TypeName", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime", "System", "Uri", "Windows.Foundation", "Uri", out clrType, out windowsRuntimeType);
			TypeDefinition typeDefinition = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections", "IEnumerator");
			TypeDefinition typeDefinition2 = context.Global.Services.TypeProvider.OptionalResolve("Windows.UI.Xaml.Interop", "IBindableIterator", _windowsAssemblyReference);
			if (typeDefinition != null && typeDefinition2 != null && AddProjection("System.Runtime", "System.Collections", "IEnumerable", "Windows.UI.Xaml.Interop", "IBindableIterable", out clrType, out windowsRuntimeType))
			{
				TypeDefinition iteratorToEnumeratorAdapter = new IteratorToEnumeratorAdapterTypeGenerator(context, context.Global.Services.TypeProvider.Corlib.MainModule, typeDefinition2, typeDefinition).Generate();
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IEnumerable", "GetEnumerator", new IEnumerableMethodBodyWriter(iteratorToEnumeratorAdapter, windowsRuntimeType).WriteGetEnumerator);
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new EnumerableCCWWriter());
			}
			TypeDefinition typeDefinition3 = null;
			TypeDefinition typeDefinition4 = null;
			TypeDefinition typeDefinition5 = null;
			TypeDefinition typeDefinition6 = null;
			if (AddProjection("System.Runtime", "System.Collections", "IList", "Windows.UI.Xaml.Interop", "IBindableVector", out clrType, out windowsRuntimeType))
			{
				TypeDefinition typeDefinition7 = clrType.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "ICollection").InterfaceType.Resolve();
				IListProjectedMethodBodyWriter object2 = new IListProjectedMethodBodyWriter(context, windowsRuntimeType);
				ICollectionProjectedMethodBodyWriter object3 = new ICollectionProjectedMethodBodyWriter(context, typeDefinition7, null, null, windowsRuntimeType);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "get_Item", object2.WriteGetItem);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "IndexOf", object2.WriteIndexOf);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "Insert", object2.WriteInsert);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "RemoveAt", object2.WriteRemoveAt);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "set_Item", object2.WriteSetItem);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "Add", object3.WriteAdd);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "Clear", object3.WriteClear);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "Contains", object3.WriteContains);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "get_IsFixedSize", object3.WriteGetIsFixedSize);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "get_IsReadOnly", object3.WriteGetIsReadOnly);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList", "Remove", object3.WriteRemove);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition7, "ICollection", "CopyTo", object3.WriteCopyTo);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition7, "ICollection", "get_Count", object3.WriteGetCount);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition7, "ICollection", "get_IsSynchronized", object3.WriteGetIsSynchronized);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition7, "ICollection", "get_SyncRoot", object3.WriteGetSyncRoot);
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new ListCCWWriter(clrType));
			}
			TypeDefinition typeDefinition8 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Collections.Generic", "IEnumerator`1");
			TypeDefinition typeDefinition9 = context.Global.Services.TypeProvider.OptionalResolve("Windows.Foundation.Collections", "IIterator`1", _windowsAssemblyReference);
			TypeDefinition typeDefinition10 = null;
			if (typeDefinition8 != null && typeDefinition9 != null && AddProjection("System.Runtime", "System.Collections.Generic", "IEnumerable`1", "Windows.Foundation.Collections", "IIterable`1", out clrType, out windowsRuntimeType))
			{
				typeDefinition10 = windowsRuntimeType;
				TypeDefinition iteratorToEnumeratorAdapter2 = new IteratorToEnumeratorAdapterTypeGenerator(context, context.Global.Services.TypeProvider.Corlib.MainModule, typeDefinition9, typeDefinition8).Generate();
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IEnumerable`1", "GetEnumerator", new IEnumerableMethodBodyWriter(iteratorToEnumeratorAdapter2, typeDefinition10).WriteGetEnumerator);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IEnumerable", "GetEnumerator", new IEnumerableMethodBodyWriter(iteratorToEnumeratorAdapter2, typeDefinition10).WriteGetEnumerator);
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new EnumerableCCWWriter());
			}
			if (AddProjection("System.Runtime", "System.Collections.Generic", "IDictionary`2", "Windows.Foundation.Collections", "IMap`2", out clrType, out windowsRuntimeType))
			{
				IDictionaryProjectedMethodBodyWriter object4 = new IDictionaryProjectedMethodBodyWriter(context, clrType, windowsRuntimeType);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IDictionary`2", "get_Item", object4.WriteGetItem);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IDictionary`2", "get_Keys", object4.WriteGetKeys);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IDictionary`2", "get_Values", object4.WriteGetValues);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IDictionary`2", "Add", object4.WriteAdd);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IDictionary`2", "ContainsKey", object4.WriteContainsKey);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IDictionary`2", "Remove", object4.WriteRemove);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IDictionary`2", "set_Item", object4.WriteSetItem);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IDictionary`2", "TryGetValue", object4.WriteTryGetValue);
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new DictionaryCCWWriter(clrType));
				typeDefinition5 = clrType;
				typeDefinition6 = windowsRuntimeType;
			}
			if (AddProjection("System.Runtime", "System.Collections.Generic", "IList`1", "Windows.Foundation.Collections", "IVector`1", out clrType, out windowsRuntimeType))
			{
				IListProjectedMethodBodyWriter object5 = new IListProjectedMethodBodyWriter(context, windowsRuntimeType);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList`1", "get_Item", object5.WriteGetItem);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList`1", "IndexOf", object5.WriteIndexOf);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList`1", "Insert", object5.WriteInsert);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList`1", "RemoveAt", object5.WriteRemoveAt);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IList`1", "set_Item", object5.WriteSetItem);
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new ListCCWWriter(clrType));
				typeDefinition3 = clrType;
				typeDefinition4 = windowsRuntimeType;
			}
			TypeDefinition typeDefinition11 = null;
			TypeDefinition typeDefinition12 = null;
			TypeDefinition typeDefinition13 = null;
			TypeDefinition typeDefinition14 = null;
			if (AddProjection("System.Runtime", "System.Collections.Generic", "IReadOnlyDictionary`2", "Windows.Foundation.Collections", "IMapView`2", out clrType, out windowsRuntimeType))
			{
				IDictionaryProjectedMethodBodyWriter object6 = new IDictionaryProjectedMethodBodyWriter(context, clrType, windowsRuntimeType);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IReadOnlyDictionary`2", "get_Item", object6.WriteGetItem);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IReadOnlyDictionary`2", "get_Keys", object6.WriteGetKeys);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IReadOnlyDictionary`2", "get_Values", object6.WriteGetValues);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IReadOnlyDictionary`2", "ContainsKey", object6.WriteContainsKey);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IReadOnlyDictionary`2", "TryGetValue", object6.WriteTryGetValue);
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new DictionaryCCWWriter(clrType));
				typeDefinition11 = clrType;
				typeDefinition12 = windowsRuntimeType;
			}
			if (AddProjection("System.Runtime", "System.Collections.Generic", "IReadOnlyList`1", "Windows.Foundation.Collections", "IVectorView`1", out clrType, out windowsRuntimeType))
			{
				IListProjectedMethodBodyWriter object7 = new IListProjectedMethodBodyWriter(context, windowsRuntimeType);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, clrType, "IReadOnlyList`1", "get_Item", object7.WriteGetItem);
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new ListCCWWriter(clrType));
				typeDefinition13 = clrType;
				typeDefinition14 = windowsRuntimeType;
			}
			if (typeDefinition4 != null || typeDefinition6 != null)
			{
				TypeDefinition typeDefinition15 = ((typeDefinition3 != null) ? typeDefinition3.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "ICollection`1").InterfaceType.Resolve() : typeDefinition5.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "ICollection`1").InterfaceType.Resolve());
				ICollectionProjectedMethodBodyWriter object8 = new ICollectionProjectedMethodBodyWriter(context, typeDefinition15, typeDefinition5, typeDefinition6, typeDefinition4);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition15, "ICollection`1", "Add", object8.WriteAdd);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition15, "ICollection`1", "Clear", object8.WriteClear);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition15, "ICollection`1", "Contains", object8.WriteContains);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition15, "ICollection`1", "CopyTo", object8.WriteCopyTo);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition15, "ICollection`1", "get_Count", object8.WriteGetCount);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition15, "ICollection`1", "get_IsReadOnly", object8.WriteGetIsReadOnly);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition15, "ICollection`1", "Remove", object8.WriteRemove);
			}
			if (typeDefinition14 != null || typeDefinition12 != null)
			{
				TypeDefinition typeDefinition16 = ((typeDefinition13 != null) ? typeDefinition13.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "IReadOnlyCollection`1").InterfaceType.Resolve() : typeDefinition11.Interfaces.Single((InterfaceImplementation i) => i.InterfaceType.Name == "IReadOnlyCollection`1").InterfaceType.Resolve());
				ICollectionProjectedMethodBodyWriter object9 = new ICollectionProjectedMethodBodyWriter(context, typeDefinition16, typeDefinition16, typeDefinition12, typeDefinition14);
				AddInterfaceAdapterMethodBodyWriter(context, dictionary, typeDefinition16, "IReadOnlyCollection`1", "get_Count", object9.WriteGetCount);
			}
			if (AddProjection("System.Runtime", "System.Collections.Generic", "KeyValuePair`2", "Windows.Foundation.Collections", "IKeyValuePair`2", out clrType, out windowsRuntimeType))
			{
				_projectedComCallableWrapperWriterMap.Add(windowsRuntimeType, new KeyValuePairCCWWriter(clrType));
			}
			AddProjection("System.Runtime.InteropServices.WindowsRuntime", "System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken", "Windows.Foundation", "EventRegistrationToken", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime", "Windows.Foundation", "Point", "Windows.Foundation", "Point", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime", "Windows.Foundation", "Rect", "Windows.Foundation", "Rect", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime", "Windows.Foundation", "Size", "Windows.Foundation", "Size", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime", "Windows.UI", "Color", "Windows.UI", "Color", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "CornerRadius", "Windows.UI.Xaml", "CornerRadius", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "Duration", "Windows.UI.Xaml", "Duration", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "DurationType", "Windows.UI.Xaml", "DurationType", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "GridLength", "Windows.UI.Xaml", "GridLength", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "GridUnitType", "Windows.UI.Xaml", "GridUnitType", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml", "Thickness", "Windows.UI.Xaml", "Thickness", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition", "Windows.UI.Xaml.Controls.Primitives", "GeneratorPosition", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media", "Matrix", "Windows.UI.Xaml.Media", "Matrix", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior", "Windows.UI.Xaml.Media.Animation", "RepeatBehavior", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media.Animation", "RepeatBehaviorType", "Windows.UI.Xaml.Media.Animation", "RepeatBehaviorType", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media.Animation", "KeyTime", "Windows.UI.Xaml.Media.Animation", "KeyTime", out clrType, out windowsRuntimeType);
			AddProjection("System.Runtime.WindowsRuntime.UI.Xaml", "Windows.UI.Xaml.Media.Media3D", "Matrix3D", "Windows.UI.Xaml.Media.Media3D", "Matrix3D", out clrType, out windowsRuntimeType);
			AddProjection("System.Numerics.Vectors", "System.Numerics", "Matrix3x2", "Windows.Foundation.Numerics", "Matrix3x2", out clrType, out windowsRuntimeType);
			AddProjection("System.Numerics.Vectors", "System.Numerics", "Matrix4x4", "Windows.Foundation.Numerics", "Matrix4x4", out clrType, out windowsRuntimeType);
			AddProjection("System.Numerics.Vectors", "System.Numerics", "Plane", "Windows.Foundation.Numerics", "Plane", out clrType, out windowsRuntimeType);
			AddProjection("System.Numerics.Vectors", "System.Numerics", "Quaternion", "Windows.Foundation.Numerics", "Quaternion", out clrType, out windowsRuntimeType);
			AddProjection("System.Numerics.Vectors", "System.Numerics", "Vector2", "Windows.Foundation.Numerics", "Vector2", out clrType, out windowsRuntimeType);
			AddProjection("System.Numerics.Vectors", "System.Numerics", "Vector3", "Windows.Foundation.Numerics", "Vector3", out clrType, out windowsRuntimeType);
			AddProjection("System.Numerics.Vectors", "System.Numerics", "Vector4", "Windows.Foundation.Numerics", "Vector4", out clrType, out windowsRuntimeType);
			_nativeToManagedInterfaceAdapterClasses = InterfaceNativeToManagedAdapterGenerator.Generate(context, _clrTypeToWindowsRuntimeTypeMap, dictionary);
		}

		private void AddInterfaceAdapterMethodBodyWriter(ReadOnlyContext context, Dictionary<TypeDefinition, Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>> interfaceAdapterMethodBodyWriters, TypeDefinition clrType, string clrDeclaringTypeName, string clrMethodName, InterfaceAdapterMethodBodyWriter methodWriter)
		{
			MethodDefinition methodDefinition = new TypeDefinition[1] { clrType }.Union(from i in clrType.GetInterfaces(context)
				select i.Resolve()).SelectMany((TypeDefinition t) => t.Methods).SingleOrDefault((MethodDefinition m) => m.DeclaringType.Name == clrDeclaringTypeName && m.Name == clrMethodName);
			if (methodDefinition != null)
			{
				if (!interfaceAdapterMethodBodyWriters.ContainsKey(clrType))
				{
					interfaceAdapterMethodBodyWriters[clrType] = new Dictionary<MethodDefinition, InterfaceAdapterMethodBodyWriter>();
				}
				interfaceAdapterMethodBodyWriters[clrType].Add(methodDefinition, methodWriter);
			}
		}

		private bool AddProjection(string clrAssembly, string clrNamespace, string clrName, string windowsRuntimeNamespace, string windowsRuntimeName, out TypeDefinition clrType, out TypeDefinition windowsRuntimeType)
		{
			TypeReference typeReference = new TypeReference(clrNamespace, clrName, _mscorlib, new AssemblyNameReference(clrAssembly, new Version(4, 0, 0, 0)));
			TypeReference typeReference2 = new TypeReference(windowsRuntimeNamespace, windowsRuntimeName, _mscorlib, _windowsAssemblyReference);
			try
			{
				clrType = typeReference.Resolve();
				windowsRuntimeType = typeReference2.Resolve();
				if (clrType != null && windowsRuntimeType != null)
				{
					_clrTypeToWindowsRuntimeTypeMap.Add(clrType, windowsRuntimeType);
					_windowsRuntimeTypeToCLRTypeMap.Add(windowsRuntimeType, clrType);
					return windowsRuntimeType.Methods.Any((MethodDefinition m) => !m.IsStripped());
				}
			}
			catch (AssemblyResolutionException)
			{
			}
			clrType = null;
			windowsRuntimeType = null;
			return false;
		}

		public TypeReference ProjectToWindowsRuntime(TypeReference clrType)
		{
			if (clrType is TypeSpecification && !clrType.IsGenericInstance)
			{
				return clrType;
			}
			if (clrType.IsGenericParameter)
			{
				return clrType;
			}
			if (_clrTypeToWindowsRuntimeTypeMap.TryGetValue(clrType.Resolve(), out var value))
			{
				return TypeResolver.For(clrType).Resolve(value);
			}
			return clrType;
		}

		public TypeDefinition ProjectToWindowsRuntime(TypeDefinition clrType)
		{
			if (_clrTypeToWindowsRuntimeTypeMap.TryGetValue(clrType, out var value))
			{
				return value;
			}
			return clrType;
		}

		public TypeReference ProjectToCLR(TypeReference windowsRuntimeType)
		{
			if (windowsRuntimeType is TypeSpecification && !windowsRuntimeType.IsGenericInstance)
			{
				return windowsRuntimeType;
			}
			if (windowsRuntimeType.IsGenericParameter)
			{
				return windowsRuntimeType;
			}
			if (_windowsRuntimeTypeToCLRTypeMap.TryGetValue(windowsRuntimeType.Resolve(), out var value))
			{
				return TypeResolver.For(windowsRuntimeType).Resolve(value);
			}
			return windowsRuntimeType;
		}

		public TypeDefinition ProjectToCLR(TypeDefinition windowsRuntimeType)
		{
			if (_windowsRuntimeTypeToCLRTypeMap.TryGetValue(windowsRuntimeType, out var value))
			{
				return value;
			}
			return windowsRuntimeType;
		}

		public IProjectedComCallableWrapperMethodWriter GetProjectedComCallableWrapperMethodWriterFor(TypeDefinition type)
		{
			_projectedComCallableWrapperWriterMap.TryGetValue(type, out var value);
			return value;
		}

		public TypeDefinition GetNativeToManagedAdapterClassFor(TypeDefinition interfaceType)
		{
			TypeDefinition value = null;
			_nativeToManagedInterfaceAdapterClasses.TryGetValue(interfaceType, out value);
			return value;
		}

		public IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> GetClrToWindowsRuntimeProjectedTypes()
		{
			return _clrTypeToWindowsRuntimeTypeMap;
		}

		public IEnumerable<KeyValuePair<TypeDefinition, TypeDefinition>> GetNativeToManagedInterfaceAdapterClasses()
		{
			return _nativeToManagedInterfaceAdapterClasses;
		}

		protected override WindowsRuntimeProjectionsComponent ThisAsFull()
		{
			return this;
		}

		protected override IWindowsRuntimeProjections ThisAsRead()
		{
			return this;
		}
	}
}
