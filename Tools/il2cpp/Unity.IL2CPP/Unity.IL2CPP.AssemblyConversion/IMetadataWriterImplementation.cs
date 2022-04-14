using Unity.IL2CPP.StringLiterals;

namespace Unity.IL2CPP.AssemblyConversion
{
	public interface IMetadataWriterImplementation
	{
		void Write(out IStringLiteralCollection stringLiteralCollection, out IFieldReferenceCollection fieldReferenceCollection);
	}
}
