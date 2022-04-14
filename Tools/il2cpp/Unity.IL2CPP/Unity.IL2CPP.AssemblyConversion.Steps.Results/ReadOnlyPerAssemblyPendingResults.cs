using Mono.Cecil;

namespace Unity.IL2CPP.AssemblyConversion.Steps.Results
{
	public sealed class ReadOnlyPerAssemblyPendingResults<TWorkerResult> : ReadOnlyPendingResults<AssemblyDefinition, TWorkerResult>
	{
		public ReadOnlyPerAssemblyPendingResults(PendingResults<AssemblyDefinition, TWorkerResult> pending)
			: base(pending)
		{
		}
	}
}
