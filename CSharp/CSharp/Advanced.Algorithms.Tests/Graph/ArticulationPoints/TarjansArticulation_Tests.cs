using Advanced.Algorithms.Graph;

using System.Linq;

namespace Advanced.Algorithms.Tests.Graph
{
    
    public class TarjansArticulation_Tests
    {
        [HuaTuo.NUnit.Framework.Test]
        public void TarjanArticulation_AdjacencyListGraph_Smoke_Test()
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

            var algorithm = new TarjansArticulationFinder<char>();

            var result = algorithm.FindArticulationPoints(graph);

            HuaTuo.NUnit.Framework.Assert.AreEqual(4, result.Count);

            var expectedResult = new char[] { 'C', 'D', 'E', 'F' };

            foreach (var v in result)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(expectedResult.Contains(v));
            }
        }

        [HuaTuo.NUnit.Framework.Test]
        public void TarjanArticulation_AdjacencyMatrixGraph_Smoke_Test()
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

            var algorithm = new TarjansArticulationFinder<char>();

            var result = algorithm.FindArticulationPoints(graph);

            HuaTuo.NUnit.Framework.Assert.AreEqual(4, result.Count);

            var expectedResult = new char[] { 'C', 'D', 'E', 'F' };

            foreach (var v in result)
            {
                HuaTuo.NUnit.Framework.Assert.IsTrue(expectedResult.Contains(v));
            }
        }
    }
}
