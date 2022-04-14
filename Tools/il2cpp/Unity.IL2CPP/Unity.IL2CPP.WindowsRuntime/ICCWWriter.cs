using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP.WindowsRuntime
{
	internal interface ICCWWriter
	{
		void Write(IGeneratedMethodCodeWriter writer);

		void WriteCreateComCallableWrapperFunctionBody(IGeneratedMethodCodeWriter writer, IRuntimeMetadataAccess metadataAccess);
	}
}
