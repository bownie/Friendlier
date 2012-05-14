using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using System.Runtime.Serialization;

namespace Xyglo
{

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
        /// For a given line number return the indent of it - we use this when the previous line has been
        /// a curly brace (say) and we want to work out what the indent of the next line should be based
        /// on context.
        /// </summary>
        /// <param name="fileBuffer"></param>
        /// <param name="line"></param>
        /// <returns></returns>
        public int testIndentDepth(FileBuffer fileBuffer, int line)
        {
            if (line >= fileBuffer.getLineCount()) return -1;

            string checkLine = fileBuffer.getLine(line);

            for (int i = 0; i < checkLine.Length; i++)
            {
                if (!Char.IsWhiteSpace(checkLine[i]))
                {
                    return i;
                }
            }

            return -1;
        }

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
