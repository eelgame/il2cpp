using System.Diagnostics;

namespace Unity.IL2CPP.AssemblyConversion.PerAssembly.Master
{
	[DebuggerDisplay("{Data} ({DisplayStatus})")]
	internal class SlaveInstanceResult
	{
		public readonly SlaveInstanceData Data;

		public readonly Shell.ExecuteArgs Args;

		public readonly Shell.ExecuteResult ShellResult;

		public bool Failed
		{
			get
			{
				if (ShellResult != null)
				{
					return ShellResult.ExitCode != 0;
				}
				return false;
			}
		}

		public bool Skipped => ShellResult == null;

		public bool Success
		{
			get
			{
				if (ShellResult != null)
				{
					return ShellResult.ExitCode == 0;
				}
				return false;
			}
		}

		public string DisplayStatus
		{
			get
			{
				if (Failed)
				{
					return "Failed";
				}
				if (Skipped)
				{
					return "Skipped";
				}
				return "Success";
			}
		}

		public SlaveInstanceResult(Shell.ExecuteArgs args, SlaveInstanceData data, Shell.ExecuteResult shellResult)
		{
			Args = args;
			Data = data;
			ShellResult = shellResult;
		}
	}
}
