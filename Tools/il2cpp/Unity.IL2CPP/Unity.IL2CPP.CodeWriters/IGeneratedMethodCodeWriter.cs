using System;
using System.Collections.ObjectModel;
using Mono.Cecil;
using NiceIO;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters
{
	public interface IGeneratedMethodCodeWriter : IGeneratedCodeWriter, ICppCodeWriter, ICodeWriter, IDisposable, IStream
	{
		bool ErrorOccurred { get; set; }

		ReadOnlyDictionary<string, MethodMetadataUsage> MethodMetadataUsages { get; }

		NPath FileName { get; }

		void Write(IGeneratedMethodCodeWriter other);

		void AddIncludeForMethodDeclaration(MethodReference method);

		void AddInternalPInvokeMethodDeclaration(string methodName, string internalPInvokeDeclaration, string moduleName, bool forForcedInternalPInvoke, bool isExplicitlyInternal);

		void WriteInternalCallResolutionStatement(MethodDefinition method, IRuntimeMetadataAccess metadataAccess);

		string VirtualCallInvokeMethod(MethodReference method, TypeResolver typeResolver, bool skipFirstParameter = false, bool? isInterfaceMethod = null, bool? isGenericInstance = null);

		void AddMetadataUsage(string identifier, MethodMetadataUsage usage);
	}
}
