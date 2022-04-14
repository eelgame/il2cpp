using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP
{
	public struct DivideByZeroCheckSupport
	{
		private readonly IGeneratedMethodCodeWriter _writer;

		private readonly MethodDefinition _methodDefinition;

		private readonly bool _divideByZeroChecksGloballyEnabled;

		public DivideByZeroCheckSupport(IGeneratedMethodCodeWriter writer, MethodDefinition methodDefinition, bool divideByZeroChecksGloballyEnabled)
		{
			_writer = writer;
			_methodDefinition = methodDefinition;
			_divideByZeroChecksGloballyEnabled = divideByZeroChecksGloballyEnabled;
		}

		public void WriteDivideByZeroCheckIfNeeded(StackInfo stackInfo)
		{
			if (ShouldEmitDivideByZeroChecksForMethod())
			{
				RecordDivideByZeroCheckEmitted();
				string text = Emit.DivideByZeroCheck(stackInfo.Type, stackInfo.Expression);
				if (!string.IsNullOrEmpty(text))
				{
					_writer.WriteLine("{0};", text);
				}
			}
		}

		private void RecordDivideByZeroCheckEmitted()
		{
			_writer.Context.Global.Collectors.Stats.RecordDivideByZeroCheckEmitted(_methodDefinition);
		}

		private bool ShouldEmitDivideByZeroChecksForMethod()
		{
			return CompilerServicesSupport.HasDivideByZeroChecksSupportEnabled(_methodDefinition, _divideByZeroChecksGloballyEnabled);
		}
	}
}
