using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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
    /// A highlight is a position range and a colour
    /// </summary>
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
        protected Project m_project;

        /// <summary>
        /// The FileBuffer we're currently interested in
        /// </summary>
        //protected FileBuffer m_fileBuffer;

        /// <summary>
        /// List of keywords for this language
        /// </summary>
        protected List<string> m_keywords;

        /// <summary>
        /// Start of a parenthesis or bracket
        /// </summary>
        public FilePosition m_parenthesisStart { get; set;  }

        /// <summary>
        /// End of a parenthesis
        /// </summary>
        public FilePosition m_parenthesisEnd { get; set;  }

        /// <summary>
        /// Return list for any highlights
        /// </summary>
        //protected List<Highlight> m_highlightList = new List<Highlight>();

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
        /// Set the FileBuffer
        /// </summary>
        /// <param name="fileBuffer"></param>
        //public void setFileBuffer(FileBuffer fileBuffer)
        //{
          //  m_fileBuffer = fileBuffer;
        //}

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
        public abstract void updateHighlighting(FileBuffer fileBuffer, int fromLine = 0);

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
        public abstract string getIndent(int line);


        /// <summary>
        /// Colour of comment
        /// </summary>
        static public Color m_commentColour = Color.Aqua;

        /// <summary>
        /// Colour of #define
        /// </summary>
        static public Color m_defineColour = Color.Yellow;


    }
}
