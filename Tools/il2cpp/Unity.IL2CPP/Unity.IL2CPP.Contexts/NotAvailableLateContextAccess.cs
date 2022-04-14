using System;

namespace Unity.IL2CPP.Contexts
{
	public class NotAvailableLateContextAccess<TContext> : LateContextAccess<TContext> where TContext : class
	{
		private readonly string _exceptionMessage;

		public override TContext Context
		{
			get
			{
				throw new InvalidOperationException(_exceptionMessage);
			}
		}

		public NotAvailableLateContextAccess(string exceptionMessage)
			: base((Func<TContext>)null, (Func<bool>)null)
		{
			_exceptionMessage = exceptionMessage;
		}
	}
}
