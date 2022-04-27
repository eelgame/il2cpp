using Advanced.Algorithms.DataStructures.Foundation;


namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class Stack_Tests
    {

        [HuaTuo.NUnit.Framework.Test]
        public void ArrayStack_Test()
        {
            var stack = new Stack<string>();

            stack.Push("a");
            stack.Push("b");

            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Count, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Peek(), "b");

            stack.Pop();

            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Peek(), "a");

            stack.Pop();

            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Count, 0);

            stack.Push("a");
            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Peek(), "a");
        }

        [HuaTuo.NUnit.Framework.Test]
        public void LinkedListStack_Test()
        {
            var stack = new Stack<string>(StackType.LinkedList);

            stack.Push("a");
            stack.Push("b");

            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Count, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Peek(), "b");

            stack.Pop();

            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Peek(), "a");

            stack.Pop();

            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Count, 0);

            stack.Push("a");
            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Count, 1);
            HuaTuo.NUnit.Framework.Assert.AreEqual(stack.Peek(), "a");
        }
    }
}
