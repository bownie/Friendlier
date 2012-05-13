using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;

namespace Xyglo
{
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

    /// <summary>
    /// A highlight is a piece of text, a position range, a colour and an optional indent
    /// </summary>
    [DataContractAttribute]
    public class Highlight : IComparable
    {
        
        public Highlight(int line, int startX, int endX, string text, Color colour, int indent = 0)
        {
            m_startHighlight = new FilePosition(startX, line);
            m_endHighlight = new FilePosition(endX, line);
            m_colour = colour;
            m_text = text;
        }

        /// <summary>
        /// Highlight start - line (Y) is included
        /// </summary>
        public FilePosition m_startHighlight { get; set; }

        /// <summary>
        /// Highlight end - line (Y) is included
        /// </summary>
        public FilePosition m_endHighlight { get; set; }

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

        int IComparable.CompareTo(object x)
        {
            Highlight h1 = (Highlight)x;

            if (h1.m_startHighlight.Y <= this.m_startHighlight.Y && h1.m_startHighlight.X < this.m_startHighlight.X)
            {
                return -1;
            }
            else if (h1.m_startHighlight.Y == this.m_startHighlight.Y && h1.m_startHighlight.X == this.m_startHighlight.X)
            {
                return 0;
            }
            else // Greater than
            {
                return 1;
            }
        }
    }

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

    /// <summary>
    /// Syntax manager sends style hints and indenting tips to anyone who wants to
    /// call it.  It accepts a Project and you need to set a FileBuffer to get anything
    /// meaningful from the main methods calls.  This is an abstract base class that
    /// needs 
    /// </summary>
    public abstract class SyntaxManager
    {
        /////////////////////////////// MEMBER VARIABLES ///////////////////////////////////

        /// <summary>
        /// The overall project
        /// </summary>
        [NonSerialized]
        protected Project m_project;

        /// <summary>
        /// List of keywords for this language
        /// </summary>
        protected List<string> m_keywords;

        /// <summary>
        /// Sorted list of brace positions
        /// </summary>
        [NonSerialized]
        protected SortedList m_bracePositions = new SortedList();

        /////////////////////////////// CONSTRUCTORS ///////////////////////////////////////

        /// <summary>
        /// FileBuffer constructor
        /// </summary>
        /// <param name="fileBuffer"></param>
        public SyntaxManager(Project project)
        {
            m_project = project;
        }

        /////////////////////////////// METHODS ///////////////////////////////////

        /// <summary>
        /// Is a line in a comment?
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public bool inComment(int line)
        {
            return false;
        }

        /// <summary>
        /// Get some highlighting suggestions from the indicated line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        //public abstract List<Highlight> getHighlighting(int line);

        /// <summary>
        /// Get some highlighting suggestions for a line range
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        //public abstract List<Highlight> getHighlighting(int startLine, int endLine);

        /// <summary>
        /// Update the highlighting information after we've made a modification
        /// </summary>
        public abstract void updateHighlighting(FileBuffer fileBuffer /*, int fromLine = 0*/);

        /// <summary>
        /// Generates all highlighting for all FileBuffers
        /// </summary>
        public abstract void generateHighlighting();

        /// <summary>
        /// Get some suggestions from the current text we're entering
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public abstract List<string> getAutoCompletes(string text);

        /// <summary>
        /// Get the indent level at a certain line in the FileBuffer
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public abstract string getIndent(FilePosition fp);

        /// <summary>
        /// Colour of comment
        /// </summary>
        static public Color m_commentColour = Color.Aqua;

        /// <summary>
        /// Colour of #define
        /// </summary>
        static public Color m_defineColour = Color.Yellow;

        /// <summary>
        /// Colour of a Brace
        /// </summary>
        static public Color m_braceColour = Color.Pink;

        /// <summary>
        /// Colour of a Paranthesis
        /// </summary>
        static public Color m_paranthesisColour = Color.Blue;


        /// <summary>
        /// Get an existing indent depth for a given x and y coordinate (integer)
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int getIndentDepth(int x, int y)
        {
            return getIndentDepth(new FilePosition(x, y));
        }

        /// <summary>
        /// Get an existing indent depth for a given x and y coordinate (FilePosition)
        /// </summary>
        public int getIndentDepth(FilePosition fp)
        {
            int lastDepth = 0;

            for (int i = 0; i < m_bracePositions.Count; i++)
            {
                BraceDepth bd = (BraceDepth)m_bracePositions.GetKey(i);

                // While the FilePosition pointer is less then our test position then
                // store the lastDepth as we'll use this when we exceed it.
                //
                if (bd.getPosition() < fp)
                {
                    lastDepth = bd.getDepth();
                }
                else
                {
                    break;
                }
            }

            return lastDepth;
        }

        /// <summary>
        /// Test a position and return brace depth or -1
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int testBraceDepth(int x, int y)
        {
            FilePosition fp = new FilePosition(x, y);
            for (int i = 0; i < m_bracePositions.Count; i++)
            {
                BraceDepth bd = (BraceDepth)m_bracePositions.GetKey(i);
                if (bd.getPosition() == fp) return bd.getDepth();
            }
            
            return -1;
        }
    }
}
