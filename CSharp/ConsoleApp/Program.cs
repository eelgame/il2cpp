using System.IO;
using System.Net;
using System.Reflection;

namespace ConsoleApp
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var managedPath = @"C:\Users\18611\Documents\apks\tmp\Tile Match Brain Puzzle Game_v1.8.6_apkpure.com\assets\bin\Data\Managed";
            var metadataPath = $@"{managedPath}\etc\mono\56fdc7a7d4d85e543b7fb78c901610bd";
            
            var bytes = File.ReadAllBytes(metadataPath);

            for (int i = 4; i < bytes.Length; i++)
            {
                bytes[i] ^= 0x6E;
            }

            if (!Directory.Exists(Path.Combine(managedPath, "Metadata")))
            {
                Directory.CreateDirectory(Path.Combine(managedPath, "Metadata"));
            }
            
            File.WriteAllBytes(Path.Combine(managedPath, "Metadata", "global-metadata.dat"), bytes);
        }
    }
}