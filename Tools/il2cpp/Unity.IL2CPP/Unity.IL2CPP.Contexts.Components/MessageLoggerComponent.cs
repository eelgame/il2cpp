using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;

namespace Unity.IL2CPP.Contexts.Components
{
	public class MessageLoggerComponent : ForkAndMergeListCollectorBase<string, ReadOnlyCollection<string>, IMessageLogger, MessageLoggerComponent>, IMessageLogger
	{
		protected override MessageLoggerComponent CreateEmptyInstance()
		{
			return new MessageLoggerComponent();
		}

		protected override MessageLoggerComponent CreateCopyInstance()
		{
			return new MessageLoggerComponent();
		}

		protected override MessageLoggerComponent ThisAsFull()
		{
			return this;
		}

		protected override IMessageLogger GetNotAvailableWrite()
		{
			throw new NotImplementedException();
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out IMessageLogger writer, out object reader, out MessageLoggerComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out IMessageLogger writer, out object reader, out MessageLoggerComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out IMessageLogger writer, out object reader, out MessageLoggerComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out IMessageLogger writer, out object reader, out MessageLoggerComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override ReadOnlyCollection<string> SortItems(IEnumerable<string> items)
		{
			return items.ToList().AsReadOnly();
		}

		protected override ReadOnlyCollection<string> BuildResults(ReadOnlyCollection<string> sortedItem)
		{
			return sortedItem;
		}

		public void LogWarning(string message)
		{
			AddInternal(message);
		}
	}
}
