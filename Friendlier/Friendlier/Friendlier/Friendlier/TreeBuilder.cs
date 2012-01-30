using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using QuickGraph;

using QuickGraph.Algorithms;
using QuickGraph.Algorithms.Search;
using QuickGraph.Algorithms.Observers;

using QuickGraph.Collections;
using QuickGraph.Contracts;
using QuickGraph.Predicates;
using QuickGraph.Serialization;
using QuickGraph.Graphviz;



namespace Xyglo
{
    class TreeBuilder
    {
        public BidirectionalGraph<string, Edge<string>> m_graph = null;


        public TreeBuilder()
        {
            initialise();

        }

        public void initialise()
        {
            m_graph = new BidirectionalGraph<string, Edge<string>>();

            //var g = new BidirectionalGraph<string, Edge<string>>();
            //var g3 = new UndirectedGraph<string, Edge<string>>();
            //var g = new AdjacencyGraph<string, Edge<string>>();

            m_graph.AddVertex("A");
            m_graph.AddVertex("B");
            m_graph.AddVertex("C");
            m_graph.AddVertex("D");
            m_graph.AddVertex("E");
            m_graph.AddVertex("F");
            m_graph.AddVertex("G");
            m_graph.AddVertex("H");
            m_graph.AddVertex("1A");
            m_graph.AddVertex("1B");
            m_graph.AddVertex("1C");

            m_graph.AddEdge(new Edge<string>("A", "B"));
            m_graph.AddEdge(new Edge<string>("B", "C"));
            m_graph.AddEdge(new Edge<string>("C", "D"));
            m_graph.AddEdge(new Edge<string>("C", "E"));
            m_graph.AddEdge(new Edge<string>("A", "C"));
            m_graph.AddEdge(new Edge<string>("F", "A"));
            m_graph.AddEdge(new Edge<string>("G", "A"));


            m_graph.AddEdge(new Edge<string>("A", "1A"));
            m_graph.AddEdge(new Edge<string>("1A", "1B"));
            m_graph.AddEdge(new Edge<string>("1A", "1C"));
            m_graph.AddEdge(new Edge<string>("1B", "D"));

            m_graph.AddEdge(new Edge<string>("H", "1A"));
        }

        public void topologicalSort()
        {
            foreach (string v in m_graph.TopologicalSort())
            {
                Logger.logMsg("TOP SORT = " + v);
            }
        }

        public int getTotalNodes()
        {
            return m_graph.Vertices.Count<string>();
        }

        public void buildTree()
        {
            var roots = m_graph.Roots().ToList();

            foreach (string v in m_graph.Vertices)
            {
                Logger.logMsg(v);
            }

            foreach (Edge<string> e in m_graph.Edges)
            {
                Logger.logMsg(e.Source + " -> " + e.Target);
            }

            //IVertexListGraph<Vertex,Edge> g=…;
            IDictionary<string, string> components = new Dictionary<string, string>();

            ///int componentCount = m_graph.StronglyConnectedComponents(components);

            //IVertexListGraph<string, Edge<string>> g2;

            foreach (string v in m_graph.TopologicalSort())
            {
                Logger.logMsg("TOP SORT = " + v);
            }


            //IVertexAndEdgeListGraph<string,TEdge> g=

            var graphviz = new GraphvizAlgorithm<string, Edge<string>>(m_graph);

            //string output = graphviz.Generate(new FileDotEngine(), "graph");

            Logger.logMsg("DOT output = " + graphviz.Generate());

            var dfs = new DepthFirstSearchAlgorithm<string, Edge<string>>(m_graph);

            //dfs.Compute();

            var observer = new VertexPredecessorRecorderObserver<string, Edge<string>>();

            observer.Attach(dfs);

            //var obs2 = new EdgePredecessorRecorderObserver<string, Edge<string>>();
            //obs2.Attach(dfs);

            dfs.Compute();

            foreach (var p in observer.VertexPredecessors)
            {
                Logger.logMsg("Predecessors = " + p.ToString());
            }

            /* Strongly Connected */

            IDictionary<string, int> components2 = new Dictionary<string, int>();

            int componentCount = m_graph.StronglyConnectedComponents<string, Edge<string>>(out components2);

            /*
            if (componentCount != 0)
            {
                Logger.logMsg("Graph is not strongly connected");
                //Logger.logMsg("Graph contains {0} strongly connected components", componentCount);
                foreach (var kv in components2)
                {

                    //Logger.logMsg("Vertex {0} is connected to {1} other strongly connected components", kv.Key, kv.Value);
                }
            }
            */

            /* Connected */

            //QuickGraph.TryFunc<
            //int componentsConnected = m_graph.TreeBreadthFirstSearch<string, Edge<string>>("A");

            /* Breadth First */

            var parents = new Dictionary<string, string>();
            var distances = new Dictionary<string, int>();
            string currentVertex = default(string);
            int currentDistance = 0;

            var algo = new BreadthFirstSearchAlgorithm<string, Edge<string>>(m_graph);

            string sourceVertex = "A";


            algo.DiscoverVertex += u =>
            {
                if (u.Equals(sourceVertex))
                    currentVertex = sourceVertex;
            };


            algo.ExamineVertex += args =>
            {
                var u = args;
                currentVertex = u;

                if (distances[u] == currentDistance + 1) // new level
                    ++currentDistance;
            };
            algo.TreeEdge += args =>
            {
                var u = args.Source;
                var v = args.Target;


                parents[v] = u;
                distances[v] = distances[u] + 1;
            };
            algo.NonTreeEdge += args =>
            {
                var u = args.Source;
                var v = args.Target;

            };

            parents.Clear();
            distances.Clear();
            currentDistance = 0;

            foreach (var v in m_graph.Vertices)
            {
                distances[v] = int.MaxValue;
                parents[v] = v;
            }
            distances[sourceVertex] = 0;
            algo.Compute(sourceVertex);

            // All white vertices should be unreachable from the source.
            foreach (var v in m_graph.Vertices)
            {
                Logger.logMsg("VERTEX " + v + " WEIGHT = " + distances[v]);
            }


            //            dfs.TreeEdge += new EdgeHandler(observer);


            //PredecessorRecorder r = new PredecessorRecorder();

            //            dfs.TreeEdge += new EdgeHandler(r.RecordPredecessor);


            //Func<TEdge, double> edgeWeights = ...;
            //IEnumerable<TEdge> mst = m_graph.MinimumSpanningTreePrim(g, edgeWeights);


            /*
                * Undirected graphs
                */
            /*
            IUndirectedGraph<string, IEdge<string>> g2;
 
            Func<IEdge<string>, double> edgeWeights;
                *
            IEnumerable<IEdge<string>> mst = g2.MinimumSpanningTreePrim<string, IEdge<string>>(g2, edgeWeights);
            */


            //            AdjacencyGraph a = new AdjacencyGraph<IVertexAndEdgeListGraph<IVertexType, IEdge<IVertexType>>, IEdge<IVertexType>>();

            /*
            AdjacencyGraph g = new AdjacencyGraph(
                new VertexAndEdgeProvider(), // vertex and edge provider
                true // directed graph
                );
 
            // adding vertices u,v
            IVertex u = m_graph.AddVertex();    // IVertexMutableGraph
            IVertex v = m_graph.AddVertex();
 
            // adding the edge (u,v)
            IEdge e = m_graph.AddEdge(u, v);     // IEdgeMutableGraph
            */

        }
    }
}
