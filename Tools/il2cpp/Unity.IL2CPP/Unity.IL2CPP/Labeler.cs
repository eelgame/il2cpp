using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Unity.IL2CPP
{
	internal class Labeler
	{
		private readonly MethodDefinition _methodDefinition;

		private readonly Dictionary<Instruction, List<Instruction>> _jumpMap = new Dictionary<Instruction, List<Instruction>>();

		public Labeler(MethodDefinition methodDefinition)
		{
			_methodDefinition = methodDefinition;
			BuildLabelMap(methodDefinition);
		}

		public bool NeedsLabel(Instruction ins)
		{
			return _jumpMap.ContainsKey(ins);
		}

		public string ForJump(Instruction targetInstruction)
		{
			return $"goto {FormatOffset(targetInstruction)};";
		}

		public string ForJump(int offset)
		{
			return $"goto {FormatOffset(offset)};";
		}

		public string ForLabel(Instruction ins)
		{
			return FormatOffset(ins) + ":";
		}

		private void BuildLabelMap(MethodDefinition methodDefinition)
		{
			foreach (Instruction instruction in methodDefinition.Body.Instructions)
			{
				if (instruction.Operand is Instruction targetInstruction)
				{
					AddJumpLabel(instruction, targetInstruction);
				}
				else if (instruction.Operand is Instruction[] array)
				{
					Instruction[] array2 = array;
					foreach (Instruction targetInstruction2 in array2)
					{
						AddJumpLabel(instruction, targetInstruction2);
					}
				}
			}
			foreach (ExceptionHandler exceptionHandler in methodDefinition.Body.ExceptionHandlers)
			{
				AddJumpLabel(null, exceptionHandler.HandlerStart);
			}
		}

		private void AddJumpLabel(Instruction ins, Instruction targetInstruction)
		{
			if (!_jumpMap.TryGetValue(targetInstruction, out var value))
			{
				_jumpMap.Add(targetInstruction, value = new List<Instruction>());
			}
			value.Add(ins);
		}

		public string FormatOffset(Instruction ins)
		{
			return FormatOffset(ins.Offset);
		}

		private string FormatOffset(int offset)
		{
			string arg = "IL";
			foreach (ExceptionHandler exceptionHandler in _methodDefinition.Body.ExceptionHandlers)
			{
				if (exceptionHandler.HandlerStart.Offset == offset)
				{
					switch (exceptionHandler.HandlerType)
					{
					case ExceptionHandlerType.Catch:
						arg = "CATCH";
						break;
					case ExceptionHandlerType.Filter:
						arg = "FILTER";
						break;
					case ExceptionHandlerType.Finally:
						arg = "FINALLY";
						break;
					case ExceptionHandlerType.Fault:
						arg = "FAULT";
						break;
					}
				}
			}
			return $"{arg}_{offset:x4}";
		}
	}
}
