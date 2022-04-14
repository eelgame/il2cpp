using System.Linq;
using System.Reflection;
using Unity.Options;

namespace il2cpp
{
	internal static class MasterToSlaveArgumentPassThrough
	{
		internal static string[] GetPassThroughArguments()
		{
			return OptionsParser.RecreateArgumentsFromCurrentState(typeof(IL2CPPOptions), PassArgumentAlongToSlave).Concat(OptionsParser.RecreateArgumentsFromCurrentState(typeof(CodeGenOptions), CodeGenOptionFilter)).ToArray();
		}

		private static bool PassArgumentAlongToSlave(FieldInfo field, object value)
		{
			switch (field.Name)
			{
			case "CompileCpp":
			case "CommandLog":
			case "GenerateCmake":
				return false;
			case "ProfilerReport":
				return false;
			default:
				return true;
			}
		}

		private static bool CodeGenOptionFilter(FieldInfo field, object value)
		{
			string name = field.Name;
			if (!(name == "ConversionMode"))
			{
				if (name == "EnableStats")
				{
					return false;
				}
				return true;
			}
			return false;
		}
	}
}
