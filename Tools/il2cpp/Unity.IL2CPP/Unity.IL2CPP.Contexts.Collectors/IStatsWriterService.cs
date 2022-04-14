using Mono.Cecil;
using NiceIO;

namespace Unity.IL2CPP.Contexts.Collectors
{
	public interface IStatsWriterService
	{
		void RecordFileWritten(NPath path);

		void RecordSharableMethod(MethodReference method);

		void RecordNullCheckEmitted(MethodDefinition methodDefinition);

		void RecordArrayBoundsCheckEmitted(MethodDefinition methodDefinition);

		void RecordMemoryBarrierEmitted(MethodDefinition methodDefinition);

		void RecordDivideByZeroCheckEmitted(MethodDefinition methodDefinition);

		void RecordTailCall(MethodDefinition methodDefinition);

		void RecordStringLiteral(string str);

		void RecordMethod(MethodReference method);

		void RecordMetadataStream(string name, long size);

		void RecordWindowsRuntimeBoxedType();

		void RecordWindowsRuntimeTypeWithName();

		void RecordNativeToManagedInterfaceAdapter();

		void RecordComCallableWrapper();

		void RecordArrayComCallableWrapper();

		void RecordImplementedComCallableWrapperMethod();

		void RecordStrippedComCallableWrapperMethod();

		void RecordForwardedToBaseClassComCallableWrapperMethod();
	}
}
