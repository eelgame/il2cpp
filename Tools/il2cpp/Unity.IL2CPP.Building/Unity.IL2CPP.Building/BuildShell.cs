using System;
using System.IO;
using NiceIO;

namespace Unity.IL2CPP.Building
{
	public class BuildShell
	{
		public enum CommandLogMode
		{
			On,
			DryRun,
			Off
		}

		private class CloseCommandLoggerContext : IDisposable
		{
			public void Dispose()
			{
				lock (lockobj)
				{
					if (CommandLogWriter != null)
					{
						CommandLogWriter.Dispose();
						CommandLogWriter = null;
						LogMode = CommandLogMode.Off;
					}
				}
			}
		}

		private class DisabledContext : IDisposable
		{
			public void Dispose()
			{
			}
		}

		private static readonly object lockobj = new object();

		private static StreamWriter CommandLogWriter;

		public static CommandLogMode LogMode { get; private set; } = CommandLogMode.Off;


		public static bool LogEnabled => LogMode != CommandLogMode.Off;

		public static Shell.ExecuteResult Execute(Shell.ExecuteArgs args)
		{
			AppendToCommandLog(args.Summary);
			if (LogMode == CommandLogMode.DryRun)
			{
				return Shell.ExecuteResult.Empty;
			}
			return Shell.Execute(args);
		}

		public static IDisposable CreateCommandLogger(NPath destDir, CommandLogMode mode)
		{
			LogMode = mode;
			if (mode == CommandLogMode.Off)
			{
				return new DisabledContext();
			}
			lock (lockobj)
			{
				if (CommandLogWriter != null)
				{
					throw new InvalidOperationException("Previous file was not closed properly");
				}
				CommandLogWriter = new StreamWriter(new FileStream(destDir.Combine("BuildCommands.txt").ToString(), FileMode.Create));
			}
			return new CloseCommandLoggerContext();
		}

		public static void AppendToCommandLog(string format, params object[] arguments)
		{
			if (CommandLogWriter != null)
			{
				lock (lockobj)
				{
					CommandLogWriter.WriteLine(format, arguments);
					CommandLogWriter.Flush();
				}
			}
		}
	}
}
