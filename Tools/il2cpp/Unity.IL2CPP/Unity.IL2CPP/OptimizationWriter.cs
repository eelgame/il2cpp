using System;
using System.Linq;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP
{
	internal class OptimizationWriter : IDisposable
	{
		private string[] _platformsWithOptimizationsDisabled;

		private ICodeWriter _writer;

		public OptimizationWriter(ICodeWriter writer, string methodFullName)
		{
			_platformsWithOptimizationsDisabled = OptimizationDatabase.GetPlatformsWithDisabledOptimizations(methodFullName);
			if (_platformsWithOptimizationsDisabled != null)
			{
				_writer = writer;
				_writer.WriteLine("#if {0}", _platformsWithOptimizationsDisabled.Aggregate((string x, string y) => x + " || " + y));
				_writer.WriteLine("IL2CPP_DISABLE_OPTIMIZATIONS");
				_writer.WriteLine("#endif");
			}
		}

		public void Dispose()
		{
			if (_platformsWithOptimizationsDisabled != null)
			{
				_writer.WriteLine("#if {0}", _platformsWithOptimizationsDisabled.Aggregate((string x, string y) => x + " || " + y));
				_writer.WriteLine("IL2CPP_ENABLE_OPTIMIZATIONS");
				_writer.WriteLine("#endif");
			}
		}
	}
}
