using Advanced.Algorithms.DataStructures.Graph.AdjacencyList;

using System.Linq;

namespace Advanced.Algorithms.Tests.DataStructures.Graph.AdjacencyList
{
    
    public class DiGraph_Tests
    {
        /// <summary>
        /// key value dictionary tests 
        /// </summary>
        [HuaTuo.NUnit.Framework.Test]
        public void DiGraph_Smoke_Test()
        {
            var graph = new DiGraph<int>();

            graph.AddVertex(1);
            graph.AddVertex(2);
            graph.AddVertex(3);
            graph.AddVertex(4);
            graph.AddVertex(5);

            graph.AddEdge(1, 2);
            HuaTuo.NUnit.Framework.Assert.IsTrue(graph.HasEdge(1, 2));
            HuaTuo.NUnit.Framework.Assert.IsFalse(graph.HasEdge(2, 1));

            graph.AddEdge(2, 3);
            graph.AddEdge(3, 4);
            graph.AddEdge(4, 5);
            graph.AddEdge(4, 1);
            graph.AddEdge(3, 5);

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(graph.VerticesCount, graph.Count());

            HuaTuo.NUnit.Framework.Assert.AreEqual(2, graph.OutEdges(4).Count());
            HuaTuo.NUnit.Framework.Assert.AreEqual(2, graph.InEdges(5).Count());

            HuaTuo.NUnit.Framework.Assert.AreEqual(5, graph.VerticesCount);

            HuaTuo.NUnit.Framework.Assert.IsTrue(graph.HasEdge(1, 2));

            graph.RemoveEdge(1, 2);

            HuaTuo.NUnit.Framework.Assert.IsFalse(graph.HasEdge(1, 2));

            graph.RemoveEdge(2, 3);
            graph.RemoveEdge(3, 4);
            graph.RemoveEdge(4, 5);
            graph.RemoveEdge(4, 1);

            HuaTuo.NUnit.Framework.Assert.IsTrue(graph.HasEdge(3, 5));
            graph.RemoveEdge(3, 5);
            HuaTuo.NUnit.Framework.Assert.IsFalse(graph.HasEdge(3, 5));

            graph.RemoveVertex(1);
            graph.RemoveVertex(2);
            graph.RemoveVertex(3);
            graph.RemoveVertex(4);

            graph.AddEdge(5, 5);
            graph.RemoveEdge(5, 5);
            graph.RemoveVertex(5);

            HuaTuo.NUnit.Framework.Assert.AreEqual(0, graph.VerticesCount);

            //IEnumerable test using linq
            HuaTuo.NUnit.Framework.Assert.AreEqual(graph.VerticesCount, graph.Count());
        }
    }
}
