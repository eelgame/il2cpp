using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome.Comparers;
using Unity.IL2CPP.Common;

namespace Unity.IL2CPP.Debugger
{
	public class SequencePointInfoComparer : IEqualityComparer<SequencePointInfo>
	{
		public bool Equals(SequencePointInfo x, SequencePointInfo y)
		{
			if (x == null && y == null)
			{
				return true;
			}
			if (x == null || y == null)
			{
				return false;
			}
			MethodDefinition method = x.Method;
			MethodDefinition method2 = y.Method;
			if (method == method2 && x.Kind == y.Kind && x.SourceFile == y.SourceFile && SourceFileHashEqual(x.SourceFileHash, y.SourceFileHash) && x.StartLine == y.StartLine && x.EndLine == y.EndLine && x.StartColumn == y.StartColumn && x.EndColumn == y.EndColumn)
			{
				return x.IlOffset == y.IlOffset;
			}
			return false;
		}

		private bool SourceFileHashEqual(byte[] x, byte[] y)
		{
			if (x == null && y == null)
			{
				return true;
			}
			if (x == null || y == null)
			{
				return false;
			}
			return x.SequenceEqual(y);
		}

		public int GetHashCode(SequencePointInfo obj)
		{
			int hashCodeFor = MethodReferenceComparer.GetHashCodeFor(obj.Method);
			hashCodeFor = HashCodeHelper.Combine(hashCodeFor, (int)obj.Kind);
			hashCodeFor = HashCodeHelper.Combine(hashCodeFor, obj.SourceFile.GetStableHashCode());
			if (obj.SourceFileHash != null)
			{
				byte[] sourceFileHash = obj.SourceFileHash;
				foreach (byte hash in sourceFileHash)
				{
					hashCodeFor = HashCodeHelper.Combine(hashCodeFor, hash);
				}
			}
			hashCodeFor = HashCodeHelper.Combine(hashCodeFor, obj.StartLine);
			hashCodeFor = HashCodeHelper.Combine(hashCodeFor, obj.EndLine);
			hashCodeFor = HashCodeHelper.Combine(hashCodeFor, obj.StartColumn);
			hashCodeFor = HashCodeHelper.Combine(hashCodeFor, obj.EndColumn);
			return HashCodeHelper.Combine(hashCodeFor, obj.IlOffset);
		}
	}
}
