using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using Unity.IL2CPP.Contexts;

namespace Unity.IL2CPP.StringLiterals
{
	public class StringLiteralWriter
	{
		public void Write(MetadataWriteContext context, Stream stringLiteralStream, Stream stringLiteralDataStream, IStringLiteralCollection stringLiteralCollection)
		{
			ReadOnlyCollection<string> stringLiterals = stringLiteralCollection.GetStringLiterals();
			int[] array = new int[stringLiterals.Count];
			List<byte> list = new List<byte>();
			for (int i = 0; i < stringLiterals.Count; i++)
			{
				array[i] = list.Count;
				string text = stringLiterals[i];
				list.AddRange(Encoding.UTF8.GetBytes(text));
				context.Global.Collectors.Stats.RecordStringLiteral(text);
			}
			byte[] array2 = new byte[stringLiterals.Count * 8];
			for (int j = 0; j < stringLiterals.Count; j++)
			{
				string s = stringLiterals[j];
				ToBytes(Encoding.UTF8.GetByteCount(s), array2, j * 8);
				ToBytes(array[j], array2, j * 8 + 4);
			}
			stringLiteralStream.Write(array2, 0, array2.Length);
			stringLiteralDataStream.Write(list.ToArray(), 0, list.Count);
		}

		private static void ToBytes(int value, byte[] bytes, int offset)
		{
			ToBytes((uint)value, bytes, offset);
		}

		private static void ToBytes(uint value, byte[] bytes, int offset)
		{
			bytes[offset] = (byte)(value & 0xFFu);
			bytes[offset + 1] = (byte)((value >> 8) & 0xFFu);
			bytes[offset + 2] = (byte)((value >> 16) & 0xFFu);
			bytes[offset + 3] = (byte)((value >> 24) & 0xFFu);
		}
	}
}
