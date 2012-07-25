#region File Description
//-----------------------------------------------------------------------------
// TextSnippet.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    /// <summary>
    /// A class that holds information about some text that we are adding or deleting
    /// from an existing FileBuffer.  This snippet also holds clues in that we can tell
    /// it to keep a track of a newly inserted line, or a deleted line or lines and works
    /// with single characters to multilines.  Still not quite there but getting there.
    /// </summary>
    public class TextSnippet
    {
        /// <summary>
        /// Lines in snippet
        /// </summary>
        public List<string> m_lines = new List<string>();

        /// <summary>
        /// Number of lines deleted
        /// </summary>
        protected int m_linesDeleted = 0;

        /// <summary>
        /// Store any highlights that have been removed as part of this TextSnippet operation
        /// </summary>
        public List<Highlight> m_highlights = new List<Highlight>();

        /// <summary>
        /// Single character delete
        /// </summary>
        protected bool m_isSingle = false;

        /// <summary>
        /// Has a new line been added as part of this snippet/command
        /// </summary>
        protected bool m_isNewLine = false;

        /// <summary>
        /// Position in the SnippetFactory - do we use this?
        /// </summary>
        protected int m_snippetFactoryPosition = 0;

        /// <summary>
        /// Number of lines deleted as part of this snippet
        /// </summary>
        /// <returns></returns>
        public int getLinesDeleted()
        {
            return m_linesDeleted;
        }

        /// <summary>
        /// Set the number of lines deleted as part of this snippet
        /// </summary>
        /// <param name="lines"></param>
        public void setLinesDeleted(int lines)
        {
            m_linesDeleted = lines;
        }

        /// <summary>
        /// Is a single character delete
        /// </summary>
        /// <returns></returns>
        public bool isSingleCharacter()
        {
            return m_isSingle;
        }

        /// <summary>
        /// Set the newline attribute of this TextSnippet
        /// </summary>
        /// <param name="newLine"></param>
        public void setNewLine(bool newLine)
        {
            m_isNewLine = newLine;
        }

        /// <summary>
        /// Is this snippet part of a NewLine insertion?
        /// </summary>
        /// <returns></returns>
        public bool isNewLine()
        {
            return m_isNewLine;
        }

        public void incrementLinesDeleted(int increment)
        {
            m_linesDeleted += increment;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public TextSnippet()
        {
            m_snippetFactoryPosition = -1;
        }

        /// <summary>
        /// Position constructor
        /// </summary>
        /// <param name="position"></param>
        public TextSnippet(int position)
        {
            m_snippetFactoryPosition = position;
        }

        /// <summary>
        /// Get position in the factory
        /// </summary>
        /// <returns></returns>
        public int getSnippetFactoryPosition()
        {
            return m_snippetFactoryPosition;
        }

        /// <summary>
        /// Set position in the factory
        /// </summary>
        /// <param name="position"></param>
        public void setSnippetFactoryPosition(int position)
        {
            m_snippetFactoryPosition = position;
        }

        /// <summary>
        /// Clear down this TextSnippet ready for re-use
        /// </summary>
        public void clear()
        {
            m_lines.Clear();
            m_highlights.Clear();
            m_linesDeleted = 0;
            //m_snippetFactoryPosition = -1;
            //m_startPoint.X = 0;
            //m_startPoint.Y = 0;
        }

        /// <summary>
        /// Set this snippet to a single line - remove any extra lines we've already stored in it
        /// </summary>
        /// <param name="line"></param>
        public void setSnippetSingle(string line)
        {
            switch (m_lines.Count())
            {
                case 0:
                    m_lines.Add(line);
                    break;

                case 1:
                    m_lines[0] = line;
                    break;

                default:
                    m_lines[0] = line;
                    m_lines.RemoveRange(1, m_lines.Count - 1);
                    break;
            }

            m_isSingle = true;
        }

        /// <summary>
        /// Return a line break formatted clipboard string
        /// </summary>
        /// <returns></returns>
        public string getClipboardString()
        {
            string rS = "";
            int i = 0;

            foreach (string line in m_lines)
            {
                // Only append a line feed if we aren't the last line of the snippet
                if (++i != m_lines.Count())
                {
                    rS += line + "\r\n";
                }
                else
                {
                    rS += line;
                }
            }

            return rS;
        }

    }
}
