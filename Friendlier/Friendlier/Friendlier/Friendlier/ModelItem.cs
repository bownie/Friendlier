using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.Xna.Framework;

namespace Xyglo
{
    /// <summary>
    /// A base Item for inclusion in a Model
    /// </summary>
    public abstract class ModelItem
    {
        public ModelItem(string name)
        {
            m_itemName = name;
            m_itemBoundary = new Vector3(0.2f, 0.2f, 0.2f);
            m_itemType = ItemType.NoType;
            zPlane = 0.0f;
        }

        /// <summary>
        /// Name of this item
        /// </summary>
        public string m_itemName;

        /// <summary>
        /// Top left front point position
        /// </summary>
        public Vector3 m_position;
        
        /// <summary>
        /// Dimensions - width, height, depth
        /// </summary>
        public Vector3 m_dimensions;
       
        /// <summary>
        /// The spacing around this item
        /// </summary>
        protected Vector3 m_itemBoundary;


        /// <summary>
        /// The type of ModelItem we are
        /// </summary>
        public enum ItemType
        {
            NoType,
            CodeFragment,
            DatabaseObject,
            TextFile,
            BinaryFile
        }


        /// <summary>
        /// Default item type is NoType
        /// </summary>
        public ItemType m_itemType;

        /// <summary>
        /// Z-plane we're working in
        /// </summary>
        public float zPlane;

        /// <summary>
        /// Return the bounding box defined by this Item including required spacing
        /// </summary>
        /// <returns></returns>
        public BoundingBox getBoundingBox()
        {
            return new BoundingBox(m_position - m_itemBoundary, m_position + m_dimensions + m_itemBoundary);
        }

        public void placeAround(ModelItem anotherItem, BoundingBox bBox)
        {

        }

    }
}
