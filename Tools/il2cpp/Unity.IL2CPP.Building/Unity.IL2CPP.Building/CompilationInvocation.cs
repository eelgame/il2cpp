using System.Collections.Generic;
using System.Text;
using NiceIO;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Building
{
	public class CompilationInvocation
	{
		public IEnumerable<string> Arguments;

		public NPath CompilerExecutable;

		public Dictionary<string, string> EnvVars;

		public NPath SourceFile;

		public Shell.ExecuteArgs ToExecuteArgs()
		{
			string executable = CompilerExecutable.ToString();
			if (PlatformUtils.IsWindows())
			{
				executable = CompilerExecutable.InQuotes();
			}
			return new Shell.ExecuteArgs
			{
				Arguments = Arguments.SeparateWithSpaces(),
				EnvVars = EnvVars,
				Executable = executable
			};
		}

		public Shell.ExecuteResult Execute()
		{
			return BuildShell.Execute(ToExecuteArgs());
		}

		public string Summary()
		{
			return ToExecuteArgs().Summary;
		}

		public string Hash(string hashForAllHeaderFilesPossiblyInfluencingCompilation)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string argument in Arguments)
			{
				stringBuilder.Append(argument);
			}
			stringBuilder.Append(CompilerExecutable);
			stringBuilder.Append(SourceFile);
			stringBuilder.Append(hashForAllHeaderFilesPossiblyInfluencingCompilation);
			stringBuilder.Append(HashTools.HashOfFile(SourceFile));
			if (TraceDebugger.TraceDebuggerEnabled)
			{
				StringBuilder stringBuilder2 = new StringBuilder();
				stringBuilder2.AppendLine("Compiler Executable: " + CompilerExecutable);
				stringBuilder2.AppendLine("Source File: " + SourceFile);
				stringBuilder2.AppendLine("Source Hash: " + HashTools.HashOfFile(SourceFile));
				stringBuilder2.AppendLine("Arguments: ");
				foreach (string argument2 in Arguments)
				{
					stringBuilder2.AppendLine("    " + argument2);
				}
				TraceDebugger.Log(stringBuilder2.ToString());
			}
			return HashTools.HashOf(stringBuilder.ToString());
		}
	}
}
