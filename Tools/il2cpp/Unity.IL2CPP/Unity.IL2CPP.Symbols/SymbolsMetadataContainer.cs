using System.Collections.Generic;
using System.IO;

namespace Unity.IL2CPP.Symbols
{
	internal class SymbolsMetadataContainer
	{
		public struct LineNumberPair
		{
			public readonly uint CppLineNumber;

			public readonly uint CsLineNumber;

			public LineNumberPair(uint cppLineNumber, uint csLineNumber)
			{
				CppLineNumber = cppLineNumber;
				CsLineNumber = csLineNumber;
			}
		}

		private Dictionary<string, Dictionary<string, List<LineNumberPair>>> m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList;

		public SymbolsMetadataContainer()
		{
			m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList = new Dictionary<string, Dictionary<string, List<LineNumberPair>>>();
		}

		public void Add(string cppFileName, string csFileName, uint cppLineNum, uint csLineNum)
		{
			List<LineNumberPair> value2;
			if (!m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList.TryGetValue(cppFileName, out var value))
			{
				List<LineNumberPair> list = new List<LineNumberPair>();
				list.Add(new LineNumberPair(cppLineNum, csLineNum));
				value = new Dictionary<string, List<LineNumberPair>>();
				value.Add(csFileName, list);
				m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList.Add(cppFileName, value);
			}
			else if (!value.TryGetValue(csFileName, out value2))
			{
				value2 = new List<LineNumberPair>();
				value2.Add(new LineNumberPair(cppLineNum, csLineNum));
				value.Add(csFileName, value2);
			}
			else
			{
				value.TryGetValue(csFileName, out value2);
				value2.Add(new LineNumberPair(cppLineNum, csLineNum));
			}
		}

		public void Merge(SymbolsMetadataContainer forked)
		{
			foreach (KeyValuePair<string, Dictionary<string, List<LineNumberPair>>> cPPFilenameToDictionaryOfCSFilenameToLineNumberPair in forked.m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList)
			{
				m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList[cPPFilenameToDictionaryOfCSFilenameToLineNumberPair.Key] = cPPFilenameToDictionaryOfCSFilenameToLineNumberPair.Value;
			}
		}

		public void SerializeToJson(StreamWriter outputStream)
		{
			outputStream.WriteLine("{");
			bool flag = true;
			foreach (KeyValuePair<string, Dictionary<string, List<LineNumberPair>>> cPPFilenameToDictionaryOfCSFilenameToLineNumberPair in m_CPPFilenameToDictionaryOfCSFilenameToLineNumberPairList)
			{
				if (flag)
				{
					flag = false;
				}
				else
				{
					outputStream.WriteLine(",");
				}
				string text = cPPFilenameToDictionaryOfCSFilenameToLineNumberPair.Key.Replace("\\", "\\\\");
				outputStream.WriteLine("\"" + text + "\" : {");
				bool flag2 = true;
				foreach (KeyValuePair<string, List<LineNumberPair>> item in cPPFilenameToDictionaryOfCSFilenameToLineNumberPair.Value)
				{
					if (flag2)
					{
						flag2 = false;
					}
					else
					{
						outputStream.WriteLine(",");
					}
					string text2 = item.Key.Replace("\\", "\\\\");
					outputStream.WriteLine("\"" + text2 + "\" : {");
					bool flag3 = true;
					foreach (LineNumberPair item2 in item.Value)
					{
						if (flag3)
						{
							flag3 = false;
						}
						else
						{
							outputStream.WriteLine(",");
						}
						outputStream.Write($"\"{item2.CppLineNumber}\" : {item2.CsLineNumber}");
					}
					outputStream.WriteLine();
					outputStream.Write("}");
				}
				outputStream.WriteLine();
				outputStream.Write("}");
			}
			outputStream.WriteLine();
			outputStream.WriteLine("}");
		}
	}
}
