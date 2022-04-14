using System;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal class CommandCCWWriter : IProjectedComCallableWrapperMethodWriter
	{
		private readonly MethodDefinition _iCommandCanExecuteClr;

		private readonly MethodDefinition _iCommandExecuteClr;

		private readonly MethodDefinition _icommandEventHelperSubscribeToCanExecuteChanged;

		private readonly MethodDefinition _icommandEventHelperUnsubscribeFromCanExecuteChanged;

		public CommandCCWWriter(ReadOnlyContext context, TypeDefinition iCommandClr)
		{
			_iCommandCanExecuteClr = iCommandClr.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "CanExecute");
			_iCommandExecuteClr = iCommandClr.Methods.SingleOrDefault((MethodDefinition m) => m.Name == "Execute");
			TypeDefinition typeDefinition = context.Global.Services.TypeProvider.OptionalResolve("Windows.UI.Xaml.Input", "ICommandEventHelper", new AssemblyNameReference("System.Runtime.WindowsRuntime.UI.Xaml", new Version(4, 0, 0, 0)))?.Resolve();
			if (typeDefinition == null)
			{
				ThrowCannotImplementMethodException("Windows.UI.Xaml.Input.ICommandEventHelper");
			}
			_icommandEventHelperSubscribeToCanExecuteChanged = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "SubscribeToCanExecuteChanged");
			if (_icommandEventHelperSubscribeToCanExecuteChanged == null)
			{
				ThrowCannotImplementMethodException("Windows.UI.Xaml.Input.ICommandEventHelper.SubscribeToCanExecuteChanged");
			}
			_icommandEventHelperUnsubscribeFromCanExecuteChanged = typeDefinition.Methods.Single((MethodDefinition m) => m.Name == "UnsubscribeFromCanExecuteChanged");
			if (_icommandEventHelperUnsubscribeFromCanExecuteChanged == null)
			{
				ThrowCannotImplementMethodException("Windows.UI.Xaml.Input.ICommandEventHelper.UnsubscribeFromCanExecuteChanged");
			}
		}

		private void ThrowCannotImplementMethodException(string missingThing)
		{
			throw new InvalidProgramException("Unable to implement Windows.UI.Xaml.Input.ICommand to System.Windows.Input.ICommand projection because " + missingThing + " was stripped. This indicates a bug in UnityLinker.");
		}

		private void ThrowCannotImplementMethodException(string methodName, string missingThing)
		{
			throw new InvalidProgramException("Unable to implement Windows.UI.Xaml.Input.ICommand." + methodName + " to System.Windows.Input.ICommand projection because " + missingThing + " was stripped. This indicates a bug in UnityLinker.");
		}

		public ComCallableWrapperMethodBodyWriter GetBodyWriter(SourceWritingContext context, MethodReference method)
		{
			switch (method.Name)
			{
			case "add_CanExecuteChanged":
				return new AddOrRemoveCanExecuteChangedMethodBodyWriter(context, method, _icommandEventHelperSubscribeToCanExecuteChanged);
			case "CanExecute":
				if (_iCommandCanExecuteClr != null)
				{
					return new ProjectedMethodBodyWriter(context, _iCommandCanExecuteClr, method);
				}
				ThrowCannotImplementMethodException("CanExecute", "System.Windows.Input.ICommand.CanExecute");
				return null;
			case "Execute":
				if (_iCommandExecuteClr != null)
				{
					return new ProjectedMethodBodyWriter(context, _iCommandExecuteClr, method);
				}
				ThrowCannotImplementMethodException("Execute", "System.Windows.Input.ICommand.Execute");
				return null;
			case "remove_CanExecuteChanged":
				return new AddOrRemoveCanExecuteChangedMethodBodyWriter(context, method, _icommandEventHelperUnsubscribeFromCanExecuteChanged);
			default:
				throw new NotSupportedException("CommandCCWWriter does not support writing method body for " + method.FullName + ".");
			}
		}

		public void WriteDependenciesFor(SourceWritingContext context, IGeneratedMethodCodeWriter writer, TypeReference interfaceType)
		{
		}
	}
}
