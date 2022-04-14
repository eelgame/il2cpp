namespace Unity.IL2CPP.Contexts.Services
{
	public struct ResultData<TItem, TResult>
	{
		public readonly TItem Item;

		public readonly TResult Result;

		public ResultData(TItem item, TResult result)
		{
			Item = item;
			Result = result;
		}
	}
}
