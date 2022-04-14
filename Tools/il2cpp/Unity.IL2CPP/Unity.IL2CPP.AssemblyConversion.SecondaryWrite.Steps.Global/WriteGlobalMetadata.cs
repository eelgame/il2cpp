using System.Collections.ObjectModel;
using Mono.Cecil;
using Unity.IL2CPP.AssemblyConversion.Steps.Base;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.StringLiterals;

namespace Unity.IL2CPP.AssemblyConversion.SecondaryWrite.Steps.Global
{
	public class WriteGlobalMetadata : GlobalSimpleScheduledStepFunc<GlobalWriteContext, (IStringLiteralCollection, IFieldReferenceCollection)>
	{
		protected override string Name => "Write Global Metadata";

		protected override bool Skip(GlobalWriteContext context)
		{
			return false;
		}

		protected override (IStringLiteralCollection, IFieldReferenceCollection) CreateEmptyResult()
		{
			return (null, null);
		}

		protected override (IStringLiteralCollection, IFieldReferenceCollection) Worker(GlobalWriteContext context, ReadOnlyCollection<AssemblyDefinition> assemblies)
		{
			context.Services.Factory.CreateMetadataWriter(context, assemblies).Write(out var stringLiteralCollection, out var fieldReferenceCollection);
			return (stringLiteralCollection, fieldReferenceCollection);
		}
	}
}
