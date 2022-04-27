using Advanced.Algorithms.DataStructures;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures
{
    
    public class DisJointSet_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void Smoke_Test_DisJointSet()
        {
            var disjointSet = new DisJointSet<int>();

            for (int i = 1; i <= 7; i++)
            {
                disjointSet.MakeSet(i);
            }

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(disjointSet.Count, disjointSet.Count());

            disjointSet.Union(1, 2);
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, disjointSet.FindSet(2));

            disjointSet.Union(2, 3);
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, disjointSet.FindSet(3));

            disjointSet.Union(4, 5);
            HuaTuo.NUnit.Framework.Assert.AreEqual(4, disjointSet.FindSet(4));

            disjointSet.Union(5, 6);
            HuaTuo.NUnit.Framework.Assert.AreEqual(4, disjointSet.FindSet(5));

            disjointSet.Union(6, 7);
            HuaTuo.NUnit.Framework.Assert.AreEqual(4, disjointSet.FindSet(6));

            HuaTuo.NUnit.Framework.Assert.AreEqual(4, disjointSet.FindSet(4));
            disjointSet.Union(3, 4);
            HuaTuo.NUnit.Framework.Assert.AreEqual(1, disjointSet.FindSet(4));

            //IEnumerable test
            HuaTuo.NUnit.Framework.Assert.AreEqual(disjointSet.Count, disjointSet.Count());

        }
    }
}
