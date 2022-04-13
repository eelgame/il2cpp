using DataStructures.Lists;
using NUnit.Framework;
using Fact = NUnit.Framework.TestAttribute;

namespace UnitTest.DataStructuresTests
{
    public static class QueueTest
    {
        [Fact]
        public static void DoTest()
        {
            var queue = new Queue<string>();
            queue.Enqueue("aaa");
            queue.Enqueue("bbb");
            queue.Enqueue("ccc");
            queue.Enqueue("ddd");
            queue.Enqueue("eee");
            queue.Enqueue("fff");
            queue.Enqueue("ggg");
            queue.Enqueue("hhh");
          Assert.AreEqual(8, queue.Count);

            var array = queue.ToArray();
            // fails if wrong size
          Assert.AreEqual(8, array.Length);

            queue.Dequeue();
            queue.Dequeue();
            var top = queue.Dequeue();
          Assert.AreEqual("ccc", top);

            queue.Dequeue();
            queue.Dequeue();
          Assert.AreEqual("fff", queue.Top);

            var array2 = queue.ToArray();
            // fails if wrong size
          Assert.AreEqual(3, array2.Length);
        }
    }
}

