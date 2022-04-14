using System;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Phases;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.AssemblyConversion.PerAssembly.Slave
{
	internal class SlaveConverter : BaseAssemblyConverter
	{
		private AssemblyDefinition _primaryAssembly;

		public override void Run(AssemblyConversionContext context)
		{
			InitializePhase.Run(context);
			SetupPhase.Run(context, context.Results.Initialize.AllAssembliesOrderedByDependency, includeWindowsRuntime: false);
			AdditionalSetup(context);
			ReadOnlyCollection<AssemblyDefinition> assemblies = new AssemblyDefinition[1] { _primaryAssembly }.AsReadOnly();
			PrimaryCollectionPhase.Run(context, assemblies, includeGenerics: false);
			PrimaryWritePhase.Run(context, assemblies, (_primaryAssembly == context.Results.Initialize.EntryAssembly) ? _primaryAssembly : null, includeGenerics: false);
		}

		private void AdditionalSetup(AssemblyConversionContext context)
		{
			_primaryAssembly = context.Results.Initialize.AllAssembliesOrderedByDependency.FirstOrDefault((AssemblyDefinition asm) => asm.MainModule.FileName == context.InputDataForTopLevel.SlaveAssembly.ToString());
			if (_primaryAssembly == null)
			{
				throw new InvalidOperationException($"Unable to locate assembly definition for : {context.InputDataForTopLevel.SlaveAssembly}");
			}
		}
	}
}
