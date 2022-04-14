using System;
using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Naming;

namespace Unity.IL2CPP
{
	public class TypeDefinitionPaddingWriter : IDisposable
	{
		private readonly ICppCodeWriter _writer;

		private readonly TypeDefinition _type;

		public TypeDefinitionPaddingWriter(ICppCodeWriter writer, TypeDefinition type)
		{
			_type = type;
			_writer = writer;
			WritePaddingStart();
		}

		public void Dispose()
		{
			WritePaddingEnd();
		}

		private void WritePaddingStart()
		{
			if (NeedsPadding())
			{
				_writer.WriteLine("union");
				_writer.BeginBlock();
				_writer.WriteLine("struct");
				_writer.BeginBlock();
			}
		}

		private void WritePaddingEnd()
		{
			if (NeedsPadding())
			{
				_writer.EndBlock(semicolon: true);
				_writer.WriteLine("uint8_t {0}[{1}];", _writer.Context.Global.Services.Naming.ForPadding(_type), _type.ClassSize);
				_writer.EndBlock(semicolon: true);
			}
		}

		private bool NeedsPadding()
		{
			return _type.ClassSize > 0;
		}
	}
}
