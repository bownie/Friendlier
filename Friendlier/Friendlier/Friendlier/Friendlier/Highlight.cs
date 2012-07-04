﻿#region File Description
//-----------------------------------------------------------------------------
// Highlight.cs
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
using System.Runtime.InteropServices;

namespace Xyglo
{
    /// <summary>
    /// A highlight is a piece of text, a position range, a colour and an optional indent
    /// </summary>
    [DataContractAttribute]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class Highlight : IComparable
    {
        // ------------------------------------ MEMBER VARIABLES -----------------------------
        //

        /// <summary>
        /// Highlight start - line (Y) is included
        /// </summary>
        public FilePosition m_startHighlight;

        /// <summary>
        /// Highlight end - line (Y) is included
        /// </summary>
        public FilePosition m_endHighlight;

        /// <summary>
        /// Indent characters
        /// </summary>
        public int m_indent = 0;

        /// <summary>
        /// Colour of the highlight
        /// </summary>
        public Color m_colour { get; set; }

        // Text that is being highlighted
        //
        public string m_text;

        // ------------------------------------ CONSTRUCTOR -----------------------------
        //
        public Highlight(int line, int startX, int endX, string text, Color colour, int indent = 0)
        {
            m_startHighlight = new FilePosition(startX, line);
            m_endHighlight = new FilePosition(endX, line);

            if (endX < startX)
            {
                throw new Exception("In a Highlight - startX has to be greater than endX");
            }

            m_colour = colour;
            m_text = text;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="hl"></param>
        public Highlight(Highlight hl)
        {
            m_startHighlight = new FilePosition(hl.m_startHighlight);
            m_endHighlight = new FilePosition(hl.m_endHighlight);
            m_colour = hl.m_colour;
            m_text = hl.m_text;
        }

        /// <summary>
        /// Must implement this for the IComparable interface so we can compares highlights
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        int IComparable.CompareTo(object x)
        {
            Highlight h1 = (Highlight)x;

            if (h1.m_startHighlight.Y < this.m_startHighlight.Y || (h1.m_startHighlight.Y == this.m_startHighlight.Y && h1.m_startHighlight.X < this.m_startHighlight.X))
            {
                return 1;
            }
            else if (h1.m_startHighlight.Y == this.m_startHighlight.Y && h1.m_startHighlight.X == this.m_startHighlight.X)
            {
                return 0;
            }
            else // Greater than
            {
                return -1;
            }
        }

        // Needed to avoid warning
        //
        public override int GetHashCode()
        {
            return m_startHighlight.X ^ m_startHighlight.Y;
        }

        // Comparison of values - not references
        //
        public override bool Equals(object obj)
        {
            Highlight hl = (Highlight)(obj);

            return ((hl.m_startHighlight == this.m_startHighlight) &&
                    (hl.m_endHighlight == this.m_endHighlight) &&
                    (hl.m_text == this.m_text) &&
                    (hl.m_colour == this.m_colour));
        }


        /// <summary>
        /// Equality operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Highlight a, Highlight b)
        {
            return ((a.m_startHighlight == b.m_startHighlight) &&
                    (a.m_endHighlight == b.m_endHighlight) &&
                    (a.m_text == b.m_text) &&
                    (a.m_colour == b.m_colour));
        }

        /// <summary>
        /// Inequality operator
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Highlight a, Highlight b)
        {
            return ((a.m_startHighlight != b.m_startHighlight) ||
                    (a.m_endHighlight != b.m_endHighlight) ||
                    (a.m_text != b.m_text) ||
                    (a.m_colour != b.m_colour));
        }

        public static T DeserializeMsg<T>(Byte[] data) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            IntPtr buff = Marshal.AllocHGlobal(objsize);

            Marshal.Copy(data, 0, buff, objsize);

            T retStruct = (T)Marshal.PtrToStructure(buff, typeof(T));

            Marshal.FreeHGlobal(buff);

            return retStruct;
        }

        public static Byte[] SerializeMessage<T>(T msg) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            Byte[] ret = new Byte[objsize];

            IntPtr buff = Marshal.AllocHGlobal(objsize);

            Marshal.StructureToPtr(msg, buff, true);

            Marshal.Copy(buff, ret, 0, objsize);

            Marshal.FreeHGlobal(buff);

            return ret;
        }
    }
}
    /// <summary>
    /// A HighlightList is a sorted (by start position) list of highlights for a file
    /// </summary>
    /*
    public class HighlightList : IComparable
    {
        private class HighlightListSorter : IComparer
        {
            // Determine the list position
            int IComparer.Compare(object x, object y)
            {
                Highlight h1 = (Highlight)x;
                Highlight h2 = (Highlight)y;

                if (h1.m_startHighlight.Y <= h2.m_startHighlight.Y && h1.m_startHighlight.X < h2.m_startHighlight.X)
                {
                    return -1;
                }
                else if (h1.m_startHighlight.Y == h2.m_startHighlight.Y && h1.m_startHighlight.X == h2.m_startHighlight.X)
                {
                    return 0;
                }
                else // Greater than
                {
                    return 1;
                }
            }
        }
    }
     * */
               