using System;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Services;

namespace Unity.IL2CPP
{
	public static class ErrorMessageWriter
	{
		public static Exception FormatException(IErrorInformationService errorInformation, Exception exception)
		{
			return new AdditionalErrorInformationException(FormatErrorMessage(errorInformation, null), exception);
		}

		public static string FormatErrorMessage(IErrorInformationService errorInformation, string additionalInformation)
		{
			return FormatErrorMessage(errorInformation, additionalInformation, GetSequencePoint);
		}

		public static string AppendLocationInformation(IErrorInformationService errorInformationService, string start)
		{
			StringBuilder stringBuilder = new StringBuilder(start);
			AppendLocationInformation(errorInformationService, stringBuilder, GetSequencePoint);
			return stringBuilder.ToString();
		}

		public static string FormatErrorMessage(IErrorInformationService errorInformation, string additionalInformation, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
		{
			if (errorInformation == null)
			{
				throw new ArgumentNullException("errorInformation");
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("IL2CPP error");
			AppendLocationInformation(errorInformation, stringBuilder, getSequencePoint);
			if (!string.IsNullOrEmpty(additionalInformation))
			{
				stringBuilder.AppendLine();
				stringBuilder.AppendFormat("Additional information: {0}", additionalInformation);
			}
			return stringBuilder.ToString();
		}

		private static void AppendLocationInformation(IErrorInformationService errorInformation, StringBuilder message, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
		{
			if (errorInformation.CurrentMethod != null)
			{
				message.AppendFormat(" for method '{0}'", errorInformation.CurrentMethod.FullName);
			}
			else if (errorInformation.CurrentField != null)
			{
				message.AppendFormat(" for field '{0}'", errorInformation.CurrentField.FullName);
			}
			else if (errorInformation.CurrentProperty != null)
			{
				message.AppendFormat(" for property '{0}'", errorInformation.CurrentProperty.FullName);
			}
			else if (errorInformation.CurrentEvent != null)
			{
				message.AppendFormat(" for event '{0}'", errorInformation.CurrentEvent.FullName);
			}
			else if (errorInformation.CurrentType != null)
			{
				message.AppendFormat(" for type '{0}'", errorInformation.CurrentType);
			}
			else
			{
				message.Append(" (no further information about what managed code was being converted is available)");
			}
			if (!AppendSourceCodeLocation(errorInformation, message, getSequencePoint) && errorInformation.CurrentType != null && errorInformation.CurrentType.Module != null)
			{
				message.AppendFormat(" in assembly '{0}'", errorInformation.CurrentType.Module.FileName ?? errorInformation.CurrentType.Module.Name);
			}
		}

		private static bool AppendSourceCodeLocation(IErrorInformationService errorInformation, StringBuilder message, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
		{
			string text = FindSourceCodeLocationForInstruction(errorInformation.CurrentInstruction, errorInformation.CurrentMethod, getSequencePoint);
			if (string.IsNullOrEmpty(text))
			{
				text = FindSourceCodeLocation(errorInformation.CurrentMethod, getSequencePoint);
			}
			if (string.IsNullOrEmpty(text) && errorInformation.CurrentType != null)
			{
				foreach (MethodDefinition method in errorInformation.CurrentType.Methods)
				{
					text = FindSourceCodeLocation(method, getSequencePoint);
					if (!string.IsNullOrEmpty(text))
					{
						break;
					}
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				message.AppendFormat(" in {0}", text);
				return true;
			}
			return false;
		}

		private static string FindSourceCodeLocation(MethodDefinition method, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
		{
			string text = string.Empty;
			if (method != null && method.HasBody)
			{
				foreach (Instruction instruction in method.Body.Instructions)
				{
					text = FindSourceCodeLocationForInstruction(instruction, method, getSequencePoint);
					if (!string.IsNullOrEmpty(text))
					{
						return text;
					}
				}
				return text;
			}
			return text;
		}

		private static SequencePoint GetSequencePoint(Instruction ins, MethodDefinition method)
		{
			if (method == null || !method.DebugInformation.HasSequencePoints)
			{
				return null;
			}
			if (method.DebugInformation.GetSequencePointMapping().TryGetValue(ins, out var value))
			{
				return value;
			}
			return null;
		}

		private static string FindSourceCodeLocationForInstruction(Instruction instruction, MethodDefinition method, Func<Instruction, MethodDefinition, SequencePoint> getSequencePoint)
		{
			if (instruction == null)
			{
				return string.Empty;
			}
			SequencePoint sequencePoint = getSequencePoint(instruction, method);
			if (sequencePoint == null)
			{
				return string.Empty;
			}
			return $"{sequencePoint.Document.Url}:{sequencePoint.StartLine}";
		}
	}
}
