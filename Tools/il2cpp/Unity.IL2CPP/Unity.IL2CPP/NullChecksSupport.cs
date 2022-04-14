using System.Collections.Generic;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP
{
	public struct NullChecksSupport
	{
		private readonly IGeneratedMethodCodeWriter _writer;

		private readonly MethodDefinition _methodDefinition;

		private readonly bool _nullChecksGloballyEnabled;

		public NullChecksSupport(IGeneratedMethodCodeWriter writer, MethodDefinition methodDefinition, bool nullChecksGloballyEnabled)
		{
			_writer = writer;
			_methodDefinition = methodDefinition;
			_nullChecksGloballyEnabled = nullChecksGloballyEnabled;
		}

		public void WriteNullCheckIfNeeded(StackInfo stackInfo)
		{
			if (ShouldEmitNullChecksForMethod() && !stackInfo.Type.IsValueType() && (!stackInfo.Type.IsByReference || !((ByReferenceType)stackInfo.Type).ElementType.IsValueType))
			{
				RecordNullCheckEmitted();
				if (!stackInfo.Type.IsValueType())
				{
					_writer.WriteStatement(Emit.NullCheck(stackInfo.Expression));
				}
			}
		}

		public void WriteNullCheckForInvocationIfNeeded(MethodReference methodReference, IList<string> args)
		{
			if (ShouldEmitNullChecksForMethod() && methodReference.HasThis && !methodReference.DeclaringType.IsValueType() && !(args[0] == "__this"))
			{
				RecordNullCheckEmitted();
				_writer.WriteStatement(Emit.NullCheck(args[0]));
			}
		}

		private void RecordNullCheckEmitted()
		{
			_writer.Context.Global.Collectors.Stats.RecordNullCheckEmitted(_methodDefinition);
		}

		private bool ShouldEmitNullChecksForMethod()
		{
			return CompilerServicesSupport.HasNullChecksSupportEnabled(_writer.Context, _methodDefinition, _nullChecksGloballyEnabled);
		}
	}
}
