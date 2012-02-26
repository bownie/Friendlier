using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xyglo
{
    public abstract class Command : IDisposable
    {
        public Command() 
        {
        }

        public Command(string name)
        {
            m_name = name;
        }

        protected string m_name;

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

        // character we use to split lines
        //
        protected char m_splitCharacter = '\n';

        protected FilePosition m_startPos;
        protected FilePosition m_endPos;
    }
}
