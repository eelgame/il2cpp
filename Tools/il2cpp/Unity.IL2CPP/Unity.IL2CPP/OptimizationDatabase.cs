using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP
{
	internal static class OptimizationDatabase
	{
		private static readonly ReadOnlyDictionary<string, string[]> _disabledOptimizationMap = new Dictionary<string, string[]> { 
		{
			"System.Void Mono.Globalization.Unicode.MSCompatUnicodeTable::.cctor()",
			new string[1] { "IL2CPP_TARGET_XBOXONE" }
		} }.AsReadOnly();

		public static string[] GetPlatformsWithDisabledOptimizations(string methodFullName)
		{
			string[] value = null;
			_disabledOptimizationMap.TryGetValue(methodFullName, out value);
			return value;
		}
	}
}
