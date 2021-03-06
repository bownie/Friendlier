﻿#region File Description
//-----------------------------------------------------------------------------
// SyntaxManager.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion


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
        protected string [] m_keywords;

        /// <summary>
        /// Sorted list of brace positions
        /// </summary>
        [NonSerialized]
        protected SortedList m_bracePositions = new SortedList();

        /// <summary>
        /// Allow another thread to interrupt this process when it's doing something lengthy like
        /// regenerating highlighting for a large file etc.
        /// </summary>
        protected volatile bool m_interruptProcessing = false;


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
        /// Allow another thread to interrupt us
        /// </summary>
        public void interruptProcessing()
        {
            m_interruptProcessing = true;
        }

        /// <summary>
        /// Update the highlighting information after we've made a modification.   Accepts a command name and
        /// a direction.  Does not run this command - only updates the highlighting following it.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="doCommand"></param>
        //public abstract void updateHighlighting(Command command, bool doCommand);

        /// <summary>
        /// Generate highlighting for a specified file range and specify characters add or removed if fromPos = toPos
        /// </summary>
        /// <param name="fileBuffer"></param>
        public abstract bool generateHighlighting(FileBuffer fileBuffer, FilePosition fromPos, FilePosition toPos, bool backgroundThread = false);

        /// <summary>
        /// Generates all highlighting for a given FileBuffer
        /// </summary>
        public abstract bool generateAllHighlighting(FileBuffer fileBuffer, bool backgroundThread = false);

        /// <summary>
        /// Ensure we have a method which initialises a list of keywords
        /// </summary>
        public abstract void initialiseKeywords();

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
        /// Colour of a Keyword
        /// </summary>
        static public Color m_keywordColour = Color.LightGreen;

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
