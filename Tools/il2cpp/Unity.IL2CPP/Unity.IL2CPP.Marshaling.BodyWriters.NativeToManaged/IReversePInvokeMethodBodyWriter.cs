using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP.Marshaling.BodyWriters.NativeToManaged
{
	internal interface IReversePInvokeMethodBodyWriter
	{
		void WriteMethodDeclaration(IGeneratedCodeWriter writer);

		void WriteMethodDefinition(IGeneratedMethodCodeWriter writer);
	}
}
