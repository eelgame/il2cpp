namespace Unity.IL2CPP.Contexts.Scheduling
{
	public class WorkItemData<TContext>
	{
		public readonly int ThreadUniqueIndex;

		public readonly TContext Context;

		public WorkItemData(TContext context, int uniqueIndex)
		{
			Context = context;
			ThreadUniqueIndex = uniqueIndex;
		}
	}
	public class WorkItemData<TContext, TTag> : WorkItemData<TContext>
	{
		public readonly TTag Tag;

		public WorkItemData(TContext context, int uniqueIndex, TTag tag)
			: base(context, uniqueIndex)
		{
			Tag = tag;
		}
	}
	public class WorkItemData<TContext, TItem, TTag> : WorkItemData<TContext, TTag>
	{
		public readonly TItem Item;

		public WorkItemData(TContext context, TItem item, int uniqueIndex, TTag tag)
			: base(context, uniqueIndex, tag)
		{
			Item = item;
		}
	}
}
