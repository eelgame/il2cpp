using Mono.Cecil.Cil;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP.Contexts.Services
{
	public interface ISourceAnnotationWriter
	{
		void EmitAnnotation(ICodeWriter writer, SequencePoint sequencePoint);
	}
}
