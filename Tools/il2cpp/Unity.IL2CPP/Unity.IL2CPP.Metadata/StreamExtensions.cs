using System;
using System.IO;

namespace Unity.IL2CPP.Metadata
{
	internal static class StreamExtensions
	{
		public static void AlignTo(this Stream stream, int alignment)
		{
			while (stream.Position % alignment != 0L)
			{
				stream.WriteByte(0);
			}
		}

		public static void WriteShort(this Stream stream, short value)
		{
			stream.WriteByte((byte)((uint)value & 0xFFu));
			stream.WriteByte((byte)((uint)(value >> 8) & 0xFFu));
		}

		public static void WriteUShort(this Stream stream, ushort value)
		{
			stream.WriteByte((byte)(value & 0xFFu));
			stream.WriteByte((byte)((uint)(value >> 8) & 0xFFu));
		}

		public static void WriteIntAsUShort(this Stream stream, int value)
		{
			if (value < 0)
			{
				throw new ArgumentException("Value of type 'int' is negative and cannot convert to ushort.", "value");
			}
			if (value > 65535)
			{
				throw new ArgumentException("Value of type 'int' is too large to convert to ushort.", "value");
			}
			stream.WriteUShort((ushort)value);
		}

		public static void WriteLongAsInt(this Stream stream, long value)
		{
			if (value > int.MaxValue)
			{
				throw new ArgumentException("Value of type 'long' is too large to convert to int.", "value");
			}
			stream.WriteInt((int)value);
		}

		public static void WriteInt(this Stream stream, int value)
		{
			stream.WriteByte((byte)((uint)value & 0xFFu));
			stream.WriteByte((byte)((uint)(value >> 8) & 0xFFu));
			stream.WriteByte((byte)((uint)(value >> 16) & 0xFFu));
			stream.WriteByte((byte)((uint)(value >> 24) & 0xFFu));
		}

		public static void WriteUInt(this Stream stream, uint value)
		{
			stream.WriteByte((byte)(value & 0xFFu));
			stream.WriteByte((byte)((value >> 8) & 0xFFu));
			stream.WriteByte((byte)((value >> 16) & 0xFFu));
			stream.WriteByte((byte)((value >> 24) & 0xFFu));
		}

		public static void WriteULong(this Stream stream, ulong value)
		{
			stream.WriteByte((byte)(value & 0xFF));
			stream.WriteByte((byte)((value >> 8) & 0xFF));
			stream.WriteByte((byte)((value >> 16) & 0xFF));
			stream.WriteByte((byte)((value >> 24) & 0xFF));
			stream.WriteByte((byte)((value >> 32) & 0xFF));
			stream.WriteByte((byte)((value >> 40) & 0xFF));
			stream.WriteByte((byte)((value >> 48) & 0xFF));
			stream.WriteByte((byte)((value >> 56) & 0xFF));
		}
	}
}
