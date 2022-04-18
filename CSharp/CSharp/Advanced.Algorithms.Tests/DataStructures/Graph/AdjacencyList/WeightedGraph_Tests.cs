﻿using Advanced.Algorithms.DataStructures.Graph.AdjacencyList;


namespace Advanced.Algorithms.Tests.DataStructures.Graph.AdjacencyList
{
    
    public class WeightedGraph_Tests
    {
        /// <summary>
        /// key value dictionary tests 
        /// </summary>
        [NUnit.Framework.Test]
        public void WeightedGraph_Smoke_Test()
        {
            var graph = new WeightedGraph<int, int>();

            graph.AddVertex(1);
            graph.AddVertex(2);
            graph.AddVertex(3);
            graph.AddVertex(4);
            graph.AddVertex(5);

            graph.AddEdge(1, 2, 1);
            graph.AddEdge(2, 3, 2);
            graph.AddEdge(3, 4, 4);
            graph.AddEdge(4, 5, 5);
            graph.AddEdge(4, 1, 1);
            graph.AddEdge(3, 5, 0);

            NUnit.Framework.Assert.AreEqual(3, graph.GetAllEdges(4).Count);
            NUnit.Framework.Assert.AreEqual(2, graph.GetAllEdges(5).Count);

            NUnit.Framework.Assert.AreEqual(5, graph.VerticesCount);

            NUnit.Framework.Assert.IsTrue(graph.HasEdge(1, 2));

            graph.RemoveEdge(1, 2);

            NUnit.Framework.Assert.IsFalse(graph.HasEdge(1, 2));

            graph.RemoveEdge(2, 3);
            graph.RemoveEdge(3, 4);
            graph.RemoveEdge(4, 5);
            graph.RemoveEdge(4, 1);

            NUnit.Framework.Assert.IsTrue(graph.HasEdge(3, 5));
            graph.RemoveEdge(3, 5);
            NUnit.Framework.Assert.IsFalse(graph.HasEdge(3, 5));

            graph.RemoveVertex(1);
            graph.RemoveVertex(2);
            graph.RemoveVertex(3);
            graph.RemoveVertex(4);
            graph.RemoveVertex(5);

            NUnit.Framework.Assert.AreEqual(0, graph.VerticesCount);
        }
    }
}
