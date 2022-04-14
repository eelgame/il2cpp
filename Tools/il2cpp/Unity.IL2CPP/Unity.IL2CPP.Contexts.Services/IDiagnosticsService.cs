using System;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface IDiagnosticsService
	{
		IDisposable BeginCollectorStateDump(GlobalFullyForkedContext context, string captureName);
	}
}
