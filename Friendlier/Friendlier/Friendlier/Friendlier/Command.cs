#region File Description
//-----------------------------------------------------------------------------
// Command.cs
//
// Copyright (C) Xyglo Ltd. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Xyglo
{
    /// <summary>
    /// Abstract base class for all Xyglo commands.
    /// </summary>
    [DataContract(Name = "Friendlier", Namespace = "http://www.xyglo.com")]
    public abstract class Command : IDisposable
    {
        /////////////// CONSTRUCTORS /////////////////

        /// <summary>
        /// Default constructor
        /// </summary>
        public Command()
        {
        }

        /// <summary>
        /// Named constructor
        /// </summary>
        /// <param name="name"></param>
        public Command(string name)
        {
            m_name = name;
        }

        /////////////// MEMBER VARIABLES ////////////////

        /// <summary>
        /// Name of this command
        /// </summary>
        [DataMember]
        protected string m_name;

        // character we use to split lines
        //
        protected char m_splitCharacter = '\n';

        /// <summary>
        /// A start position for something
        /// </summary>
        [DataMember]
        protected FilePosition m_startPos;

        /// <summary>
        /// An end position for something
        /// </summary>
        [DataMember]
        protected FilePosition m_endPos;

        /// <summary>
        /// Store the start of the highlight for undo purposes
        /// </summary>
        [DataMember]
        protected ScreenPosition m_highlightStart;

        /// <summary>
        /// Store the end of the highlight
        /// </summary>
        [DataMember]
        protected ScreenPosition m_highlightEnd;

        /// <summary>
        /// Position at which this action is taking place
        /// </summary>
        [DataMember]
        protected ScreenPosition m_screenPosition = new ScreenPosition();

        /// <summary>
        /// Define an abstract command for (re)do that returns a modified cursor position in ScreenPosition
        /// </summary>
        public abstract ScreenPosition doCommand();

        /// <summary>
        /// Define an asbtract command for undo that returns a modified cursor position in ScreenPosition
        /// </summary>
        public abstract ScreenPosition undoCommand();

        /// <summary>
        /// Abstract method to dispose of this object
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // so that Dispose (false) isn't called later
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Dispose all managed objects
            }

            // Release unmanaged resources
        }

        /// <summary>
        /// Store our project pointer in the command for things like tab spaces etc
        /// </summary>
        protected Project m_project;

        /// <summary>
        /// The FileBuffer
        /// </summary>
        protected FileBuffer m_fileBuffer;

        /// <summary>
        /// A snippet is where we can store text that we're adding/removing
        /// </summary>
        [DataMember]
        protected TextSnippet m_snippet = SnippetFactory.getSnippet();

        // ------------------------------ METHODS --------------------------------------
        //

        /// <summary>
        /// Use this to order end points in selections
        /// </summary>
        public void positionOrder()
        {
            FilePosition swap;

            // Swap the end points if start is greater than end - gives us a predictable way of
            // handling text editing.
            //
            if (m_startPos.Y > m_endPos.Y || (m_startPos.Y == m_endPos.Y && (m_startPos.X > m_endPos.X)))
            {
                swap = m_startPos;
                m_startPos = m_endPos;
                m_endPos = swap;
            }
        }

        /// <summary>
        /// HighlightEnd position
        /// </summary>
        /// <returns></returns>
        public ScreenPosition getHighlightStart()
        {
            return m_highlightStart;
        }

        /// <summary>
        /// HighlightEnd position
        /// </summary>
        /// <returns></returns>
        public ScreenPosition getHighlightEnd()
        {
            return m_highlightEnd;
        }

        /// <summary>
        /// Start position of a change
        /// </summary>
        /// <returns></returns>
        public FilePosition getStartPos()
        {
            return m_startPos;
        }

        /// <summary>
        /// End position of a change
        /// </summary>
        /// <returns></returns>
        public FilePosition getEndPos()
        {
            return m_endPos;
        }

        /// <summary>
        /// Return the snippet that this command uses
        /// </summary>
        /// <returns></returns>
        public TextSnippet getSnippet()
        {
            return m_snippet;
        }

        /// <summary>
        /// Return the FileBuffer for this command
        /// </summary>
        /// <returns></returns>
        public FileBuffer getFileBuffer()
        {
            return m_fileBuffer;
        }

    }
}