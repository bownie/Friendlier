using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Xyglo
{
    /// <summary>
    /// A highlight is a position range and a colour
    /// </summary>
    public class Highlight
    {
        public FilePosition m_startHighlight { get; set; }
        public FilePosition m_endHighlight { get; set; }
        public Color m_colour { get; set; }
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
        protected FileBuffer m_fileBuffer;

        /// <summary>
        /// List of keywords for this language
        /// </summary>
        protected List<string> m_keywords;

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
        public void setFileBuffer(FileBuffer fileBuffer)
        {
            m_fileBuffer = fileBuffer;
        }

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
        public abstract List<Highlight> getHighlighting(int line);

        /// <summary>
        /// Get some suggestions from the current text we're entering
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public abstract List<string> getSuggestions(string text);

        /// <summary>
        /// Get the indent level at a certain line in the FileBuffer
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public abstract string getIndent(int line);
    }
}
