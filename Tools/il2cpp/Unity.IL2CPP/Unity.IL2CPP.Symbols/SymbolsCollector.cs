using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NiceIO;
using Unity.IL2CPP.Contexts;
using Unity.IL2CPP.Contexts.Collectors;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Results;
using Unity.IL2CPP.Diagnostics;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Symbols
{
	public class SymbolsCollector : CompletableStatefulComponentBase<ISymbolsCollectorResults, ISymbolsCollector, SymbolsCollector>, ISymbolsCollector
	{
		private class NotAvailable : ISymbolsCollector, ISymbolsCollectorResults
		{
			public void CollectLineNumberInformation(ReadOnlyContext context, NPath CppSourcePath)
			{
				throw new NotSupportedException();
			}

			public void SerializeToJson(StreamWriter outputStream)
			{
				throw new NotSupportedException();
			}
		}

		private class Results : ISymbolsCollectorResults
		{
			private readonly SymbolsMetadataContainer _symbolsMetadataContainer;

			public Results(SymbolsMetadataContainer symbolsMetadataContainer)
			{
				_symbolsMetadataContainer = symbolsMetadataContainer;
			}

			public void SerializeToJson(StreamWriter outputStream)
			{
				_symbolsMetadataContainer.SerializeToJson(outputStream);
			}
		}

		private SymbolsMetadataContainer m_SymbolsMetadataContainer;

		private List<string> m_visitedCppFiles;

		private const string REGEX_PATTERN_STRING = "\t*//<source_info:(.+):(\\d+)>";

		private Regex m_regexPattern;

		public SymbolsCollector()
		{
			m_SymbolsMetadataContainer = new SymbolsMetadataContainer();
			m_visitedCppFiles = new List<string>();
			m_regexPattern = new Regex("\t*//<source_info:(.+):(\\d+)>", RegexOptions.Compiled);
		}

		public void CollectLineNumberInformation(ReadOnlyContext context, NPath CppSourcePath)
		{
			if (!context.Global.Parameters.EmitSourceMapping)
			{
				return;
			}
			using (MiniProfiler.Section("SymbolsCollection"))
			{
				if (m_visitedCppFiles.Contains(CppSourcePath.ToString()))
				{
					return;
				}
				using (StreamReader streamReader = new StreamReader(CppSourcePath.ToString()))
				{
					uint num = 0u;
					while (!streamReader.EndOfStream)
					{
						string input = streamReader.ReadLine();
						num++;
						Match match = m_regexPattern.Match(input);
						if (match.Success)
						{
							string value = match.Groups[1].Value;
							uint csLineNum = Convert.ToUInt32(match.Groups[2].Value);
							m_SymbolsMetadataContainer.Add(CppSourcePath.ToString(), value, num, csLineNum);
						}
					}
				}
				m_visitedCppFiles.Add(CppSourcePath.ToString());
			}
		}

		protected override void DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendCollection(builder, "m_visitedCppFiles", m_visitedCppFiles.ToSortedCollection());
		}

		protected override void HandleMergeForAdd(SymbolsCollector forked)
		{
			m_SymbolsMetadataContainer.Merge(forked.m_SymbolsMetadataContainer);
			m_visitedCppFiles.AddRange(forked.m_visitedCppFiles);
		}

		protected override void HandleMergeForMergeValues(SymbolsCollector forked)
		{
			throw new NotSupportedException();
		}

		protected override SymbolsCollector CreateEmptyInstance()
		{
			return new SymbolsCollector();
		}

		protected override SymbolsCollector CreateCopyInstance()
		{
			throw new NotSupportedException();
		}

		protected override SymbolsCollector ThisAsFull()
		{
			return this;
		}

		protected override ISymbolsCollector GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override ISymbolsCollectorResults GetResults()
		{
			return new Results(m_SymbolsMetadataContainer);
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out ISymbolsCollector writer, out object reader, out SymbolsCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out ISymbolsCollector writer, out object reader, out SymbolsCollector full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out ISymbolsCollector writer, out object reader, out SymbolsCollector full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out ISymbolsCollector writer, out object reader, out SymbolsCollector full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}
	}
}
