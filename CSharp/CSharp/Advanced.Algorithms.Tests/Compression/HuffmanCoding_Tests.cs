using Advanced.Algorithms.Compression;


namespace Advanced.Algorithms.Tests.Compression
{
    
    public class HuffmanCoding_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void HuffmanCoding_Test()
        {
            var encoder = new HuffmanCoding<char>();

            var compressed = encoder
                .Compress("abcasdasdasdcaaaaaadqwerdasd".ToCharArray());

            HuaTuo.NUnit.Framework.Assert.AreEqual(compressed['a'].Length, 1);
        }
    }
}
