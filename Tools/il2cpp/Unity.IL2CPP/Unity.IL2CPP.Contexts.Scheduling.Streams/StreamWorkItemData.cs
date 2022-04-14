using NiceIO;

namespace Unity.IL2CPP.Contexts.Scheduling.Streams
{
	public struct StreamWorkItemData<TItem, TStream>
	{
		public readonly SourceWritingContext Context;

		public readonly TStream Stream;

		public readonly TItem Item;

		public readonly NPath FilePath;

		public StreamWorkItemData(SourceWritingContext context, TItem item, TStream stream, NPath filePath)
		{
			Context = context;
			Item = item;
			Stream = stream;
			FilePath = filePath;
		}
	}
}
