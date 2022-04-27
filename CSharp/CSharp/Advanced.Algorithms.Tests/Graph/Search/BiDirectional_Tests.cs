using Advanced.Algorithms.Graph;



namespace Advanced.Algorithms.Tests.Graph
{
    
    public class BiDirectional_Tests
    {

        [HuaTuo.NUnit.Framework.Test]
        public void BiDirectional_AdjancencyListGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyList.DiGraph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('E');

            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');
            graph.AddVertex('I');

            graph.AddEdge('A', 'B');
            graph.AddEdge('B', 'C');
            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');
            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'H');
            graph.AddEdge('H', 'I');

            var algorithm = new BiDirectional<char>();

            HuaTuo.NUnit.Framework.Assert.IsTrue(algorithm.PathExists(graph, 'A', 'I'));

            graph.RemoveEdge('D', 'E');
            graph.AddEdge('E', 'D');

            HuaTuo.NUnit.Framework.Assert.IsFalse(algorithm.PathExists(graph, 'A', 'I'));

        }

        [HuaTuo.NUnit.Framework.Test]
        public void BiDirectional_AdjancencyMatrixGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyMatrix.DiGraph<char>();

            graph.AddVertex('A');
            graph.AddVertex('B');
            graph.AddVertex('C');
            graph.AddVertex('D');
            graph.AddVertex('E');

            graph.AddVertex('F');
            graph.AddVertex('G');
            graph.AddVertex('H');
            graph.AddVertex('I');

            graph.AddEdge('A', 'B');
            graph.AddEdge('B', 'C');
            graph.AddEdge('C', 'D');
            graph.AddEdge('D', 'E');
            graph.AddEdge('E', 'F');
            graph.AddEdge('F', 'G');
            graph.AddEdge('G', 'H');
            graph.AddEdge('H', 'I');

            var algorithm = new BiDirectional<char>();

            HuaTuo.NUnit.Framework.Assert.IsTrue(algorithm.PathExists(graph, 'A', 'I'));

            graph.RemoveEdge('D', 'E');
            graph.AddEdge('E', 'D');

            HuaTuo.NUnit.Framework.Assert.IsFalse(algorithm.PathExists(graph, 'A', 'I'));

        }
    }
}
