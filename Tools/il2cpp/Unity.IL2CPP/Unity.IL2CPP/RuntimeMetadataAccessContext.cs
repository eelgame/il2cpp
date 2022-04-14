using System;
using System.Linq;
using Mono.Cecil.Cil;
using Unity.Cecil.Awesome.CFG;

namespace Unity.IL2CPP
{
	public static class RuntimeMetadataAccessContext
	{
		private class NopContext : IDisposable
		{
			public void Dispose()
			{
			}
		}

		private class InitMetadataInlineContext : IDisposable
		{
			private readonly IRuntimeMetadataAccess _runtimeMetadataAccess;

			public InitMetadataInlineContext(IRuntimeMetadataAccess runtimeMetadataAccess)
			{
				_runtimeMetadataAccess = runtimeMetadataAccess;
				_runtimeMetadataAccess.StartInitMetadataInline();
			}

			public void Dispose()
			{
				_runtimeMetadataAccess.EndInitMetadataInline();
			}
		}

		private static readonly NopContext Nop = new NopContext();

		private const int MaxPathSearchLength = 4;

		public static IDisposable Create(IRuntimeMetadataAccess runtimeMetadataAccess, ExceptionSupport.Node node)
		{
			if (node.Type == ExceptionSupport.NodeType.Catch || node.Type == ExceptionSupport.NodeType.Fault || node.Type == ExceptionSupport.NodeType.Filter || IsExceptionPath(node.Block, 0))
			{
				return new InitMetadataInlineContext(runtimeMetadataAccess);
			}
			return Nop;
		}

		public static IDisposable CreateForCatchHandlers(IRuntimeMetadataAccess runtimeMetadataAccess)
		{
			return new InitMetadataInlineContext(runtimeMetadataAccess);
		}

		private static bool IsExceptionPath(InstructionBlock insBlock, int pathLength)
		{
			if (insBlock == null)
			{
				return false;
			}
			if (insBlock.Last.OpCode == OpCodes.Throw)
			{
				return true;
			}
			if (pathLength > 4 || !insBlock.Successors.Any())
			{
				return false;
			}
			return insBlock.Successors.All((InstructionBlock ins) => IsExceptionPath(ins, pathLength + 1));
		}
	}
}
