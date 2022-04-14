using System;
using Unity.IL2CPP.Contexts.Components;
using Unity.IL2CPP.Contexts.Forking;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Contexts.Scheduling
{
	public static class PhaseWorkSchedulerFactory
	{
		public static IPhaseWorkScheduler<GlobalWriteContext> ForPrimaryWrite(AssemblyConversionContext context)
		{
			if (context.Parameters.EnableSerialConversion)
			{
				return new PhaseWorkSchedulerNoThreading<GlobalWriteContext>(context.GlobalWriteContext);
			}
			return CreateScheduler(context, PhaseWorkSchedulerFactoryUtils.ForkForPrimaryWrite, (GlobalWriteContext workerContext, Exception e) => ErrorMessageWriter.FormatException(workerContext.Services.ErrorInformation, e));
		}

		public static IPhaseWorkScheduler<GlobalPrimaryCollectionContext> ForPrimaryCollection(AssemblyConversionContext context)
		{
			if (context.Parameters.EnableSerialConversion)
			{
				return new PhaseWorkSchedulerNoThreading<GlobalPrimaryCollectionContext>(context.GlobalPrimaryCollectionContext);
			}
			return CreateScheduler(context, PhaseWorkSchedulerFactoryUtils.ForkForPrimaryCollection, (GlobalPrimaryCollectionContext workerContext, Exception e) => ErrorMessageWriter.FormatException(workerContext.Services.ErrorInformation, e));
		}

		public static IPhaseWorkScheduler<GlobalSecondaryCollectionContext> ForSecondaryCollection(AssemblyConversionContext context)
		{
			if (context.Parameters.EnableSerialConversion)
			{
				return new PhaseWorkSchedulerNoThreading<GlobalSecondaryCollectionContext>(context.GlobalSecondaryCollectionContext);
			}
			return CreateScheduler(context, PhaseWorkSchedulerFactoryUtils.ForkForSecondaryCollection, (GlobalSecondaryCollectionContext workerContext, Exception e) => ErrorMessageWriter.FormatException(workerContext.Services.ErrorInformation, e));
		}

		public static IPhaseWorkScheduler<GlobalWriteContext> ForSecondaryWrite(AssemblyConversionContext context)
		{
			if (context.Parameters.EnableSerialConversion)
			{
				return new PhaseWorkSchedulerNoThreading<GlobalWriteContext>(context.GlobalWriteContext);
			}
			return CreateScheduler(context, PhaseWorkSchedulerFactoryUtils.ForkForSecondaryWrite, (GlobalWriteContext workerContext, Exception e) => ErrorMessageWriter.FormatException(workerContext.Services.ErrorInformation, e));
		}

		public static PhaseWorkScheduler<TContext> CreateScheduler<TContext>(AssemblyConversionContext context, Func<AssemblyConversionContext, OverrideObjects, int, ForkedContextScope<int, TContext>> forker, Func<TContext, Exception, Exception> workerItemExceptionHandler)
		{
			using (MiniProfiler.Section("Create Scheduler"))
			{
				RealSchedulerComponent realSchedulerComponent = new RealSchedulerComponent();
				OverrideObjects overrideObjects = new OverrideObjects(realSchedulerComponent);
				int workerCount = (context.Parameters.EnableSerialConversion ? 1 : context.InputData.JobCount);
				PhaseWorkScheduler<TContext> phaseWorkScheduler = new PhaseWorkScheduler<TContext>((int count) => forker(context, overrideObjects, count), workerCount, workerItemExceptionHandler);
				realSchedulerComponent.Initialize(phaseWorkScheduler);
				return phaseWorkScheduler;
			}
		}
	}
}
