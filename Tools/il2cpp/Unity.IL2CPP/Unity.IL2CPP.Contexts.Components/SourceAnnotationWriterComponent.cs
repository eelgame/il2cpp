using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Mono.Cecil.Cil;
using NiceIO;
using Unity.IL2CPP.CodeWriters;
using Unity.IL2CPP.Common;
using Unity.IL2CPP.Contexts.Components.Base;
using Unity.IL2CPP.Contexts.Forking.Steps;
using Unity.IL2CPP.Contexts.Services;
using Unity.IL2CPP.Diagnostics;

namespace Unity.IL2CPP.Contexts.Components
{
	public class SourceAnnotationWriterComponent : StatefulComponentBase<ISourceAnnotationWriter, object, SourceAnnotationWriterComponent>, ISourceAnnotationWriter
	{
		private class NotAvailable : ISourceAnnotationWriter
		{
			public void EmitAnnotation(ICodeWriter writer, SequencePoint sequencePoint)
			{
				throw new NotSupportedException();
			}
		}

		private readonly Dictionary<NPath, string[]> _cachedFiles;

		public SourceAnnotationWriterComponent()
		{
			_cachedFiles = new Dictionary<NPath, string[]>();
		}

		private SourceAnnotationWriterComponent(Dictionary<NPath, string[]> existingData)
		{
			_cachedFiles = new Dictionary<NPath, string[]>(existingData);
		}

		public void EmitAnnotation(ICodeWriter writer, SequencePoint sequencePoint)
		{
			string[] fileLines = GetFileLines(sequencePoint.Document.Url.ToNPath());
			if (fileLines == null)
			{
				return;
			}
			int startLine = sequencePoint.StartLine;
			int num = sequencePoint.EndLine;
			if (startLine < 1 || startLine > fileLines.Length)
			{
				return;
			}
			if (num == -1)
			{
				num = startLine;
			}
			startLine--;
			num--;
			if (fileLines.Length <= sequencePoint.EndLine)
			{
				return;
			}
			ArrayView<string> arrayView = new ArrayView<string>(fileLines, startLine, num - startLine + 1);
			int num2 = int.MaxValue;
			for (int i = 0; i < arrayView.Length; i++)
			{
				string text = arrayView[i];
				if (!string.IsNullOrWhiteSpace(text))
				{
					int j;
					for (j = 0; j < text.Length && text[j] == ' '; j++)
					{
					}
					if (num2 > j)
					{
						num2 = j;
					}
				}
			}
			for (int k = 0; k < arrayView.Length; k++)
			{
				string text2 = arrayView[k];
				if (text2.Length >= num2)
				{
					text2 = text2.Substring(num2);
				}
				text2 = text2.TrimEnd(Array.Empty<char>());
				if (writer.Context.Global.Parameters.EmitSourceMapping)
				{
					writer.WriteLine($"//<source_info:{sequencePoint.Document.Url.ToNPath()}:{startLine + k + 1}>");
				}
				writer.WriteLine("// " + text2);
			}
		}

		private string[] GetFileLines(NPath path)
		{
			string[] value = null;
			if (_cachedFiles.TryGetValue(path, out value))
			{
				return value;
			}
			try
			{
				if (path.FileExists())
				{
					value = File.ReadAllLines(path.ToString());
					int num = value.Length;
					for (int i = 0; i < num; i++)
					{
						value[i] = value[i].Replace("\t", "    ").TrimEnd(Array.Empty<char>());
						if (value[i].EndsWith("\\"))
						{
							value[i] += ".";
						}
					}
				}
			}
			catch
			{
			}
			_cachedFiles.Add(path, value);
			return value;
		}

		protected override void DumpState(StringBuilder builder)
		{
			CollectorStateDumper.AppendCollection(builder, "_cachedFiles", _cachedFiles.Keys.ToSortedCollectionBy((NPath p) => p.ToString()));
		}

		protected override void HandleMergeForAdd(SourceAnnotationWriterComponent forked)
		{
			foreach (KeyValuePair<NPath, string[]> cachedFile in forked._cachedFiles)
			{
				if (!_cachedFiles.ContainsKey(cachedFile.Key))
				{
					_cachedFiles.Add(cachedFile.Key, cachedFile.Value);
				}
			}
		}

		protected override void HandleMergeForMergeValues(SourceAnnotationWriterComponent forked)
		{
			throw new NotSupportedException();
		}

		protected override SourceAnnotationWriterComponent CreateEmptyInstance()
		{
			return new SourceAnnotationWriterComponent();
		}

		protected override SourceAnnotationWriterComponent CreateCopyInstance()
		{
			return new SourceAnnotationWriterComponent(_cachedFiles);
		}

		protected override SourceAnnotationWriterComponent ThisAsFull()
		{
			return this;
		}

		protected override object ThisAsRead()
		{
			return this;
		}

		protected override ISourceAnnotationWriter GetNotAvailableWrite()
		{
			return new NotAvailable();
		}

		protected override object GetNotAvailableRead()
		{
			throw new NotSupportedException();
		}

		protected override void ForkForSecondaryCollection(SecondaryCollectionLateAccessForkingContainer lateAccess, out ISourceAnnotationWriter writer, out object reader, out SourceAnnotationWriterComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryWrite(PrimaryWriteAssembliesLateAccessForkingContainer lateAccess, out ISourceAnnotationWriter writer, out object reader, out SourceAnnotationWriterComponent full)
		{
			WriteOnlyFork(out writer, out reader, out full);
		}

		protected override void ForkForPrimaryCollection(PrimaryCollectionLateAccessForkingContainer lateAccess, out ISourceAnnotationWriter writer, out object reader, out SourceAnnotationWriterComponent full)
		{
			NotAvailableFork(out writer, out reader, out full);
		}

		protected override void ForkForSecondaryWrite(SecondaryWriteLateAccessForkingContainer lateAccess, out ISourceAnnotationWriter writer, out object reader, out SourceAnnotationWriterComponent full)
		{
			((ComponentBase<ISourceAnnotationWriter, object, SourceAnnotationWriterComponent>)this).WriteOnlyFork(out writer, out reader, out full, ForkMode.Copy, MergeMode.Add);
		}
	}
}
