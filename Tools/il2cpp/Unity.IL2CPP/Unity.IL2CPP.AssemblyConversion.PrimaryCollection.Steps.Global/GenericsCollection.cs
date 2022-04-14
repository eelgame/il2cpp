using System;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Generics;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.GenericsCollection;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.AssemblyConversion.PrimaryCollection.Steps.Global
{
	public class GenericsCollection : GlobalScheduledStepFunc<GlobalPrimaryCollectionContext, object, ReadOnlyInflatedCollectionCollector>
	{
		private readonly bool _includeGenerics;

		protected override string Name => "Generics Collection";

		public GenericsCollection(bool includeGenerics)
		{
			_includeGenerics = includeGenerics;
		}

		protected override bool Skip(GlobalPrimaryCollectionContext context)
		{
			return !_includeGenerics;
		}

		protected override ReadOnlyInflatedCollectionCollector ProcessAllItems(GlobalPrimaryCollectionContext context, ReadOnlyCollection<AssemblyDefinition> items)
		{
			PrimaryCollectionContext context2 = context.CreateCollectionContext();
			InflatedCollectionCollector inflatedCollectionCollector;
			using (MiniProfiler.Section("GenericsCollector.Collect"))
			{
				inflatedCollectionCollector = GenericsCollector.Collect(context2, items);
			}
			using (MiniProfiler.Section("CollectGenericVirtualMethods.Collect"))
			{
				CollectGenericVirtualMethods(context2, inflatedCollectionCollector, items);
			}
			using (MiniProfiler.Section("AddExtraTypes"))
			{
				AddExtraTypes(context2, inflatedCollectionCollector, items);
			}
			ReadOnlyInflatedCollectionCollector readOnlyInflatedCollectionCollector = inflatedCollectionCollector.AsReadOnly();
			new WindowsRuntimeDataCollectionForGenerics(readOnlyInflatedCollectionCollector).Run(context);
			new CCWMarshallingFunctionsCollectionForGenerics(readOnlyInflatedCollectionCollector).Run(context);
			return readOnlyInflatedCollectionCollector;
		}

		protected override void ProcessItem(GlobalPrimaryCollectionContext context, AssemblyDefinition item, object globalState)
		{
			throw new NotSupportedException();
		}

		protected override ReadOnlyInflatedCollectionCollector CreateEmptyResult()
		{
			return new InflatedCollectionCollector().AsReadOnly();
		}

		protected override object CreateGlobalState(GlobalPrimaryCollectionContext context)
		{
			throw new NotSupportedException();
		}

		protected override ReadOnlyInflatedCollectionCollector GetResults(GlobalPrimaryCollectionContext context, object globalState)
		{
			throw new NotSupportedException();
		}

		private static void CollectGenericVirtualMethods(PrimaryCollectionContext context, InflatedCollectionCollector allGenerics, ReadOnlyCollection<AssemblyDefinition> assembliesOrderedByDependency)
		{
			GenericVirtualMethodCollector genericVirtualMethodCollector = new GenericVirtualMethodCollector();
			TypeDefinition[] types = assembliesOrderedByDependency.SelectMany((AssemblyDefinition a) => a.MainModule.GetAllTypes()).ToArray();
			genericVirtualMethodCollector.Collect(context, allGenerics, types, context.Global.Collectors.VTable);
		}

		private static void AddExtraTypes(PrimaryCollectionContext context, InflatedCollectionCollector genericsCollectionCollector, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			ExtraTypesSupport extraTypesSupport = new ExtraTypesSupport(context, genericsCollectionCollector, assemblies);
			foreach (string item in ExtraTypesSupport.BuildExtraTypesList(context.Global.InputData.ExtraTypesFiles))
			{
				TypeNameParseInfo typeNameParseInfo = TypeNameParser.Parse(item);
				if (typeNameParseInfo == null)
				{
					ConsoleOutput.Info.WriteLine("WARNING: Cannot parse type name {0} from the extra types list. Skipping.", item);
				}
				else if (!extraTypesSupport.AddType(typeNameParseInfo))
				{
					ConsoleOutput.Info.WriteLine("WARNING: Cannot add extra type {0}. Skipping.", item);
				}
			}
		}
	}
}
