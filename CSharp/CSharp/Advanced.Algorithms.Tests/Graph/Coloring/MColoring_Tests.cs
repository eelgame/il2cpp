using Advanced.Algorithms.Graph;


namespace Advanced.Algorithms.Tests.Graph
{
    
    public class MColoring_Tests
    {
        [NUnit.Framework.Test]
        public void MColoring_AdjacencyListGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyList.Graph<int>();

            graph.AddVertex(0);
            graph.AddVertex(1);
            graph.AddVertex(2);
            graph.AddVertex(3);

            graph.AddEdge(0, 1);
            graph.AddEdge(0, 2);
            graph.AddEdge(0, 3);
            graph.AddEdge(1, 2);
            graph.AddEdge(2, 3);

            var algorithm = new MColorer<int, string>();

            var result = algorithm.Color(graph, new string[] { "red", "green", "blue" });

            NUnit.Framework.Assert.IsTrue(result.CanColor);
        }

        [NUnit.Framework.Test]
        public void MColoring_AdjacencyMatrixGraph_Smoke_Test()
        {
            var graph = new Advanced.Algorithms.DataStructures.Graph.AdjacencyMatrix.Graph<int>();

            graph.AddVertex(0);
            graph.AddVertex(1);
            graph.AddVertex(2);
            graph.AddVertex(3);

            graph.AddEdge(0, 1);
            graph.AddEdge(0, 2);
            graph.AddEdge(0, 3);
            graph.AddEdge(1, 2);
            graph.AddEdge(2, 3);

            var algorithm = new MColorer<int, string>();

            var result = algorithm.Color(graph, new string[] { "red", "green", "blue" });

            NUnit.Framework.Assert.IsTrue(result.CanColor);
        }
    }
}
