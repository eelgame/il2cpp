using Mono.Cecil;
using Unity.IL2CPP.CodeWriters;

namespace Unity.IL2CPP
{
	internal class BenchmarkSupport
	{
		private const string BenchmarkAttributeName = "BenchmarkAttribute";

		private static bool IsBenchmarkAttribute(CustomAttribute attribute)
		{
			return attribute.AttributeType.Name == "BenchmarkAttribute";
		}

		private static bool IsBenchmarkMethod(IMemberDefinition methodDefinition)
		{
			if (methodDefinition == null)
			{
				return false;
			}
			foreach (CustomAttribute customAttribute in methodDefinition.CustomAttributes)
			{
				if (IsBenchmarkAttribute(customAttribute))
				{
					return true;
				}
			}
			return false;
		}

		public static bool BeginBenchmark(MethodReference method, IGeneratedMethodCodeWriter writer)
		{
			if (writer.Context.Global.Parameters.GoogleBenchmark && method != null && IsBenchmarkMethod(method.Resolve()))
			{
				writer.AddStdInclude("benchmark/benchmark.h");
				writer.WriteLine("benchmark::RegisterBenchmark(\"" + method.DeclaringType.Name + "." + method.Name + "\", [=](benchmark::State & state) {");
				writer.Write("for (auto _ : state) ");
				return true;
			}
			return false;
		}

		public static void EndBenchmark(bool benchmarkMethod, IGeneratedMethodCodeWriter writer)
		{
			if (benchmarkMethod)
			{
				writer.WriteLine("});");
			}
		}
	}
}
