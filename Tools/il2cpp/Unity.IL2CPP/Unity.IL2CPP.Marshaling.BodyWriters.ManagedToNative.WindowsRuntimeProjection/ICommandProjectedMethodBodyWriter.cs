using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Marshaling.BodyWriters.ManagedToNative.WindowsRuntimeProjection
{
	internal class ICommandProjectedMethodBodyWriter
	{
		private readonly MinimalContext _context;

		private readonly TypeDefinition _winrtICommand;

		private readonly TypeDefinition _systemFunc2;

		private readonly MethodDefinition _systemFunc2Ctor;

		private readonly TypeDefinition _systemAction1;

		private readonly MethodDefinition _systemAction1Ctor;

		private readonly GenericInstanceType _systemEventHandler1Instance;

		private readonly TypeDefinition _eventRegistrationToken;

		private readonly MethodDefinition _icommandEventHelperAddDelegateConverter;

		private readonly MethodDefinition _icommandEventHelperRemoveDelegateConverter;

		private readonly MethodDefinition _windowsRuntimeMarshalAddEventHandler;

		private readonly MethodDefinition _windowsRuntimeMarshalRemoveEventHandler;

		public ICommandProjectedMethodBodyWriter(MinimalContext context, TypeDefinition winrtICommand)
		{
			_context = context;
			_winrtICommand = winrtICommand;
			_systemFunc2 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "Func`2")?.Resolve();
			if (_systemFunc2 == null)
			{
				ThrowCannotImplementMethodException("System.Func`2");
			}
			_systemFunc2Ctor = _systemFunc2.Methods.SingleOrDefault((MethodDefinition m) => m.IsConstructor);
			if (_systemFunc2Ctor == null)
			{
				ThrowCannotImplementMethodException("System.Func`2 constructor");
			}
			_systemAction1 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "Action`1")?.Resolve();
			if (_systemAction1 == null)
			{
				ThrowCannotImplementMethodException("System.Action`1");
			}
			_systemAction1Ctor = _systemAction1.Methods.SingleOrDefault((MethodDefinition m) => m.IsConstructor);
			if (_systemAction1Ctor == null)
			{
				ThrowCannotImplementMethodException("System.Action`1 constructor");
			}
			TypeDefinition typeDefinition = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System", "EventHandler`1")?.Resolve();
			if (typeDefinition == null)
			{
				ThrowCannotImplementMethodException("System.EventHandler`1");
			}
			_systemEventHandler1Instance = new GenericInstanceType(typeDefinition);
			_systemEventHandler1Instance.GenericArguments.Add(context.Global.Services.TypeProvider.SystemObject);
			_eventRegistrationToken = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Runtime.InteropServices.WindowsRuntime", "EventRegistrationToken")?.Resolve();
			if (_eventRegistrationToken == null)
			{
				ThrowCannotImplementMethodException("System.Runtime.InteropServices.WindowsRuntime.EventRegistrationToken");
			}
			TypeDefinition typeDefinition2 = context.Global.Services.TypeProvider.OptionalResolve("Windows.UI.Xaml.Input", "ICommandEventHelper", new AssemblyNameReference("System.Runtime.WindowsRuntime.UI.Xaml", new Version(4, 0, 0, 0)))?.Resolve();
			if (typeDefinition2 == null)
			{
				ThrowCannotImplementMethodException("Windows.UI.Xaml.Input.ICommandEventHelper");
			}
			_icommandEventHelperAddDelegateConverter = typeDefinition2.Methods.Single((MethodDefinition m) => m.Name == "GetGenericEventHandlerForNonGenericEventHandlerForSubscribing");
			if (_icommandEventHelperAddDelegateConverter == null)
			{
				ThrowCannotImplementMethodException("Windows.UI.Xaml.Input.ICommandEventHelper.GetGenericEventHandlerForNonGenericEventHandlerForSubscribing");
			}
			_icommandEventHelperRemoveDelegateConverter = typeDefinition2.Methods.Single((MethodDefinition m) => m.Name == "GetGenericEventHandlerForNonGenericEventHandlerForUnsubscribing");
			if (_icommandEventHelperRemoveDelegateConverter == null)
			{
				ThrowCannotImplementMethodException("Windows.UI.Xaml.Input.ICommandEventHelper.GetGenericEventHandlerForNonGenericEventHandlerForUnsubscribing");
			}
			TypeDefinition typeDefinition3 = context.Global.Services.TypeProvider.OptionalResolveInCoreLibrary("System.Runtime.InteropServices.WindowsRuntime", "WindowsRuntimeMarshal")?.Resolve();
			if (typeDefinition3 == null)
			{
				ThrowCannotImplementMethodException("System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal");
			}
			_windowsRuntimeMarshalAddEventHandler = typeDefinition3.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "AddEventHandler");
			if (_windowsRuntimeMarshalAddEventHandler == null)
			{
				ThrowCannotImplementMethodException("System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.AddEventHandler");
			}
			_windowsRuntimeMarshalRemoveEventHandler = typeDefinition3.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "RemoveEventHandler");
			if (_windowsRuntimeMarshalRemoveEventHandler == null)
			{
				ThrowCannotImplementMethodException("System.Runtime.InteropServices.WindowsRuntime.WindowsRuntimeMarshal.RemoveEventHandler");
			}
		}

		private void ThrowCannotImplementMethodException(string missingThing)
		{
			throw new InvalidProgramException("Unable to implement System.Windows.Input.ICommand projection to Windows.UI.Xaml.Input.ICommand because " + missingThing + " was stripped. This indicates a bug in UnityLinker.");
		}

		public void WriteAddCanExecuteChanged(MethodDefinition method)
		{
			MethodReference method2 = TypeResolver.For(new GenericInstanceType(_systemFunc2)
			{
				GenericArguments = 
				{
					(TypeReference)_systemEventHandler1Instance,
					(TypeReference)_eventRegistrationToken
				}
			}).Resolve(_systemFunc2Ctor);
			MethodReference method3 = TypeResolver.For(new GenericInstanceType(_systemAction1)
			{
				GenericArguments = { (TypeReference)_eventRegistrationToken }
			}).Resolve(_systemAction1Ctor);
			MethodDefinition method4 = _winrtICommand.Methods.Single((MethodDefinition m) => m.Name == "add_CanExecuteChanged");
			MethodDefinition method5 = _winrtICommand.Methods.Single((MethodDefinition m) => m.Name == "remove_CanExecuteChanged");
			GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(_windowsRuntimeMarshalAddEventHandler);
			genericInstanceMethod.GenericArguments.Add(_systemEventHandler1Instance);
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Dup);
			iLProcessor.Emit(OpCodes.Ldvirtftn, method4);
			iLProcessor.Emit(OpCodes.Newobj, method2);
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Dup);
			iLProcessor.Emit(OpCodes.Ldvirtftn, method5);
			iLProcessor.Emit(OpCodes.Newobj, method3);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Call, _icommandEventHelperAddDelegateConverter);
			iLProcessor.Emit(OpCodes.Call, genericInstanceMethod);
			iLProcessor.Emit(OpCodes.Ret);
		}

		public void WriteRemoveCanExecuteChanged(MethodDefinition method)
		{
			MethodReference method2 = TypeResolver.For(new GenericInstanceType(_systemAction1)
			{
				GenericArguments = { (TypeReference)_eventRegistrationToken }
			}).Resolve(_systemAction1Ctor);
			MethodDefinition method3 = _winrtICommand.Methods.Single((MethodDefinition m) => m.Name == "remove_CanExecuteChanged");
			GenericInstanceMethod genericInstanceMethod = new GenericInstanceMethod(_windowsRuntimeMarshalRemoveEventHandler);
			genericInstanceMethod.GenericArguments.Add(_systemEventHandler1Instance);
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Dup);
			iLProcessor.Emit(OpCodes.Ldvirtftn, method3);
			iLProcessor.Emit(OpCodes.Newobj, method2);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Call, _icommandEventHelperRemoveDelegateConverter);
			iLProcessor.Emit(OpCodes.Call, genericInstanceMethod);
			iLProcessor.Emit(OpCodes.Ret);
		}

		public void WriteCanExecute(MethodDefinition method)
		{
			ForwardToMethodWithOneArg(method, "CanExecute");
		}

		public void WriteExecute(MethodDefinition method)
		{
			ForwardToMethodWithOneArg(method, "Execute");
		}

		private void ForwardToMethodWithOneArg(MethodDefinition method, string methodName)
		{
			ILProcessor iLProcessor = method.Body.GetILProcessor();
			iLProcessor.Emit(OpCodes.Ldarg_0);
			iLProcessor.Emit(OpCodes.Ldarg_1);
			iLProcessor.Emit(OpCodes.Callvirt, _winrtICommand.Methods.Single((MethodDefinition m) => m.Name == methodName));
			iLProcessor.Emit(OpCodes.Ret);
		}
	}
}
