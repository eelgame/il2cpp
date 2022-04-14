using System;
using System.Collections.Generic;

namespace Unity.IL2CPP
{
	public class AggregateErrorInformationAlreadyProcessedException : AggregateException
	{
		public AggregateErrorInformationAlreadyProcessedException()
		{
		}

		public AggregateErrorInformationAlreadyProcessedException(string message, IEnumerable<Exception> innerExceptions)
			: base(message, innerExceptions)
		{
		}
	}
}
