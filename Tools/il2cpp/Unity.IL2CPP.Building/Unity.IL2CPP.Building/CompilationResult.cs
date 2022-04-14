using System;

namespace Unity.IL2CPP.Building
{
	public class CompilationResult : ProvideObjectResult
	{
		public TimeSpan Duration;

		public string InterestingOutput;

		public CompilationInvocation Invocation;

		public bool Success;
	}
}
