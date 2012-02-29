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
        [DataMember()]
        protected string m_name;

        // character we use to split lines
        //
        protected char m_splitCharacter = '\n';

        [DataMember()]
        protected FilePosition m_startPos;

        [DataMember()]
        protected FilePosition m_endPos;

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
