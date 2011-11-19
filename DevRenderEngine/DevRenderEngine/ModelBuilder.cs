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
        public ModelBuilder()
        {
        }

        public ModelBuilder(TreeBuilder tb)
        {
            treeBuilder = tb;

            // Start with some depth in the buffer
            //
            startZ = -2.0f;
            incrementZ = -1.0f;
            currentZ = startZ;

        }

        TreeBuilder treeBuilder;

        /// <summary>
        /// Do the build
        /// </summary>
        public void build()
        {
            Console.WriteLine("Got tree with " + treeBuilder.getTotalNodes());


            // For the moment we do a Topological sort only so we expect our graphic to
            // be acyclic.  We fetch the top-most node with this search and then iterate
            // each node placing it in a 3D space according to how much space we think we 
            // have left on the current viewing plane.  This will probably mean we'll have
            // to do multiple passes of the sort initially to work out best placement at each
            // plane level.   As we reach a limit of complexity for each plane in the Z buffer
            // we move further away from the origin.
            //
            foreach (string vertex in treeBuilder.m_graph.TopologicalSort<string, Edge<string>>())
            {
                if (bestPlace(vertex) == false)
                {
                    Console.WriteLine("Couldn't place " + vertex);
                }
            }
           
        }

        /// <summary>
        /// Position of the initial eye - the further away from the origin the flatter the image
        /// of the items arranged around the viewing window with other restrictions.
        /// </summary>
        Vector3 eyePosition = new Vector3(0.0f, 0.0f, 10.0f);

        // Define Fields of View for the X and Y axis
        //
        protected double fovX = 3 * Math.PI / 4;
        protected double fovY = Math.PI / 2;

        // Define a Field of View width which will then define the height
        //
        protected float fovWidth = 20.0f;

        /// <summary>
        /// Our starting Z layer or position
        /// </summary>
        protected float startZ = 0.0f;

        /// <summary>
        /// Our current Z layer 
        /// </summary>
        protected float currentZ = 0.0f;

        /// <summary>
        /// Increment amount for Z buffer
        /// </summary>
        protected float incrementZ = -3.0f;

        /// <summary>
        /// Default ModelItem Width
        /// </summary>
        protected float itemWidth = 0.6f;

        /// <summary>
        /// Default ModelItem Height
        /// </summary>
        protected float itemHeight = 0.8f;

        // Don't define a FOV for Z for the moment

        /// <summary>
        /// Best place a node for the given Z value
        /// </summary>
        /// <param name="zLayer"></param>
        bool bestPlace(string vertex)
        {
            Console.WriteLine("Placing vertex " + vertex);

            int degree = treeBuilder.m_graph.Degree(vertex);

            // Initial placePosition is in the middle of the current Z field
            //
            Vector3 placePosition = new Vector3(-itemWidth / 2, itemHeight / 2, currentZ);

            ModelCodeFragment mcf = new ModelCodeFragment(vertex, placePosition);

            currentZ += incrementZ;

            // Now add the item to the itemList
            //
            itemList.Add(vertex, mcf);

            return true;

            // Distance to current Z from EyePosition - assume currentZ is backing
            // away from origin (negative) and Eye is positive Z.
            //
            float totalZ = eyePosition.Z - currentZ;

            // xMin and xMax derived as follows
            float xMax = totalZ * (float)(Math.Tan(fovX/2));
            float xMin = -xMax;

            // yMin and yMax also
            float yMax = totalZ * (float)(Math.Tan(fovY/2));
            float yMin = -yMax;

            BoundingBox viewAreaBBox =
                        new BoundingBox(new Vector3(xMin, yMin, currentZ),
                                        new Vector3(xMax, yMax, currentZ));

            if (!viewAreaBBox.Intersects(mcf.getBoundingBox()))
            {
                Console.WriteLine("bestPlace(" + vertex + ") - initial placement outside view area");
                return false;
            }

            bool stillPlacing = true;
            ArrayList avoid = new ArrayList();
            int placementAttempt = 0;

            while (stillPlacing)
            {

                // We want to step through our existing items 
                foreach (string itemName in itemList.Keys)
                {
                    // Is the position of the new item overlapping an existing one?
                    //

                    BoundingBox itemListBB = itemList[itemName].getBoundingBox();
                    BoundingBox mcfBB = mcf.getBoundingBox();

                    //if (itemList[itemName].getBoundingBox().Intersects(mcf.getBoundingBox()))
                    if(itemListBB.Intersects(mcfBB))
                    {
                        // Move outside the bounding box of the item but make sure we're still inside
                        // the view area box.
                        avoid.Add(itemList[itemName]);
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
                        mcf.position.X += mcf.getBoundingBox().Max.X;
                    }

                    placementAttempt++;
                }
                else
                {
                    stillPlacing = false;
                    placementAttempt = 0;
                }

                if (placementAttempt > 10)
                {
                    currentZ += incrementZ;

                    mcf.position.X = itemWidth / 2;
                    mcf.position.Y = itemHeight / 2;
                    mcf.position.Z = currentZ;

                    placementAttempt = 0;
                }
            }

            // Now add the item to the itemList
            //
            itemList.Add(vertex, mcf);

            // If this vertex is singly connected then put it in its own plane
            //
            if (degree == 1)
            {
                currentZ += incrementZ;
            }


            return true;
        }

        /// <summary>
        /// our list of items
        /// </summary>
        public Dictionary<string, ModelItem> itemList = new Dictionary<string, ModelItem>();


        /// <summary>
        /// Position on the previous Z of where we dropped the last item - we need to be able
        /// to make this relevant to the viewing position though so it's not a reliable thing
        /// to use.
        /// </summary>
        BoundingBox lastDropped;


    }
}
