using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.Tiny
{
	public class TinyStringMetadataCollector
	{
		private class Result : ITinyStringMetadataResults
		{
			private readonly ReadOnlyCollection<string> _stringLines32;

			private readonly ReadOnlyCollection<string> _stringLines64;

			private readonly ReadOnlyCollection<StringLiteralEntry> _stringEntries;

			public Result(ReadOnlyCollection<string> stringLines32, ReadOnlyCollection<string> stringLines64, ReadOnlyCollection<StringLiteralEntry> stringEntries)
			{
				_stringLines32 = stringLines32;
				_stringLines64 = stringLines64;
				_stringEntries = stringEntries;
			}

			public int GetStringLiteralCount()
			{
				return _stringEntries.Count;
			}

			public ReadOnlyCollection<StringLiteralEntry> GetEntries()
			{
				return _stringEntries;
			}

			public IEnumerable<string> GetStringLines32()
			{
				return _stringLines32;
			}

			public IEnumerable<string> GetStringLines64()
			{
				return _stringLines64;
			}
		}

		private readonly Dictionary<string, StringLiteralEntry> _stringLiterals = new Dictionary<string, StringLiteralEntry>();

		private readonly List<StringLiteralEntry> _stringEntries = new List<StringLiteralEntry>();

		private readonly List<uint> _stream32 = new List<uint>();

		private readonly List<ulong> _stream64 = new List<ulong>();

		private readonly StringBuilder _stagingStringBuilder = new StringBuilder();

		private readonly List<string> _stringLines32 = new List<string>();

		private readonly List<string> _stringLines64 = new List<string>();

		private uint _lastOffset32;

		private ulong _lastOffset64;

		public static ITinyStringMetadataResults Collect(ReadOnlyContext context, ReadOnlyCollection<string> tinyStrings)
		{
			TinyStringMetadataCollector tinyStringMetadataCollector = new TinyStringMetadataCollector();
			foreach (string tinyString in tinyStrings)
			{
				tinyStringMetadataCollector.AddStringLiteralOffsets(context, tinyString);
			}
			return new Result(tinyStringMetadataCollector._stringLines32.AsReadOnly(), tinyStringMetadataCollector._stringLines64.AsReadOnly(), tinyStringMetadataCollector._stringEntries.AsReadOnly());
		}

		private void AddStringLiteralOffsets(ReadOnlyContext context, string literal)
		{
			if (!_stringLiterals.TryGetValue(literal, out var value))
			{
				value = new StringLiteralEntry(_lastOffset32, _lastOffset64, context.Global.Services.Naming.TinyStringOffsetNameFor(literal));
				_stringLiterals.Add(literal, value);
				_stringEntries.Add(value);
				FillStreamsFromLiteral(literal, _stream32, _stream64);
				_stringLines32.Add(DumpStreamToBytes32(literal, _stream32, _stagingStringBuilder));
				_stringLines64.Add(DumpStreamToBytes64(literal, _stream64, _stagingStringBuilder));
				_lastOffset32 += (uint)(4 * _stream32.Count);
				_lastOffset64 += (ulong)(8L * (long)_stream64.Count);
			}
		}

		private static void FillStreamsFromLiteral(string literal, List<uint> stream32, List<ulong> stream64)
		{
			stream32.Clear();
			stream64.Clear();
			stream32.Add(0u);
			stream32.Add((uint)literal.Length);
			for (int i = 0; i < literal.Length / 2; i++)
			{
				stream32.Add(literal[i * 2] + ((uint)literal[i * 2 + 1] << 16));
			}
			if (literal.Length % 2 != 0)
			{
				stream32.Add(literal[literal.Length - 1]);
			}
			else
			{
				stream32.Add(0u);
			}
			stream64.Add(0uL);
			ulong num = (uint)literal.Length;
			if (literal.Length > 0)
			{
				num += (ulong)literal[0] << 32;
				if (literal.Length > 1)
				{
					num += (ulong)literal[1] << 48;
				}
			}
			stream64.Add(num);
			if (literal.Length <= 1)
			{
				return;
			}
			int num2 = literal.Length - 2;
			for (int j = 0; j < num2 / 4; j++)
			{
				stream64.Add(literal[j * 4 + 2] + ((ulong)literal[j * 4 + 3] << 16) + ((ulong)literal[j * 4 + 4] << 32) + ((ulong)literal[j * 4 + 5] << 48));
			}
			int num3 = num2 % 4 + 1;
			if (num3 != 0)
			{
				ulong num4 = 0uL;
				for (int k = 1; k < num3; k++)
				{
					num4 = (num4 << 16) + literal[literal.Length - k];
				}
				stream64.Add(num4);
			}
		}

		private static string DumpStreamToBytes32(string literal, List<uint> stream32, StringBuilder stagingBuilder)
		{
			stagingBuilder.Clear();
			foreach (uint item in stream32)
			{
				stagingBuilder.Append("0x" + item.ToString("X8") + ", ");
			}
			stagingBuilder.Append("/* " + literal.Replace("*/", "*(il2cpp)/") + " */");
			return stagingBuilder.ToString();
		}

		private static string DumpStreamToBytes64(string literal, List<ulong> stream64, StringBuilder stagingBuilder)
		{
			stagingBuilder.Clear();
			foreach (ulong item in stream64)
			{
				stagingBuilder.Append("0x" + item.ToString("X16") + ", ");
			}
			stagingBuilder.Append("/* " + literal.Replace("*/", "*(il2cpp)/") + " */");
			return stagingBuilder.ToString();
		}
	}
}
