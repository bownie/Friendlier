using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace Xyglo
{
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
        protected FilePosition m_highlightStart;

        /// <summary>
        /// Store the end of the highlight
        /// </summary>
        [DataMember]
        protected FilePosition m_highlightEnd;

        /// <summary>
        /// Define an abstract command for (re)do that returns a modified cursor position
        /// </summary>
        public abstract FilePosition doCommand();

        /// <summary>
        /// Define an asbtract command for undo that returns a modified cursor position
        /// </summary>
        public abstract FilePosition undoCommand();

        /// <summary>
        /// Abstract method to dispose of this object
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Store our project pointer in the command for things like tab spaces etc
        /// </summary>
        protected Project m_project;

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
    }
}
