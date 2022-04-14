using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Debugger
{
	public class SequencePointCollector : ISequencePointCollector, ISequencePointProvider
	{
		public class SourceFileData : ISequencePointSourceFileData
		{
			public string File { get; }

			public byte[] Hash { get; }

			public SourceFileData(string file, byte[] hash)
			{
				File = file;
				Hash = hash;
			}
		}

		private static readonly SequencePointInfoComparer s_seqPointComparer = new SequencePointInfoComparer();

		private readonly Dictionary<MethodDefinition, HashSet<SequencePointInfo>> sequencePointsByMethod = new Dictionary<MethodDefinition, HashSet<SequencePointInfo>>();

		private readonly List<SequencePointInfo> allSequencePoints = new List<SequencePointInfo>();

		private readonly Dictionary<MethodDefinition, HashSet<int>> pausePointsByMethod = new Dictionary<MethodDefinition, HashSet<int>>();

		private readonly Dictionary<SequencePointInfo, int> seqPointIndexes = new Dictionary<SequencePointInfo, int>(s_seqPointComparer);

		private readonly Dictionary<string, int> sourceFileIndexes = new Dictionary<string, int>();

		private readonly List<ISequencePointSourceFileData> allSourceFiles = new List<ISequencePointSourceFileData>();

		private readonly Dictionary<string, int> _variableNames = new Dictionary<string, int>();

		private readonly List<VariableData> _variables = new List<VariableData>();

		private readonly Dictionary<MethodDefinition, Range> _variableMap = new Dictionary<MethodDefinition, Range>();

		private readonly List<Range> _scopes = new List<Range>();

		private readonly Dictionary<MethodDefinition, Range> _scopeMap = new Dictionary<MethodDefinition, Range>();

		private bool isComplete;

		public int NumSeqPoints => allSequencePoints.Count;

		public void AddSequencePoint(SequencePointInfo seqPoint)
		{
			if (isComplete)
			{
				throw new InvalidOperationException("Cannot add new sequence points after collection is complete.");
			}
			SequencePointInfo adjustedSequencePoint = GetAdjustedSequencePoint(seqPoint);
			AddSequencePointToMethod(adjustedSequencePoint);
			if (!seqPointIndexes.ContainsKey(seqPoint))
			{
				AddAndIndexSequencePoint(seqPoint, adjustedSequencePoint);
			}
		}

		public void AddPausePoint(MethodDefinition method, int offset)
		{
			if (isComplete)
			{
				throw new InvalidOperationException("Cannot add new pause points after collection is complete.");
			}
			if (pausePointsByMethod.TryGetValue(method, out var value))
			{
				value.Add(offset);
				return;
			}
			value = new HashSet<int>();
			value.Add(offset);
			pausePointsByMethod.Add(method, value);
		}

		public void Complete()
		{
			isComplete = true;
		}

		public SequencePointInfo GetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind)
		{
			if (!sequencePointsByMethod.TryGetValue(method, out var value))
			{
				throw new KeyNotFoundException("Could not find a sequence point with specified properties");
			}
			foreach (SequencePointInfo item in value)
			{
				if (item.IlOffset == ilOffset && item.Kind == kind)
				{
					return item;
				}
			}
			throw new KeyNotFoundException("Could not find a sequence point with specified properties");
		}

		public bool TryGetSequencePointAt(MethodDefinition method, int ilOffset, SequencePointKind kind, out SequencePointInfo info)
		{
			info = null;
			if (!sequencePointsByMethod.TryGetValue(method, out var value))
			{
				return false;
			}
			foreach (SequencePointInfo item in value)
			{
				if (item.IlOffset == ilOffset && item.Kind == kind)
				{
					info = item;
					return true;
				}
			}
			return false;
		}

		public ReadOnlyCollection<SequencePointInfo> GetAllSequencePoints()
		{
			if (!isComplete)
			{
				throw new InvalidOperationException("Cannot retrieve sequence points before collection is complete.");
			}
			return allSequencePoints.AsReadOnly();
		}

		public int GetSeqPointIndex(SequencePointInfo seqPoint)
		{
			if (seqPointIndexes.ContainsKey(seqPoint))
			{
				return seqPointIndexes[seqPoint];
			}
			SequencePointInfo adjustedSequencePoint = GetAdjustedSequencePoint(seqPoint);
			AddSequencePointToMethod(adjustedSequencePoint);
			return AddAndIndexSequencePoint(seqPoint, adjustedSequencePoint);
		}

		public int GetSourceFileIndex(string sourceFile)
		{
			if (sourceFileIndexes.TryGetValue(sourceFile, out var value))
			{
				return value;
			}
			return -1;
		}

		public ReadOnlyCollection<ISequencePointSourceFileData> GetAllSourceFiles()
		{
			if (!isComplete)
			{
				throw new InvalidOperationException("Cannot retrieve sequence point source files before collection is complete.");
			}
			return allSourceFiles.AsReadOnly();
		}

		private SequencePointInfo GetAdjustedSequencePoint(SequencePointInfo seqPoint)
		{
			SequencePointInfo result = seqPoint;
			if (seqPoint.StartLine == 16707566)
			{
				int startLine = allSequencePoints.Last().StartLine;
				result = new SequencePointInfo(endLine: (seqPoint.EndLine != 16707566) ? seqPoint.EndLine : allSequencePoints.Last().EndLine, method: seqPoint.Method, kind: seqPoint.Kind, sourceFile: seqPoint.SourceFile, sourceFileHash: seqPoint.SourceFileHash, startLine: startLine, startColumn: seqPoint.StartColumn, endColumn: seqPoint.EndColumn, ilOffset: seqPoint.IlOffset);
			}
			return result;
		}

		private void AddSequencePointToMethod(SequencePointInfo adjustedSeqPoint)
		{
			if (!sequencePointsByMethod.TryGetValue(adjustedSeqPoint.Method, out var value))
			{
				value = (sequencePointsByMethod[adjustedSeqPoint.Method] = new HashSet<SequencePointInfo>(s_seqPointComparer));
			}
			value.Add(adjustedSeqPoint);
		}

		private int AddAndIndexSequencePoint(SequencePointInfo seqPoint, SequencePointInfo adjustedSeqPoint)
		{
			int count = allSequencePoints.Count;
			allSequencePoints.Add(adjustedSeqPoint);
			seqPointIndexes.Add(seqPoint, count);
			if (!s_seqPointComparer.Equals(seqPoint, adjustedSeqPoint))
			{
				seqPointIndexes.Add(adjustedSeqPoint, count);
			}
			if (!sourceFileIndexes.ContainsKey(seqPoint.SourceFile))
			{
				sourceFileIndexes.Add(seqPoint.SourceFile, sourceFileIndexes.Count);
				allSourceFiles.Add(new SourceFileData(seqPoint.SourceFile, seqPoint.SourceFileHash));
			}
			return count;
		}

		public ReadOnlyCollection<string> GetAllContextInfoStrings()
		{
			return _variableNames.KeysSortedByValue();
		}

		public ReadOnlyCollection<VariableData> GetVariables()
		{
			return _variables.AsReadOnly();
		}

		public ReadOnlyCollection<Range> GetScopes()
		{
			return _scopes.AsReadOnly();
		}

		public bool TryGetScopeRange(MethodDefinition method, out Range range)
		{
			return _scopeMap.TryGetValue(method, out range);
		}

		public bool MethodHasSequencePoints(MethodDefinition method)
		{
			if (!sequencePointsByMethod.TryGetValue(method, out var value))
			{
				return false;
			}
			return value.Count > 0;
		}

		public bool MethodHasPausePointAtOffset(MethodDefinition method, int offset)
		{
			if (!pausePointsByMethod.TryGetValue(method, out var value))
			{
				return false;
			}
			return value.Contains(offset);
		}

		public bool TryGetVariableRange(MethodDefinition method, out Range range)
		{
			return _variableMap.TryGetValue(method, out range);
		}

		public void AddVariables(PrimaryCollectionContext context, MethodDefinition method)
		{
			if (!method.Body.HasVariables)
			{
				return;
			}
			int count = _scopes.Count;
			int num = 0;
			int[] array = new int[method.Body.Variables.Count];
			foreach (ScopeDebugInformation scope in method.DebugInformation.GetScopes())
			{
				int length = ((scope.End.IsEndOfMethod || scope.End.Offset == 0) ? method.Body.CodeSize : scope.End.Offset);
				int count2 = _scopes.Count;
				_scopes.Add(new Range(scope.Start.Offset, length));
				num++;
				foreach (VariableDebugInformation variable in scope.Variables)
				{
					array[variable.Index] = count2;
				}
			}
			int count3 = _variables.Count;
			int num2 = 0;
			foreach (VariableDefinition variable2 in method.Body.Variables)
			{
				if (method.DebugInformation.TryGetName(variable2, out var name))
				{
					if (!_variableNames.TryGetValue(name, out var value))
					{
						value = _variableNames.Count;
						_variableNames.Add(name, value);
					}
					num2++;
					_variables.Add(new VariableData(context.Global.Collectors.Types.Add(variable2.VariableType), value, array[variable2.Index]));
				}
			}
			if (num2 != 0)
			{
				_variableMap.Add(method, new Range(count3, num2));
			}
			if (num != 0)
			{
				_scopeMap.Add(method, new Range(count, num));
			}
		}
	}
}
