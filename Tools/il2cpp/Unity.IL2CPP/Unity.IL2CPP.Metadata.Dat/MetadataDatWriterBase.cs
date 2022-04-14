using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Unity.IL2CPP.Contexts;
using Unity.MiniProfiling;

namespace Unity.IL2CPP.Metadata.Dat
{
	public abstract class MetadataDatWriterBase
	{
		protected enum PackingSize
		{
			Zero,
			One,
			Two,
			Four,
			Eight,
			Sixteen,
			ThirtyTwo,
			SixtyFour,
			OneHundredTwentyEight
		}

		protected const int kMinimumStreamAlignment = 4;

		protected readonly MetadataWriteContext _context;

		protected abstract int Version { get; }

		protected MetadataDatWriterBase(MetadataWriteContext context)
		{
			_context = context;
		}

		public virtual void Write()
		{
			using (FileStream fileStream = new FileStream(_context.Global.InputData.MetadataFolder.CreateDirectory().Combine("global-metadata.dat").ToString(), FileMode.Create, FileAccess.Write))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					using (MemoryStream memoryStream2 = new MemoryStream())
					{
						WriteToStream(fileStream, memoryStream, memoryStream2);
						fileStream.WriteUInt(4205910959u);
						fileStream.WriteInt(Version);
						memoryStream.Seek(0L, SeekOrigin.Begin);
						memoryStream.CopyTo(fileStream);
						memoryStream2.Seek(0L, SeekOrigin.Begin);
						memoryStream2.CopyTo(fileStream);
					}
				}
			}
		}

		protected abstract void WriteToStream(Stream binary, MemoryStream headerStream, MemoryStream dataStream);

		protected void WriteMetadataToStream(string name, MemoryStream headerStream, int headerSize, MemoryStream dataStream, Action<MemoryStream> callback)
		{
			using (MiniProfiler.Section(name))
			{
				using (MemoryStream memoryStream = new MemoryStream())
				{
					callback(memoryStream);
					AddStreamAndRecordHeader(name, headerStream, headerSize, dataStream, memoryStream);
				}
			}
		}

		protected void AddStreamAndRecordHeader(string name, Stream headerStream, int headerSize, Stream dataStream, Stream stream)
		{
			if (dataStream.Position % 4 != 0L)
			{
				throw new ArgumentException($"Data stream is not aligned to minimum alignment of {4}", "dataStream");
			}
			if (stream.Position % 4 != 0L)
			{
				throw new ArgumentException($"Stream is not aligned to minimum alignment of {4}", "stream");
			}
			_context.Global.Collectors.Stats.RecordMetadataStream(name, stream.Position);
			headerStream.WriteLongAsInt(headerSize + dataStream.Position);
			headerStream.WriteLongAsInt(stream.Position);
			stream.Seek(0L, SeekOrigin.Begin);
			stream.CopyTo(dataStream);
		}

		private static Dictionary<string, List<MethodReference>> ImagesToMethodDictionaryFor(IEnumerable<MethodReference> methods)
		{
			Dictionary<string, List<MethodReference>> dictionary = new Dictionary<string, List<MethodReference>>();
			foreach (MethodReference method in methods)
			{
				string name = method.DeclaringType.Module.Assembly.Name.Name;
				if (!dictionary.ContainsKey(name))
				{
					dictionary.Add(name, new List<MethodReference>());
				}
				dictionary[name].Add(method);
			}
			return dictionary;
		}

		protected static Dictionary<string, List<TypeReference>> ImagesToTypeDictionaryFor(IEnumerable<TypeReference> types)
		{
			Dictionary<string, List<TypeReference>> dictionary = new Dictionary<string, List<TypeReference>>();
			foreach (TypeReference type in types)
			{
				string name = type.Module.Assembly.Name.Name;
				if (!dictionary.ContainsKey(name))
				{
					dictionary.Add(name, new List<TypeReference>());
				}
				dictionary[name].Add(type);
			}
			return dictionary;
		}

		protected static PackingSize ConvertPackingSizeToCompressedEnum(int packingSize)
		{
			switch (packingSize)
			{
			case 0:
				return PackingSize.Zero;
			case 1:
				return PackingSize.One;
			case 2:
				return PackingSize.Two;
			case 4:
				return PackingSize.Four;
			case 8:
				return PackingSize.Eight;
			case 16:
				return PackingSize.Sixteen;
			case 32:
				return PackingSize.ThirtyTwo;
			case 64:
				return PackingSize.SixtyFour;
			case 128:
				return PackingSize.OneHundredTwentyEight;
			default:
				throw new InvalidOperationException($"The packing size of {packingSize} is not valid. Valid values are 0, 1, 2, 4, 8, 16, 32, 64, or 128.");
			}
		}
	}
}
