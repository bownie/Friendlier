using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;


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
    /// <summary>
    /// Builds a 3D ready model from an arbitrary acyclic directed or undirected graph
    /// </summary>
    public class ModelBuilder
    {
        /// <summary>
        /// Graph created by a TreeBuilder
        /// </summary>
        protected TreeBuilderGraph m_treeBuilderGraph;

        /// <summary>
        /// Define field of view for the X axis
        /// </summary>
        protected double fovX = 3 * Math.PI / 4;

        /// <summary>
        /// Define field of view for the Y axis
        /// </summary>
        protected double fovY = Math.PI / 2;

        // Define a Field of View width which will then define the height
        //
        protected float fovWidth = 20.0f;

        /// <summary>
        /// Our starting Z layer or position
        /// </summary>
        protected float m_startZ = 0.0f;

        /// <summary>
        /// Our current Z layer 
        /// </summary>
        protected float m_currentZ = 0.0f;

        /// <summary>
        /// Increment amount for Z buffer
        /// </summary>
        protected float m_incrementZ = 0.0f;

        /// <summary>
        /// Default ModelItem Width
        /// </summary>
        protected float m_itemWidth = 0.0f;

        /// <summary>
        /// Default ModelItem Height
        /// </summary>
        protected float m_itemHeight = 20.0f;

        /// <summary>
        /// The position when the model will be generated from - top of tree
        /// </summary>
        protected Vector3 m_modelPosition;

        /// <summary>
        /// Count how many vertices we've placed
        /// </summary>
        protected int m_verticesPlaced;

        /// <summary>
        /// The list of model items that will be generated from the tree
        /// </summary>
        protected Dictionary<string, ModelItem> m_itemList = new Dictionary<string, ModelItem>();

        // --------------------------------- CONSTRUCTORS -------------------------------

        /// <summary>
        /// Constructor accepts a TreeBuilderGraph
        /// </summary>
        /// <param name="tbg"></param>
        public ModelBuilder(TreeBuilderGraph tbg)
        {
            m_treeBuilderGraph = tbg;

            // Start with some depth in the buffer
            //
            m_startZ = -2.0f;
            m_incrementZ = -1.0f;
            m_currentZ = m_startZ;
        }


        // --------------------------------  METHODS -----------------------------

        /// <summary>
        /// Do the build
        /// </summary>
        public Dictionary<string, ModelItem> build(Vector3 startPosition)
        {
            Logger.logMsg("ModelBuilder::build() - building model from tree with " + m_treeBuilderGraph.Vertices.Count<string>() + " vertices");

            // Store the model position and set up some initial conditions
            //
            m_modelPosition = startPosition;
            m_verticesPlaced = 0;
            m_rootFound = false;

            // The rate at which we expand the tree has to be proportional to the square of the
            // number of vertices.  Somehow.
            //
            //m_treeBuilderGraph.VertexCount

            // Firstly build up our vertex maps and levels - this will help us define the
            // number of 
            //
            buildLevels();

            // Find the max width of the levels like this
            //
            int maxWidth = m_levelNumbers.Max(item => item.Value);

            Logger.logMsg("ModelBuilder::build() - max width = " + maxWidth);

            foreach (int key in m_levelNumbers.Keys)
            {
                Logger.logMsg("ModelBuilder::build() - level " + key + " = " + m_levelNumbers[key]);
            }

            // Store the depth
            //
            int depth = m_levelNumbers.Count();

            // Now set the width and the item sizes according to the width and depth
            //
            m_incrementZ = 100.0f / depth;
            m_itemWidth = 10.0f;
            m_itemHeight = 10.0f;

            return m_itemList;

            // For the moment we do a Topological sort only so we expect our graphic to
            // be acyclic.  We fetch the top-most node with this search and then iterate
            // each node placing it in a 3D space according to how much space we think we 
            // have left on the current viewing plane.  This will probably mean we'll have
            // to do multiple passes of the sort initially to work out best placement at each
            // plane level.   As we reach a limit of complexity for each plane in the Z buffer
            // we move further away from the origin.
            //
            foreach (string vertex in m_treeBuilderGraph.TopologicalSort<string, Edge<string>>())
            {
                Logger.logMsg("ModelBuilder::build() - vertex " + vertex + " has " + m_vertexTargets[vertex] + " targets");
                Logger.logMsg("ModelBuilder::build() - vertex " + vertex + " is at level " + m_vertexLevel[vertex]);

                if (bestPlace(vertex) == false)
                {
                    Logger.logMsg("ModelBuilder::build() - couldn't place " + vertex);
                }
            }

            return m_itemList;
        }

        /// <summary>
        /// First pass through the tree we build up the number of vertex targets and
        /// the vertices living at each level.
        /// </summary>
        protected void buildLevels()
        {
            // Clear down our target dictionaries
            //
            m_vertexTargets.Clear();
            m_vertexLevel.Clear();
            m_levelNumbers.Clear();

            // For every vertex, find the edges and increment the number of them in
            // the dictionary.
            //
            foreach (string vertex in m_treeBuilderGraph.TopologicalSort<string, Edge<string>>())
            {
                List<Edge<string>> edges = getEdgeTargets(vertex);

                // When we have no edges we are at the root - level 0
                //
                if (edges.Count == 0)
                {
                    // Initialise the root vertex
                    //
                    m_vertexLevel[vertex] = 0;
                }

                // for each source vertex how many targets do we have?
                //
                foreach(Edge<string> edge in edges)
                {
                    // Increment number of vertex targets
                    //
                    if (m_vertexTargets.ContainsKey(edge.Source))
                    {
                        m_vertexTargets[edge.Source]++;
                    }
                    else
                    {
                        m_vertexTargets[edge.Source] = 0;
                    }

                    // Store depth of targets - as we're doing this depth first it should work????
                    //
                    int sourceLevel = m_vertexLevel[edge.Source];

                    if (!m_vertexLevel.ContainsKey(edge.Target))
                    {
                        m_vertexLevel[edge.Target] = sourceLevel + 1;
                    }
                    else
                    {
                        if (m_vertexLevel[edge.Target] != sourceLevel + 1)
                        {
                            Logger.logMsg("ModelBuilder::buildLevels() - trying to set vertex level for " + edge.Target + " to a different value to that already stored");
                        }
                    }
                }
            }

            // Finally we should have all the vertex levels stored - now we want a summary of
            // each level.
            //
            foreach (string vertex in m_vertexLevel.Keys)
            {
                // Then do we have a level defined for it?
                //
                if (m_levelNumbers.ContainsKey(m_vertexLevel[vertex]))
                {
                    m_levelNumbers[m_vertexLevel[vertex]]++;
                }
                else
                {
                    m_levelNumbers[m_vertexLevel[vertex]] = 1;
                }
            }
        }

        /// <summary>
        /// Store how many targets each vertex has
        /// </summary>
        protected Dictionary<string, int> m_vertexTargets = new Dictionary<string, int>();

        /// <summary>
        /// Store level of each vertex
        /// </summary>
        protected Dictionary<string, int> m_vertexLevel = new Dictionary<string, int>();

        /// <summary>
        /// Store how many vertices will live at each level of our graph
        /// </summary>
        protected Dictionary<int, int> m_levelNumbers = new Dictionary<int, int>();

        /// <summary>
        /// Have we found a root already when laying out?
        /// </summary>
        protected bool m_rootFound = false;

        /// <summary>
        /// Get a list of edges for a given vertex
        /// </summary>
        /// <param name="vertex"></param>
        /// <returns></returns>
        protected List<Edge<string>> getEdgeTargets(string vertex)
        {
            List<Edge<string>> rL = new List<Edge<string>>();

            //var bfs = new BreadthFirstSearchAlgorithm<string, Edge<string>>(m_treeBuilderGraph);
            var dfs = new DepthFirstSearchAlgorithm<string, Edge<string>>(m_treeBuilderGraph);
            var observer = new VertexPredecessorRecorderObserver<string, Edge<string>>(); 

            using (observer.Attach(dfs)) // attach, detach to dfs events
                dfs.Compute(); // you can specify a root vertex in Compute() if you like

            IEnumerable<Edge<string>> edges;
            if (observer.TryGetPath(vertex, out edges))
            {
#if MODEL_BUILDER_DEBUG
                Logger.logMsg("ModelBuilder::getEdgeTargets() - To get to vertex '" + vertex + "', take the following edges:");
#endif
                foreach (Edge<string> edge in edges)
                {
                    rL.Add(edge);
#if MODEL_BUILDER_DEBUG
                    Logger.logMsg("ModelBuilder::getEdgeTargets() - " + edge.Source + " -> " + edge.Target);
#endif
                }
            }

            return rL;
        }

        /// <summary>
        /// Best place a node for the given Z value
        /// </summary>
        /// <param name="zLayer"></param>
        bool bestPlace(string vertex)
        {
            Logger.logMsg("ModelBuilder::bestPlace() - placing vertex " + vertex);

            // Initial placePosition is in the middle of the current Z field
            //
            Vector3 placePosition = new Vector3(-m_itemWidth / 2, m_itemHeight / 2, m_currentZ);

            ModelCodeFragment mcf = new ModelCodeFragment(vertex, placePosition);

            Logger.logMsg("ModelBuilder::bestPlace() - has got " + getEdgeTargets(vertex).Count + " connections");

            List<Edge<string>> edgeTargets = getEdgeTargets(vertex);

            // If we have no targets from a given vertex then we're at a root 
            if (edgeTargets.Count == 0)
            {
                if (m_rootFound)
                {
                    Logger.logMsg("ModelBuilder::bestPlace() - already found a root in this tree");
                    throw new System.Exception("Already found a root in this tree - can't build a model");
                }

                m_rootFound = true;
            }

            // Degree is how many connections this node has
            //
            int degree = m_treeBuilderGraph.Degree(vertex);
            
            // Isolated vertices
            //
            /*
            List<string> isolatedVertices = m_treeBuilderGraph.IsolatedVertices<string, Edge<string>>().ToList();

            if (isolatedVertices.Contains(vertex))
            {

            }
            */
            //m_treeBuilderGraph.

            //m_treeBuilderGraph.

            // Distance to current Z from EyePosition - assume currentZ is backing
            // away from origin (negative) and Eye is positive Z.
            //
            float totalZ = m_modelPosition.Z - m_currentZ;

            // xMin and xMax derived as follows
            float xMax = totalZ * (float)(Math.Tan(fovX/2));
            float xMin = -xMax;

            // yMin and yMax also
            float yMax = totalZ * (float)(Math.Tan(fovY/2));
            float yMin = -yMax;

            BoundingBox viewAreaBBox =
                        new BoundingBox(new Vector3(xMin, yMin, m_currentZ),
                                        new Vector3(xMax, yMax, m_currentZ));

            if (!viewAreaBBox.Intersects(mcf.getBoundingBox()))
            {
                Logger.logMsg("bestPlace(" + vertex + ") - initial placement outside view area");
                return false;
            }

            bool stillPlacing = true;
            ArrayList avoid = new ArrayList();

            while (stillPlacing)
            {

                // We want to step through our existing items 
                foreach (string itemName in m_itemList.Keys)
                {
                    // Is the position of the new item overlapping an existing one?
                    //
                    if (m_itemList[itemName].getBoundingBox().Intersects(mcf.getBoundingBox()))
                    {
                        // Move outside the bounding box of the item but make sure we're still inside
                        // the view area box.
                        avoid.Add(m_itemList[itemName]);
                    }
                }

                // Do we have any items to avoid?
                //
                if (avoid.Count > 0)
                {
                    // Can we place this item within the view area?
                    //
                    //foreach(ModelItem avdItem in avoid)
                    //{
                        //while (mcf.getBoundingBox().Intersects 
                    //}

                    // Reposition in the X axis only for the moment
                    //
                    if (mcf.getBoundingBox().Max.X < viewAreaBBox.Max.X)
                    {
                        mcf.m_position.X += mcf.getBoundingBox().Max.X;
                    }
                }
                else
                {
                    stillPlacing = false;
                }


                // Temporary
                stillPlacing = false;
            }

            // Now add the item to the itemList
            //
            m_itemList.Add(vertex, mcf);

            // If this vertex is singly connected then put it in its own plane
            //
            if (degree == 1)
            {
                m_currentZ += m_incrementZ;
            }
            return true;
        }
    }
}
