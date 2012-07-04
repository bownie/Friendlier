#region File Description
//-----------------------------------------------------------------------------
// BraceDepth.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;

namespace Xyglo
{

    /// <summary>
    /// BraceDepth holds a value for the number of indents (tabs) in we are at a certain position
    /// in a file.  This way we can hold the markers where the indents change.
    /// </summary>
    public class BraceDepth : IComparable
    {
        /// <summary>
        /// Position of the brace
        /// </summary>
        protected FilePosition m_position;

        /// <summary>
        /// Depth at the brace
        /// </summary>
        protected int m_depth;

        /// <summary>
        /// Integer constructor
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="depth"></param>
        public BraceDepth(int x, int y, int depth)
        {
            m_position = new FilePosition(x, y);
            m_depth = depth;
        }

        /// <summary>
        /// FilePosition constructor
        /// </summary>
        /// <param name="position"></param>
        /// <param name="depth"></param>
        public BraceDepth(FilePosition position, int depth)
        {
            m_position = position;
            m_depth = depth;
        }

        /// <summary>
        /// Implemented for as we are an IComparable derivative so we can be used in SortedList
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int CompareTo(object item)
        {
            if (item == null) return 1;

            BraceDepth otherBD = item as BraceDepth;

            if (otherBD.getPosition() < m_position) return 1;
            else if (otherBD.getPosition() == m_position) return 0;
            else return -1;
        }

        /// <summary>
        /// Return the position
        /// </summary>
        /// <returns></returns>
        public FilePosition getPosition()
        {
            return m_position;
        }

        /// <summary>
        /// Return the depth
        /// </summary>
        /// <returns></returns>
        public int getDepth()
        {
            return m_depth;
        }
    }
}
