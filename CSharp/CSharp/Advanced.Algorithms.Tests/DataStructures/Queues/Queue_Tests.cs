using Advanced.Algorithms.DataStructures.Foundation;


namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Queue_Tests
    {

        [HuaTuo.NUnit.Framework.Test]
        public void ArrayQueue_Test()
        {
            var Queue = new Queue<string>();

            Queue.Enqueue("a");
            Queue.Enqueue("b");
            Queue.Enqueue("c");

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "a");


            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "b");

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "c");

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 0);

            Queue.Enqueue("a");

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "a");

        }

        [HuaTuo.NUnit.Framework.Test]
        public void LinkedListQueue_Test()
        {
            var Queue = new Queue<string>(QueueType.LinkedList);

            Queue.Enqueue("a");
            Queue.Enqueue("b");
            Queue.Enqueue("c");

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "a");


            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "b");

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "c");

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 0);

            Queue.Enqueue("a");

            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(Queue.Dequeue(), "a");

        }
    }
}
