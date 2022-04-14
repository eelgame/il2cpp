using Mono.Cecil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP
{
	public class ArrayBoundsCheckSupport
	{
		private readonly MethodDefinition _methodDefinition;

		private readonly bool _arrayBoundsChecksGloballyEnabled;

		public ArrayBoundsCheckSupport(MethodDefinition methodDefinition, bool arrayBoundsChecksGloballyEnabled)
		{
			_methodDefinition = methodDefinition;
			_arrayBoundsChecksGloballyEnabled = arrayBoundsChecksGloballyEnabled;
		}

		public bool ShouldEmitBoundsChecksForMethod()
		{
			return CompilerServicesSupport.HasArrayBoundsChecksSupportEnabled(_methodDefinition, _arrayBoundsChecksGloballyEnabled);
		}

		public void RecordArrayBoundsCheckEmitted(MinimalContext context)
		{
			if (ShouldEmitBoundsChecksForMethod())
			{
				context.Global.Collectors.Stats.RecordArrayBoundsCheckEmitted(_methodDefinition);
			}
		}
	}
}
