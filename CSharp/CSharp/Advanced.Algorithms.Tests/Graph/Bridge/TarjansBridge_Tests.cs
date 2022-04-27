using Advanced.Algorithms.Graph;

using System.Collections.Generic;
using System.Linq;

namespace Advanced.Algorithms.Tests.Graph
{
    
    public class TarjansBridge_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void TarjanBridge_AdjacencyListGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyList.Graph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('E');
            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');


            graph.AddEdge('A', 'B');
            graph.AddEdge('A', 'C');
            graph.AddEdge('B', 'C');

            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');

            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'E');

            graph.AddEdge('F', 'H');

            var algorithm = new TarjansBridgeFinder<char>();

            var result = algorithm.FindBridges(graph);

            HuaTuo.NUnit.Framework.Assert.AreEqual(3, result.Count);

            var expected = new List<char[]>()
            {
                new char[] { 'C', 'D'},
                new char[] { 'D', 'E' },
                new char[] { 'F', 'H'}
            };

            foreach (var bridge in result)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(expected.Any(x => bridge.vertexA == x[0]
                                    && bridge.vertexB == x[1]));
            }
        }

        [HuaTuo.NUnit.Framework.Test]
        public void TarjanBridge_AdjacencyMatrixGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyMatrix.Graph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('E');
            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');


            graph.AddEdge('A', 'B');
            graph.AddEdge('A', 'C');
            graph.AddEdge('B', 'C');

            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');

            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'E');

            graph.AddEdge('F', 'H');

            var algorithm = new TarjansBridgeFinder<char>();

            var result = algorithm.FindBridges(graph);

            HuaTuo.NUnit.Framework.Assert.AreEqual(3, result.Count);

            var expected = new List<char[]>()
            {
                new char[] { 'C', 'D'},
                new char[] { 'D', 'E' },
                new char[] { 'F', 'H'}
            };

            foreach (var bridge in result)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(expected.Any(x => bridge.vertexA == x[0]
                                    && bridge.vertexB == x[1]));
            }
        }
    }
}
