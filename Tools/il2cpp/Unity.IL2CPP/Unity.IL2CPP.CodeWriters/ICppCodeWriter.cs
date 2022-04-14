using System;
using Unity.IL2CPP.Contexts.Scheduling.Streams;

namespace Unity.IL2CPP.CodeWriters
{
	public interface ICppCodeWriter : ICodeWriter, IDisposable, IStream
	{
		void AddInclude(string include);

		void AddForwardDeclaration(string type);

		void AddMethodForwardDeclaration(string declaration);

		void AddStdInclude(string include);
	}
}
