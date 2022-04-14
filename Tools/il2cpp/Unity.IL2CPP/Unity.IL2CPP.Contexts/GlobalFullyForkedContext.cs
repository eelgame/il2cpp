using System;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Contexts.Forking;
using Unity.IL2CPP.Contexts.Forking.Providers;

namespace Unity.IL2CPP.Contexts
{
	public class GlobalFullyForkedContext
	{
		private readonly TinyGlobalWriteContext _tinyGlobalWriteContext;

		private readonly GlobalWriteContext _globalWriteContext;

		private readonly GlobalMinimalContext _globalMinimalContext;

		private readonly GlobalReadOnlyContext _globalReadOnlyContext;

		private readonly GlobalPrimaryCollectionContext _globalPrimaryCollectionContext;

		private readonly GlobalSecondaryCollectionContext _globalSecondaryCollectionContext;

		private readonly GlobalMetadataWriteContext _globalMetadataWriteContext;

		private readonly AssemblyConversionResults _phaseResultsContainer;

		private readonly ForkedDataContainer _container;

		public GlobalWriteContext GlobalWriteContext => _globalWriteContext;

		public GlobalPrimaryCollectionContext GlobalPrimaryCollectionContext => _globalPrimaryCollectionContext;

		public GlobalSecondaryCollectionContext GlobalSecondaryCollectionContext => _globalSecondaryCollectionContext;

		public GlobalMetadataWriteContext GlobalMetadataWriteContext => _globalMetadataWriteContext;

		public TinyGlobalWriteContext TinyGlobalWriteContext => _tinyGlobalWriteContext;

		public GlobalReadOnlyContext GlobalReadOnlyContext => _globalReadOnlyContext;

		public GlobalMinimalContext GlobalMinimalContext => _globalMinimalContext;

		public AssemblyConversionResults Results => _phaseResultsContainer;

		public IUnrestrictedContextCollectorProvider Collectors => _container;

		public IUnrestrictedContextStatefulServicesProvider StatefulServices => _container;

		public GlobalReadOnlyContext.ContextServices Services => GlobalReadOnlyContext.Services;

		public GlobalFullyForkedContext(GlobalReadOnlyContext readOnlyContext, GlobalMinimalContext minimalContext, GlobalPrimaryCollectionContext primaryCollectionContext, GlobalWriteContext writeContext, GlobalSecondaryCollectionContext secondaryCollectionContext, GlobalMetadataWriteContext metadataWriteContext, TinyGlobalWriteContext tinyGlobalWriteContext, AssemblyConversionResults phaseResultsContainer, ForkedDataContainer container)
		{
			if (readOnlyContext == null)
			{
				throw new ArgumentNullException("readOnlyContext");
			}
			if (minimalContext == null)
			{
				throw new ArgumentNullException("minimalContext");
			}
			if (primaryCollectionContext == null)
			{
				throw new ArgumentNullException("primaryCollectionContext");
			}
			if (writeContext == null)
			{
				throw new ArgumentNullException("writeContext");
			}
			if (secondaryCollectionContext == null)
			{
				throw new ArgumentNullException("secondaryCollectionContext");
			}
			if (metadataWriteContext == null)
			{
				throw new ArgumentNullException("metadataWriteContext");
			}
			if (tinyGlobalWriteContext == null)
			{
				throw new ArgumentNullException("tinyGlobalWriteContext");
			}
			_globalReadOnlyContext = readOnlyContext;
			_globalMinimalContext = minimalContext;
			_globalPrimaryCollectionContext = primaryCollectionContext;
			_globalWriteContext = writeContext;
			_globalSecondaryCollectionContext = secondaryCollectionContext;
			_globalMetadataWriteContext = metadataWriteContext;
			_tinyGlobalWriteContext = tinyGlobalWriteContext;
			_phaseResultsContainer = phaseResultsContainer;
			_container = container;
		}

		public ReadOnlyContext CreateReadOnlyContext()
		{
			return GlobalReadOnlyContext.GetReadOnlyContext();
		}
	}
}
