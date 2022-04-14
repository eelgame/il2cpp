using System.Collections.Generic;
using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion;
using Unity.IL2CPP.Marshaling;
using Unity.IL2CPP.Marshaling.MarshalInfoWriters;
using Unity.IL2CPP.Metadata.Dat;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface IObjectFactory
	{
		IRuntimeMetadataAccess GetDefaultRuntimeMetadataAccess(SourceWritingContext context, MethodReference method, MethodMetadataUsage methodMetadataUsage, MethodUsage methodUsage);

		IMetadataWriterImplementation CreateMetadataWriter(GlobalWriteContext context, ReadOnlyCollection<AssemblyDefinition> assemblies);

		MetadataDatWriterBase CreateMetadataDatWriter(GlobalMetadataWriteContext context);

		IMetadataWriterStep CreateClassLibrarySpecificBigMetadataWriterStep(SourceWritingContext context);

		DefaultMarshalInfoWriter CreateMarshalInfoWriter(ReadOnlyContext context, TypeReference type, MarshalType marshalType, MarshalInfo marshalInfo, bool useUnicodeCharSet, bool forByReferenceType, bool forFieldMarshaling, bool forReturnValue, bool forNativeToManagedWrapper, HashSet<TypeReference> typesForRecursiveFields);
	}
}
