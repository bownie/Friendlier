#region File Description
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
    /// What type of highlight are we going to create
    /// </summary>
    public enum HighlightType
    {
        Comment,
        Define,
        Keyword,
        UserHighlight
    }

    /// <summary>
    /// A Highlight is a piece of text, a position range and a type which defines what kind of 
    /// Highlight this is.  Highlight can then tell whoever is calling what colour it is.
    /// </summary>
    [DataContractAttribute]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public sealed class Highlight : IComparable
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
        /// Highlight type
        /// </summary>
        public HighlightType m_type;

        // Text that is being highlighted
        //
        public string m_text;

        // ------------------------------------ CONSTRUCTOR -----------------------------
        //

        /// <summary>
        /// Main constructor
        /// </summary>
        /// <param name="line"></param>
        /// <param name="startX"></param>
        /// <param name="endX"></param>
        /// <param name="text"></param>
        /// <param name="type"></param>
        /// <param name="indent"></param>
        public Highlight(int line, int startX, int endX, string text, HighlightType type, int indent = 0)
        {
            m_startHighlight = new FilePosition(startX, line);
            m_endHighlight = new FilePosition(endX, line);

            if (endX < startX)
            {
                throw new Exception("In a Highlight - startX has to be greater than endX");
            }

            m_type = type;
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
            m_type = hl.m_type;
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

        /// <summary>
        /// Needed to avoid warning
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return m_startHighlight.X ^ m_startHighlight.Y;
        }

        /// <summary>
        /// Comparison of values - not references
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            Highlight hl = (Highlight)(obj);

            return ((hl.m_startHighlight == this.m_startHighlight) &&
                    (hl.m_endHighlight == this.m_endHighlight) &&
                    (hl.m_text == this.m_text) &&
                    (hl.m_type == this.m_type));
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
                    (a.m_type == b.m_type));
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
                    (a.m_type != b.m_type));
        }

        /// <summary>
        /// We do want to serialise Highlights at some point or other - although during
        /// development we'll keep regenerating them every time we start up.  Keeps these
        /// methods here though.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <returns></returns>
        public static T DeserializeMsg<T>(Byte[] data) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            IntPtr buff = Marshal.AllocHGlobal(objsize);

            Marshal.Copy(data, 0, buff, objsize);

            T retStruct = (T)Marshal.PtrToStructure(buff, typeof(T));

            Marshal.FreeHGlobal(buff);

            return retStruct;
        }

        /// <summary>
        /// We do want to serialise Highlights at some point or other - although during
        /// development we'll keep regenerating them every time we start up.  Keeps these
        /// methods here though.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Return the defined colour for this type - we use this as our central API for 
        /// highlight colour management.
        /// </summary>
        /// <returns></returns>
        public Color getColour()
        {
            switch (m_type)
            {
                case HighlightType.Comment:
                    return SyntaxManager.m_commentColour;

                case HighlightType.Define:
                    return SyntaxManager.m_defineColour;

                case HighlightType.Keyword:
                    return SyntaxManager.m_keywordColour;

                case HighlightType.UserHighlight:
                    return new Color(230, 230, 0, 180);

                default:
                    return Color.White;
            }
        }
    }
}