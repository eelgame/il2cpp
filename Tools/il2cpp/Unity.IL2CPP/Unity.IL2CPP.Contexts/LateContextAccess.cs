using System;

namespace Unity.IL2CPP.Contexts
{
	public class LateContextAccess<TContext> where TContext : class
	{
		private readonly Func<bool> _isInitialized;

		private readonly Func<TContext> _getContext;

		private TContext _cachedValue;

		public virtual TContext Context
		{
			get
			{
				if (_cachedValue != null)
				{
					return _cachedValue;
				}
				if (!_isInitialized())
				{
					throw new InvalidOperationException("The context cannot be accessed yet.  It is not fully initialized");
				}
				_cachedValue = _getContext();
				return _cachedValue;
			}
		}

		public LateContextAccess(Func<TContext> getContext, Func<bool> isInitialized)
		{
			_isInitialized = isInitialized;
			_getContext = getContext;
		}
	}
}
