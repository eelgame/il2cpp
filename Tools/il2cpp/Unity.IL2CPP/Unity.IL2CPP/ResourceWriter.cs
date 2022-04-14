using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Cecil;

namespace Unity.IL2CPP
{
	public class ResourceWriter
	{
		private class ResourceRecord
		{
			private readonly byte[] name;

			private readonly byte[] data;

			private readonly int size;

			public ResourceRecord(string name, int size, byte[] data)
			{
				this.name = Encoding.UTF8.GetBytes(name);
				this.size = size;
				this.data = data;
			}

			public void WriteRecord(BinaryWriter writer)
			{
				writer.Write(size);
				writer.Write(name.Length);
				writer.Write(name);
			}

			public void WriteData(BinaryWriter writer)
			{
				writer.Write(data);
			}

			public int GetRecordSize()
			{
				return 4 + name.Length + 4;
			}
		}

		public static void WriteEmbeddedResources(AssemblyDefinition assembly, Stream stream)
		{
			WriteResourceInformation(stream, GenerateResourceInfomation(assembly));
		}

		private static List<ResourceRecord> GenerateResourceInfomation(AssemblyDefinition assembly)
		{
			List<ResourceRecord> list = new List<ResourceRecord>();
			foreach (EmbeddedResource item in assembly.MainModule.Resources.OfType<EmbeddedResource>())
			{
				byte[] resourceData = item.GetResourceData();
				list.Add(new ResourceRecord(item.Name, resourceData.Length, resourceData));
			}
			return list;
		}

		private static void WriteResourceInformation(Stream stream, List<ResourceRecord> resourceRecords)
		{
			int value = GetSumOfAllRecordSizes(resourceRecords) + GetSizeOfNumberOfRecords();
			BinaryWriter binaryWriter = new BinaryWriter(stream);
			binaryWriter.Write(value);
			binaryWriter.Write(resourceRecords.Count);
			foreach (ResourceRecord resourceRecord in resourceRecords)
			{
				resourceRecord.WriteRecord(binaryWriter);
			}
			foreach (ResourceRecord resourceRecord2 in resourceRecords)
			{
				resourceRecord2.WriteData(binaryWriter);
			}
		}

		private static int GetSumOfAllRecordSizes(IEnumerable<ResourceRecord> resourceRecords)
		{
			return resourceRecords.Sum((ResourceRecord resourceRecord) => resourceRecord.GetRecordSize());
		}

		private static int GetSizeOfNumberOfRecords()
		{
			return 4;
		}
	}
}
