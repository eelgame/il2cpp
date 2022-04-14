using System.Text.RegularExpressions;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP
{
	public class SourceExtractor
	{
		private readonly string m_HayStack;

		public SourceExtractor(string haystack)
		{
			m_HayStack = haystack;
		}

		public string FindMethodInSourceCode(string needle, SourceCodeSearcher searcher)
		{
			return FindMethod(searcher.StartRegexForMethodInCode(needle), searcher.EndRegexForMethodInCode(needle));
		}

		private string FindMethod(string startRegex, string endRegex)
		{
			Match match = Regex.Match(m_HayStack, startRegex, RegexOptions.Multiline);
			if (match.Success)
			{
				Match match2 = Regex.Match(m_HayStack.Substring(match.Index + 1), endRegex, RegexOptions.Multiline);
				if (match2.Success)
				{
					return m_HayStack.Substring(match.Index, match2.Index + match2.Length + 1);
				}
			}
			return string.Empty;
		}
	}
}
