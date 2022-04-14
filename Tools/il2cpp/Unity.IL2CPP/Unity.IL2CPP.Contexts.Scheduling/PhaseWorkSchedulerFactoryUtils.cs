using System;
using System.Linq;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Providers;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Contexts.Scheduling
{
	public static class PhaseWorkSchedulerFactoryUtils
	{
		public static ForkedContextScope<int, GlobalWriteContext> ForkForPrimaryWrite(AssemblyConversionContext context, OverrideObjects overrideObjects, int count)
		{
			return context.GlobalWriteContext.ForkFor((IUnrestrictedContextDataProvider parent) => CreateScopeForPrimaryWrite(parent, count, overrideObjects));
		}

		public static ForkedContextScope<int, GlobalPrimaryCollectionContext> ForkForPrimaryCollection(AssemblyConversionContext context, OverrideObjects overrideObjects, int count)
		{
			return context.GlobalPrimaryCollectionContext.ForkFor((IUnrestrictedContextDataProvider parent) => CreateScopeForPrimaryCollection(parent, count, overrideObjects));
		}

		public static ForkedContextScope<int, GlobalSecondaryCollectionContext> ForkForSecondaryCollection(AssemblyConversionContext context, OverrideObjects overrideObjects, int count)
		{
			return context.GlobalSecondaryCollectionContext.ForkFor((IUnrestrictedContextDataProvider parent) => CreateScopeForSecondaryCollection(parent, count, overrideObjects));
		}

		public static ForkedContextScope<int, GlobalWriteContext> ForkForSecondaryWrite(AssemblyConversionContext context, OverrideObjects overrideObjects, int count)
		{
			return context.GlobalWriteContext.ForkFor((IUnrestrictedContextDataProvider parent) => CreateScopeForSecondaryWrite(parent, count, overrideObjects));
		}

		private static ForkedContextScope<int, GlobalWriteContext> CreateScopeForPrimaryWrite(IUnrestrictedContextDataProvider context, int count, OverrideObjects overrideObjects)
		{
			return CreateScope(context, count, overrideObjects, (IUnrestrictedContextDataProvider provider) => new PrimaryWriteAssembliesDataForker(provider));
		}

		private static ForkedContextScope<int, GlobalPrimaryCollectionContext> CreateScopeForPrimaryCollection(IUnrestrictedContextDataProvider context, int count, OverrideObjects overrideObjects)
		{
			return CreateScope(context, count, overrideObjects, (IUnrestrictedContextDataProvider provider) => new PrimaryCollectionDataForker(provider));
		}

		private static ForkedContextScope<int, GlobalSecondaryCollectionContext> CreateScopeForSecondaryCollection(IUnrestrictedContextDataProvider context, int count, OverrideObjects overrideObjects)
		{
			return CreateScope(context, count, overrideObjects, (IUnrestrictedContextDataProvider provider) => new SecondaryCollectionDataForker(provider));
		}

		private static ForkedContextScope<int, GlobalWriteContext> CreateScopeForSecondaryWrite(IUnrestrictedContextDataProvider context, int count, OverrideObjects overrideObjects)
		{
			return CreateScope(context, count, overrideObjects, (IUnrestrictedContextDataProvider provider) => new SecondaryWriteDataForker(provider));
		}

		private static ForkedContextScope<int, TContext> CreateScope<TContext>(IUnrestrictedContextDataProvider context, int count, OverrideObjects overrideObjects, Func<IUnrestrictedContextDataProvider, IDataForker<TContext>> createDataForker)
		{
			return new ForkedContextScope<int, TContext>(context, Enumerable.Range(0, count).ToArray().AsReadOnly(), createDataForker, overrideObjects);
		}
	}
}
